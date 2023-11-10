using Bridge.Html5;
using Core.Clients;
using Core.Extensions;
using Core.MVVM;
using System;
using System.Linq;

namespace Core.Components
{
    public enum Direction
    {
        top, right, bottom, left
    }

    public enum Position
    {
        absolute, @fixed, inherit, initial, relative, @static, sticky, unset
    }

    public static class Renderer
    {
        public static Html ColSpan(this Html html, int colSpan)
        {
            return html.Attr("colspan", colSpan.ToString());
        }

        public static Html RowSpan(this Html html, int colSpan)
        {
            return html.Attr("rowspan", colSpan.ToString());
        }

        public static Html Panel(this Html html, string text = string.Empty)
        {
            html.Div.Render();
            if (!string.IsNullOrEmpty(text))
            {
                html.Label.ClassName("header").IText(text).End.Render();
            }

            return html;
        }

        public static Html Button(this Html html, string text = string.Empty, string className = "button info small", string icon = string.Empty)
        {
            html.Button.Render();
            if (!string.IsNullOrEmpty(icon))
            {
                html.Span.ClassName(icon).End.Text(" ").Render();
            }
            return html.ClassName(className).IText(text);
        }

        public static Html SmallInput(this Html html, string value = string.Empty, string align = "left")
        {
            return html.Input.ClassName("input-small " + align).Attr("data-role", "input").Value(value);
        }

        public static Html PlaceHolder(this Html html, string langKey)
        {
            if (langKey.IsNullOrWhiteSpace())
            {
                return html;
            }
            MarkLangProp(html.GetContext(), langKey, "placeholder");

            return html.Attr("placeholder", LangSelect.Get(langKey));
        }

        private static void MarkLangProp(Node ctx, string langKey, string propName, params object[] parameters)
        {
            ctx[LangSelect.LangKey + propName] = langKey;
            if (parameters.HasElement())
            {
                ctx[LangSelect.LangParam + propName] = parameters;
            }

            var prop = ctx[LangSelect.LangProp];
            var newProp = prop == null ? propName : string.Concat(prop, ",", propName);
            ctx[LangSelect.LangProp] = newProp.Split(",").Distinct().Combine();
        }

        public static Html Title(this Html html, string langKey)
        {
            if (langKey.IsNullOrWhiteSpace())
            {
                return html;
            }
            MarkLangProp(html.GetContext(), langKey, "title");
            return html.Attr("title", LangSelect.Get(langKey));
        }

        public static Html SmallCheckbox(this Html html, bool value = false)
        {
            html.Label.ClassName("checkbox input-small transition-on style2")
                .Input.Attr("type", "checkbox").Type("checkbox").End
                .Span.ClassName("check myCheckbox");
            var chk = Html.Context.PreviousElementSibling as HTMLInputElement;
            chk.Checked = value;
            return html;
        }

        public static Html Disabled(this Html html, bool disabled)
        {
            if (disabled == false)
            {
                Html.Context.RemoveAttribute("disabled");
                return html;
            }
            return html.Attr("disabled", "disabled");
        }

        public static Html Margin(this Html html, Direction direction, double margin, string unit = "px")
        {
            return html.Style($"margin-{direction} : {margin}{unit}");
        }

        public static Html MarginRem(this Html html, Direction direction, double margin)
        {
            return html.Style($"margin-{direction} : {margin}rem");
        }

        public static Html Padding(this Html html, Direction direction, double padding, string unit = "px")
        {
            return html.Style($"padding-{direction} : {padding}{unit}");
        }

        public static Html Width(this Html html, string width)
        {
            return html.Style($"width: {width}");
        }

        /// <summary>
        /// Set sticky position to the Html Context
        /// </summary>
        /// <param name="html"></param>
        /// <param name="zIndex">Default is 1</param>
        /// <param name="top">Set top to 0 if it's aligned top with previous element</param>
        /// <param name="left">Set top to 0 if it's aligned left with previous element</param>
        /// <returns></returns>
        public static Html Sticky(this Html html, string top = null, string left = null)
        {
            var context = Html.Context;
            if (context is null)
            {
                return html;
            }
            if (context.PreviousElementSibling != null && context.GetType() == context.PreviousElementSibling.GetType())
            {
                if (left == 0.ToString())
                {
                    left = context.OffsetLeft + Utils.Pixel;
                }
                else if (top == 0.ToString())
                {
                    top = context.OffsetTop + Utils.Pixel;
                }
            }
            if (top != null)
            {
                html.Style($"top: {top};");
            }
            if (left != null)
            {
                html.Style($"left: {left};");
            }
            return html.Style("position: sticky; z-index: 1;");
        }

