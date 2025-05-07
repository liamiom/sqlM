using sqlM;
using Spectre.Console;
using System.Reflection;

if (args.Length == 0)
{
    string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "";

    if (Config.Exists() )
    {
        AnsiConsole.MarkupLine($"[bold]sqlM {version}[/]");
    }
    else
    {
        AnsiConsole.MarkupLine($@"

[bold]sqlM {version}[/]

[green]Object relational mapping made easy.[/]

I allow you to convert simple .sql files into C# methods with typed parameters and return values. 
This is all written directly on top of ADO with none of the overhead you get with most ORM tools.

Once you have your configuration setup, simply run this tool to build a C# data access layer.

Use [bold]sqlM help[/] for more info.
        ");
    }
}

if (!Directory.GetFiles(Environment.CurrentDirectory, "*.csproj").Any())
{
    AnsiConsole.MarkupLine("[red]I can't find a project file here. Try changing directory into the project folder.[/]");
    return;
}

if (DotNet.IsDotnetCoreProject() && !DotNet.CheckForReference("System.Data.SqlClient"))
{
    AnsiConsole.MarkupLine($@"
[red]I can't find System.Data.SqlClient in this project.[/]
Try adding a System.Data.SqlClient reference and then runing sqlM again. You can do this from the dotnet tool like this.

dotnet add package System.Data.SqlClient");
    return;
}

if (!Config.TryLoad(out sqlM.State.Container state))
{
    return;
}


StartupParams startupParams = new(args);

if (!startupParams.Check())
{
    return;
}

sqlM.Actions.IAction action = startupParams.GetActionClass();
action.Go(startupParams, state);
