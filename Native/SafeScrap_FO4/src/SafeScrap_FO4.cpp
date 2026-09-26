// SafeScrap_FO4 - an F4SE plugin. Two console commands that scrap junk in the settlement you are in, with the
// workshop's own scrap routine:
//
//   SafeScrap        scraps every object whose base is in Data\F4SE\Plugins\SafeScrap.txt (the list the SafeScrap
//                    app exports: only what you marked as safe junk).
//   ScrapAllOfThese  scraps every object with the SAME base as the object selected in the console (click it first).
//                    It does not look at the list: it is for what the rules do not cover (user's decision, 26-sep).
//
// HOW (Tools\re-docs\RE_SAFESCRAP_WORKSHOP_SCRAP_2026-09-26.md, Fallout4.exe 1.11.240):
//
//   The engine already has a UI-free scrap: the console command ScrapAll (entry 0x142F03FA0, op 0x2FB). Its worker
//   0x14037D070 sets the player's workshop in [0x1430F7698], builds the scrap context, walks the loaded cells with
//   ScrapAllCellVisitor 0x1403A7AD0 and restores the handle. For each ref the visitor does:
//
//     0x1403A7B44  call CanScrap(ref, 0)            SITE A     true  -> workshop? ->
//     0x1403A7B5F  call IsRefInBuildArea(ws, ref)    AREA SITE  true  -> ScrapRef(ctx, &ref, null)
//     0x1403A7BB8  call CanStore(ref)               SITE B     (only when CanScrap said no) true -> Store it
//
//   This plugin does NOT re-implement any of that. It redirects the THREE calls at their call sites and, ONLY while
//   one of its commands runs, answers:
//     site A: "yes" only if the base passes the command's filter AND the engine's CanScrap says yes; otherwise "no";
//     area site: the engine's answer, except for a ref with power (see below), which gets "no";
//     site B: always "no" (SafeScrap scraps; it never stores what it did not select).
//   With no command of ours running the three calls go straight to the engine: the vanilla ScrapAll is untouched.
//   So the cell walk, the lock, the build-area test, the workshop handle, the context and the scrap itself are
//   exactly the engine's.
//
//   POWER (RE 8.2): the workshop menu's scrap runs a step ScrapAll skips (0x14039A070: finds wires physically
//   touching the ref and fixes their links). Whether skipping it leaves dangling wires is NOT measured, so the
//   commands leave alone every ref with power (0x14038E380: WorkshopPowerConnection actor value != 0 or keyword
//   PowerConnection). They are counted at the AREA site, so only those inside the settlement count (the visitor walks
//   every loaded cell, and site A comes before the area test - rev-64), and the console says how many there are.
//
//   COMMANDS: the exe has no command registration (RE 4.3); an unused STUB entry of the console table is
//   overwritten in place, keeping its opcode (candidates in SafeScrapSites.h; ForceRSXCrash is F4SE's).
//
// LOG: errors only, in Documents\My Games\Fallout4\F4SE\SafeScrap_FO4.log. If all is well it is never created.

#include "F4sePluginKit.h"    // FO4_Base_Library\Native: version block, log, scanner, call redirection
#include "SafeScrapSites.h"   // WHERE (pure resolution: the probe runs the same code)
#include "SafeScrapList.h"    // the list file (the app's reading law)

#include <string>
#include <unordered_set>

using f4kit::Err;

// ============================================================================================
// F4SE version block
// ============================================================================================
// addressIndependence = 0 ON PURPOSE, as in NPC_Manager_FO4_LoadBake: F4SE skips the compatibleVersions walk if a
// plugin declares independence, and on an executable nobody verified this plugin must not load.
extern "C" __declspec(dllexport) f4kit::F4SEPluginVersionData F4SEPlugin_Version =
{
    f4kit::F4SEPluginVersionData::kVersion,
    1,
    "SafeScrap",
    "Manolo",
    0, 0,
    { f4kit::kRuntime_1_11_240,   // VERIFIED against the RE (MD5 c5791a0ce539465701c6e38fa465960c)
      0 },
    0, 0, 0, { 0 }
};

