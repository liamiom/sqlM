namespace sqlM;

internal static class Templates
{
    public static string JoinClasses(string staticClass, string methodClass, string entityClass) =>
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
using System.Data.SqlClient;
using System.Collections.Generic;

namespace sqlM
{{
    {staticClass}{methodClass}{entityClass}
}}"
            .Replace("\r\n", "\n");

        public static string StaticClass(string methodName, string content, string scriptTypeClassName) =>
            $@"
    public static partial class {scriptTypeClassName}
    {{
        public const string {methodName} = @""
{content.Replace("\"", "\"\"")}"";
    }}
    ";

        public static string MethodClass(string methodName, string methodParams, string sqlParams, string returnType, string queryAssignment) =>
            $@"
    public partial class Database
    {{
        public {returnType} {methodName}({methodParams})
        {{
            {sqlParams}
            {queryAssignment};
        }}
    }}
";

    public static string ReturnType(bool isQuery, bool isScalar, string entityName, string scalarTypeName)
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

    public static string EntityTypeClass(string entityName, string properties) =>
        string.IsNullOrWhiteSpace(properties)
            ? string.Empty
        : $@"
    public class {entityName} 
    {{
{properties}
    }}
";

    public static string Parameters(string sqlParams) =>
        string.IsNullOrWhiteSpace(sqlParams)
            ? "SqlParameter[] parameters = new SqlParameter[0];"
            : $@"SqlParameter[] parameters = new SqlParameter[]
            {{{sqlParams}
            }};
";

    public static string PropertyString(string dataType, string nullFlag, string columnName, string defaultValue) =>
        $"\t\tpublic {dataType}{nullFlag} {columnName} {{ get; set; }}{defaultValue}";


    public static string PropertySet(string dataType, string nullFlag, string columnName) =>
        $"{columnName} = ";

    public static string QueryAssignment(string entityName, string methodName, string propertySet, string scriptTypeClassName) =>
        @$"SqlDataReader dr = Generic_OpenReader(parameters, {scriptTypeClassName}.{methodName});
		    List<{entityName}> output = new List<{entityName}>();
		    while (dr.Read())
		    {{
			    output.Add(new {entityName}
			    {{
{propertySet}
			    }});
		    }}

		return output";

    public static string QueryScalarAssignment(string methodName, string propertySet, string scriptTypeClassName) =>
        @$"SqlDataReader dr = Generic_OpenReader(parameters, {scriptTypeClassName}.{methodName});
		    dr.Read();
		    return {propertySet};
		";

    public static string QueryNonAssignment(string methodName, string scriptTypeClassName) =>
        @$"return Generic_ExecuteNonQuery(parameters, {scriptTypeClassName}.{methodName}) != 0";

    public static string StoredProcedureAssignment(string entityName, string methodName, string propertySet) =>
        @$"SqlDataReader dr = Generic_StoredProcedureReader(parameters, ""{entityName}"");
		    List<{entityName}> output = new List<{entityName}>();
		    while (dr.Read())
		    {{
			    output.Add(new {entityName}
			    {{
{propertySet}
			    }});
		    }}

		    return output";

    public static string StoredProcedureScalarAssignment(string methodName, string propertySet) =>
        @$"SqlDataReader dr = Generic_StoredProcedureReader(parameters, ""{methodName}"");
		    dr.Read();
		    return {propertySet};
		";

    public static string StoredProcedureNonAssignment(string methodName) =>
        @$"return Generic_StoredProcedureNonQuery(parameters, ""{methodName}"") != 0";
}
