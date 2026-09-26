// SafeScrapList.h - reading Data\F4SE\Plugins\SafeScrap.txt, the list the app writes (SafeScrap
// Engine\Evaluator.vb ListWriter: one line per object "Plugin|XXXXXX ; EditorID", comment lines start with ';').
//
// THE LAW IS THE APP'S, transcribed: ListWriter.ParseLine (.NET) = this ParseLine (C++), and the identifier part
// is FO4_Base_Library FormIdentifiers.TryParse: the text before the FIRST '|' is the plugin (trimmed, not empty),
// the text after it up to the first ';' is the object id in hex (trimmed; a 32-bit value, masked to 24 bits).
// The two implementations are held to each other by one table of cases (list-cases.json next to this project),
// read by the .NET gate and by the native probe.
//
// PURE: no engine access. The plugin compares a key with the engine's view of a form (see SafeScrap_FO4.cpp).
#pragma once

#include <cstdint>
#include <string>
#include <unordered_set>

namespace safescrap
{

enum class LineKind { Skip = 0, Entry = 1, Invalid = 2 };

// .NET String.Trim is Unicode whitespace; the file the app writes only ever has ASCII spaces, tabs and CR.
inline std::string TrimAscii(const std::string& s)
{
    size_t a = 0, b = s.size();
    while (a < b && (s[a] == ' ' || s[a] == '\t' || s[a] == '\r' || s[a] == '\n')) ++a;
    while (b > a && (s[b - 1] == ' ' || s[b - 1] == '\t' || s[b - 1] == '\r' || s[b - 1] == '\n')) --b;
    return s.substr(a, b - a);
}

// UInteger.TryParse(hex, HexNumber): hex digits only (no sign, no "0x"), leading zeros allowed, the value must fit
// in 32 bits.
inline bool ParseHex32(const std::string& t, uint32_t& out)
{
    if (t.empty()) return false;
    uint64_t v = 0;
    for (char ch : t)
    {
        int d;
        if (ch >= '0' && ch <= '9') d = ch - '0';
        else if (ch >= 'a' && ch <= 'f') d = ch - 'a' + 10;
        else if (ch >= 'A' && ch <= 'F') d = ch - 'A' + 10;
        else return false;
        v = v * 16 + (uint64_t)d;
        if (v > 0xFFFFFFFFull) return false;
    }
    out = (uint32_t)v;
    return true;
}

// One line of the file.
inline LineKind ParseLine(const std::string& line, std::string& plugin, uint32_t& localId)
{
    plugin.clear();
    localId = 0;
    const std::string t = TrimAscii(line);
    if (t.empty() || t[0] == ';') return LineKind::Skip;
    // The identifier: up to the first ';' AFTER the pipe (a plugin name cannot contain '|'; the EditorID comment
    // follows the id).
    const size_t pipe0 = line.find('|');
    std::string identifier = line;
    if (pipe0 != std::string::npos)
    {
        const size_t semi = line.find(';', pipe0);
        if (semi != std::string::npos) identifier = line.substr(0, semi);
    }
    // FormIdentifiers.TryParse
    const size_t pipe = identifier.find('|');
    if (pipe == std::string::npos || pipe == 0 || pipe >= identifier.size() - 1) return LineKind::Invalid;
    const std::string master = TrimAscii(identifier.substr(0, pipe));
    if (master.empty()) return LineKind::Invalid;
    uint32_t parsed = 0;
    if (!ParseHex32(TrimAscii(identifier.substr(pipe + 1)), parsed)) return LineKind::Invalid;
    plugin = master;
    localId = parsed & 0xFFFFFFu;
    return LineKind::Entry;
}

// The key a form is compared by: plugin name folded to ASCII lower case (the engine resolves file names without
// regard to case; the app writes the name as the load order or the disk spells it) + the local object id.
inline std::string FoldAscii(const char* s)
{
    std::string r(s ? s : "");
    for (auto& ch : r) if (ch >= 'A' && ch <= 'Z') ch = (char)(ch - 'A' + 'a');
    return r;
}

inline std::string MakeKey(const std::string& foldedPlugin, uint32_t localId)
{
    char buf[16];
    snprintf(buf, sizeof buf, "|%06X", localId);
    return foldedPlugin + buf;
}

struct ListStats { unsigned entries = 0; unsigned invalid = 0; };

// Reads the whole file into a set of keys. False if the file cannot be opened.
inline bool LoadList(const wchar_t* path, std::unordered_set<std::string>& keys, ListStats& stats)
{
    keys.clear();
    stats = {};
    FILE* f = nullptr;
    if (_wfopen_s(&f, path, L"rb") != 0 || !f) return false;
    std::string line;
    int ch;
    auto flush = [&]()
    {
        std::string plugin;
        uint32_t id = 0;
        switch (ParseLine(line, plugin, id))
        {
            case LineKind::Entry:   keys.insert(MakeKey(FoldAscii(plugin.c_str()), id)); ++stats.entries; break;
            case LineKind::Invalid: ++stats.invalid; break;
            default: break;
        }
        line.clear();
    };
    while ((ch = fgetc(f)) != EOF)
    {
        if (ch == '\n') flush();
        else line.push_back((char)ch);
    }
    if (!line.empty()) flush();
    fclose(f);
    return true;
}

} // namespace safescrap
