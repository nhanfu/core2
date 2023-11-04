using Core.Models;
using System.Collections.Generic;

namespace TMS.API.ViewModels
{
    public enum SqlTypeEnum
    {
        Query,
        Update,
        Mixed,
        Json
    }

    public enum SqlStatusEnum
    {
        Success,
        Failed,
    }

    public class SqlViewModel
    {
        public string CmdId { get; set; }
        public string CmdType { get; set; }
        public string Modules { get; set; }
        public SqlTypeEnum QueryType { get; set; }
        public string Entity { get; set; }
        public Models.Component Component { get; set; }
        public string Select { get; set; } = "*";
        public string Where { get; set; }
        public string GroupBy { get; set; }
        public string Having { get; set; }
        public string OrderBy { get; set; }
        public string Paging { get; set; }
        public bool Count { get; set; }
    }

    public class SqlQueryResult
    {
        public string Query { get; set; }
        public string XQuery { get; set; }
        public string ConnectionStr { get; set; }
    }
}
