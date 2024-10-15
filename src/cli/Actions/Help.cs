using sqlM.Extensions;
using sqlM.ResultClassTypes;
using Spectre.Console;

namespace sqlM.Actions;
public class Help : IAction
{
    public void Go(StartupParams startupParams, State.Container state)
    {
        BaseClassFile readMeFile = FileHandler.GetEmbeddedFile($"README.md", "", "sqlM.");

        string header = readMeFile
            .Content
            .RegexFind(@"^#\W(\w.+)")
            .RegexReplace(@"^#\W(\w.+)", @"$1");

        string readMeContent = readMeFile
            .Content
            .RegexReplace(@"^#\W(\w.+)", @"")
            .RegexReplace(@"##\W(\w.+)", @"[bold]$1[/]")
            .RegexReplace(@"(^```\W\S+)", @"[grey]")
            .RegexReplace(@"(^```$)", @"[/]")
            .RegexReplace(@"\t", @"     ");

        FigletText headerFiglet = new FigletText(header)
            .LeftJustified()
            .Color(Color.Green);

        AnsiConsole.Write(headerFiglet);
        AnsiConsole.MarkupLine(readMeContent);
    }
}
