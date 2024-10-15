using sqlM.Extensions;
using Spectre.Console;

namespace sqlM;
public class StartupParams
{
    public enum Actions { None, Help, Scaffold, Add, Edit, Remove }
    public enum ObjectTypes { None, Query, Table, View, Function, StoredProcedure }
    public Actions Action { get; set; }
    public ObjectTypes ObjectType { get; set; }
    public string Name { get; set; }
    public bool InteractiveMode { get; set; } = false;

    private readonly Dictionary<string, string> _alias = new Dictionary<string, string> 
    {
        { "h", "Help" },
        { "s", "Scaffold" },
        { "a", "Add" },
        { "e", "Edit" },
        { "r", "Remove" },
        { "q", "Query" },
        { "t", "Table" },
        { "v", "View" },
        { "f", "Function" },
        { "sp", "StoredProcedure" },
        { "-h", "Help" },
        { "-s", "Scaffold" },
        { "-a", "Add" },
        { "-e", "Edit" },
        { "-r", "Remove" },
        { "-q", "Query" },
        { "-t", "Table" },
        { "-v", "View" },
        { "-f", "Function" },
        { "-sp", "StoredProcedure" },
    };

    public StartupParams(string[] args)
    {
        Action = args.Length < 1
            ? Actions.None 
            : args[0].ToEnum(Actions.None, _alias);
        ObjectType = args.Length < 2
            ? ObjectTypes.None
            : args[1].ToEnum(ObjectTypes.None, _alias);
        Name = args.Length < 3 
            ? "" 
            : args[2];
    }

    public bool Check()
    {
        InteractiveMode = 
            Action == Actions.None || 
            ObjectType == ObjectTypes.None || 
            string.IsNullOrWhiteSpace(Name);

        if (Action == Actions.None && !PickAction())
        {
            return false;
        }

        if (Action == Actions.None || Action == Actions.Help || Action == Actions.Scaffold)
        {
            return true;
        }

        if (ObjectType == ObjectTypes.None && !PickObjectType())
        {
            return false;
        }

        return true;
    }

    public bool PickAction()
    {
        string[] actionNames = Enum.GetNames(typeof(Actions));
        string name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an action")
                .AddChoices(actionNames)
            );
        Action = name.ToEnum(Actions.None);

        return Actions.None != Action;
    }

    public bool PickObjectType()
    {
        string[] objectTypeNames = Enum.GetNames(typeof(ObjectTypes));
        string name = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select an object type")
                .AddChoices( objectTypeNames)
            );
        ObjectType = name.ToEnum(ObjectTypes.None);

        return ObjectTypes.None != ObjectType;
    }

    public bool PickName()
    {
        if (!string.IsNullOrWhiteSpace(Name))
        {
            return true;
        }

        Name = AnsiConsole.Ask<string>($"What do you want to call the {ObjectType}?");

        return !string.IsNullOrWhiteSpace(Name);
    }

    internal sqlM.Actions.IAction GetActionClass() => 
        Action switch
        {
            Actions.Help => new sqlM.Actions.Help(),
            Actions.Scaffold => new sqlM.Actions.Scaffold(),
            Actions.Add => new sqlM.Actions.Add(),
            Actions.Edit => new sqlM.Actions.Edit(),
            Actions.Remove => new sqlM.Actions.Remove(),
            _ => new sqlM.Actions.None(),
        };
}
