using sqlM.Converters;

namespace sqlM.ResultClassTypes;

internal class ScriptClassFile : BaseClassFile
{
    public enum ObjectReturnTypes { QueryResult, QueryScalarResult, QueryNoResult, StoredProcedureResult, StoredProcedureScalarResult, StoredProcedureNoResult, Table, View, Function }

    private List<Column> Columns { get; set; }
    private string MethodName { get; set; }
    private string SqlContent { get; set; }
    private string MethodParams { get; set; }
    private string SqlParams { get; set; }

    public ScriptClassFile(
        string fileName,
        string entityName,
        string methodName,
        List<Column> columns,
        string sqlContent,
        string methodParams,
        string sqlParams,
        ObjectReturnTypes objectType,
        State.SqlFile.ObjectTypes ScriptType)
    {
        FileName = fileName;
        EntityName = entityName;
        MethodName = methodName;
        Columns = columns;
        SqlContent = sqlContent.Trim();
        MethodParams = methodParams;
        SqlParams = Templates.Parameters(sqlParams);


        bool isQuery = Columns?.Count > 0;
        bool isScalar = columns.Count() == 1;
        string scalarTypeName = isScalar 
            ? columns.First().FullDataType 
            : "";

        string typeStaticClassName = objectType == ObjectReturnTypes.QueryNoResult || objectType == ObjectReturnTypes.QueryScalarResult || objectType == ObjectReturnTypes.QueryResult
            ? "SqlQueryStrings"
            : "DatabaseUpdateStrings";

        string queryAssignment = GetAssignment(objectType, EntityName, MethodName, Columns, typeStaticClassName);
        string returnType = Templates.ReturnType(isQuery, isScalar, EntityName, scalarTypeName);
        string staticClass = Templates.StaticClass(MethodName, SqlContent, typeStaticClassName);
        string methodClass = ScriptType == State.SqlFile.ObjectTypes.Query || ScriptType == State.SqlFile.ObjectTypes.StoredProcedure
            ? Templates.MethodClass(MethodName, MethodParams, SqlParams, returnType, queryAssignment)
            : "";
        string entityTypeClass = isScalar
            ? ""
            : Templates.EntityTypeClass(EntityName, GetPropertyClassLines(Columns));

        string crudClass = "";
        if (ScriptType == State.SqlFile.ObjectTypes.Table)
        {
            IEnumerable<Column> idColumns = columns.Where(i => i.IsIdentity);
            IEnumerable<Column> dataColumns = columns.Where(i => !i.IsIdentity);

            string readDeleteSqlParams = idColumns
                .Select(i => $"\n            new SqlParameter(\"{i.ColumnName}\", {i.ColumnName}),")
                .Join();
            readDeleteSqlParams = Templates.Parameters(readDeleteSqlParams);

            string fieldList = dataColumns
                .Select(i => $"\n   {i.ColumnName}")
                .Join(",");
            string dataPropsList = dataColumns
                .Select(i => $"\n   @{i.ColumnName}")
                .Join(",");
            string insertScript = $"\nINSERT INTO {entityName} ({fieldList}\n) \nVALUES ({dataPropsList}\n)";

            string updateList = dataColumns
                .Select(i => $"\n   {i.ColumnName} = @{i.ColumnName}")
                .Join(",");
            string updateWhere = idColumns
                .Select(i => $"\n   {i.ColumnName} = ISNULLL(@{i.ColumnName}, {i.ColumnName})")
                .Join(" AND");
            string updateScript = $"\nUPDATE {entityName}\nSET\n{updateList}\nWHERE{updateWhere}";

            string deleteWhere = idColumns
                .Select(i => $"\n   {i.ColumnName} = @{i.ColumnName}")
                .Join(" AND");
            string deleteScript = $"\nDELETE {entityName}\nWHERE {deleteWhere}";

            string add = Templates.CrudAddMethod(entityName, insertScript, SqlParams, returnType, queryAssignment);
            string read = Templates.CrudReadMethod(entityName, methodParams, readDeleteSqlParams, returnType, queryAssignment);
            string update = Templates.CrudUpdateMethod(entityName, updateScript, SqlParams, returnType, queryAssignment);
            string delete = Templates.CrudDeleteMethod(entityName, methodParams, deleteScript, readDeleteSqlParams, returnType, queryAssignment);
            crudClass += Templates.CrudClass(add, read, update, delete);
        }

        Content = Templates.JoinClasses(staticClass, methodClass, entityTypeClass, crudClass);
    }

    private static string GetAssignment(ObjectReturnTypes objectType, string entityName, string methodName, List<Column> columns, string typeStaticClassName) => 
        objectType switch
        {
            ObjectReturnTypes.QueryNoResult => Templates.QueryNonAssignment(methodName, typeStaticClassName),
            ObjectReturnTypes.QueryScalarResult => Templates.QueryScalarAssignment(methodName, propertySet: $"dr.{GetTypeRequest(columns.First().FullDataType)}(0)", typeStaticClassName),
            ObjectReturnTypes.QueryResult => Templates.QueryAssignment(entityName, methodName, propertySet: GetNewObject(columns), typeStaticClassName),
            ObjectReturnTypes.StoredProcedureNoResult => Templates.StoredProcedureNonAssignment(methodName),
            ObjectReturnTypes.StoredProcedureScalarResult => Templates.StoredProcedureScalarAssignment(methodName, propertySet: $"dr.{GetTypeRequest(columns.First().FullDataType)}(0)"),
            ObjectReturnTypes.StoredProcedureResult => Templates.StoredProcedureAssignment(entityName, methodName, propertySet: GetNewObject(columns)),
            _ => ""
        };

    private static string GetPropertyClassLines(List<Column> properties) =>
        properties
            .Select(i =>
                Templates.PropertyString(
                    i.DataType,
                    i.NullFlag,
                    i.ColumnName,
                    i.DefaultValue
                )
            )
            .ToMultiLineString();

    private static string GetNewObject(List<Column> properties) =>
        properties
            .Select(i =>
                $"\t\t\t\t{i.ColumnName} = dr.{GetTypeRequest(i.FullDataType)}({i.Index}),"
            )
            .ToMultiLineString();

    private static string GetTypeRequest(string dataType) =>
        dataType switch
        {
            "Int32" => "GetInt32",
            "Int32?" => "GetNullableInt32",
            "int" => "GetInt32",
            "int?" => "GetNullableInt32",
            "DateTime" => "GetDateTime",
            "DateTime?" => "GetNullableDateTime",
            "decimal" => "GetDecimal",
            "decimal?" => "GetNullableDecimal",
            "double" => "GetDouble",
            "double?" => "GetNullableDouble",
            "bool" => "GetBoolean",
            "bool?" => "GetNullableBoolean",
            "byte[]" => "GetByteArray",
            "byte[]?" => "GetNullableByteArray",
            "Guid" => "GetGuid",
            "Guid?" => "GetNullableGuid",
            "string?" => "GetNullableString",
            _ => "GetString",
        };
}
