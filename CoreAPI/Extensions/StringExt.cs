using System.Text.RegularExpressions;

namespace Core.Extensions
{
    public static class StringExt
    {
        public static bool IsNullOrEmpty(this string value)
        {
            return string.IsNullOrEmpty(value);
        }

        public static bool IsNullOrWhiteSpace(this string value)
        {
            return string.IsNullOrWhiteSpace(value) || value == "null";
        }

        public static bool HasAnyChar(this string value)
        {
            return value != null && !string.IsNullOrEmpty(value);
        }

        public static bool HasNonSpaceChar(this string value)
        {
            return value != null && !string.IsNullOrWhiteSpace(value);
        }

        public static int CountChar(this string word, char countableLetter)
        {
            int count = 0;
            foreach (char c in word)
            {
                if (countableLetter == c)
                {
                    count++;
                }
            }
            return count;
        }

        public static bool IsMatch(this string text, string regText)
        {
            var regEx = new Regex(regText);
            return regEx.IsMatch(text);
        }

        static readonly Regex trimmer = new Regex(@"\s\s+");

        public static string TrimAndRemoveWhiteSpace(this string text)
        {
            if (text == null)
            {
                return null;
            }

            return trimmer.Replace(text.Trim(), " ");
        }

        public static string ToLower(this bool val)
        {
            return val.ToString().ToLower();
        }

        public static string SubStrIndex(this string value, int startIndex)
        {
            if (value is null)
            {
                return value;
            }
            if (startIndex < 0 || startIndex > value.Length - 1)
            {
                startIndex = 0;
            }
            return value.Substring(startIndex, value.Length - startIndex);
        }

        public static string SubStrIndex(this string value, int startIndex, int endIndex)
        {
            if (value is null)
            {
                return value;
            }
            if (startIndex < 0 || startIndex > value.Length - 1)
            {
                startIndex = 0;
            }
            if (endIndex < 0 || endIndex > value.Length - 1)
            {
                endIndex = value.Length - 1;
            }
            return value.Substring(startIndex, endIndex - startIndex);
        }
    }
}
