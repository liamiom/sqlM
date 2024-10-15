using System.Data;
using System.Data.SqlClient;
using System.Text;

namespace sqlM;

public class ResultClasses
{
    public static string GetClassString(List<SQLScript> Scripts)
    {
        string conString = @"Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=sqlMTest;Data Source=localhost";
        Scripts = GetTableSchemas(Scripts, conString);

        StringBuilder sourceBuilder = new(@"
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using sqlM.Entities;

namespace sqlM
{
    public static class Config
    {
        public static string ConnectionString { get; set; }
    }

    internal static class Utils
    {
        public static SqlDataReader OpenReader(SqlParameter[] parameters, string script)
        {
            SqlConnection conn = new SqlConnection(Config.ConnectionString);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = script;
            cmd.Connection = conn;
            if (parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            } 

            conn.Open();
            return cmd.ExecuteReader();
        }

        public static int ExecuteNonQuery(SqlParameter[] parameters, string script)
        {
            SqlConnection conn = new SqlConnection(Config.ConnectionString);
            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = script;
            cmd.Connection = conn;
            if (parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

            conn.Open();
            return cmd.ExecuteNonQuery();
        }
    }
}

");
        foreach (SQLScript item in Scripts)
        {
            sourceBuilder.Append(GetModelClass(item));
            sourceBuilder.Append(GetEntityClass(item));
        }

        return sourceBuilder.ToString();
    }

    private static List<SQLScript> GetTableSchemas(List<SQLScript> Scripts, string conString)
    {
        foreach (SQLScript script in Scripts)
        {
            script.TableSchema = GetTableSchema(script, conString);
        }

        return Scripts;
    }

    private static DataTable? GetTableSchema(SQLScript script, string conString)
    {
        SqlConnection conn = new(conString);

        conn.Open();
        SqlTransaction transaction = conn.BeginTransaction();
        SqlCommand cmd = new(script.Content, conn, transaction);

        foreach (KeyValuePair<string, Type> param in script.Paramiters)
        {
            cmd.Parameters.AddWithValue(param.Key, GetDefaultValue(param.Value));
        }

        SqlDataReader rdr = cmd.ExecuteReader();
        DataTable? tableSchema = rdr.GetSchemaTable();

        rdr.Close();
        transaction.Rollback();
        conn.Close();

        return tableSchema;
    }

    private static string GetModelClass(SQLScript Script)
    {
        string methodParams =
            Script.Paramiters
                .Select(i => $"{i.Value.Name } {i.Key}")
                .ToDelimitedString();

        string methodParamsNoType =
            Script.Paramiters
                .Select(i => i.Key)
                .ToDelimitedString();

        string sqlParams =
            Script.Paramiters
                .Select(i => $"\t\t\t\tnew SqlParameter(\"@{i.Key}\", {i.Key}),")
                .ToMultiLineString();

        string fileContent =
            $"{Environment.NewLine}-- From {Script.FileName}{Environment.NewLine}{Script.Content}"
                .Replace("\"", "\"\"");

        string methodSet = Script.IsQuery
            ? GetQueryMethods(Script, methodParams, methodParamsNoType, sqlParams)
            : GetNonQueryMethods(methodParams, sqlParams);

        string classString = $@"
namespace sqlM.Models
{{
    public static class {Script.CleanFileName}
    {{
        public const string Script = @""{fileContent}"";
{methodSet}

    }}
}}

";
        return classString;
    }

    private static string GetQueryMethods(SQLScript Script, string methodParams, string methodParamsNoType, string sqlParams) => 
        $@"
        public static IEnumerable<{Script.ReturnClassName}> GetList({methodParams})
        {{
            SqlParameter[] parameters = new SqlParameter[]
            {{
{sqlParams}
            }};
            SqlDataReader rdr = Utils.OpenReader(parameters, Script);

            while (rdr.Read())
            {{
                yield return {Script.ReturnClassName}.FromSQLReader(rdr);
            }}
        }}

        public static {Script.ReturnClassName}? GetOne({methodParams})
        {{
            return GetList({methodParamsNoType}).FirstOrDefault();
        }}
";

    private static string GetNonQueryMethods(string methodParams, string sqlParams)
    {
        return $@"
        public static int ExecuteNonQuery({methodParams})
        {{
            SqlParameter[] parameters = new SqlParameter[]
            {{
{sqlParams}
            }};

            return Utils.ExecuteNonQuery(parameters, Script);
        }}
";
    }

    private static string GetEntityClass(SQLScript Script)
    {
        if (!Script.IsQuery)
        {
            return "";
        }

        string className = Script.ReturnClassName;

        IEnumerable<DataRow> rows = Script.TableSchema != null 
            ? Script.TableSchema.Rows.ToRowEnumerable()
            : new List<DataRow>();

        string classString = $@"
namespace sqlM.Entities
{{
    public class {className} 
    {{
{ GetProperties(rows) }
{ GetCastingMethod(rows, className) }
    }}
}}

";

        return classString;
    }

    private static string GetProperties(IEnumerable<DataRow> rows) => 
        rows
            .Select(row =>
                GetPropertyString(
                    dataType: row["DataType"].ToString(),
                    nullFlag: (((bool)row["AllowDBNull"]) == true && row["DataType"].ToString() != "System.String" ? "?" : ""),
                    columnName: row["ColumnName"].ToString()
                )
            )
            .ToMultiLineString();

    private static string GetPropertyString(string dataType, string nullFlag, string columnName) => 
        $"\t\tpublic {dataType}{nullFlag} {columnName} {{ get; set; }}";

    private static string GetCastingMethod(IEnumerable<DataRow> rows, string className) => 
        $@"
        public static {className} FromSQLReader(SqlDataReader rdr) =>
            new {className} {{
{GetCastingLines(rows)}
            }};
        ";

    private static string GetCastingLines(IEnumerable<DataRow> rows) =>
        rows
        .WithIndex()
        .Select(row => 
            GetCastingLine(
                columnName: row.Value["ColumnName"].ToString(),
                nullableCheck: ((bool)row.Value["AllowDBNull"]) == true ? $"rdr.IsDBNull({row.Key}) ? null : " : "",
                dataType: row.Value["DataType"].ToString(),
                index: row.Key
                )
        )
        .ToMultiLineString();

    private static string GetCastingLine(string columnName, string nullableCheck, string dataType, int index) =>
        $"\t\t\t\t{columnName} = {nullableCheck}rdr.GetFieldValue<{dataType}>({index}),";

    private static object? GetDefaultValue(Type type) => 
        type.IsValueType == true
            ? Activator.CreateInstance(type) 
            : DBNull.Value;


}
