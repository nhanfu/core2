using Bridge.Html5;
using Core.Models;

namespace Core.Components
{
    public class Textarea : Textbox
    {
        public Textarea(Component ui, HTMLElement ele = null) : base(ui, ele)
        {
            MultipleLine = true;
        }
    }

    public class Password : Textbox
    {
        public Password(Component ui, HTMLElement ele = null) : base(ui, ele)
        {
            Password = true;
        }
    }
}
