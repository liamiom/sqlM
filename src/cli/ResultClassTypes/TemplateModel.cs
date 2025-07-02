namespace sqlM.ResultClassTypes;
internal class TemplateModel
{
    public required ScriptClassFile.ObjectReturnTypes ObjectType;
    public required State.SqlFile.ObjectTypes ScriptType;
    public required string TypeStaticClassName;
    public required string MethodName;
    public required string EntityName;
    public required string StaticSqlContent;
    public required bool IsQuery;
    public required bool IsScalar;
    public required bool IsMethodType;
    public required bool IsTableType;
    public required string ScalarTypeName;
    public required string ReturnType;
    public required string MethodParams;
    public required string SqlParams;
    public required string GetParams;
    public required string UpdateSet;
    public required string InsertColumns;
    public required string InsertParams;
    public required string QueryParams;
    public required string UpdateParams;
    public required List<Column> Columns;
}