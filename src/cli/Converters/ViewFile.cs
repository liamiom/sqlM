using sqlM.ResultClassTypes;
using sqlM.State;
using Spectre.Console;
using System.Text.RegularExpressions;

namespace sqlM.Converters;
internal class ViewFile
{
    public static State.SqlFile Parse(string fileName, string workingDirectory, ProgressTask taskProgress)
    {
        string content = System.IO.File.ReadAllText(fileName);
        string paramText = Regex.Replace(content, @".*(CREATE|ALTER)\s+VIEW\s+\[\S+\](.*)AS.*", "$2", RegexOptions.Singleline | RegexOptions.IgnoreCase);


        List<KeyValuePair<string, Type>> parms = SqlFile.GetParams(paramText);

        string hash = SqlFile.GetFileHash(fileName);
        // Todo: this should check the hash code against the generated files and skip files that are not changed


        // Todo: add in Get method generation compatible with a view and then add this back in
        string entityName = Regex.Replace(content, @".*(CREATE|ALTER)\s+VIEW\s(\[\S+\]\.)\[?([^\]|\s]+)\]?.*", "$3", RegexOptions.Singleline | RegexOptions.IgnoreCase);

        taskProgress.Increment(1);
        return new State.SqlFile()
        {
            FileName = fileName,
            CleanFileName = SqlFile.CleanFileName(fileName),
            EntityName = "",
            Content = content,
            Paramiters = parms,
            Names = new Dictionary<string, string>(),
            Hash = hash,
            Path = Path.GetRelativePath(workingDirectory, fileName),
            ScriptType = State.SqlFile.ObjectTypes.View,
        };
    }

    public static BaseClassFile GenerateClassFile(Container state, State.SqlFile sqlFile)
    {
        return new ScriptClassFile(
            fileName: $"{sqlFile.CleanFileName}.cs",
            entityName: sqlFile.EntityName,
            methodName: sqlFile.CleanFileName,
            columns: new List<Column>(),
            sqlContent: sqlFile.Content,
            methodParams: "",
            sqlParams: "",
            objectType: ScriptClassFile.ObjectReturnTypes.View,
            ScriptType: sqlFile.ScriptType
        );
    }
}
