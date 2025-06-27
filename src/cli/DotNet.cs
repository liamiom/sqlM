using System.Diagnostics;

namespace sqlM;
internal class DotNet
{
    private static bool? _dotnetCoreProject = null;
    public static bool IsDotnetCoreProject()
    {
        if (_dotnetCoreProject.HasValue)
        {
            return _dotnetCoreProject.Value;
        }

        string? firstProject = Directory
            .EnumerateFiles(Directory.GetCurrentDirectory(), "*.csproj")
            .FirstOrDefault();

        if (string.IsNullOrEmpty(firstProject))
        {
            _dotnetCoreProject = false;
            return _dotnetCoreProject.Value;
        }

        string contents = File.ReadAllText(firstProject);
        _dotnetCoreProject =
            !contents.Contains("<TargetFrameworkVersion>") && 
            contents.Contains("<TargetFramework>");

        return _dotnetCoreProject.Value;
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
