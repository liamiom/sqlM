﻿using Spectre.Console;
using sqlM.Extensions;
using sqlM.ResultClassTypes;
using sqlM.State;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;

namespace sqlM;

internal class FileHandler
{
    private readonly string _currentDirectory;
    private readonly string _outputDirectory;

    public List<SQLScript> Scripts { get; set; }

    public FileHandler(string currentDirectory)
    {
        _currentDirectory = currentDirectory;
        _outputDirectory = _currentDirectory + @"\sqlM";

        if (!Directory.Exists(_outputDirectory))
        {
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public static FileCollection GetFiles(Container state)
    {
        string queryScriptsPath = Path.Combine(state.SourceDirectory, "Query");
        string tableScriptsPath = Path.Combine(state.SourceDirectory, "Table");
        string viewScriptsPath = Path.Combine(state.SourceDirectory, "View");
        string functionScriptsPath = Path.Combine(state.SourceDirectory, "Function");
        string storedProcedureScriptsPath = Path.Combine(state.SourceDirectory, "StoredProcedure");
        
        // Add or update the Query read me file
        if (!Directory.Exists(queryScriptsPath))
        {
            Directory.CreateDirectory(queryScriptsPath);
            BaseClassFile sourceReadMeFile = GetSqlFolderREADMEFile();
            string fullFileName = Path.Combine(queryScriptsPath, sourceReadMeFile.FileName);
            System.IO.File.WriteAllText(fullFileName, sourceReadMeFile.Content);
        }

        return new FileCollection
        {
            Query = GetFiles(queryScriptsPath),
            QueryPath = queryScriptsPath,
            Table = GetFiles(tableScriptsPath),
            TablePath = tableScriptsPath,
            View = GetFiles(viewScriptsPath),
            ViewPath = viewScriptsPath,
            Function = GetFiles(functionScriptsPath),
            FunctionPath = functionScriptsPath,
            StoredProcedure = GetFiles(storedProcedureScriptsPath),
            StoredProcedurePath = storedProcedureScriptsPath,
        };
    }

    public static SqlFile[] GetSqlFiles(FileCollection files, ProgressTask taskProgress)
    {
        List<SqlFile> sqlFiles = new();
        sqlFiles.AddRange(files.Query.Select(i => Converters.QueryFile.Parse(i, files.QueryPath, taskProgress)));
        sqlFiles.AddRange(files.Table.Select(i => Converters.TableFile.Parse(i, files.TablePath, taskProgress)));
        sqlFiles.AddRange(files.View.Select(i => Converters.ViewFile.Parse(i, files.ViewPath, taskProgress)));
        sqlFiles.AddRange(files.Function.Select(i => Converters.FunctionFile.Parse(i, files.FunctionPath, taskProgress)));
        sqlFiles.AddRange(files.StoredProcedure.Select(i => Converters.StoredProcedureFile.Parse(i, files.StoredProcedurePath, taskProgress)));

        return sqlFiles.ToArray();
    }

    private static List<string> GetFiles(string path)
    {
        if (!Directory.Exists(path))
        {
            return new List<string>();
        }

        List<string> files = Directory
            .GetFiles(path, "*.sql")
            .ToList();

        foreach (string subFolder in Directory.GetDirectories(path))
        {
            files.AddRange(GetFiles(subFolder));
        }

        return files;
    }

    public static BaseClassFile[] GenerateClassFiles(Container state, ProgressTask taskProgress)
    {
        List<BaseClassFile> classFiles = new() { GetBaseClassFile(state) };

        foreach (var sqlFile in state.SqlFiles)
        {
            classFiles.Add(Converters.SqlFile.GenerateClassFile(state, sqlFile));
            taskProgress.Increment(1);
        }

        return classFiles.ToArray();
    }

    public static bool CheckForDuplicates(BaseClassFile[] classFiles, out string name, out string firstFileName, out string lastFileName)
    {
        // Check for duplicate names
        var duplicateNameList = classFiles
            .GroupBy(i => i.EntityName)
            .Where(i => i.Count() > 1)
            .ToList();


        if (duplicateNameList.Any())
        {
            var firstDuplicate = duplicateNameList.First();
            name = firstDuplicate.Key.AnsiSafe();
            firstFileName = firstDuplicate.First().FileName.AnsiSafe();
            lastFileName = firstDuplicate.Last().FileName.AnsiSafe();
        }
        else
        {
            name = "";
            firstFileName = "";
            lastFileName = "";
        }

        return duplicateNameList.Any();
    }

    public static bool SaveClassFiles(Container state, BaseClassFile[] classFiles, ProgressTask taskProgress)
    {
        foreach (BaseClassFile classFile in classFiles)
        {
            string fullFileName = Path.Combine(state.OutputDirectory, classFile.FileName);
            System.IO.File.WriteAllText(fullFileName, classFile.Content);
            taskProgress.Increment(1);
        }
     
        return true;
    }

    private static BaseClassFile GetBaseClassFile(Container state)
    {
        BaseClassFile dbFile = GetEmbeddedFile("Database.cs", "Database.cs");

        var sqlFiles = OrderByDependencies(state.SqlFiles);

        string updateScripts = sqlFiles
            .Where(i => i.ScriptType != SqlFile.ObjectTypes.Query &&  i.ScriptType != SqlFile.ObjectTypes.None)
            .OrderBy(i => i.SortOrder)
            .Select(i => $"\t\t\tnew(\"{i.CleanFileName}\", DatabaseUpdateStrings.{i.CleanFileName}),\n")
            .Join();

        dbFile.Content = dbFile.Content.Replace(
            $"UpdateScript[] updateScripts = Array.Empty<UpdateScript>(); // Database update scripts go here", 
            $"UpdateScript[] updateScripts = new UpdateScript[] {{ \n{updateScripts} \t\t}};"
            );

        return dbFile;
    }

    public static SqlFile[] FindDependencies(SqlFile[] sqlFiles, ProgressTask taskProgress)
    {
        foreach (SqlFile sqlFile in sqlFiles.Where(i => i.ScriptType != SqlFile.ObjectTypes.Query))
        {
            sqlFile.Dependencies = sqlFiles
                .Where(i => 
                    i.CleanFileName != sqlFile.CleanFileName &&
                    i.ScriptType != SqlFile.ObjectTypes.Query &&
                    Regex.IsMatch(sqlFile.ContentNoComments, $@"[\[\s]{i.CleanFileName}[\]\s]", RegexOptions.Singleline | RegexOptions.IgnoreCase)
                    )
                .ToList();
            taskProgress.Increment(1);
        }

        return sqlFiles;
    }

    private static SqlFile[] OrderByDependencies(SqlFile[] sqlFiles)
    {
        SqlFile[] filesToSort = sqlFiles.Where(i => i.SortOrder == 0).ToArray();
        int sortCount = 1;

        while (filesToSort.Any())
        {
            SqlFile[] batch = filesToSort
                .Where(i => !i.Dependencies.Any(d => filesToSort.Any(f => d.CleanFileName == f.CleanFileName)) )
                .ToArray();

            if (filesToSort.Any() && !batch.Any())
            {
                string fileNames = filesToSort
                    .Select(i => i.FileName)
                    .Aggregate((x, y) => $"{x}\n{y}");
                AnsiConsole.MarkupLine($"\n[red]It looks like there is a circular reference in the following files.[/]\n{fileNames}");

                return Array.Empty<SqlFile>();
            }

            foreach (var item in batch)
            {
                item.SortOrder = sortCount;
                sortCount++;
            }

            filesToSort = sqlFiles.Where(i => i.SortOrder == 0).ToArray();
        }

        return sqlFiles;
    }

    private static BaseClassFile GetSqlFolderREADMEFile() => 
        GetEmbeddedFile("SqlFolderREADME.md", "README.md");

    public static BaseClassFile GetEmbeddedFile(string resourceName, string fileName, string resourceFolder = "sqlM.OutputFiles.")
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        resourceName = resourceFolder + resourceName;

        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        using StreamReader reader = new(stream);

        return new BaseClassFile
        {
            FileName = fileName,
            Content = reader.ReadToEnd().Replace("\r\n", "\n"),
        };
    }

    public static void OpenWithDefaultProgram(string path)
    {
        using Process fileopener = new();

        fileopener.StartInfo.FileName = "explorer";
        fileopener.StartInfo.Arguments = "\"" + path + "\"";
        fileopener.Start();
    }

}
