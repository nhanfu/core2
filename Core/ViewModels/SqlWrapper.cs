using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.ViewModels
{
    public class SqlWrapper
    {
        public string Entity { get; set; }
        public SignedCom Component { get; set; }
        public string Paging { get; set; }
        public string Where { get; set; }
        public string OrderBy { get; set; }
        public string GroupBy { get; set; }
        public string Having { get; set; }
        public bool Count { get; set; }
    }

    public class SignedCom
    {
        public string Query { get; set; }
        public string Signed { get; set; }
    }
}
