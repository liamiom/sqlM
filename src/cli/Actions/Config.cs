using Spectre.Console;
using sqlM.State;

namespace sqlM.Actions;

public class Config : IAction
{
    public void Go(StartupParams startupParams, Container state)
    {
        if (!sqlM.Config.TryLoad(out Container config))
        {
            return;
        }

        bool updateConfig = AnsiConsole.Confirm($@"
sqlM is currently configured like this.

   ConnectionString: {config.ConnectionString}
   SqlFolder: {config.SourceDirectory}
   OutputDirectory: {config.OutputDirectory}
   GenerateInterfaceClass: {config.GenerateInterfaceClass}
   GenerateSyncMethods: {config.GenerateSyncMethods}
   GenerateAsyncMethods: {config.GenerateAsyncMethods}

The configuration is stored in [green].sqlM/config[/] Would you like to change the configuration?
        ", defaultValue: false);

        if (!updateConfig)
        {
            return;
        }

        sqlM.Config.Set();
    }
}
