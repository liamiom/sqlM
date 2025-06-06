﻿using System.Text.RegularExpressions;

namespace sqlM.State;

public class SqlFile : File
{
    public enum ObjectTypes { None, Query, Table, View, Function, StoredProcedure }
    public List<KeyValuePair<string, Type>> Paramiters { get; set; }
    public Dictionary<string, string> Names { get; set; }
    public ObjectTypes ScriptType { get; set; }
    public List<SqlFile> Dependencies { get; set; } = new List<SqlFile>();
    public int SortOrder { get; set; } = 0;

    public string[] ContentSplitOnGo()
    {
        string tidyGoLines = Regex.Replace(Content, @"^(\s*GO\s*)+$", "GO", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return tidyGoLines.Split("\nGO", StringSplitOptions.RemoveEmptyEntries);
    }
}
