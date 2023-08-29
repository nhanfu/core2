namespace Core.ViewModels
{
    public class HotKeyModel
    {
        public int? Operator { get; set; }
        public string OperatorText { get; set; }
        public string Value { get; set; }
        public string FieldName { get; set; }
        public string ValueText { get; set; }
        public bool Shift { get; set; }
    }
}
