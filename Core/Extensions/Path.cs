using Core.Extensions;
using System.Linq;

namespace System.IO
{
    public static class Path
    {
        public static string[] ImgExt = new string[] { "tif", "pjp", "xbm", "jxl", "svgz", "jpeg", "ico", "tiff", "gif", "svg", "jfif", "webp", "png", "bmp", "pjpeg", "avif", "jpg" };
        public static bool IsImage(string path)
        {
            var isImage = ImgExt.Contains(GetExtension(path).ToLower().SubStrIndex(1));
            return isImage;
        }

        public static string CombineHostAndPath(string host, string path)
        {
            var containHost = ContainHost(path);
            return containHost ? path : Combine(host, path);
        }

        public static bool ContainHost(string path)
        {
            return path.Contains("http://") || path.Contains("https://");
        }

        /// <summary>
        /// Get file's extension including "."
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            return path.Substring(path.LastIndexOf("."));
        }

        public static string GetFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            var lastSlash = path.LastIndexOf("/");
            return lastSlash >= 0 ? path.Substring(lastSlash + 1) : path;
        }

        public static string GetFileNameWithoutExtension(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }
            var lastSlashIndex = path.LastIndexOf("/");
            if (lastSlashIndex < 0)
            {
                lastSlashIndex = 0;
            }
            var lastDotIndex = path.LastIndexOf(".");
            return path.Substring(lastSlashIndex + 1, lastDotIndex - 1 - (lastSlashIndex >= 0 ? lastSlashIndex : 0));
        }

        public static string Combine(params string[] path)
        {
            if (path is null || path.Length == 0)
            {
                return string.Empty;
            }
            var nonEmptyPath = path.Where(x => !string.IsNullOrEmpty(x)).Select(x =>
            {
                var heading = x[0] == '/' ? 1 : 0;
                var traling = x[x.Length - 1] == '/' ? x.Length - 1 : x.Length;
                return x.Substring(heading, traling - heading);
            }).ToArray();
            return string.Join("/", nonEmptyPath);
        }
    }
}
