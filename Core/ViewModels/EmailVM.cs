using Core.Clients;
using System.Collections.Generic;

namespace Core.ViewModels
{
    public class EmailVM
    {
        public string ConnKey { get; set; } = Client.ConnKey;
        public string FromAddress { get; set; }
        public List<string> ToAddresses { get; set; }
        public List<string> CC { get; set; }
        public List<string> BCC { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public List<string> PdfText { get; set; } = new List<string>();
        public List<string> FileName { get; set; } = new List<string>();
        public ICollection<string> Attachements { get; set; } = new HashSet<string>();
#if NETCOREAPP
        public ICollection<string> ServerAttachements { get; set; } = new HashSet<string>();
#endif
    }
}