// ============================================================================================
// TRACE - measurement instrument, GATED. With TRACE 0 (what ships) not one instruction of it remains; with TRACE 1
// the plugin always writes the log. The build guard refuses to ship a TRACE build (marker "[trace]").
// ============================================================================================
#define TRACE 0

#if TRACE
static void Trace(const char* fmt, ...)
{
    va_list a;
    va_start(a, fmt);
    f4kit::LogLineV(fmt, a);
    va_end(a);
}
#endif

// ============================================================================================
// Engine functions (resolved in SafeScrapSites.h)
// ============================================================================================
typedef bool  (*FnCanScrap)(void* ref, bool storeMode);          // 0x140384910
typedef bool  (*FnCanStore)(void* ref);                          // 0x14039F0A0
typedef bool  (*FnPowerGate)(void* ref);                         // 0x14038E380
typedef void* (*FnGetFile)(void* form, int idx);                 // 0x1403123D0
typedef bool  (*FnIsLight)(void* file);                          // 0x1402F91A0
typedef void  (*FnScrapAllWorker)(void* scrapped, void* stored); // 0x14037D070, as ScrapAll's execute calls it
typedef void  (*FnConsolePrint)(void* console, const char* fmt, ...);  // 0x14103C0C0, printf-like (RE 0)

static safescrap::Sites g_sites;
static FnCanScrap       g_origCanScrap = nullptr;
static FnCanStore       g_origCanStore = nullptr;

// Form layout, measured: base form of a ref [ref+0xE0] (CanScrap 0x140384920), FormID [form+0x14] (LoadBake:
// the predicate's `cmp dword [rbx+0x14],7`), file name [file+0x70] (RE 8.4).
static void*  BaseOf(void* ref)      { return ref ? *(void**)((unsigned char*)ref + 0xE0) : nullptr; }
static UINT32 FormIdOf(void* form)   { return *(UINT32*)((unsigned char*)form + 0x14); }
static const char* FileName(void* f) { return (const char*)((unsigned char*)f + 0x70); }

// ============================================================================================
// The mode: which filter applies while one of our commands runs
// ============================================================================================
// Both commands run in the console's execute, the SAME context in which the vanilla ScrapAll calls the worker
// (RE 1.4); the worker calls the visitor synchronously. So the mode lives exactly for the duration of our call.
enum class Mode { Off, List, SameBase };
static Mode                             g_mode = Mode::Off;
static std::unordered_set<std::string>  g_keys;          // Mode::List
static void*                            g_sameBase = nullptr;   // Mode::SameBase
static unsigned                         g_powered = 0;   // refs left alone because they have power

// The key of a base form, by the engine's own view: its ORIGIN file (GetFile(form, 0), as the FaceGeom path builder
// 0x140658EB1 asks), the name in that file and the local id masked to 12 bits for a light file, 24 otherwise
// (0x140658ED4..EF8). The app writes the same identity (FormIdentifiers.Build).
static bool InList(void* base)
{
    auto getFile = (FnGetFile)g_sites.getFile;
    auto isLight = (FnIsLight)g_sites.isLight;
    void* file = getFile(base, 0);
    if (!file) return false;
    const UINT32 local = FormIdOf(base) & (isLight(file) ? 0xFFFu : 0xFFFFFFu);
    return g_keys.count(safescrap::MakeKey(safescrap::FoldAscii(FileName(file)), local)) != 0;
}

static bool PassesFilter(void* base)
{
    if (!base) return false;
    if (g_mode == Mode::SameBase) return base == g_sameBase;
    return InList(base);
}

// A listed ref with power, let past site A so that the visitor's OWN build-area test decides whether it counts
// (rev-64: site A comes BEFORE the area test and the visitor walks every loaded cell, so counting there would count
// objects outside the settlement). Consumed by the area site below; the visitor is sequential.
static void* g_pendingPowered = nullptr;

