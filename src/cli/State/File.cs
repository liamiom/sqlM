using sqlM.Extensions;
using System.Text.RegularExpressions;

namespace sqlM.State;

public class File
{
    public string FileName { get; set; }
    public string CleanFileName { get; set; }
    public string EntityName { get; set; }
    public string ObjectName { get; set; }
    public string Path { get; set; }
    public string Hash { get; set; }
    public string Content { get; set; }
    public string ContentNoComments => Content
        .RegexReplace(@"--.*$", "", RegexOptions.Multiline) // Trim out single line comments
        .RegexReplace(@"/\*.+\*/", "", RegexOptions.Multiline); // Trim out multi line comments
    public string ContentNoTableConstraints => // Add line to suppress foreign key constraints to allow the temporary insertion of random data
        "EXEC sp_MSforeachtable \"ALTER TABLE ? NOCHECK CONSTRAINT all\" \n" + Content;
}
