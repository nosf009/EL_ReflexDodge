using System.Text.RegularExpressions;

namespace Kiqqi.Localization
{
    /// <summary>
    /// Replaces {placeholders} in a localized string with values from an anonymous object.
    /// Example:
    ///   Apply("Reloading in {seconds}...", new { seconds = 3 })
    ///   => "Reloading in 3..."
    /// </summary>
    public static class KiqqiTemplateFormatter
    {
        private static readonly Regex placeholderPattern = new(@"\{(\w+)\}");

        public static string Apply(string template, object args)
        {
            if (string.IsNullOrEmpty(template) || args == null)
                return template ?? string.Empty;

            var type = args.GetType();

            string result = placeholderPattern.Replace(template, match =>
            {
                string name = match.Groups[1].Value;

                var prop = type.GetProperty(name);
                if (prop != null)
                {
                    object value = prop.GetValue(args, null);
                    return value?.ToString() ?? "";
                }

                var field = type.GetField(name);
                if (field != null)
                {
                    object value = field.GetValue(args);
                    return value?.ToString() ?? "";
                }

                return match.Value; // leave as-is if not found
            });

            return result;
        }
    }
}
