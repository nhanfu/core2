using System.Reflection;
using System.Text;

namespace FTP.TMS
{
    public static partial class Utils
    {
        public const int SystemId = 1;
        public const string TenantField = "t";
        public const string Pixel = "px";
        public const string FeatureField = "f";
        public const string QuestionMark = "?";
        public const string Amp = "&";
        public const string ApplicationJson = "application/json";
        public const string Authorization = "Authorization";
        public const int SelfVendorId = 65;
        public const string AutoSaveReason = "Tự động cập nhật";
        public const string IdField = "Id";
        public const string NewLine = "\r\n";
        public const string Indent = "\t";
        public const string Dot = ".";
        public const string Comma = ",";
        public const string Semicolon = ";";
        public const string Space = " ";
        public const int ComponentId = 20;
        public const int ComponentGroupId = 30;
        public const int GridPolicyId = 2077;
        public const int HistoryId = 4199;
        public const string InsertedBy = "InsertedBy";
        public const string OwnerId = "OwnerId";

        public const string GOOGLE_MAP = "https://maps.googleapis.com/maps/api/js?key=AIzaSyCr_2PaKJplCyvwN4q78lBkX3UBpfZ_HsY";
        public const string GOOGLE_MAP_PLACES = "https://maps.googleapis.com/maps/api/js?key=AIzaSyBfVrTUFatsZTyqaCKwRzbj09DD72VxSwc&libraries=places";
        public const string GOOGLE_MAP_GEOMETRY = "https://maps.googleapis.com/maps/api/js?key=AIzaSyBfVrTUFatsZTyqaCKwRzbj09DD72VxSwc&libraries=geometry";
        public const string GOOGLE_MAP_WEEKLY = "https://maps.googleapis.com/maps/api/js?key=AIzaSyBfVrTUFatsZTyqaCKwRzbj09DD72VxSwc&libraries=&v=weekly";
        public const string GOOGLE_MAP_GEO_REQUEST = "https://maps.googleapis.com/maps/api/geocode/json?key=AIzaSyBfVrTUFatsZTyqaCKwRzbj09DD72VxSwc";
        public static Dictionary<char, string> SpecialChar = new Dictionary<char, string>()
        {
            { '+', "%2B" },
            { '/', "%2F" },
            { '?', "%3F" },
            { '#', "%23" },
            { '&', "%26" },
        };
        public static Dictionary<string, char> ReverseSpecialChar = SpecialChar.ToDictionary(x => x.Value, x => x.Key);
        public static string EncodeSpecialChar(this string str)
        {
            if (str is null)
            {
                return null;
            }

            var arr = str.ToCharArray();
            var res = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                if (SpecialChar.ContainsKey(arr[i]))
                {
                    res.Append(SpecialChar[arr[i]]);
                }
                else
                {
                    res.Append(arr[i]);
                }
            }
            return res.ToString();
        }

        public static object EncodeProperties(this object value)
        {
            PropertyInfo[] props = value?.GetType().GetProperties();
            foreach (var pi in props)
            {
                if (pi.PropertyType == typeof(string) && pi.CanWrite)
                {
                    var oldValue = pi.GetValue(value, null)?.ToString();
                    pi.SetValue(value, oldValue.EncodeSpecialChar(), null);
                }
            }
            return value;
        }

        public static object DecodeProperties(this object value)
        {
            PropertyInfo[] props = value?.GetType().GetProperties();
            foreach (var pi in props)
            {
                if (pi.PropertyType == typeof(string) && pi.CanWrite)
                {
                    var oldValue = pi.GetValue(value, null)?.ToString();
                    pi.SetValue(value, oldValue.DecodeSpecialChar(), null);
                }
            }
            return value;
        }

        public static string DecodeSpecialChar(this string str)
        {
            if (str is null)
            {
                return null;
            }

            var arr = str.ToCharArray();
            var res = new StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == '%' && i + 3 <= arr.Length && ReverseSpecialChar.ContainsKey(str.Substring(i, 3)))
                {
                    res.Append(ReverseSpecialChar[str.Substring(i, 3)]);
                    i += 2;
                }
                else
                {
                    res.Append(arr[i]);
                }
            }
            return res.ToString();
        }
    }
}
