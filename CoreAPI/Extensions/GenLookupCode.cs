namespace Core.Extensions
{
    public class GenLookupCode
    {
        public static string Gen(string prefix, int id)
        {
            var now = DateTime.Now;
            var result = $"{prefix}{Get3RandomCharacters()}{now:yyMM}{id:00000}";
            return result;
        }

        private static string Get3RandomCharacters()
        {
            var random = new Random();
            string result = string.Empty;

            for (int i = 1; i <= 3; i++)
            {
                result += (char)random.Next(65, 91);
            }
            return result;
        }
    }
}
