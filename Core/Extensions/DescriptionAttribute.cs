namespace System.ComponentModel
{
    public sealed class DescriptionAttribute : Attribute
    {
        public string Description { get; set; }
        public DescriptionAttribute(string description)
        {
            Description = description;
        }
    }
}
