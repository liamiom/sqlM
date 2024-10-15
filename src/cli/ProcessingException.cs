namespace sqlM;
internal class ProcessingException : Exception
{
    public string ConsoleMessage { get; }
    public string FileName { get; }
    public string CleanFileName { get; }

    public ProcessingException(string message, Exception inner, string consoleMessage, string fileName, string cleanFileName) : base(message, inner)
    {
        ConsoleMessage = consoleMessage;
        FileName = fileName;
        CleanFileName = cleanFileName;

    }
}
