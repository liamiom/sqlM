using System.Data;

namespace sqlM;

internal static class DataRowCollectionExtension
{
    public static IEnumerable<DataRow> ToRowEnumerable(this DataRowCollection rowCollection)
    {
        foreach (DataRow row in rowCollection)
        {
            yield return row;
        }
    }
}
