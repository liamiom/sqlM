using Spectre.Console;

namespace sqlM.Actions;
public class Remove : IAction
{
    public void Go(StartupParams startupParams, State.Container state)
    {
        string objectTypeString = startupParams.ObjectType.ToString();
        string typeDirectory = Path.Combine(state.SourceDirectory, objectTypeString);
        
        if (!Directory.Exists(typeDirectory))
        {
            return;
        }

        string[] files = Directory
            .GetFiles(typeDirectory, "*.sql")
            .Select(i => Path.GetFileName(i))
            .ToArray();


        string fileName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a file")
                .AddChoices(files)
            );

        string fullFileName = Path.Combine(typeDirectory, fileName);
        
        if (!File.Exists(fullFileName))
        {
            return;
        }

        File.Delete(fullFileName);
        Console.WriteLine($"{fileName} removed");
    }
}
