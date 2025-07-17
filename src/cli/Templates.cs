using sqlM.ResultClassTypes;
using static sqlM.ResultClassTypes.ScriptClassFile;

namespace sqlM;

internal static class Templates
{

    public static string ToString(TemplateModel model) =>
        $@"
// ##########################################################################################
// #                                                                                        #
// #   This file has been generated with the sqlM Cli. Be careful making any                #
// #   changes as changes made to this file will be overridden next time the scaffold       #
// #   code is rebuilt by the Cli.                                                          #
// #                                                                                        #
// #   If you want to extend this class it is recommended to add your code to a partial     #
// #   class in a different folder.                                                         #
// #                                                                                        #
// ##########################################################################################

using System;
using {(DotNet.IsDotnetCoreProject() ? "Microsoft.Data.SqlClient" : "System.Data.SqlClient")};
using System.Collections.Generic;

namespace sqlM
{{
    {StaticClass(model)}{MethodClass(model)}{CrudClass(model)}{EntityTypeClass(model)}
}}"
            .Replace("\r\n", "\n");

    private static string StaticClass(TemplateModel model) =>
        $@"
    public static partial class {model.TypeStaticClassName}
    {{
        public const string {model.MethodName} = @""
{model.StaticSqlContent}"";
    }}
    ";

    private static string MethodClass(TemplateModel model) =>
        !model.IsMethodType
            ? ""
            : $@"
    public partial class Database
    {{
        public {model.ReturnType} {model.MethodName}({model.MethodParams})
        {{
            {Parameters(model.SqlParams)}
            {GetAssignment(model)};
        }}
    }}
";

    private static string CrudClass(TemplateModel model) =>
        !model.IsTableType
        ? ""
        : $@"
    public partial class Database
    {{
        public List<{model.EntityName}> {model.MethodName}_Get({model.GetParams})
        {{
            string script = @""SELECT * FROM {model.MethodName} {GetCrudWhereFilter(model.Columns)}"";

            {Parameters(model.SqlParams)}
            SqlDataReader dr = Generic_OpenReader(parameters, script);
            List<{model.EntityName}> output = new List<{model.EntityName}>();
		    while (dr.Read())
		    {{
			    output.Add(new {model.EntityName}
			    {{
{ToPropertySet(model.Columns)}
			    }});
		    }}

		    return output;
        }}

        public int {model.MethodName}_Set({model.MethodName} item) =>
            {GetCrudUpdateCheck(model.Columns)}
                ? {model.MethodName}_Update(item)
                : {model.MethodName}_Add(item);


        public int {model.MethodName}_Update({model.MethodName} item)
        {{
            {Parameters(model.UpdateParams)}
            string script = @""
                UPDATE {model.MethodName} 
                SET
                     {model.UpdateSet}
                {GetCrudWhereFilter(model.Columns)}

                SELECT CAST(SCOPE_IDENTITY() AS int) "";

            object result = Generic_OpenSingle(parameters, script);
            return result is DBNull
                ? 0 
                : (int)result;
        }}

        public int {model.MethodName}_Add({model.MethodName} item)
        {{
            {Parameters(model.UpdateParams)}
            string script = @""
                INSERT INTO {model.MethodName} (
                    {model.InsertColumns}
                )
                VALUES (
                    {model.InsertParams}
                )

                SELECT CAST(SCOPE_IDENTITY() AS int) 
                "";

            object result = Generic_OpenSingle(parameters, script);
            return result is DBNull
                ? 0 
                : (int)result;
        }}

        public bool {model.MethodName}_Del({model.GetParams})
        {{
            string script = @""DELETE {model.MethodName} {GetCrudWhereFilter(model.Columns)}"";
            {Parameters(model.SqlParams)}
            Generic_ExecuteNonQuery(parameters, script);
            return true;
        }}
    }}
";

    private static string GetCrudUpdateCheck(List<Column> columns) =>
        columns.Count == 0
            ? "false"
            : columns
                .Where(i => i.IsIdentity && i.DataType == "int")
                .Select(i => $"item.{i.ColumnName} > 0")
                .Join(" && ");

    private static string GetCrudWhereFilter(List<Column> columns) =>
        columns.Count == 0
            ? ""
            : "WHERE " + columns
                .Where(i => i.IsIdentity)
                .Select(GetCrudGetFilterItem)
                .Join(" AND ");

