using Spectre.Console;
using sqlM.ResultClassTypes;
using sqlM.State;
using System.Data;
using Microsoft.Data.SqlClient;

namespace sqlM.Converters;
internal class QueryFile
{
    public static State.SqlFile Parse(string fileName, string workingDirectory, ProgressTask taskProgress)
    {
        string[] content = System.IO.File.ReadAllLines(fileName);

        Dictionary<string, List<string>> sections = SplitLines(content);

        string paramText = sections.ContainsKey("PARAMS")
            ? string.Join('\n', sections["PARAMS"])
            : "";

        string mainText = sections.ContainsKey("MAIN")
            ? string.Join('\n', sections["MAIN"])
            : "";

        List<string> namesLines = sections.ContainsKey("NAMES")
            ? sections["NAMES"]
            : new List<string>();

        List<KeyValuePair<string, Type>> parms = SqlFile.GetParams(paramText);
        Dictionary<string, string> names = GetNames(namesLines);

        string hash = SqlFile.GetFileHash(fileName);
        // Todo: this should check the hash code against the generated files and skip files that are not changed


        string entityName = names.ContainsKey("typename")
            ? names["typename"]
            : SqlFile.CleanFileName(fileName) + "_Result";

        taskProgress.Increment(1);
        return new State.SqlFile()
        {
            FileName = fileName,
            CleanFileName = SqlFile.CleanFileName(fileName),
            EntityName = entityName,
            Content = mainText,
            Paramiters = parms,
            Names = names,
            Hash = hash,
            Path = Path.GetRelativePath(workingDirectory, fileName),
            ScriptType = State.SqlFile.ObjectTypes.Query,
        };
    }

    private static Dictionary<string, List<string>> SplitLines(string[] lines)
    {
        List<KeyValuePair<string, List<string>>> pairList = new();
        KeyValuePair<string, List<string>> section = new();

        foreach (string line in lines)
        {
            if (line.StartsWith("---") && line.EndsWith("---"))
            {
                section = new KeyValuePair<string, List<string>>(line.Replace("---", "").Trim(), new List<string>());
                pairList.Add(section);
            }
            else if (section.Key != null)
            {
                section.Value.Add(line);
            }
        }

        Dictionary<string, List<string>> output = pairList.ToDictionary(x => x.Key, x => x.Value);
        if (!output.ContainsKey("MAIN"))
        {
            output.Add("MAIN", lines.ToList());
        }

        return output;
    }

    private static Dictionary<string, string> GetNames(List<string> lines) =>
        lines
            .Select(i => i.Replace("--", "").Split('='))
            .Where(i => i.Length == 2)
            .ToDictionary(x => x[0].Trim().ToLower(), x => x[1].Trim());

    public static BaseClassFile GenerateClassFile(Container state, State.SqlFile sqlFile)
    {
        string methodName = sqlFile.CleanFileName;
        string methodParams = "";
        string sqlParams = "";

        if (sqlFile.Paramiters != null && sqlFile.Paramiters?.Count > 0)
        {
            methodParams =
                sqlFile.Paramiters
                .Select(i => $"\n        {SqlFile.CleanTypeName(i.Value.FullName) ?? ""}? {i.Key}")
                .Aggregate((a, b) => $"{a},{b}");

            sqlParams =
                sqlFile.Paramiters
                .Select(i => $"\n            ToSqlParameter(\"{i.Key}\", {i.Key}),")
                .Aggregate((a, b) => $"{a}{b}");
        }

        IEnumerable<DataRow> rows = GetRows(sqlFile, state.ConnectionString);
        List<Column> columns = GetProperties(rows);

        ScriptClassFile.ObjectReturnTypes objectType = columns.Count switch
        { 
            0 => ScriptClassFile.ObjectReturnTypes.QueryNoResult,
            1 => ScriptClassFile.ObjectReturnTypes.QueryScalarResult,
            _ => ScriptClassFile.ObjectReturnTypes.QueryResult
        };

        string errorMessage = "";
        bool externalType = state.EntityTypeCache.Any(i => i.Name == sqlFile.EntityName);
        if (externalType)
        {
            ResultTypeAbstract? itemType = state.EntityTypeCache.Where(i => i.Name == sqlFile.EntityName).FirstOrDefault();
            if (!itemType.ColumnsMatch(columns, out string error))
            {
                errorMessage = error;
            }
        }
        else
        {
            ResultTypeAbstract tableAbstract = new()
            {
                Name = sqlFile.EntityName,
                Columns = columns,
            };
            state.EntityTypeCache.Add(tableAbstract);
        }

        return new ScriptClassFile(
            fileName: $"{sqlFile.CleanFileName}.cs",
            entityName: sqlFile.EntityName,
            methodName: methodName,
            columns: columns,
            sqlContent: sqlFile.Content,
            methodParams: methodParams,
            sqlParams: sqlParams,
            objectType: objectType,
            ScriptType: sqlFile.ScriptType,
            generateType: !externalType,
            errorMessage: errorMessage
        );
    }

    private static IEnumerable<DataRow> GetRows(State.SqlFile sqlFile, string conString) =>
        GetTableSchema(sqlFile, conString)?.Rows.ToRowEnumerable() ?? new List<DataRow>();

    private static List<Column> GetProperties(IEnumerable<DataRow> rows) =>
        rows
            .Select((row, index) => new Column
            {
                DataType = SqlFile.CleanTypeName(row["DataType"]?.ToString() ?? "", row["DataTypeName"].ToString() ?? ""),
                NullFlag = (((bool)row["AllowDBNull"]) == true ? "?" : ""),
                ColumnName = row["ColumnName"]?.ToString().Replace(" ", "_") ?? "",
                DefaultValue = (((bool)row["AllowDBNull"]) != true && row["DataType"].ToString() == "System.String" ? " = System.String.Empty;" : ""),
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
            SqlCommand cmd = new(script.ContentNoTableConstraints, conn, transaction);

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
        catch (Exception ex)
        {
            string splitterLine = "".PadRight(70, '*');
            string errorMessage = $"\n[red]Error running {script.CleanFileName}[/]\n\n{splitterLine}\n{script.Content}\n{splitterLine}\n{ex.Message}\n".Replace("[", "[[").Replace("]", "]]");

            throw new ProcessingException(ex.Message, ex, $"\n[red]Error running {script.CleanFileName}\n\n[/]{errorMessage}\n", script.FileName, script.CleanFileName);
        }
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
}
