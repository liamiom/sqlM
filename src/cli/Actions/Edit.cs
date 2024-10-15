using sqlM.ResultClassTypes;
using Spectre.Console;
using System.Diagnostics;

namespace sqlM.Actions;
public class Edit : IAction
{
    private readonly string[] _dataTypes = new string[]
        {
            "int",
            "varchar",
            "decimal",
        };

    private readonly string[] _actions = new string[]
        {
            "Add Column",
            "Open",
        };

    public void Go(StartupParams startupParams, State.Container state)
    {
        string objectTypeString = startupParams.ObjectType.ToString();
        string typeDirectory = Path.Combine(state.SourceDirectory, objectTypeString);

        if (startupParams.ObjectType == StartupParams.ObjectTypes.Table)
        {
            Table(typeDirectory);
            return;
        }

        if (!Directory.Exists(typeDirectory))
        {
            Console.WriteLine($"The {startupParams.ObjectType} directory seems to be empty.");
            return;
        }

        string[] files = Directory
            .GetFiles(typeDirectory, "*.sql")
            .Select(i => Path.GetFileNameWithoutExtension(i))
            .ToArray();

        string fileName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an object")
                .AddChoices(files)
            );

        string fullFileName = Path.Combine(typeDirectory, fileName + ".sql");

        OpenWithDefaultProgram(fullFileName);
    }

    private void Table(string typeDirectory)
    {
        string[] files = Directory
            .GetFiles(typeDirectory, "*.sql")
            .Select(i => Path.GetFileNameWithoutExtension(i))
            .ToArray();

        string fileName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a table")
                .AddChoices(files)
            );

        string fullFileName = Path.Combine(typeDirectory, fileName + ".sql");
        string action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a table")
                .AddChoices(_actions)
            );

        if (action == "Open")
        {
            OpenWithDefaultProgram(fullFileName);
            return;
        }

        string columnName = AnsiConsole.Ask<string>($"What is the column name?");
        string dataType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a datatype")
                .AddChoices(_dataTypes)
            );
        int dataLength = TypeHasLength(dataType) 
            ? AnsiConsole.Ask<int>($"What is the field length?")
            : 0;

        bool nullable = AnsiConsole.Confirm("Is the field nullable");

        BaseClassFile addColumnFile = FileHandler.GetEmbeddedFile($"Table_AddColumn.sql", "");

        addColumnFile.Content = addColumnFile.Content
            .Replace("{tableName}", fileName)
            .Replace("{columnName}", columnName)
            .Replace("{dataType}", dataType)
            .Replace("{dataLength}", dataLength == 0 ? "" : $"({dataLength})")
            .Replace("{nullable}", nullable ? "NULL" : "NOT NULL");

        File.AppendAllText(fullFileName, addColumnFile.Content);
    }

    private static bool TypeHasLength(string dataType) => 
        dataType switch
        {
            "int" => false,
            _ => true
        };

    private static void OpenWithDefaultProgram(string path)
    {
        using Process fileopener = new();

        fileopener.StartInfo.FileName = "explorer";
        fileopener.StartInfo.Arguments = "\"" + path + "\"";
        fileopener.Start();
    }
}
