using Core.Extensions;

namespace Core.ViewModels
{
    public class EmailVM
    {
        public string ConnKey { get; set; } = Utils.ConnKey;
        public string FromAddress { get; set; }
        public List<string> ToAddresses { get; set; }
        public List<string> CC { get; set; }
        public List<string> BCC { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<string> PdfText { get; set; } = [];
        public List<string> FileName { get; set; } = [];
        public ICollection<string> Attachements { get; set; } = [];
        public ICollection<string> ServerAttachements { get; set; } = [];
    }
}
