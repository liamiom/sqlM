namespace sqlM
{
    public static class Parse
    {
        private enum FileSections { None, Params, Tests, Main }

        public static List<SQLScript> Load(string path) =>
            Directory.GetFiles(path)
                .Select(i => ParseFile(i))
                .ToList();

        private static SQLScript ParseFile(string fileName)
        {
            string[] content = File.ReadAllLines(fileName);
            FileSections section = FileSections.None;
            string paramContent = "";
            string mainContent = "";

            foreach (string line in content)
            {
                string lineCheck = line.Replace(" ", "").ToUpper();
                if (lineCheck == "---PARAMS---")
                {
                    section = FileSections.Params;
                }
                else if (lineCheck == "---TEST---")
                {
                    section = FileSections.Params;
                }
                else if (lineCheck == "---MAIN---")
                {
                    section = FileSections.Main;
                }
                else if (section == FileSections.Params)
                {
                    paramContent += line;
                }
                else if (section == FileSections.Main)
                {
                    mainContent += line + "\n";
                }
            }

            // Fix content line endings
            mainContent = mainContent.Replace("\r\n", "\n");
            if (Environment.NewLine == "\r\n")
            {
                mainContent = mainContent.Replace("\n", Environment.NewLine);
            }

            List<KeyValuePair<string, Type>> parms = GetParams(paramContent);

            return new SQLScript()
            {
                FileName = fileName,
                CleanFileName = Path.GetFileNameWithoutExtension(fileName),
                Content = mainContent,
                Paramiters = parms,
                QueryClassName = Path.GetFileNameWithoutExtension(fileName) + "_Query",
                ReturnClassName = Path.GetFileNameWithoutExtension(fileName) + "_Entity",
            };
        }

        private static List<KeyValuePair<string, Type>> GetParams(string content)
        {
            content = (content ?? "").Trim();
            if (content.StartsWith("DECLARE"))
            {
                content = content.Substring(7);
            }

            return RemoveDuplicateSpaces(content)
                .Replace("\t", "")
                .Replace("@", "")
                .Split(',')
                .Select(i => i.Trim().Split(' '))
                .Where(i => i.Length == 2)
                .Select(i => new KeyValuePair<string, Type>(i[0], GetTypeFromSqlName(i[1])))
                .ToList();
        }

        private static string RemoveDuplicateSpaces(string source) =>
            source.Contains("  ") ?
                RemoveDuplicateSpaces(source.Replace("  ", " ")) :
                source;

        private static Type GetTypeFromSqlName(string sqlType)
        {
            if (sqlType.ToLower() == "int")
            {
                return typeof(int);
            }
            if (sqlType.ToLower() == "datetime")
            {
                return typeof(int);
            }
            if (sqlType.ToLower().StartsWith("decimal"))
            {
                return typeof(decimal);
            }

            return typeof(string);
        }
    }
}
