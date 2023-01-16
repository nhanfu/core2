namespace Core.Components.Forms
{
    public class PopupEditor : TabEditor
    {
        public PopupEditor(string entity) : base(entity)
        {
            Popup = true;
            ParentElement = TabEditor?.Element;
            ShouldLoadEntity = false;
        }
    }
}
