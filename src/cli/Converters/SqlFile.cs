using sqlM.ResultClassTypes;
using sqlM.State;
using System.Data;
using System.Security.Cryptography;
using sqlM.Extensions;
using System.Text.RegularExpressions;

namespace sqlM.Converters;

internal class SqlFile
{
    private enum FileSections { None, Params, Tests, Main }


    public static List<KeyValuePair<string, Type>> GetParams(string content)
    {
        content = (content ?? "").Trim();
        if (content.StartsWith("DECLARE", StringComparison.OrdinalIgnoreCase))
        {
            content = content.Substring(7);
        }

        return RemoveDuplicateSpaces(content)
            .RegexReplace(@"\n\s*AS\s.*", "", RegexOptions.Singleline | RegexOptions.IgnoreCase) // Trim out the script body
            .RegexReplace(@"--.*$", "", RegexOptions.Multiline) // Trim out single line comments
            .RegexReplace(@"/\*.+\*/", "", RegexOptions.Multiline) // Trim out multi line comments
            .RegexMatchAll(@"@[\w\d]+\s+\[?(int|datetime|date|time|decimal|bit|varchar|char|varbinary|float|text|nvarchar)+\]?")
            .Select(i => i.Trim().Split(' '))
            .Where(i => i.Length == 2)
            .Select(i => new KeyValuePair<string, Type>(TidyParamName(i[0]), GetTypeFromTidySqlName(i[1])))
            .ToList();
    }


    private static string TidyParamName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        // Todo: This is just the first example to come up, this should check the C# reserved word list.
        // It looks like this is possible using System.CodeDom.Compiler.CodeDomProvider CSprovider
        string[] skipList = ["@class"];
        if (skipList.Any(i => i.Equals(name, StringComparison.InvariantCultureIgnoreCase)))
        {
            return name;
        }

        if (name.StartsWith('@'))
        {
            name = name[1..];
        }

        if (name.Length > 1 && name != name.ToUpper())
        {
            name = name[..1].ToLower() + name[1..];
        }

        return name;
    }

    public static string GetFileHash(string fileName)
    {
        using MD5 md5 = MD5.Create();
        using FileStream stream = System.IO.File.OpenRead(fileName);
        return System.Text.Encoding.UTF8.GetString(md5.ComputeHash(stream));
    }

    private static string RemoveDuplicateSpaces(string source) =>
        source.Contains("  ") 
            ? RemoveDuplicateSpaces(source.Replace("  ", " ")) 
            : source;

    public static Type GetTypeFromTidySqlName(string sqlType) => 
        sqlType.Trim().ToLower().Replace("[", "").Replace("]", "") switch
        {
            "int" => typeof(int),
            "datetime" => typeof(DateTime),
            "date" => typeof(DateOnly),
            "time" => typeof(TimeOnly),
            "decimal" => typeof(decimal),
            "float" => typeof(decimal),
            "double" => typeof(decimal),
            "bit" => typeof(bool),
            "varbinary" => typeof(MemoryStream),
            _ => typeof(string)
        };

    public static BaseClassFile GenerateClassFile(Container state, State.SqlFile sqlFile) => 
        sqlFile.ScriptType switch
        {
            State.SqlFile.ObjectTypes.Table => TableFile.GenerateClassFile(state, sqlFile),
            State.SqlFile.ObjectTypes.View => ViewFile.GenerateClassFile(state, sqlFile),
            State.SqlFile.ObjectTypes.Function => FunctionFile.GenerateClassFile(state, sqlFile),
            State.SqlFile.ObjectTypes.StoredProcedure => StoredProcedureFile.GenerateClassFile(state, sqlFile),
            State.SqlFile.ObjectTypes.Query => QueryFile.GenerateClassFile(state, sqlFile),
            _ => GenerateUpdateOnlyClassFile(state, sqlFile)
        };

    private static BaseClassFile GenerateUpdateOnlyClassFile(Container state, State.SqlFile sqlFile) => 
        new ScriptClassFile(
            fileName: $"{sqlFile.CleanFileName}.cs",
            entityName: sqlFile.EntityName,
            methodName: "",
            columns: new List<Column>(),
            sqlContent: sqlFile.Content,
            methodParams: "",
            sqlParams: "",
            objectType: ScriptClassFile.ObjectReturnTypes.Table,
            ScriptType: sqlFile.ScriptType
        );

    public static string CleanFileName(string fileName) => 
        Path
            .GetFileNameWithoutExtension(fileName)
            .Replace(" ", "")
            .Replace("-", "_")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("'", "")
            .Replace("~", "");

    public static string CleanTypeName(string fullTypeName, string sqlTypeName = "")
    {
        if (fullTypeName == "System.DateTime" && sqlTypeName.ToLower() == "date")
        {
            fullTypeName = "DateOnly";
        }

        return fullTypeName
            .Replace("System.Int32", "int")
            .Replace("System.Boolean", "bool")
            .Replace("System.Byte", "byte")
            .Replace("System.Decimal", "decimal")
            .Replace("System.DateTime", "DateTime")
            .Replace("System.Double", "double")
            .Replace("System.TimeSpan", "DateTime")
            .Replace("System.String", "string");
    }
}
