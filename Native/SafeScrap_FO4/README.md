# SafeScrap_FO4

An F4SE plugin that adds two console commands to scrap junk in the settlement you are in, using the
**workshop's own scrap routine**:

| command | what it scraps |
|---|---|
| `SafeScrap` | every object whose base is in `Data\F4SE\Plugins\SafeScrap.txt` — the list the SafeScrap app exports (only what you marked as safe junk) |
| `ScrapAllOfThese` | every object with the **same base** as the object selected in the console (click it first). It does not look at the list: it is for what the rules do not cover |

Both scrap only inside the build area of the workshop the engine associates with you (the same area the workshop
uses), give the components to the workbench exactly as a normal scrap does, and fire the same
`OnWorkshopObjectDestroyed` event. **Objects with power** (a WorkshopPowerConnection value or the PowerConnection
keyword) are left alone and counted: scrap those from the workshop menu (see *Power* below).

## How it works

The engine already has a scrap that needs no menu: the console command **`ScrapAll`**. Its worker sets your
workshop, builds the scrap context, walks the loaded cells and, for every reference:

```
0x1403A7B44  call CanScrap(ref, 0)             SITE A     yes -> a workshop? ->
0x1403A7B5F  call IsRefInBuildArea(ws, ref)    AREA SITE  yes -> ScrapRef(ctx, &ref, null)
0x1403A7BB8  call CanStore(ref)                SITE B     (only when CanScrap said no) yes -> Store it in the workshop
```

This plugin does **not** re-implement any of that. It redirects those three calls at their call sites (no function
prologue is touched) and, **only while one of its commands runs**, answers:

- site A: yes only if the base passes the command's filter and the engine's `CanScrap` says yes;
- area site: the engine's answer, except no for a reference with power (counted only when it is inside the
  settlement: the walk covers every loaded cell);
- site B: no (SafeScrap scraps what you selected; it never stores anything else).

With none of its commands running the three calls go straight to the engine: the vanilla `ScrapAll` behaves exactly
as before. The cell walk, the lock, the build-area test, the workshop handle, the context and the scrap itself are the
engine's own.

The commands take two unused console entries (`PyConsole`, `LuaConsole`, `GetOrbisModInfo`,
`ClearPlaystationModSpace`, in that order, only if they are still empty stubs). The executable has no way to add
commands; an unused entry is overwritten in place, keeping its opcode. `ForceRSXCrash` is never taken: F4SE uses it
for `GetF4SEVersion`. Known gap: a mod that takes the same entry by opcode **after** this plugin loads would win, and
cannot be detected without F4SE's API.

Every address is found by a byte signature with **exactly one match**, or by data (the `ScrapAll` console entry), and
cross-checked (the call sites must sit at their measured offsets inside the visitor and call the expected functions;
ScrapAll's execute must call the same worker). If anything does not line up it **does not hook**, writes why in the
log, and the game is left untouched. The measurements are in
`Tools\re-docs\RE_SAFESCRAP_WORKSHOP_SCRAP_2026-09-26.md`; `Tools\SafeScrapNativeGate` checks every address against
them on the installed executable.

## The list

`Data\F4SE\Plugins\SafeScrap.txt`, written by the SafeScrap app: one line per object, `Plugin|XXXXXX ; EditorID`;
lines starting with `;` are comments. It is re-read every time `SafeScrap` runs: export again from the app and run
the command, no restart needed. An object is matched by its **origin** plugin (compared without regard to case) and
its local id (12 bits for a light plugin, 24 otherwise) — the same identity the app writes.

## Power

The workshop menu's scrap runs one step that `ScrapAll` skips: it looks for wires physically touching the object and
fixes their links. What skipping it leaves behind is not measured, so these commands do not scrap objects with power.

## Installation

The SafeScrap app installs it with **Export to game** (`Data\F4SE\Plugins\SafeScrap_FO4.dll`) and removes it with
**Remove from game**. Requires F4SE.

## Log

`Documents\My Games\Fallout4\F4SE\SafeScrap_FO4.log`, **only if something fails**. A `#define TRACE 1` build writes
one line per scrapped object: that is a measurement build, and the app's build refuses to ship it.

## Game versions

Pinned to **1.11.240** (MD5 `c5791a0ce539465701c6e38fa465960c`) through `compatibleVersions`, with
`addressIndependence = 0`: on another runtime F4SE does not load it and says so.

## Source

`src\SafeScrap_FO4.cpp` (hooks and commands), `src\SafeScrapSites.h` (where: pure resolution, shared with the probe),
`src\SafeScrapList.h` (the list reader: the app's law, held to it by `list-cases.json`), and the workspace kit
`FO4_Base_Library\Native\F4sePluginKit.h` (scanner, log, call redirection — shared with NPC_Manager_FO4_LoadBake).
Source is public, as the F4SE readme requires.
