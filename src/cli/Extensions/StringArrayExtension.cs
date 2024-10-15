namespace sqlM;

internal static class StringArrayExtension
{
    public static string ToMultiLineString(this string[] stringArray) => 
        ToDelimitedString(stringArray, Environment.NewLine);

    public static string ToMultiLineString(this List<string> stringList) =>
        ToDelimitedString(stringList.ToArray(), Environment.NewLine);

    public static string ToMultiLineString(this IEnumerable<string> stringEnumerable) =>
        ToDelimitedString(stringEnumerable.ToArray(), Environment.NewLine);
    
    public static string ToDelimitedString(this string[] stringArray, string delimiter = ", ") => 
        string.Join(delimiter, stringArray);

    public static string ToDelimitedString(this IEnumerable<string> stringEnumerable, string delimiter = ", ") =>
        ToDelimitedString(stringEnumerable.ToArray(), delimiter);

    public static string Join(this IEnumerable<string> stringEnumerable, string delimiter = "") =>
        ToDelimitedString(stringEnumerable.ToArray(), delimiter);

    public static IEnumerable<KeyValuePair<int, T>> WithIndex<T>(this IEnumerable<T> itemList)
    {
        int index = 0;
        foreach (T item in itemList)
        {
            yield return new KeyValuePair<int, T>(index, item);
            index++;
        }
    }
}
