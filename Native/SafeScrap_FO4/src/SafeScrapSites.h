// SafeScrapSites.h - WHERE SafeScrap_FO4 hooks, resolved from the image. PURE: it reads and writes nothing,
// so the plugin (in the game) and the probe (Fallout4.exe mapped from disk) run THIS SAME code.
//
// Every address comes from Tools\re-docs\RE_SAFESCRAP_WORKSHOP_SCRAP_2026-09-26.md (Fallout4.exe 1.11.240,
// MD5 C5791A0CE539465701C6E38FA465960C) and is found here by a byte signature with EXACTLY ONE match in
// .text, or by data (the "ScrapAll" console entry). Nothing is hardcoded; the VAs in the comments are the ones
// measured on 1.11.240, which the probe checks.
#pragma once

#include "F4sePluginKit.h"

namespace safescrap
{

// ---------------------------------------------------------------------------------------------- signatures

// ScrapAllWorker 0x14037D070 (RE 1.4): the worker of the console command ScrapAll.
inline const unsigned char kSigWorkerB[] = { 0x48,0x8B,0xC4,0x41,0x56,0x48,0x81,0xEC,0xA0,0x00,0x00,0x00,0x48,0x89,0x58,0x18,
                                             0x48,0x89,0x70,0xE8,0x48,0x8B,0xF1,0x48,0x89,0x78,0xE0,0x48,0x8B,0xFA,0x4C,0x89,0x60,0xD8 };

// ScrapAllCellVisitor 0x1403A7AD0 (RE 1.4).
inline const unsigned char kSigVisitorB[] = { 0x40,0x53,0x41,0x57,0x48,0x83,0xEC,0x58,0x48,0x89,0x6C,0x24,0x78,0x48,0x8D,0x99,0xC0,0x00,0x00,0x00,
                                              0x48,0x8B,0xE9,0x48,0x89,0x74,0x24,0x50,0x4C,0x89,0x74,0x24,0x40 };

// Site A (RE 8.1): the CanScrap call in the visitor, E8 at +0x13 (0x1403A7B44 -> 0x140384910).
//   8B 47 10 C1 E8 05 A8 01 0F 85 ?? ?? ?? ?? 33 D2 48 8B CF E8 ?? ?? ?? ?? 84 C0 74 68 49 8B 06
inline const unsigned char kSigSiteAB[] = { 0x8B,0x47,0x10,0xC1,0xE8,0x05,0xA8,0x01,0x0F,0x85,0,0,0,0,0x33,0xD2,0x48,0x8B,0xCF,
                                            0xE8,0,0,0,0,0x84,0xC0,0x74,0x68,0x49,0x8B,0x06 };
inline const bool kSigSiteAF[] = { 1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1, 1,0,0,0,0,1,1,1,1,1,1,1 };
constexpr ptrdiff_t kSiteAOff = 0x13;

// Site B (RE 8.1): the CanStore call in the visitor, E8 at +0x05 (0x1403A7BB8 -> 0x14039F0A0).
//   EB 52 48 8B CF E8 ?? ?? ?? ?? 84 C0 74 68 48 8D 94 24 80 00 00 00 48 8B CF E8 ?? ?? ?? ??
inline const unsigned char kSigSiteBB[] = { 0xEB,0x52,0x48,0x8B,0xCF,0xE8,0,0,0,0,0x84,0xC0,0x74,0x68,0x48,0x8D,0x94,0x24,0x80,0x00,0x00,0x00,
                                            0x48,0x8B,0xCF,0xE8,0,0,0,0 };
inline const bool kSigSiteBF[] = { 1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1, 1,1,1,1,0,0,0,0 };
constexpr ptrdiff_t kSiteBOff = 0x05;
// Where each site sits inside the visitor (RE 8.1: 0x1403A7B44 / 0x1403A7BB8 in 0x1403A7AD0).
constexpr ptrdiff_t kSiteAInVisitor = 0x74;
constexpr ptrdiff_t kSiteBInVisitor = 0xE8;
// The build-area test the visitor makes after site A (RE 8.1: `0x1403A7B5F E8 AC CC FD FF call 0x140384810`,
// reached only when CanScrap said yes and there is a workshop): +0x8F in the visitor.
constexpr ptrdiff_t kSiteAreaInVisitor = 0x8F;

// IsRefInBuildArea(workshop, ref) 0x140384810 (RE 3.1).
inline const unsigned char kSigInAreaB[] = { 0x4C,0x8B,0x8A,0xB8,0x00,0x00,0x00,0x4C,0x8B,0xD2,0x4D,0x85,0xC9,0x74,0x44,0x4C,
                                             0x8B,0x81,0xB8,0x00,0x00,0x00,0x4D,0x85,0xC0,0x74,0x10,0x41,0xF6,0x40,0x40,0x01 };

// CanScrap(ref, storeMode) 0x140384910 (RE 2).
inline const unsigned char kSigCanScrapB[] = { 0x48,0x89,0x5C,0x24,0x18,0x48,0x89,0x7C,0x24,0x20,0x41,0x56,0x48,0x83,0xEC,0x20,
                                               0x48,0x8B,0x81,0xE0,0x00,0x00,0x00,0x32,0xDB,0x44,0x0F,0xB6,0xF2,0x48,0x8B,0xF9 };

// Power gate 0x14038E380 (RE 8.6): WorkshopPowerConnection actor value != 0, or keyword PowerConnection.
//   40 53 48 83 EC 20 48 8B D9 E8 ?? ?? ?? ?? 4C 8B 43 58 48 8D 4B 58 48 8B 90 00 04 00 00
inline const unsigned char kSigPowerB[] = { 0x40,0x53,0x48,0x83,0xEC,0x20,0x48,0x8B,0xD9,0xE8,0,0,0,0,0x4C,0x8B,0x43,0x58,0x48,0x8D,0x4B,0x58,
                                            0x48,0x8B,0x90,0x00,0x04,0x00,0x00 };
inline const bool kSigPowerF[] = { 1,1,1,1,1,1,1,1,1,1,0,0,0,0,1,1,1,1,1,1,1,1, 1,1,1,1,1,1,1 };

// GetFile(form, idx) 0x1403123D0 (RE 8.4): the full body.
inline const unsigned char kSigGetFileB[] = { 0x4C,0x8B,0x41,0x08,0x4D,0x85,0xC0,0x74,0x1D,0x41,0x8B,0x40,0x08,0x85,0xC0,0x7E,0x15,0x85,0xD2,0x78,
                                              0x04,0x3B,0xD0,0x7C,0x03,0x8D,0x50,0xFF,0x49,0x8B,0x00,0x8B,0xCA,0x48,0x8B,0x04,0xC8,0xC3 };

// IsLight(file) 0x1402F91A0 (RE 8.4): ([file+0x334] >> 9) & 1, the full body.
inline const unsigned char kSigIsLightB[] = { 0x8B,0x81,0x34,0x03,0x00,0x00,0xC1,0xE8,0x09,0x24,0x01,0xC3 };

// ---------------------------------------------------------------------------------------------- console table
// Entry layout (RE 4.2), 0x50 bytes.
constexpr size_t kEntrySize      = 0x50;
constexpr size_t kEntLongName    = 0x00;
constexpr size_t kEntShortName   = 0x08;
constexpr size_t kEntOpcode      = 0x10;
constexpr size_t kEntHelp        = 0x18;
constexpr size_t kEntNeedsParent = 0x20;
constexpr size_t kEntNumParams   = 0x22;
constexpr size_t kEntParams      = 0x28;
constexpr size_t kEntExecute     = 0x30;
constexpr UINT32 kFirstOpcode    = 0x100;       // RE 4.1: "Show"
constexpr UINT32 kScrapAllOpcode = 0x2FB;       // RE 1.4
constexpr UINT32 kLastOpcode     = 0x30B;       // RE 4.1; 0x30C is the " " sentinel
// ScrapAll's execute 0x1405FA9A0 (RE 1.4 / 6): +0x18 E8 -> ScrapAllWorker; +0x2E `48 8B 0D disp32` -> the console
// print target [0x1430F83B8]; +0x35 E8 -> the printf-like 0x14103C0C0.
constexpr size_t kExecCallWorker = 0x18;
constexpr size_t kExecLoadTarget = 0x2E;
constexpr size_t kExecCallPrint  = 0x35;

// The stub entries this plugin may take (RE 8.3: exec exactly B0 01 C3, 0 parameters, named by no installed
// plugin), in order. ForceRSXCrash is EXCLUDED: F4SE finds it by name before loading plugins and writes
// GetF4SEVersion into it AFTER (Hooks_ObScript.cpp; f4se.cpp:188,202).
inline const char* const kCandidates[] = { "PyConsole", "LuaConsole", "GetOrbisModInfo", "ClearPlaystationModSpace" };
inline const char        kF4seTaken[]  = "ForceRSXCrash";
inline const unsigned char kStubExec[] = { 0xB0, 0x01, 0xC3 };      // mov al,1; ret

// ---------------------------------------------------------------------------------------------- result

struct Sites
{
    void*          worker    = nullptr;   // 0x14037D070
    unsigned char* visitor   = nullptr;   // 0x1403A7AD0
    unsigned char* siteA     = nullptr;   // 0x1403A7B44 (E8 -> canScrap)
    unsigned char* siteB     = nullptr;   // 0x1403A7BB8 (E8 -> canStore)
    unsigned char* siteArea  = nullptr;   // 0x1403A7B5F (E8 -> inArea)
    void*          inArea    = nullptr;   // 0x140384810
    void*          canScrap  = nullptr;   // 0x140384910
    void*          canStore  = nullptr;   // 0x14039F0A0 (derived from site B)
    void*          powerGate = nullptr;   // 0x14038E380
    void*          getFile   = nullptr;   // 0x1403123D0
    void*          isLight   = nullptr;   // 0x1402F91A0
    unsigned char* table     = nullptr;   // 0x142EFA130
    unsigned char* scrapAllEntry = nullptr; // 0x142F03FA0
    unsigned char* scrapAllExec  = nullptr; // 0x1405FA9A0
    void*          print     = nullptr;   // 0x14103C0C0
    unsigned char* printTarget = nullptr; // 0x1430F83B8 (a global holding the console object)
    unsigned char* candidates[4] = {};    // the entries of kCandidates that are still stubs (nullptr = not usable)
};

// Pointers stored in .data hold ADDRESSES: in the game they are relocated to the module base; in an image mapped
// from disk they are not, and still refer to the preferred base. `ptrBase` is the base those stored pointers refer
// to, so a stored pointer converts to an address inside the image being read.
struct Image
{
    unsigned char* base;
    UINT64         ptrBase;
    unsigned char* At(UINT64 storedPointer) const { return base + (storedPointer - ptrBase); }
    UINT64 Stored(const unsigned char* p) const { return ptrBase + (UINT64)(p - base); }
};

inline bool InRange(const f4kit::Range& r, const unsigned char* p, size_t n) { return p >= r.start && p + n <= r.start + r.len; }

inline unsigned char* FindExact(const f4kit::Range& text, const unsigned char* bytes, size_t n, const char* who)
{
    bool fixed[64];
    if (n > sizeof fixed) { f4kit::Err("[sig:%s] signature longer than 64 bytes. Not hooking.", who); return nullptr; }
    for (size_t i = 0; i < n; ++i) fixed[i] = true;
    f4kit::Signature s{ bytes, fixed, n };
    return f4kit::FindUnique(text, s, who);
}

// A null-terminated string that STARTS at a string boundary (preceded by 0), exactly once in the section.
inline unsigned char* FindString(const f4kit::Range& r, const char* str, const char* who)
{
    const size_t n = strlen(str) + 1;            // with its terminator
    unsigned char* found = nullptr;
    int count = 0;
    for (size_t i = 1; i + n <= r.len; ++i)
        if (r.start[i - 1] == 0 && memcmp(r.start + i, str, n) == 0)
        {
            if (++count > 1) { f4kit::Err("[data:%s] string found %d times; expected 1. Not hooking.", who, count); return nullptr; }
            found = r.start + i;
        }
    if (!found) f4kit::Err("[data:%s] string not present. Unsupported game version. Not hooking.", who);
    return found;
}

// The 8-byte-aligned slot of `data` that stores `value`, exactly once.
inline unsigned char* FindPointer(const f4kit::Range& data, UINT64 value, const char* who)
{
    unsigned char* found = nullptr;
    int count = 0;
    for (size_t i = 0; i + 8 <= data.len; i += 8)
    {
        UINT64 v;
        memcpy(&v, data.start + i, 8);
        if (v != value) continue;
        if (++count > 1) { f4kit::Err("[data:%s] pointer found %d times; expected 1. Not hooking.", who, count); return nullptr; }
        found = data.start + i;
    }
    if (!found) f4kit::Err("[data:%s] pointer not present. Not hooking.", who);
    return found;
}

inline UINT32 EntryOpcode(const unsigned char* e) { UINT32 v; memcpy(&v, e + kEntOpcode, 4); return v; }
inline UINT64 EntryPtr(const unsigned char* e, size_t off) { UINT64 v; memcpy(&v, e + off, 8); return v; }

// Resolves everything. False (with the reason in the log) if ANYTHING does not line up.
inline bool Resolve(const Image& img, Sites& out)
{
    f4kit::Range text{}, data{}, rdata{};
    if (!f4kit::GetSection(img.base, ".text", text) || !f4kit::GetSection(img.base, ".data", data) ||
        !f4kit::GetSection(img.base, ".rdata", rdata))
    { f4kit::Err("[init] could not locate .text/.data/.rdata of the executable. Not hooking."); return false; }

    // Functions by their own signatures.
    if (!(out.worker    = FindExact(text, kSigWorkerB,   sizeof kSigWorkerB,   "scrapall-worker")))  return false;
    if (!(out.visitor   = FindExact(text, kSigVisitorB,  sizeof kSigVisitorB,  "scrapall-visitor"))) return false;
    if (!(out.canScrap  = FindExact(text, kSigCanScrapB, sizeof kSigCanScrapB, "can-scrap")))        return false;
    if (!(out.getFile   = FindExact(text, kSigGetFileB,  sizeof kSigGetFileB,  "get-file")))         return false;
    if (!(out.isLight   = FindExact(text, kSigIsLightB,  sizeof kSigIsLightB,  "is-light")))         return false;
    f4kit::Signature sp{ kSigPowerB, kSigPowerF, sizeof kSigPowerB };
    if (!(out.powerGate = f4kit::FindUnique(text, sp, "power-gate"))) return false;

    // The two call sites: each by its own signature, INSIDE the visitor, E8 to the expected function.
    f4kit::Signature sa{ kSigSiteAB, kSigSiteAF, sizeof kSigSiteAB };
    unsigned char* a = f4kit::FindUnique(text, sa, "site-a");
    if (!a) return false;
    out.siteA = a + kSiteAOff;
    f4kit::Signature sb{ kSigSiteBB, kSigSiteBF, sizeof kSigSiteBB };
    unsigned char* b = f4kit::FindUnique(text, sb, "site-b");
    if (!b) return false;
    out.siteB = b + kSiteBOff;
    // Both sites belong to THE visitor, at the offsets measured in RE 8.1: 0x1403A7B44 - 0x1403A7AD0 = 0x74 and
    // 0x1403A7BB8 - 0x1403A7AD0 = 0xE8. Anything else is another function that happens to match.
    if (out.siteA != out.visitor + kSiteAInVisitor || out.siteB != out.visitor + kSiteBInVisitor)
    { f4kit::Err("[guard] the call sites are not at +0x74 / +0xE8 of ScrapAllCellVisitor (0x%p / 0x%p vs 0x%p). Not hooking.", out.siteA, out.siteB, out.visitor); return false; }
    if (f4kit::CallTarget(out.siteA, "site-a") != out.canScrap)
    { f4kit::Err("[guard] site A does not call CanScrap. Not hooking."); return false; }
    if (!(out.canStore = f4kit::CallTarget(out.siteB, "site-b"))) return false;
    // The build-area call: at its measured place in the visitor, and to IsRefInBuildArea found by its own signature.
    if (!(out.inArea = FindExact(text, kSigInAreaB, sizeof kSigInAreaB, "in-build-area"))) return false;
    out.siteArea = out.visitor + kSiteAreaInVisitor;
    if (f4kit::CallTarget(out.siteArea, "site-area") != out.inArea)
    { f4kit::Err("[guard] the visitor does not call IsRefInBuildArea at +0x8F. Not hooking."); return false; }

    // The console table, by data: "ScrapAll" in .rdata -> the ONE pointer to it in .data is its entry.
    unsigned char* name = FindString(rdata, "ScrapAll", "scrapall-name");
    if (!name) return false;
    unsigned char* entry = FindPointer(data, img.Stored(name), "scrapall-entry");
    if (!entry) return false;
    if (EntryOpcode(entry) != kScrapAllOpcode)
    { f4kit::Err("[guard] the ScrapAll entry has opcode 0x%X, not 0x%X. Not hooking.", EntryOpcode(entry), kScrapAllOpcode); return false; }
    out.scrapAllEntry = entry;
    unsigned char* table = entry - (size_t)(kScrapAllOpcode - kFirstOpcode) * kEntrySize;
    for (UINT32 op = kFirstOpcode; op <= kLastOpcode; ++op)
    {
        const unsigned char* e = table + (size_t)(op - kFirstOpcode) * kEntrySize;
        if (!InRange(data, e, kEntrySize) || EntryOpcode(e) != op)
        { f4kit::Err("[guard] console table not continuous at opcode 0x%X. Not hooking.", op); return false; }
    }
    out.table = table;

    // ScrapAll's execute: the worker it calls must be the one found above; the print function and its target.
    unsigned char* exec = img.At(EntryPtr(entry, kEntExecute));
    if (!InRange(text, exec, kExecCallPrint + 5)) { f4kit::Err("[guard] ScrapAll's execute is outside .text. Not hooking."); return false; }
    out.scrapAllExec = exec;
    if (f4kit::CallTarget(exec + kExecCallWorker, "scrapall-exec-worker") != out.worker)
    { f4kit::Err("[guard] ScrapAll's execute does not call the worker. Not hooking."); return false; }
    if (!(exec[kExecLoadTarget] == 0x48 && exec[kExecLoadTarget + 1] == 0x8B && exec[kExecLoadTarget + 2] == 0x0D))
    { f4kit::Err("[guard] ScrapAll's execute does not load the console at +0x2E. Not hooking."); return false; }
    out.printTarget = f4kit::RipTarget(exec + kExecLoadTarget, 3, 7);
    if (!InRange(data, out.printTarget, 8)) { f4kit::Err("[guard] the console print target is outside .data. Not hooking."); return false; }
    if (!(out.print = f4kit::CallTarget(exec + kExecCallPrint, "scrapall-exec-print"))) return false;

    // The candidate entries that are still stubs with 0 parameters.
    for (size_t c = 0; c < 4; ++c)
    {
        out.candidates[c] = nullptr;
        for (UINT32 op = kFirstOpcode; op <= kLastOpcode; ++op)
        {
            unsigned char* e = table + (size_t)(op - kFirstOpcode) * kEntrySize;
            const char* ln = (const char*)img.At(EntryPtr(e, kEntLongName));
            if (!InRange(rdata, (const unsigned char*)ln, 1) || strcmp(ln, kCandidates[c]) != 0) continue;
            UINT16 np; memcpy(&np, e + kEntNumParams, 2);
            const unsigned char* ex = img.At(EntryPtr(e, kEntExecute));
            if (np == 0 && InRange(text, ex, 3) && memcmp(ex, kStubExec, 3) == 0) out.candidates[c] = e;
            break;
        }
    }
    return true;
}

} // namespace safescrap
