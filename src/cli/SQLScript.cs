using System.Data;

namespace sqlM;

public class SQLScript
{
    public string FileName { get; set; }
    public string CleanFileName { get; set; }
    public string Content { get; set; }
    public List<KeyValuePair<string, Type>> Paramiters { get; set; }
    public string ReturnType { get; set; }
    public string QueryClassName { get; set; }
    public string ReturnClassName { get; set; }
    public DataTable? TableSchema { get; set; }
    public bool IsQuery { get => TableSchema != null; }
}
