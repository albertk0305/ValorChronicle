using System.Text.RegularExpressions;

namespace ValorChronicle.Core.IDs
{
    public static class ContentIdValidator
    {
        private static readonly Regex ValidIdPattern = new Regex(
            "^[a-z0-9]+(?:_[a-z0-9]+)*$",
            RegexOptions.CultureInvariant);

        public static bool TryValidate(string id, out string errorMessage)
        {
            if (string.IsNullOrEmpty(id))
            {
                errorMessage = "Content ID cannot be null or empty.";
                return false;
            }

            if (id != id.Trim())
            {
                errorMessage = "Content ID cannot have leading or trailing whitespace.";
                return false;
            }

            if (!ValidIdPattern.IsMatch(id))
            {
                errorMessage =
                    "Content ID must contain lowercase letters, digits, and single underscores only.";
                return false;
            }

            errorMessage = null;
            return true;
        }
    }
}
