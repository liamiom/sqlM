using Spectre.Console;

namespace sqlM;
internal class Config
{
    private static string _folderPath =  $"{Environment.CurrentDirectory}/.sqlM";
    private static string _filePath = $"{_folderPath}/config";

    public static bool Exists() => 
        File.Exists(_filePath);

    public static State.Container? Set()
    {
        string serverName = AnsiConsole.Ask("SQL Server:", defaultValue: "localhost");
        string userName = AnsiConsole.Ask("SQL UserName: (Leave blank for windows auth)", defaultValue: "");
        string password = !string.IsNullOrWhiteSpace(userName)
            ? AnsiConsole.Ask<string>("SQL Password:")
            : "";

        string connectionString = MakeConnectionString(serverName, userName, password);

        bool connected = false;
        AnsiConsole
            .Status()
            .Spinner(Spinner.Known.Star)
            .Start("Testing connection string...", ctx => {
                connected = Database.TestConnectionString(connectionString);
            });

        if (!connected)
        {
            AnsiConsole.Markup("Database connection failed");
            return null;
        }


        List<string> databases = Database.GetDatabaseNames(connectionString);
        string databaseName = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a database")
                .AddChoices(databases)
            );

        State.Container config = new()
        {
            ConnectionString = MakeConnectionString(serverName, userName, password, databaseName),
            SourceDirectory = AnsiConsole.Ask("Source .sql files directory:", defaultValue: "Source"),
            OutputDirectory = AnsiConsole.Ask("Generated .cs files directory:", defaultValue: "sqlM")
        };

        Save(config);
        Console.WriteLine("New config saved");

        return config;
    }

    public static string MakeConnectionString(string serverName, string userName = "", string password = "", string database = "master") =>
        string.IsNullOrWhiteSpace(userName)
            ? $"Data Source={serverName};Initial Catalog={database};Integrated Security=SSPI;TrustServerCertificate=True;"
            : $"Data Source={serverName};Initial Catalog={database};User id={userName};Password={password};TrustServerCertificate=True;";

    public static void Save(State.Container config)
    {
        if (!Directory.Exists(_folderPath))
        {
            Directory.CreateDirectory(_folderPath);
        }

        if (!Directory.Exists(config.SourceDirectory))
        {
            Directory.CreateDirectory(config.SourceDirectory);
        }

        if (!Directory.Exists(config.OutputDirectory))
        {
            Directory.CreateDirectory(config.OutputDirectory);
        }


        File.WriteAllText(
            _filePath, 
            $"ConnectionString: {config.ConnectionString}\n" +
            $"SqlFolder: {config.SourceDirectory}\n" +
            $"OutputDirectory: {config.OutputDirectory}\n");
    }

    public static bool TryLoad(out State.Container config)
    {
        if (Exists())
        {
            config = Load();
            return true;
        }

        if (!AnsiConsole.Confirm("sqlM is not configured for this directory, do you want to add the configuration now?"))
        {
            config = new State.Container();
            return false;
        }

        State.Container? configTest = Set();

        if (configTest == null)
        {
            config = new State.Container();
            return false;
        }

        config = configTest;
        Save(config);

        return false;
    }

    public static State.Container Load()
    {
        if (!File.Exists(_filePath))
        {
            File.Create(_filePath);
        }

        string[] lines = File.ReadAllLines(_filePath);

        return new State.Container()
        {
            CurrentDirectory = Directory.GetCurrentDirectory(),
            ConnectionString = GetLine(lines, "ConnectionString"),
            SourceDirectory = Path.Combine(Directory.GetCurrentDirectory(), GetLine(lines, "SqlFolder")),
            OutputDirectory = Path.Combine(Directory.GetCurrentDirectory(), GetLine(lines, "OutputDirectory")),
        };
    }

    private static string GetLine(string[] lines, string propName) => 
        lines
            .Where(i => i.StartsWith(propName))
            .FirstOrDefault("")
            .Replace($"{propName}:", "")
            .Trim();

}
