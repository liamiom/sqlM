namespace sqlM.State;

public class Container
{
    public string CurrentDirectory { get; set; } = "";
    public string OutputDirectory { get; set; } = "";
    public string SourceDirectory { get; set; } = "";
    public string ConnectionString { get; set; } = "";
    public bool GenerateInterfaceClass { get; set; } = true;
    public bool GenerateSyncMethods { get; set; } = true;
    public bool GenerateAsyncMethods { get; set; } = true;
    public SqlFile[] SqlFiles { get; set; } = Array.Empty<SqlFile>();
    public List<File> GeneratedEntitiesFiles { get; set; } = new List<File>();
    public List<File> GeneratedModelsFiles { get; set; } = new List<File>();
    public List<ResultClassTypes.ResultTypeAbstract> EntityTypeCache { get; set; } = [];
}
