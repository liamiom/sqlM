namespace sqlM.ResultClassTypes;

internal class BaseClassFile
{
    public string FileName { get; set; }
    public string Content { get; set; }
    public string EntityName { get; set; }
    public string? MethodSigniture { get; set; }
    public string ErrorMessage { get; set; } = "";
}
