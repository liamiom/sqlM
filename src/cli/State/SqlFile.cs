using System.Text.RegularExpressions;

namespace sqlM.State;

public class SqlFile : File
{
    public enum ObjectTypes { None = 0, Query = 5, Table = 1, View = 2, Function = 3, StoredProcedure = 4 }
    public List<KeyValuePair<string, Type>> Paramiters { get; set; }
    public Dictionary<string, string> Names { get; set; }
    public ObjectTypes ScriptType { get; set; }
    public List<SqlFile> Dependencies { get; set; } = new List<SqlFile>();
    public int SortOrder { get; set; } = 0;
    public ResultClassTypes.Flags OverrideFlags { get; set; } = new ResultClassTypes.Flags();

    public string[] ContentSplitOnGo()
    {
        string tidyGoLines = Regex.Replace(Content, @"^(\s*GO\s*)+$", "GO", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return tidyGoLines.Split("\nGO", StringSplitOptions.RemoveEmptyEntries);
    }
}