// SITE A 0x1403A7B44: CanScrap(ref, 0) inside the ScrapAll visitor.
static bool HookCanScrap(void* ref, bool storeMode)
{
    if (g_mode == Mode::Off) return g_origCanScrap(ref, storeMode);
    g_pendingPowered = nullptr;
    if (!PassesFilter(BaseOf(ref))) return false;
    if (!g_origCanScrap(ref, storeMode)) return false;
    if (((FnPowerGate)g_sites.powerGate)(ref)) { g_pendingPowered = ref; return true; }   // decided at the area site
#if TRACE
    Trace("[trace] scrap %08X base %08X", FormIdOf(ref), FormIdOf(BaseOf(ref)));
#endif
    return true;
}

// AREA SITE 0x1403A7B5F: IsRefInBuildArea(workshop, ref), reached only after site A said yes. A ref with power that IS
// in the settlement is counted and answered "no" (the visitor then skips it: je 0x1403A7C29); outside, the engine's
// "no" stands and it is neither scrapped nor counted.
typedef bool (*FnInArea)(void* workshop, void* ref);
static FnInArea g_origInArea = nullptr;

static bool HookInArea(void* workshop, void* ref)
{
    const bool inside = g_origInArea(workshop, ref);
    if (g_mode == Mode::Off || ref != g_pendingPowered) return inside;
    g_pendingPowered = nullptr;
    if (inside) ++g_powered;
    return false;
}

// SITE B 0x1403A7BB8: CanStore(ref), reached only when site A said no.
static bool HookCanStore(void* ref)
{
    if (g_mode == Mode::Off) return g_origCanStore(ref);
    return false;
}

// ============================================================================================
// Running the engine's ScrapAll worker under a mode
// ============================================================================================
static void Print(const char* fmt, unsigned a, unsigned b = 0, unsigned c = 0)
{
    void* console = *(void**)g_sites.printTarget;
    if (console) ((FnConsolePrint)g_sites.print)(console, fmt, a, b, c);
}

// The worker's two counters: 8 bytes each, zeroed (their width is not measured; the visitor increments them).
static unsigned RunWorker(Mode mode)
{
    UINT64 scrapped = 0, stored = 0;
    g_powered = 0;
    g_mode = mode;
    ((FnScrapAllWorker)g_sites.worker)(&scrapped, &stored);
    g_mode = Mode::Off;
    return (unsigned)(scrapped & 0xFFFFFFFFu);
}

static std::wstring ListPath()
{
    wchar_t exe[MAX_PATH]{};
    GetModuleFileNameW(nullptr, exe, MAX_PATH);
    std::wstring p(exe);
    const size_t slash = p.find_last_of(L"\\/");
    p = (slash == std::wstring::npos ? L"" : p.substr(0, slash + 1)) + L"Data\\F4SE\\Plugins\\SafeScrap.txt";
    return p;
}

// ============================================================================================
// The two commands (console execute ABI, RE 4.2: rcx paramInfo, rdx scriptData, r8 thisObj, r9 containingObj,
// then scriptObj, locals, result, opcodeOffsetPtr; returns al)
// ============================================================================================
static bool ExecSafeScrap(void*, void*, void*, void*, void*, void*, double*, void*)
{
    safescrap::ListStats stats;
    if (!safescrap::LoadList(ListPath().c_str(), g_keys, stats))
    {
        Print("SafeScrap: Data\\F4SE\\Plugins\\SafeScrap.txt not found. Export the list from the SafeScrap app.", 0);
        return true;
    }
    const unsigned n = RunWorker(Mode::List);
    Print("SafeScrap: %u objects scrapped (list: %u objects). %u objects with power in this settlement were left in place: scrap them from the workshop menu.",
          n, stats.entries, g_powered);
    if (stats.invalid) Print("SafeScrap: %u lines of SafeScrap.txt could not be read.", stats.invalid);
    g_keys.clear();
    return true;
}