    private static string GetCrudGetFilterItem(Column column) =>
        $"{column.ColumnName} = ISNULL(@{column.ColumnName}, {column.ColumnName})";

    private static string EntityTypeClass(TemplateModel model) =>
        model.Columns.Count == 0 || model.IsScalar
            ? ""
            : $@"
    public partial class {model.EntityName} 
    {{
{GetPropertyClassLines(model.Columns)}
    }}
";

    private static string Parameters(string sqlParams) =>
        string.IsNullOrWhiteSpace(sqlParams)
            ? "SqlParameter[] parameters = new SqlParameter[0];"
            : $@"SqlParameter[] parameters = new SqlParameter[]
            {{{sqlParams}
            }};
";

    private static string GetPropertyClassLines(List<Column> properties) =>
        properties
            .Select(i =>
                PropertyString(
                    DotNet.IsDotnetCoreProject() ? "required " : "",
                    i.DataType,
                    FilterOutUnstupportedNullableTypes(i.DataType, i.NullFlag),
                    i.ColumnName,
                    i.DefaultValue
                )
            )
            .ToMultiLineString();

    private static string FilterOutUnstupportedNullableTypes(string dataType, string nullFlag) =>
        (!DotNet.IsDotnetCoreProject() && dataType == "string") ||
        dataType == "byte[]"
            ? ""
            : nullFlag;

    private static string PropertyString(string requiredFlag, string dataType, string nullFlag, string columnName, string defaultValue) =>
        $"\t\tpublic {requiredFlag}{dataType}{nullFlag} {columnName} {{ get; set; }}{defaultValue}";

    private static string GetAssignment(TemplateModel model) =>
        model.ObjectType switch
        {
            ObjectReturnTypes.QueryNoResult => QueryNonAssignment(model),
            ObjectReturnTypes.QueryScalarResult => QueryScalarAssignment(model),
            ObjectReturnTypes.QueryResult => QueryAssignment(model),
            ObjectReturnTypes.StoredProcedureNoResult => StoredProcedureNonAssignment(model),
            ObjectReturnTypes.StoredProcedureScalarResult => StoredProcedureScalarAssignment(model),
            ObjectReturnTypes.StoredProcedureResult => StoredProcedureAssignment(model),
            _ => ""
        };

    private static string QueryNonAssignment(TemplateModel model) =>
        @$"return Generic_ExecuteNonQuery(parameters, {model.TypeStaticClassName}.{model.MethodName}) != 0";

    private static string QueryScalarAssignment(TemplateModel model) =>
        @$"SqlDataReader dr = Generic_OpenReader(parameters, {model.TypeStaticClassName}.{model.MethodName});
		    dr.Read();
		    return dr.{GetTypeRequest(model.Columns.First().FullDataType)}(0);
		";

    private static string QueryAssignment(TemplateModel model) =>
        @$"SqlDataReader dr = Generic_OpenReader(parameters, {model.TypeStaticClassName}.{model.MethodName});
		    List<{model.EntityName}> output = new List<{model.EntityName}>();
		    while (dr.Read())
		    {{
			    output.Add(new {model.EntityName}
			    {{
{ToPropertySet(model.Columns)}
			    }});
		    }}

		return output";

    private static string StoredProcedureNonAssignment(TemplateModel model) =>
        @$"return Generic_StoredProcedureNonQuery(parameters, ""{model.MethodName}"") != 0";

    private static string StoredProcedureScalarAssignment(TemplateModel model) =>
        @$"SqlDataReader dr = Generic_StoredProcedureReader(parameters, ""{model.MethodName}"");
		    dr.Read();
		    return dr.{GetTypeRequest(model.Columns.First().FullDataType)}(0);
		";

    private static string StoredProcedureAssignment(TemplateModel model) =>
        @$"SqlDataReader dr = Generic_StoredProcedureReader(parameters, ""{model.MethodName}"");
		    List<{model.EntityName}> output = new List<{model.EntityName}>();
		    while (dr.Read())
		    {{
			    output.Add(new {model.EntityName}
			    {{
{ToPropertySet(model.Columns)}
			    }});
		    }}

		    return output";

    private static string ToPropertySet(List<Column> columns) =>
        columns
            .Select(i =>
                $"\t\t\t\t\t{i.ColumnName} = dr.{GetTypeRequest(i.FullDataType)}({i.Index}),"
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
