using Bridge.Html5;
using Core.Enums;
using System;

namespace Core.Extensions
{
    public static class EventExt
    {
        public static float Top(this Event e) => (float)e["clientY"];
        public static float Left(this Event e) => (float)e["clientX"];
        public static int KeyCode(this Event e) => (int?)e["keyCode"] ?? -1;
        public static KeyCodeEnum? KeyCodeEnum(this Event e)
        {
            if (e["keyCode"] == null)
            {
                return null;
            }
            var parsed = Enum.TryParse(e["keyCode"].ToString().ToUpper(), out KeyCodeEnum res);
            return parsed ? (KeyCodeEnum?)res : null;
        }
        public static bool ShiftKey(this Event e) => (bool)e["shiftKey"];
        /// <summary>
        /// Detect if the user press Ctrl or Command key while the event occurs
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public static bool CtrlOrMetaKey(this Event e) => (bool)e["ctrlKey"] || (bool)e["metaKey"];
        public static bool AltKey(this Event e) => (bool)e["altKey"];
    }
}
