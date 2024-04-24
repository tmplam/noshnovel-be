using System.Text.RegularExpressions;
using System.Text;

namespace NoshNovel.Plugins.Utilities
{
    public class HelperClass
    {
        public static string GenerateSlug(string str)
        {
            string normalizedString = str.Trim().Normalize(NormalizationForm.FormD);

            Regex regex = new Regex(@"\p{Mn}", RegexOptions.Compiled);
            string removedDiacritics = regex.Replace(normalizedString, string.Empty);

            string replacedSpaces = Regex.Replace(removedDiacritics, @"\s+", "-");

            string result = replacedSpaces.ToLower().Replace("đ", "d");

            return result;
        }
    }
}
