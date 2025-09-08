using sqlM.ResultClassTypes;
using sqlM.State;
using Spectre.Console;
using System.Text.RegularExpressions;
using System.Data;
using Microsoft.Data.SqlClient;
using sqlM.Extensions;

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

        Flags flags = new(content);
        string entityName = flags.EntityName;
        string tableName = Regex.Replace(content, @".*(CREATE|ALTER)\s+TABLE\s(\[\S+\]\.)\[?([^\]|\s]+)\]?.*", "$3", RegexOptions.Singleline | RegexOptions.IgnoreCase);


        // Check that the entity name looks reasonable
        if (tableName.Length > 100 || tableName.Contains(' ') || tableName.Contains(')'))
        {
            tableName = "";
        }

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
            OverrideFlags = flags,
            TableName = tableName,
        };
    }

    public static BaseClassFile GenerateClassFile(Container state, State.SqlFile sqlFile)
    {
        string methodParams = "";
        string sqlParams = "";

        IEnumerable<DataRow> rows = GetRows(sqlFile, state.ConnectionString);
        string[] keys = GetPrimaryKeys(sqlFile.Content);
        List<Column> columns = GetProperties(rows, keys);
        sqlFile.Paramiters = columns
            .Where(i => i.IsKey)
            .Select(i => new KeyValuePair<string, Type>(i.ColumnName, SqlFile.GetTypeFromTidySqlName(i.FullDataType)))
            .ToList();


        ResultTypeAbstract tableAbstract = new()
        {
            Name = sqlFile.EntityName,
            Columns = columns,
        };
        state.EntityTypeCache.Add(tableAbstract);

        if (sqlFile.Paramiters != null && sqlFile.Paramiters?.Count > 0)
        {
            methodParams =
                sqlFile.Paramiters
                .Select(i => $"\n        {SqlFile.CleanTypeName(i.Value.FullName) ?? ""} {i.Key}")
                .Aggregate((a, b) => $"{a},{b}");

            sqlParams =
                sqlFile.Paramiters
                .Select(i => $"\n                ToSqlParameter(\"{i.Key}\", {i.Key}),")
                .Aggregate((a, b) => $"{a}{b}");
        }

        string updateParams = "";
        if (columns.Count > 0)
        {
            updateParams = columns
                .Select(i => new KeyValuePair<string, Type>(i.ColumnName, SqlFile.GetTypeFromTidySqlName(i.FullDataType)))
                .Select(i => $"\n                ToSqlParameter(\"{i.Key}\", item.{i.Key}),")
                .Aggregate((a, b) => $"{a}{b}");
        }


        return new ScriptClassFile(
            fileName: $"{sqlFile.CleanFileName}.cs",
            entityName: sqlFile.EntityName,
            methodName: sqlFile.CleanFileName,
            columns: columns,
            sqlContent: sqlFile.Content,
            methodParams: methodParams,
            sqlParams: sqlParams,
            objectType: ScriptClassFile.ObjectReturnTypes.Table,
            ScriptType: sqlFile.ScriptType,
            updateParams: updateParams,
            crudMethods: sqlFile.OverrideFlags.GenerateCrudMethods
        );
    }

    private static string[] GetPrimaryKeys(string content) =>
        !content.Contains("PRIMARY KEY")
            ? []
            : content
                .RegexReplace(".*PRIMARY KEY[^\\(]*\\(([^\\)]*).*", "$1", RegexOptions.Singleline) // Trim down to the keys section 
                .RegexReplace(".*\\[(.+)\\].*", "$1 ") // Split out the column names 
                .Split(' ')
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .Select(i => i.Trim())
                .ToArray();

    private static IEnumerable<DataRow> GetRows(State.SqlFile sqlFile, string conString) =>
        string.IsNullOrWhiteSpace(sqlFile.TableName)
            ? new List<DataRow>()
            : GetTableSchema(sqlFile, conString)?.Rows.ToRowEnumerable() ?? new List<DataRow>();

    private static List<Column> GetProperties(IEnumerable<DataRow> rows, string[] keys) =>
        rows
            .Select((row, index) => new Column
            {
                DataType = SqlFile.CleanTypeName(row["DataType"]?.ToString() ?? "", row["DataTypeName"].ToString() ?? ""),
                NullFlag = ((bool)row["AllowDBNull"]) == true ? "?" : "",
                ColumnName = row["ColumnName"]?.ToString()?.Replace(" ", "_") ?? "",
                DefaultValue = ((bool)row["AllowDBNull"]) != true && row["DataType"].ToString() == "System.String" ? " = System.String.Empty;" : "",
                IsKey = keys.Contains(row["ColumnName"]?.ToString()?.Replace(" ", "_")),
                IsIdentity = (bool)row["IsIdentity"],
                Index = index
            })
            .ToList();

    private static DataTable? GetTableSchema(State.SqlFile script, string conString)
    {
        try
        {
            string sqlString = $"SELECT * FROM {script.TableName}";
            SqlConnection conn = new(conString);

            conn.Open();
            SqlTransaction transaction = conn.BeginTransaction();
            SqlCommand cmd = new(sqlString, conn, transaction);

            SqlDataReader rdr = cmd.ExecuteReader();
            DataTable? tableSchema = rdr.GetSchemaTable();

            rdr.Close();
            transaction.Rollback();
            conn.Close();

            return tableSchema;
        }
        catch (Exception ex)
        {
            string splitterLine = "".PadRight(70, '*');
            string errorMessage = $"\n\n{splitterLine}\n{script.Content}\n{splitterLine}\n{ex.Message}\n".Replace("[", "[[").Replace("]", "]]");

            throw new ProcessingException(ex.Message, ex, $"\n[red]Error running {script.CleanFileName}\n\n[/]{errorMessage}\n", script.FileName, script.CleanFileName);
        }
    }

    private static object GetDefaultValue(Type type) =>
        type.IsValueType
            ? Activator.CreateInstance(type) ?? DBNull.Value
            : DBNull.Value;
}
