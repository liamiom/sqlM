using sqlM.Extensions;
using sqlM.ResultClassTypes;
using sqlM.State;
using Spectre.Console;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.RegularExpressions;

namespace sqlM.Converters;
internal class StoredProcedureFile
{
    public static State.SqlFile Parse(string fileName, string workingDirectory, ProgressTask taskProgress)
    {
        string content = System.IO.File.ReadAllText(fileName);
        string spClean = Regex.Replace(content, @".*(CREATE|ALTER)\s+PROCEDURE\s*(\[\S+\]\.)\[?([^\]|\s]+)\]?(.*)AS\s*\n.*", "$3--- PARAMS ---\n$4", RegexOptions.Singleline | RegexOptions.IgnoreCase);
        string[] cleanLines = spClean.Split("--- PARAMS ---");

        string objectName = cleanLines.FirstOrDefault("");
        string paramText = cleanLines.LastOrDefault("");

        string typeNameOverride = content.RegexFind(@"--\W+typename\W*=\W*([^\W]+).*", @"$1");
        string entityName = string.IsNullOrWhiteSpace(typeNameOverride)
            ? objectName
            : typeNameOverride;

        List<KeyValuePair<string, Type>> parms = SqlFile.GetParams(paramText);

        string hash = SqlFile.GetFileHash(fileName);
        // Todo: this should check the hash code against the generated files and skip files that are not changed

        taskProgress.Increment(1);
        return new State.SqlFile()
        {
            FileName = fileName,
            CleanFileName = SqlFile.CleanFileName(fileName),
            EntityName = entityName,
            ObjectName = objectName,
            Content = content,
            Paramiters = parms,
            Names = new Dictionary<string, string>(),
            Hash = hash,
            Path = Path.GetRelativePath(workingDirectory, fileName),
            ScriptType = State.SqlFile.ObjectTypes.StoredProcedure,
        };
    }

    public static BaseClassFile GenerateClassFile(Container state, State.SqlFile sqlFile)
    {
        string methodName = sqlFile.CleanFileName;
        string methodParams = "";
        string sqlParams = "";

        if (sqlFile.Paramiters != null && sqlFile.Paramiters?.Count > 0)
        {
            methodParams =
                sqlFile.Paramiters
                .Select(i => $"\n\t\t\t{SqlFile.CleanTypeName(i.Value.FullName) ?? ""} {i.Key}")
                .Aggregate((a, b) => $"{a},{b}");

            sqlParams =
                sqlFile.Paramiters
                .Select(i => $"\n\t\t\t\tnew SqlParameter(\"{i.Key}\", {i.Key}),")
                .Aggregate((a, b) => $"{a}{b}");
        }

        IEnumerable<DataRow> rows = GetTableSchema(sqlFile, state.ConnectionString)?.Rows.ToRowEnumerable() ?? new List<DataRow>();
        List<Column> columns = GetProperties(rows);

        ScriptClassFile.ObjectReturnTypes objectType = columns.Count switch
        {
            0 => ScriptClassFile.ObjectReturnTypes.StoredProcedureNoResult,
            1 => ScriptClassFile.ObjectReturnTypes.StoredProcedureScalarResult,
            _ => ScriptClassFile.ObjectReturnTypes.StoredProcedureResult
        };

        return new ScriptClassFile(
            fileName: $"{sqlFile.CleanFileName}.cs",
            entityName: sqlFile.EntityName,
            methodName: methodName,
            columns: columns,
            sqlContent: sqlFile.Content,
            methodParams: methodParams,
            sqlParams: sqlParams,
            objectType: objectType,
            ScriptType: sqlFile.ScriptType
        );
    }

    private static List<Column> GetProperties(IEnumerable<DataRow> rows) =>
        rows
            .Select((row, index) => new Column
            {
                DataType = SqlFile.CleanTypeName(row["DataType"]?.ToString() ?? ""),
                NullFlag = (((bool)row["AllowDBNull"]) == true ? "?" : ""),
                ColumnName = row["ColumnName"]?.ToString().Replace(" ", "_") ?? "",
                DefaultValue = (((bool)row["AllowDBNull"]) != true && row["DataType"].ToString() == "System.String" ? " = string.Empty;" : ""),
                Index = index
            })
            .ToList();

    private static DataTable? GetTableSchema(State.SqlFile script, string conString)
    {
        string testQuery = "";
        try
        {
            SqlConnection conn = new(conString);

            conn.Open();
            SqlTransaction transaction = conn.BeginTransaction();

            // Add the stored procedure and its dependencies to the database
            string[] sqlSections = SplitOnGo(script.Content);
            foreach (string sql in sqlSections)
            {
                SqlCommand createCmd = new(sql, conn, transaction);
                createCmd.ExecuteNonQuery();
            }


            // Execute the stored procedure to get the table schema
            SqlCommand runCmd = new(script.ObjectName, conn, transaction)
            {
                CommandType = CommandType.StoredProcedure
            };

            foreach (KeyValuePair<string, Type> param in script.Paramiters)
            {
                runCmd.Parameters.AddWithValue(param.Key, GetDefaultValue(param.Value));
            }

            testQuery = script.Paramiters.Count > 0
                ? $"EXEC {script.EntityName}\n" + script.Paramiters
                        .Select(i => $"   {i.Key} = {GetDefaultValueAsString(i.Value)}")
                        .Aggregate((x, y) => x + ",\n" + y)
                : $"EXEC {script.EntityName}\n";

            SqlDataReader rdr = runCmd.ExecuteReader();
            DataTable? tableSchema = rdr.GetSchemaTable();

            rdr.Close();
            transaction.Rollback();
            conn.Close();

            return tableSchema;
        }
        catch (Exception ex)
        {
            string splitterLine = "".PadRight(70, '*');
            string errorMessage = $"{splitterLine}\n{script.Content}\n{splitterLine}\n{ex.Message}\n".AnsiSafe();
            if (!string.IsNullOrWhiteSpace(testQuery))
            {
                errorMessage += $"\nThe test query was \n{testQuery}";
            }

            throw new ProcessingException(ex.Message, ex, $"\n[red]Error running {script.CleanFileName}\n\n[/]{errorMessage}\n", script.FileName, script.CleanFileName);
        }
    }

    public static string[] SplitOnGo(string sql)
    {
        string tidyGoLines = Regex.Replace(sql, @"^(\s*GO\s*)+$", "GO", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return tidyGoLines.Split("\nGO", StringSplitOptions.RemoveEmptyEntries);
    }

    private static object GetDefaultValue(Type type)
    {
        if (type == typeof(string))
        {
            return "";
        }
        else if (type == typeof(DateTime))
        {
            return DateTime.Now;
        }
        else if (type == typeof(DateOnly))
        {
            return DateTime.Today;
        }
        else if (type == typeof(TimeOnly))
        {
            return DateTime.Now;
        }
        else if (type == typeof(MemoryStream)) 
        {
            return new MemoryStream();
        }

        return type.IsValueType
            ? Activator.CreateInstance(type) ?? DBNull.Value
            : DBNull.Value;
    }

    private static string GetDefaultValueAsString(Type type)
    {
        object value = GetDefaultValue(type);

        return value switch
        {
            DBNull => "null",
            DateTime => $"'{((DateTime)value).ToLongDateString()}'",
            DateOnly => $"'{((DateOnly)value).ToLongDateString()}'",
            TimeOnly => $"'{((TimeOnly)value).ToLongTimeString()}'",
            string => "''",
            _ => value?.ToString() ?? "''"
        };
    }
}