static bool ExecScrapAllOfThese(void*, void*, void* thisObj, void*, void*, void*, double*, void*)
{
    // The console runs a line with the picked ref as thisObj (RE 4.2: 0x141036958..0x141036983).
    void* base = BaseOf(thisObj);
    if (!thisObj || !base)
    {
        Print("ScrapAllOfThese: select an object in the console first (click it), then run the command.", 0);
        return true;
    }
    g_sameBase = base;
    const unsigned n = RunWorker(Mode::SameBase);
    g_sameBase = nullptr;
    Print("ScrapAllOfThese: %u objects scrapped. %u objects with power in this settlement were left in place: scrap them from the workshop menu.", n, g_powered);
    return true;
}

// ============================================================================================
// Installing
// ============================================================================================
struct CommandDef { const char* longName; const char* shortName; const char* help; void* execute; };

static const CommandDef kCommands[] = {
    { "SafeScrap", "", "SafeScrap: scraps the objects of Data\\F4SE\\Plugins\\SafeScrap.txt in this settlement (objects with power are left alone).",
      (void*)&ExecSafeScrap },
    { "ScrapAllOfThese", "", "SafeScrap: scraps every object with the same base as the selected one in this settlement (objects with power are left alone).",
      (void*)&ExecScrapAllOfThese },
};

// Overwrites a stub entry in place, keeping its opcode (RE 4.3): names, help, no parent, no parameters, execute.
static void TakeEntry(unsigned char* e, const CommandDef& c)
{
    const char* ln = c.longName; const char* sn = c.shortName; const char* hp = c.help;
    memcpy(e + safescrap::kEntLongName,  &ln, 8);
    memcpy(e + safescrap::kEntShortName, &sn, 8);
    memcpy(e + safescrap::kEntHelp,      &hp, 8);
    e[safescrap::kEntNeedsParent] = 0;
    const UINT16 zero = 0;
    memcpy(e + safescrap::kEntNumParams, &zero, 2);
    memcpy(e + safescrap::kEntExecute, &c.execute, 8);
}

static void Install()
{
    auto base = (unsigned char*)GetModuleHandleW(nullptr);
    safescrap::Image img{ base, (UINT64)base };          // in the game .data pointers are relocated to the base
    if (!safescrap::Resolve(img, g_sites)) return;

    // Two free stub entries are needed BEFORE anything is written.
    unsigned char* slots[2] = {};
    int found = 0;
    for (auto e : g_sites.candidates)
        if (e && found < 2) slots[found++] = e;
    if (found < 2) { Err("[commands] fewer than two free console entries (PyConsole, LuaConsole, GetOrbisModInfo, ClearPlaystationModSpace are taken). Not hooking."); return; }

    // Site A first: with it alone the plugin changes nothing (the mode is Off until a command runs, and the commands
    // are written last). Site B without site A would never be reached in our mode anyway.
    void* a = f4kit::RedirectCall(g_sites.siteA, (void*)&HookCanScrap, "can-scrap");
    if (!a) return;
    g_origCanScrap = (FnCanScrap)a;
    void* area = f4kit::RedirectCall(g_sites.siteArea, (void*)&HookInArea, "in-build-area");
    if (!area) { Err("[hook] site A got hooked and the area site did not: the commands are not installed."); return; }
    g_origInArea = (FnInArea)area;
    void* b = f4kit::RedirectCall(g_sites.siteB, (void*)&HookCanStore, "can-store");
    if (!b) { Err("[hook] sites A and area got hooked and site B did not: the commands are not installed."); return; }
    g_origCanStore = (FnCanStore)b;

    TakeEntry(slots[0], kCommands[0]);
    TakeEntry(slots[1], kCommands[1]);
#if TRACE
    Trace("[trace] installed. MEASUREMENT build: always writes the log (TRACE=1).");
#endif
}

extern "C" __declspec(dllexport) bool F4SEPlugin_Load(const void* /*f4se*/)
{
    f4kit::InitLog("SafeScrap_FO4.log");
    Install();
    return true;        // we never return false: we do not block the game over this
}

BOOL APIENTRY DllMain(HMODULE, DWORD, LPVOID) { return TRUE; }
