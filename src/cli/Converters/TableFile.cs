using sqlM.ResultClassTypes;
using sqlM.State;
using Spectre.Console;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data;

namespace sqlM.Converters;
internal class TableFile
{
    public static State.SqlFile Parse(string fileName, string workingDirectory, ProgressTask taskProgress)
    {
        string content = System.IO.File.ReadAllText(fileName);
        string paramText = Regex.Replace(content, @".*(CREATE|ALTER)\s+TABLE\s+\[\S+\](.*)AS.*", "$2", RegexOptions.Singleline | RegexOptions.IgnoreCase);


        List<KeyValuePair<string, Type>> parms = SqlFile.GetParams(paramText);

        string hash = SqlFile.GetFileHash(fileName);
        // Todo: this should check the hash code against the generated files and skip files that are not changed


        string entityName = Regex.Replace(content, @".*(CREATE|ALTER)\s+TABLE\s(\[\S+\]\.)\[?([^\]|\s]+)\]?.*", "$3", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        taskProgress.Increment(1);
        return new State.SqlFile()
        {
            FileName = fileName,
            CleanFileName = SqlFile.CleanFileName(fileName),
            EntityName = entityName,
            Content = content,
            Paramiters = parms,
            Names = new Dictionary<string, string>(),
            Hash = hash,
            Path = Path.GetRelativePath(workingDirectory, fileName),
            ScriptType = State.SqlFile.ObjectTypes.Table,
        };
    }

    public static BaseClassFile GenerateClassFile(Container state, State.SqlFile sqlFile)
    {
        IEnumerable<DataRow> rows = GetRows(sqlFile, state.ConnectionString);
        List<Column> columns = GetProperties(rows);

        return new ScriptClassFile(
            fileName: $"{sqlFile.CleanFileName}.cs",
            entityName: sqlFile.EntityName,
            methodName: sqlFile.CleanFileName,
            columns: columns,
            sqlContent: sqlFile.Content,
            methodParams: "",
            sqlParams: "",
            objectType: ScriptClassFile.ObjectReturnTypes.Table,
            ScriptType: sqlFile.ScriptType
        );
    }

    private static IEnumerable<DataRow> GetRows(State.SqlFile sqlFile, string conString) =>
        GetTableSchema(sqlFile, conString)?.Rows.ToRowEnumerable() ?? new List<DataRow>();

    private static List<Column> GetProperties(IEnumerable<DataRow> rows) =>
        rows
            .Select((row, index) => new Column
            {
                DataType = SqlFile.CleanTypeName(row["DataType"]?.ToString() ?? ""),
                NullFlag = (((bool)row["AllowDBNull"]) == true ? "?" : ""),
                ColumnName = row["ColumnName"]?.ToString().Replace(" ", "_") ?? "",
                DefaultValue = (((bool)row["AllowDBNull"]) != true && row["DataType"].ToString() == "System.String" ? " = System.String.Empty;" : ""),
                IsIdentity = ((bool)row["IsIdentity"]) == true,
                Index = index
            })
            .ToList();

    private static DataTable? GetTableSchema(State.SqlFile script, string conString)
    {
        try
        {
            SqlConnection conn = new(conString);

            conn.Open();
            SqlTransaction transaction = conn.BeginTransaction();
            string[] sections = SplitOnGo(script.Content);
            foreach (string section in sections)
            {
                SqlCommand createCmd = new(section, conn, transaction);
                createCmd.ExecuteNonQuery();
            }

            SqlCommand selectCmd = new($"SELECT TOP 1 * FROM {script.EntityName}", conn, transaction);

            SqlDataReader rdr = selectCmd.ExecuteReader();
            DataTable? tableSchema = rdr.GetSchemaTable();

            rdr.Close();
            transaction.Rollback();
            conn.Close();

            return tableSchema;
        }
        catch (Exception ex)
        {
            string splitterLine = "".PadRight(70, '*');
            string errorMessage = $"\n[red]Error running {script.CleanFileName}[/]\n\n{splitterLine}\n{script.Content}\n{splitterLine}\n{ex.Message}\n".Replace("[", "[[").Replace("]", "]]");

            throw new ProcessingException(ex.Message, ex, $"\n[red]Error running {script.CleanFileName}\n\n[/]{errorMessage}\n", script.FileName, script.CleanFileName);
        }
    }

    public static string[] SplitOnGo(string sql)
    {
        string tidyGoLines = Regex.Replace(sql, @"^(\s*GO\s*)+$", "GO", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        return tidyGoLines.Split("\nGO", StringSplitOptions.RemoveEmptyEntries);
    }
}