        public static Html TextAlign(this Html html, Enums.TextAlign? alignment = Enums.TextAlign.unset)
        {
            return html.Style("text-align: " + alignment.ToString());
        }

        public static Html Position(this Html html, Direction direction, double value)
        {
            return html.Style($"{direction.GetEnumDescription()}: {value}px");
        }

        public static Html Position(this Html html, Position position)
        {
            return html.Style($"position: {position.GetEnumDescription()}");
        }

        /// <summary>
        /// Render icon inside span, non auto-closing
        /// </summary>
        /// <param name="html"></param>
        /// <param name="icon"></param>
        /// <returns></returns>
        public static Html Icon(this Html html, string icon)
        {
            var isIconClass = icon.Contains("mif") || icon.Contains("fa") || icon.Contains("fa-");
            html.Span.ClassName("icon");
            if (isIconClass)
            {
                html.ClassName(icon).Render();
            }
            else
            {
                html.Style($"background-image: url({Client.Origin + icon});").ClassName("iconBg").Render();
            }

            return html;
        }

        public static Html Escape(this Html html, Action<Event> action)
        {
            var div = Html.Context;
            div.TabIndex = -1;
            div.Focus();
            div.AddEventListener(EventType.KeyDown, (e) =>
            {
                if (e["keyCode"].As<int?>() == 27)
                {
                    var parent = div.ParentElement;
                    e.StopPropagation();
                    action(e);
                    parent.Focus();
                }
            });
            return html;
        }

        public static Html IconForSpan(this Html html, string iconClass)
        {
            if (iconClass.IsNullOrWhiteSpace())
            {
                return html;
            }

            iconClass = iconClass.Trim();
            var span = Html.Context;
            html.ClassName("icon");
            var isIconClass = iconClass.Contains("mif") || iconClass.Contains("fa") || iconClass.Contains("fa-");
            if (isIconClass)
            {
                span.AddClass(iconClass);
            }
            else
            {
                span.AddClass("iconBg");
                span.Style["background-image"] = "url(" + iconClass + ")";
            }
            return html;
        }

        public static Html Floating(this Html html, double top, double left)
        {
            return html.Position(Components.Position.@fixed)
                .Position(Direction.top, top)
                .Position(Direction.left, left);
        }

        public static Html TabIndex(this Html html, int tabIndex)
        {
            return html.Attr("tabindex", tabIndex.ToString());
        }

        public static Html ClassName(this Html html, string className)
        {
            if (string.IsNullOrEmpty(className))
            {
                return html;
            }
            var ctx = Html.Context;
            var translated = LangSelect.Get(className);
            MarkLangProp(ctx, className, nameof(HTMLElement.ClassName));
            var res = ctx.ClassName + " " + translated;
            ctx.ClassName = res.Trim();
            return html;
        }

        public static Html IHtml(this Html html, string langKey, params object[] parameters)
        {
            if (string.IsNullOrEmpty(langKey))
            {
                return html;
            }
            var ctx = Html.Context;
            var translated = LangSelect.Get(langKey);
            MarkLangProp(ctx, langKey, nameof(HTMLElement.InnerHTML), parameters);
            ctx.InnerHTML = translated;
            return html;
        }

        public static Html IText(this Html html, string langKey, params object[] parameters)
        {
            if (string.IsNullOrEmpty(langKey))
            {
                return html;
            }
            var translated = LangSelect.Get(langKey);
            var textNode = new Text(parameters.HasElement() ? string.Format(translated, parameters) : translated);
            MarkLangProp(textNode, langKey, nameof(Text.TextContent), parameters);
            html.GetContext().AppendChild(textNode);
            return html;
        }
    }
}
