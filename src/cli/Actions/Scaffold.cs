﻿using sqlM.ResultClassTypes;
using Spectre.Console;

namespace sqlM.Actions;
public class Scaffold : IAction
{
    public void Go(StartupParams startupParams, State.Container state)
    {
        string errorMessage = "";
        ProcessingException procEx = null;
        State.FileCollection files;
        AnsiConsole.Progress()
            .Start(ctx =>
            {
                try
                {
                    files = FileHandler.GetFiles(state);
                    ProgressTask parseSqlTask = ctx.AddTask("[green]Parsing SQL files[/]", maxValue: files.FileCount);
                    state.SqlFiles = FileHandler.GetSqlFiles(files, parseSqlTask);


                    ProgressTask findDependenciesTask = ctx.AddTask("[green]Finding dependencies[/]", maxValue: state.SqlFiles.Length);
                    Directory.CreateDirectory(state.OutputDirectory);
                    state.SqlFiles = FileHandler.FindDependencies(state.SqlFiles, findDependenciesTask);


                    ProgressTask generateClassesTask = ctx.AddTask("[green]Generating class files[/]", maxValue: state.SqlFiles.Length);
                    BaseClassFile[] classFiles = FileHandler.GenerateClassFiles(state, generateClassesTask);
                    if (FileHandler.CheckForDuplicates(classFiles, out string name, out string firstFileName, out string lastFileName))
                    {
                        errorMessage = $"\n[red]The type name \"{name}\" is duplicated in {firstFileName} and {lastFileName}[/]";
                        return;
                    }

                    ProgressTask savingClassesTask = ctx.AddTask("[green]Saving class files[/]", maxValue: state.SqlFiles.Length);
                    FileHandler.SaveClassFiles(state, classFiles, savingClassesTask);
                }
                catch (ProcessingException ex)
                {
                    procEx = ex;
                    errorMessage = procEx.ConsoleMessage;
                }
            });


        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            AnsiConsole.Write(new Markup("[bold green]Class Scaffolding Complete[/]"));
        }
        else
        {
            AnsiConsole.MarkupLine(errorMessage);
            if (procEx != null && AnsiConsole.Confirm($"Do you want me to open {procEx.CleanFileName}?", defaultValue: false))
            {
                FileHandler.OpenWithDefaultProgram(procEx.FileName);
            }
        }
    }
}
