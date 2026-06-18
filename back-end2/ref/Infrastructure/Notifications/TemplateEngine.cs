using System.Text.RegularExpressions;

namespace Infrastructure.Notifications;

/// <summary>
/// Simple template engine for notification messages.
/// Replaces {placeholder} with actual values from data dictionary.
/// </summary>
internal static class TemplateEngine
{
    private static readonly Regex PlaceholderPattern = new(@"\{(\w+)\}", RegexOptions.Compiled);

    /// <summary>
    /// Processes a template string by replacing placeholders with values.
    /// </summary>
    /// <param name="template">Template with {placeholder} syntax.</param>
    /// <param name="data">Dictionary of placeholder values.</param>
    /// <returns>Processed string with placeholders replaced.</returns>
    public static string Process(string template, Dictionary<string, object> data)
    {
        if (string.IsNullOrEmpty(template) || data.Count == 0)
        {
            return template;
        }

        return PlaceholderPattern.Replace(template, match =>
        {
            string key = match.Groups[1].Value;

            if (data.TryGetValue(key, out object? value))
            {
                return value?.ToString() ?? string.Empty;
            }

            // Leave placeholder as-is if not found
            return match.Value;
        });
    }

    /// <summary>
    /// Converts an object to a dictionary of properties.
    /// </summary>
    public static Dictionary<string, object> ObjectToDictionary(object obj)
    {
        if (obj is Dictionary<string, object> dict)
        {
            return dict;
        }

        var result = new Dictionary<string, object>();

        System.Reflection.PropertyInfo[] properties = obj.GetType().GetProperties();
        foreach (System.Reflection.PropertyInfo property in properties)
        {
            object? value = property.GetValue(obj);
            if (value is not null)
            {
                result[property.Name] = value;
            }
        }

        return result;
    }
}
