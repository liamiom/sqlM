using sqlM.Extensions;

namespace sqlM.ResultClassTypes;
public class Flags
{
    public string EntityName { get; init; }
    public bool GenerateCrudMethods { get; init ; }

    public Flags()
    {
        EntityName = "";
        GenerateCrudMethods = false;
    }

    public Flags(string content)
    {
        Dictionary<string, string> flagLines = GetFlagLines(content);
        EntityName = GetFlagString(flagLines, "TypeName");
        GenerateCrudMethods = GetFlagBool(flagLines, "CrudMethods") && !string.IsNullOrWhiteSpace(EntityName);
    }

    private static Dictionary<string, string> GetFlagLines(string content) =>
        content
            .RegexMatchThenReplaceAll(@"--\s*(\S+)\s*=\s*(\S+)", "$1=$2")
            .Select(i => i.Split('='))
            .Where(i => i.Length == 2)
            .ToDictionary(i => i[0], i => i[1]);

    private static string GetFlagString(Dictionary<string, string> lines, string item) =>
        lines.ContainsKey(item)
            ? lines[item]
            : "";

    private static bool GetFlagBool(Dictionary<string, string> lines, string item) =>
        bool.TryParse(GetFlagString(lines, item), out bool flag)
            ? flag
            : false;
}
