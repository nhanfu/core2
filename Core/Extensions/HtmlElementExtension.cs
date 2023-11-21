using Bridge.Html5;
using System;
using System.Collections.Generic;

namespace Core.Extensions
{
    public class OutOfViewPort
    {
        public bool Top { get; set; }
        public bool Left { get; set; }
        public bool Right { get; set; }
        public bool Bottom { get; set; }
        public bool Any { get; set; }
        public bool All { get; set; }
    }

    public static class HtmlElementExtension
    {
        public static bool HasClass(this Node node, string className)
        {
            if (node is null)
            {
                throw new InvalidOperationException($"{nameof(node)} is null");
            }

            if (!className.HasAnyChar())
            {
                return false;
            }

            var ele = node as Element;
            return ele.ClassName.Contains(className);
        }

        public static void ReplaceClass(this Node node, string oldClass, string newClass)
        {
            if (string.IsNullOrEmpty(oldClass))
            {
                return;
            }

            var element = node as Element;
            element.ClassName = element.ClassName.Replace(oldClass, newClass)
                .Trim().Replace(new RegExp(@"\s+"), " ");
        }

        public static void AddClass(this Node node, string className)
        {
            if (node is null || string.IsNullOrEmpty(className) || HasClass(node, className))
            {
                return;
            }

            var element = node as Element;
            element.ClassName = (element.ClassName + " " + className).Trim();
        }

        public static HTMLElement Closest(this HTMLElement node, string selector)
        {
            if (node is null || string.IsNullOrEmpty(selector))
            {
                return null;
            }
            var func = node["closest"] as Func<string, HTMLElement>;
            return (HTMLElement)func.Call(node, selector);
        }

        public static void Prepend(this HTMLElement node, HTMLElement child)
        {
            if (node is null )
            {
                return;
            }
            var func = node["prepend"] as Action<HTMLElement>;
            func.Call(node, child);
        }

        public static double GetFullHeight(this HTMLElement node)
        {
            if (node is null)
            {
                return 0;
            }

            var style = Window.GetComputedStyle(node);
            var topPx = style.MarginTop.HasAnyChar() ? style.MarginTop.Replace(Utils.Pixel, string.Empty) : 0.ToString();
            var bottomPx = style.MarginTop.HasAnyChar() ? style.MarginBottom.Replace(Utils.Pixel, string.Empty) : 0.ToString();
            var marginHeight = Convert.ToDouble(topPx) + Convert.ToDouble(bottomPx);
            return node.ScrollHeight + marginHeight;
        }

        public static void RemoveClass(this Node node, string className)
        {
            if (node is null || string.IsNullOrEmpty(className))
            {
                return;
            }
            node.ReplaceClass(className, string.Empty);
        }

        public static void ToggleClass(this Node node, string className)
        {
            if (node is null || string.IsNullOrEmpty(className))
            {
                return;
            }

            var hasClass = (node as HTMLElement).ClassName.Contains(className);
            if (hasClass)
            {
                RemoveClass(node, className);
            }
            else
            {
                AddClass(node, className);
            }
        }

        public static void Show(this HTMLElement ele)
        {
            ele.Style.Display = string.Empty;
        }

        public static CSSStyleDeclaration GetComputedStyle(this HTMLElement ele)
        {
            return Window.GetComputedStyle(ele);
        }

        public static void Hide(this HTMLElement ele)
        {
            ele.Style.Display = Display.None;
        }

        public static bool Hidden(this HTMLElement ele)
        {
            if (ele is null)
            {
                return true;
            }

            var x = ele.GetBoundingClientRect();
            var style = Window.GetComputedStyle(ele);
            return style.Display.ToString() == "none" || x.Bottom == 0 && x.Top == 0 && x.Width == 0 && x.Height == 0;
        }

        public static OutOfViewPort OutOfViewport(this HTMLElement elem)
        {
            // Get element's bounding
            var bounding = elem.GetBoundingClientRect();

            // Check if it's out of the viewport on each side
            var res = new OutOfViewPort
            {
                Top = bounding.Top < 0,
                Left = bounding.Left < 0,
                Bottom = bounding.Bottom > Window.InnerHeight,
                Right = bounding.Right > Window.InnerWidth
            };
            res.Any = res.Top || res.Left || res.Bottom || res.Right;
            res.All = res.Top && res.Left && res.Bottom && res.Right;

            return res;
        }

        public static IEnumerable<HTMLElement> FilterElement(this HTMLElement ele, Func<HTMLElement, bool> predicate, HashSet<HTMLElement> visited = null)
        {
            if (ele is null)
            {
                yield break;
            }
            if (visited == null)
            {
                visited = new HashSet<HTMLElement>();
            }
            if (visited.Contains(ele))
            {
                yield break;
            }
            visited.Add(ele);
            if (predicate == null || predicate(ele))
            {
                yield return ele;
            }
            foreach (var child in ele.Children)
            {
                foreach (var match in FilterElement(child, predicate, visited))
                {
                    yield return match;
                }

            }
        }
    }
}
