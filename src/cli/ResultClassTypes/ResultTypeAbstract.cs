namespace sqlM.ResultClassTypes;
public class ResultTypeAbstract
{
    public required string Name { get; set; }
    public required List<Column> Columns { get; set; }

    public bool ColumnsMatch(List<Column> columns, out string error)
    {
        error = ColumnsMatch(columns);
        return string.IsNullOrWhiteSpace(error);
    }

    public string ColumnsMatch(List<Column> externalColumns)
    {
        List<Column> missing = Columns.Where(i => !externalColumns.Any(a => a.ColumnName == i.ColumnName)).ToList();
        if (missing.Count > 0)
        {
            string missingColumns = missing.Select(i => i.ColumnName).Join("\n");
            return $"The following columns exist in the previously established type but are missing from this\n\n{missingColumns}";
        }

        List<Column> extra = Columns.Where(i => !externalColumns.Any(a => a.ColumnName == i.ColumnName)).ToList();
        if (extra.Count > 0)
        {
            string extraColumns = extra.Select(i => i.ColumnName).Join("\n");
            return $"The following columns do not exist in the previously established type\n\n{extraColumns}";
        }

        if (Columns.Count != externalColumns.Count)
        {
            string columnTable = LineUpAsColumns(
                Columns.Select(i => i.ColumnName).Prepend(Name).ToArray(),
                externalColumns.Select(i => i.ColumnName).Prepend("This").ToArray()
                );
            return $"Column miss match. The previously established type \"{Name}\" returns {Columns.Count} columns where as this script returns {externalColumns.Count} columns\n\n{columnTable}";
        }

        Column[] columns1 = Columns.OrderBy(i => i.ColumnName).ToArray();
        Column[] columns2 = externalColumns.OrderBy(i => i.ColumnName).ToArray();

        for (int i = 0; i < Columns.Count; i++)
        {
            if (!ColumnMatches(columns1[i], columns2[i]))
            {
                return $"The column {columns1[i].ColumnName} returns as {columns1[i].DataType}{columns1[i].NullFlag} where the {columns2[i].ColumnName} type in \"{Name}\" is {columns2[i].DataType}{columns2[i].NullFlag}";
            }
        }

        return "";
    }

    private static bool ColumnMatches(Column column1, Column column2) =>
        column1.DataType == column2.DataType &&
        column1.NullFlag == column2.NullFlag &&
        column1.ColumnName == column2.ColumnName;

    private static string LineUpAsColumns(string[] list1, string[] list2)
    {
        string output = "";
        int largerCount = list1.Length > list2.Length
                ? list1.Length
                : list2.Length;

        for (int i = 0; i < largerCount; i++)
        {
            output += GetItemIfThere(list1, i).PadRight(40) + GetItemIfThere(list2, i).PadRight(40) + "\n";

            // Colour the headers
            if (i == 0)
            {
                output = $"[green]{output}[/]";
            }
        }

        return output;
    }

    private static string GetItemIfThere(string[] list, int index) => 
        index < 0 || index >= list.Length
            ? ""
            : list[index];
}
