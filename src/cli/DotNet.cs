using System.Diagnostics;

namespace sqlM;
internal class DotNet
{
    public static bool IsDotnetCoreProject()
    {
        string? firstProject = Directory
            .EnumerateFiles(Directory.GetCurrentDirectory(), "*.csproj")
            .FirstOrDefault();

        if (string.IsNullOrEmpty(firstProject))
        {
            return false;
        }

        string contents = File.ReadAllText(firstProject);
        return 
            !contents.Contains("<TargetFrameworkVersion>") && 
            contents.Contains("<TargetFramework>");
    }

    public static bool CheckForReference(string referenceName)
    {
        Process p = new()
        {
            StartInfo =
            {
                FileName = "dotnet",
                WorkingDirectory = Directory.GetCurrentDirectory(),
                Arguments = "list package",
                RedirectStandardOutput = true,
            }
        };

        if (!p.Start())
        {
            return false;
        }

        string output = p.StandardOutput.ReadToEnd().TrimEnd();
        return output.Contains(referenceName);
    }
}
