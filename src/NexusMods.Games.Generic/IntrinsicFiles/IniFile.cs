using NexusMods.Abstractions.GameLocators;
using NexusMods.Abstractions.Loadouts;
using NexusMods.Abstractions.Loadouts.Synchronizers;
using NexusMods.MnemonicDB.Abstractions;

namespace NexusMods.Games.Generic.IntrinsicFiles;

public class IniFile : IIntrinsicFile
{
    public IniFile(GamePath path)
    {
        Path = path;
    }
    
    public GamePath Path { get; }
    
    public Task Write(Stream stream, Loadout.ReadOnly loadout, Dictionary<GamePath, SyncNode> syncTree)
    {
        var db = loadout.Db;
        var entries = LoadEntries(loadout, db);
        return Task.CompletedTask;
    }

    private Dictionary<(string Section, string Key), string> LoadEntries(Loadout.ReadOnly loadout, IDb db)
    {
        var entries = db.Connection.Query<(string, string, string)>(
            $"""
             SELECT Section, Key, Value FROM mdb_IniFileEntry(Db=>{db}) entry
             INNER JOIN mdb_IniFileDefinition(Db=>{db}) file ON entry.IniFile = file.Id 
             WHERE file.Path.Item2 = {Path.LocationId} 
                 AND file.Path.Item3 = {Path.FileName}
                 AND file.Loadout = {loadout.Id}
            """
        );
        return Index(entries);
    }

    public Task Ingest(Stream stream, Loadout.ReadOnly loadout, Dictionary<GamePath, SyncNode> syncTree, ITransaction tx)
    {
        var data = Read(stream);
        var newEntries = LoadEntries(loadout, loadout.Db);
        var existingEntries = Index(data.SelectMany(x => x.Value.Select(y => (x.Key, y.Key, y.Value))));
        
        throw new NotImplementedException();
    }

    private Dictionary<(string Section, string Key), string> Index(IEnumerable<(string Section, string Key, string Value)> entries)
    {
        var dictionary = new Dictionary<(string, string), string>(_comparer);

        foreach (var (section, key, value) in entries)
        {
            dictionary[(section, key)] = value;
        }
        return dictionary;
    }

    private static readonly EntryComparator _comparer = new();
    
    internal class EntryComparator : IEqualityComparer<(string, string)>
    {
        public bool Equals((string, string) x, (string, string) y)
        {
            return x.Item1.Equals(y.Item1, StringComparison.InvariantCultureIgnoreCase)
                && x.Item2.Equals(y.Item2, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode((string, string) obj)
        {
            return string.GetHashCode(obj.Item1, StringComparison.InvariantCultureIgnoreCase) ^
                string.GetHashCode(obj.Item2, StringComparison.InvariantCultureIgnoreCase);
        }
    }

    public Dictionary<string, Dictionary<string, string>> Read(Stream stream)
    {
        var result = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        using var sr = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);

        string currentSection = "";
        var currentMap = GetOrAddSection(result, currentSection);
        string? line;
        int lineNo = 0;

        while ((line = sr.ReadLine()) != null)
        {
            lineNo++;
            line = line.Trim();

            // Skip blanks and full-line comments
            if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#"))
                continue;

            // Section header
            if (line[0] == '[' && line.EndsWith("]"))
            {
                currentSection = line.Substring(1, line.Length - 2).Trim();
                currentMap = GetOrAddSection(result, currentSection);
                continue;
            }

            // Key/value
            int sep = FindKeyValueSeparator(line);
            if (sep < 0)
            {
                // Not a key/value line; ignore gracefully (or throw if you prefer)
                continue;
            }

            var key = line.Substring(0, sep).Trim();
            var value = line.Substring(sep + 1);

            // Support values like: key = "some text ; not a comment" ; real comment
            value = StripInlineComment(value).Trim();

            // Simple quoted value support with common escapes
            if (value.Length >= 2 && value[0] == '"' && value[^1] == '"')
            {
                value = Unescape(value.Substring(1, value.Length - 2));
            }

            // Optional: handle trailing line continuation with backslash
            // (only if not in quotes — we trimmed above so just detect last char)
            while (value.EndsWith("\\"))
            {
                value = value.Substring(0, value.Length - 1); // remove trailing '\'
                var next = sr.ReadLine();
                lineNo++;
                if (next is null) break;
                // Append the next line as-is (minus trailing inline comment)
                value += "\n" + StripInlineComment(next).TrimEnd();
            }

            if (key.Length == 0)
                continue; // ignore empty keys

            currentMap[key] = value;
        }

        return result;
    }

    private static Dictionary<string, string> GetOrAddSection(
        Dictionary<string, Dictionary<string, string>> dict,
        string name)
    {
        if (!dict.TryGetValue(name, out var section))
        {
            section = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            dict[name] = section;
        }
        return section;
    }

    // Finds '=' or ':' not inside quotes
    private static int FindKeyValueSeparator(string s)
    {
        bool inQuotes = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '"')
            {
                bool escaped = i > 0 && s[i - 1] == '\\';
                if (!escaped) inQuotes = !inQuotes;
            }
            else if (!inQuotes && (c == '=' || c == ':'))
            {
                return i;
            }
        }
        return -1;
    }

    // Strips ';' or '#' comments that are not inside quotes
    private static string StripInlineComment(string s)
    {
        bool inQuotes = false;
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '"')
            {
                bool escaped = i > 0 && s[i - 1] == '\\';
                if (!escaped) inQuotes = !inQuotes;
            }
            else if (!inQuotes && (c == ';' || c == '#'))
            {
                return s.Substring(0, i).TrimEnd();
            }
        }
        return s.TrimEnd();
    }

    private static string Unescape(string s)
    {
        // Minimal, common escapes. Expand if you need unicode, etc.
        return s
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t")
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\");
    }
    
    
    
}
