namespace sqlM.ResultClassTypes;

internal class ScriptClassFile : BaseClassFile
{
    public enum ObjectReturnTypes { QueryResult, QueryScalarResult, QueryNoResult, StoredProcedureResult, StoredProcedureScalarResult, StoredProcedureNoResult, Table, View, Function }

    public ScriptClassFile(
        string fileName,
        string entityName,
        string methodName,
        List<Column> columns,
        string sqlContent,
        string methodParams,
        string sqlParams,
        ObjectReturnTypes objectType,
        State.SqlFile.ObjectTypes ScriptType,
        string updateParams = "")
    {
        columns = DeduplicateColumnNames(columns);

        FileName = fileName;
        EntityName = entityName;
        Content = GetContent(
            entityName,
            methodName,
            columns,
            sqlContent,
            methodParams,
            sqlParams,
            objectType,
            ScriptType,
            updateParams
            );
        MethodSigniture = GetMethodSigniture(
            ScriptType,
            entityName,
            methodName,
            columns,
            methodParams
            );
    }

    private static string GetContent(
        string entityName,
        string methodName,
        List<Column> columns,
        string sqlContent,
        string methodParams,
        string sqlParams,
        ObjectReturnTypes objectType,
        State.SqlFile.ObjectTypes ScriptType,
        string updateParams)
    {
        bool isMethodType =
            ScriptType == State.SqlFile.ObjectTypes.Query ||
            ScriptType == State.SqlFile.ObjectTypes.StoredProcedure;

        bool isTableType =
            ScriptType == State.SqlFile.ObjectTypes.Table;

        bool isQueryType =
            objectType == ObjectReturnTypes.QueryNoResult ||
            objectType == ObjectReturnTypes.QueryScalarResult ||
            objectType == ObjectReturnTypes.QueryResult;

        string typeStaticClassName = isQueryType
            ? "SqlQueryStrings"
            : "DatabaseUpdateStrings";

        bool isQuery = columns?.Count > 0;
        bool isScalar = columns?.Count == 1;
        string scalarTypeName = isScalar && columns != null
            ? columns.First().FullDataType
            : "";

        string returnType = GetReturnType(isQuery, isScalar, entityName, scalarTypeName);

        string getParams = "";
        string updateSet = "";
        string insertColumns = "";
        string insertParams = "";

        if (ScriptType == State.SqlFile.ObjectTypes.Table)
        {
            getParams = columns
            .Where(i => i.IsIdentity)
            .Select(i => $"{i.FullDataType}? {i.ColumnName} = null")
            .Join(", ");

            updateSet = columns
                .Where(i => !i.IsIdentity)
                .Select(i => $"{i.ColumnName} = @{i.ColumnName}")
                .Join(Environment.NewLine + "                    ,");

            insertColumns = columns
                .Where(i => !i.IsIdentity)
                .Select(i => i.ColumnName)
                .Join(Environment.NewLine + "                    ,");
            insertParams = columns
                .Where(i => !i.IsIdentity)
                .Select(i => "@" + i.ColumnName)
                .Join(Environment.NewLine + "                    ,");
        }

        TemplateModel model = new()
        {
            ObjectType = objectType,
            ScriptType = ScriptType,
            TypeStaticClassName = typeStaticClassName,
            MethodName = methodName,
            EntityName = entityName,
            StaticSqlContent = sqlContent.Trim().Replace("\"", "\"\""),
            IsQuery = isQuery,
            IsScalar = isScalar,
            IsMethodType = isMethodType,
            IsTableType = isTableType,
            ScalarTypeName = scalarTypeName,
            ReturnType = returnType,
            MethodParams = methodParams,
            SqlParams = sqlParams,
            GetParams = getParams,
            UpdateSet = updateSet,
            InsertColumns = insertColumns,
            InsertParams = insertParams,
            QueryParams = sqlParams,
            UpdateParams = updateParams,
            Columns = columns,
        };

        return Templates.ToString(model);
    }

    private static string GetReturnType(bool isQuery, bool isScalar, string entityName, string scalarTypeName)
    {
        if (!isQuery)
        {
            return "bool";
        }

        if (isScalar && scalarTypeName == "string?")
        {
            return "string";
        }

        if (isScalar && scalarTypeName == "byte[]?")
        {
            return "byte[]";
        }

        if (isScalar)
        {
            return scalarTypeName;
        }

        return $"List<{entityName}>";
    }

    private static string GetMethodSigniture(
        State.SqlFile.ObjectTypes ScriptType,
        string entityName,
        string methodName,
        List<Column> columns,
        string methodParams)
    {
        bool isQuery = columns?.Count > 0;
        bool isScalar = columns?.Count == 1;
        string scalarTypeName = isScalar && columns != null
            ? columns.First().FullDataType
            : "";

        string getParams = columns
            .Where(i => i.IsIdentity)
            .Select(i => $"{i.FullDataType}? {i.ColumnName} = null")
            .Join(", ");

        string returnType = GetReturnType(isQuery, isScalar, entityName, scalarTypeName);

        return ScriptType == State.SqlFile.ObjectTypes.Table
            ? GetCRUDMethodSigniture(returnType, methodName, getParams)
            : GetSingleMethodSigniture(returnType, methodName, methodParams);
    }

    private static string GetSingleMethodSigniture(string returnType, string methodName, string methodParams) =>
        @$"        public {returnType} {methodName}({methodParams});";

    private static string GetCRUDMethodSigniture(string returnType, string methodName, string getParams) =>
        $"        public {returnType} {methodName}_Get({getParams});\n" +
        $"        public int {methodName}_Set({methodName} item);\n" +
        $"        public int {methodName}_Update({methodName} item);\n" +
        $"        public int {methodName}_Add({methodName} item);";

    private static List<Column> DeduplicateColumnNames(List<Column> columns) => 
        columns
            .Select(i => CheckColumnNameForduplicates(i, columns))
            .ToList();

    private static Column CheckColumnNameForduplicates(Column column, List<Column> columns)
    {
        int matchedNameCount = columns.Count(i => i.ColumnName == column.ColumnName && i.Index < column.Index);
        if (matchedNameCount > 0)
        {
            column.ColumnName = $"{column.ColumnName}_{matchedNameCount}";
        }

        return column;
    }
}
