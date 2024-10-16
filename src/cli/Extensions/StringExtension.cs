﻿using System.Text.RegularExpressions;

namespace sqlM.Extensions;
internal static class StringExtension
{
    public static T ToEnum<T>(this string value, T defaultValue) =>
        value.ToEnum(defaultValue, new Dictionary<string, string>());

    public static T ToEnum<T>(this string value, T defaultValue, Dictionary<string, string> alias) =>
        !Enum.TryParse(typeof(T), Replace(value, alias), ignoreCase: true, out object? result) 
            ? defaultValue 
            : result.ToEnum(defaultValue);

    private static T ToEnum<T>(this object? value, T defaultValue) => 
        value == null 
            ? defaultValue 
            : (T)value;

    public static string Replace(this string value, Dictionary<string, string> alias)
    {
        if (alias == null)
        {
            return value;
        }

        return alias.ContainsKey(value) 
            ? alias[value] 
            : value;
    }

    public static string RegexReplace(this string input, string pattern, string replacement) => 
        Regex.Replace(input, pattern, replacement, RegexOptions.Multiline);

    public static string RegexReplace(this string input, string pattern, string replacement, RegexOptions options) =>
        Regex.Replace(input, pattern, replacement, options);

    public static string[] RegexMatchAll(this string input, string pattern, RegexOptions options = RegexOptions.IgnoreCase) =>
        Regex.Matches(input, pattern, options).Select(x => x.Value).ToArray();

    public static string RegexFind(this string input, string pattern) =>
        Regex.Match(input, pattern, RegexOptions.Multiline).ToString();

    public static string AnsiSafe(this string input) =>
        input.Replace("[", "[[").Replace("]", "]]");
}
