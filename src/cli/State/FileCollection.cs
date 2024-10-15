namespace sqlM.State;
internal class FileCollection
{
    public List<string> Query { get; set; }
    public string QueryPath { get; set; }
    public List<string> Table { get; set; }
    public string TablePath { get; set; }
    public List<string> View { get; set; }
    public string ViewPath { get; set; }
    public List<string> Function { get; set; }
    public string FunctionPath { get; set; }
    public List<string> StoredProcedure { get; set; }
    public string StoredProcedurePath { get; set; }
    public int FileCount =>
                    Query.Count +
                    Table.Count +
                    View.Count +
                    Function.Count +
                    StoredProcedure.Count;
}
