namespace sqlM.State;

public class SqlFile : File
{
    public enum ObjectTypes { None, Query, Table, View, Function, StoredProcedure }
    public List<KeyValuePair<string, Type>> Paramiters { get; set; }
    public Dictionary<string, string> Names { get; set; }
    public ObjectTypes ScriptType { get; set; }
    public List<SqlFile> Dependencies { get; set; } = new List<SqlFile>();
    public string ContentWithDependencies =>
        Dependencies.Any()
            ? Dependencies.Select(i => i.ContentWithDependencies).Aggregate((x, y) => $"{x}\nGO\n\n\n{y}") + $"\nGO\n\n\n{Content}"
            : Content;
    public int SortOrder { get; set; } = 0;
}
