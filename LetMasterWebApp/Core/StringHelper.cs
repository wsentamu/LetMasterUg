using System.Text.RegularExpressions;

namespace LetMasterWebApp.Core;

public static class StringHelper
{
    private static string CleanUpString(string data)
    {
        // Remove non-digit characters
        var returnData = Regex.Replace(data, @"\D", "");
        return returnData;
    }
    public static string CleanPhone(this string data)
    {
        var clean = CleanUpString(data);
        if (clean.StartsWith("256"))
            return clean;
        if (clean.StartsWith("+256"))
            return clean.Replace("+256", "256");
        if (clean.StartsWith("0"))
            return "256" + clean.Substring(1);
        if (clean.Length == 9)
            return "256" + clean;
        return clean;
    }
}
