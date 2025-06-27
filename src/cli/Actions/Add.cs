using sqlM.ResultClassTypes;
using Spectre.Console;
using System.Diagnostics;

namespace sqlM.Actions;
public class Add : IAction
{
    public void Go(StartupParams startupParams, State.Container state)
    {
        if (!startupParams.PickName())
        {
            return;
        }

        if (startupParams.ObjectType == StartupParams.ObjectTypes.Function)
        {
            Functions(startupParams, state);
        }

        if (startupParams.ObjectType == StartupParams.ObjectTypes.Table)
        {
            Table(startupParams, state);
        }

        if (startupParams.ObjectType == StartupParams.ObjectTypes.StoredProcedure)
        {
            StoredProcedure(startupParams, state);
        }

        if (startupParams.ObjectType == StartupParams.ObjectTypes.Query)
        {
            Query(startupParams, state);
        }

        if (startupParams.ObjectType == StartupParams.ObjectTypes.View)
        {
            View(startupParams, state);
        }
    }

    private static void Functions(StartupParams startupParams, State.Container state) => 
        GetFile(startupParams, state, startupParams.ObjectType);

    private static void Query(StartupParams startupParams, State.Container state) => 
        GetFile(startupParams, state, startupParams.ObjectType);

    private static void StoredProcedure(StartupParams startupParams, State.Container state) => 
        GetFile(startupParams, state, startupParams.ObjectType);

    private static void Table(StartupParams startupParams, State.Container state) => 
        GetFile(startupParams, state, startupParams.ObjectType);

    private static void View(StartupParams startupParams, State.Container state) => 
        GetFile(startupParams, state, startupParams.ObjectType);

    private static void GetFile(StartupParams startupParams, State.Container state, StartupParams.ObjectTypes objectType)
    {
        string objectTypeString = GetPath(objectType);
        BaseClassFile fileResult = FileHandler.GetEmbeddedFile($"{objectType}.sql", $"{startupParams.Name}.sql");
        string fullFileName = Path.Combine(state.SourceDirectory, objectTypeString, fileResult.FileName);
        string relativePath = Path.GetRelativePath(state.SourceDirectory, fullFileName);

        if (File.Exists(fullFileName))
        {
            if (!AnsiConsole.Confirm($"It looks like {relativePath} already exists, do you want me to overwrite the existing file?", defaultValue: false))
            {
                return;
            }
        }

        fileResult.Content = fileResult.Content.Replace("{Name}", startupParams.Name);


        string directoryName = Path.GetDirectoryName(fullFileName) ?? "";
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        File.WriteAllText(fullFileName, fileResult.Content);


        if (startupParams.InteractiveMode)
        {
            if (AnsiConsole.Confirm($"{relativePath} created, do you want me to open the file?", defaultValue: false))
            {
                OpenWithDefaultProgram(fullFileName);
            }
        }
        else
        {
            Console.WriteLine($"{relativePath} created");
        }
    }

    private static string GetPath(StartupParams.ObjectTypes objectType) => 
        objectType switch
            {
                StartupParams.ObjectTypes.Query => "Queries",
                StartupParams.ObjectTypes.Table => "Tables",
                StartupParams.ObjectTypes.View => "Views",
                StartupParams.ObjectTypes.Function => "Functions",
                StartupParams.ObjectTypes.StoredProcedure => "Procedures",
                _ => "",
            };

    private static void OpenWithDefaultProgram(string path)
    {
        using Process fileopener = new();

        fileopener.StartInfo.FileName = "explorer";
        fileopener.StartInfo.Arguments = "\"" + path + "\"";
        fileopener.Start();
    }
}
