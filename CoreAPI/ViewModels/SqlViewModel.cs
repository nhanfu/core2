using Core.Models;

namespace Core.ViewModels
{
    public enum SqlTypeEnum
    {
        Query,
        Update,
        Mixed,
        Json
    }

    public class SqlViewModel
    {
        public string SvcId { get; set; }
        public string ComId { get; set; }
        public string Action { get; set; }
        public SqlTypeEnum QueryType { get; set; }
        public string Entity { get; set; }
        public Component Component { get; set; }
        public string AnnonymousTenant { get; set; }
        public string Select { get; set; } = "*";
        public string Where { get; set; }
        public string GroupBy { get; set; }
        public string Having { get; set; }
        public string OrderBy { get; set; }
        public string Paging { get; set; }
        public bool Count { get; set; }
        public List<string> FieldName { get; set; }
    }

    public class SqlQueryResult
    {
        public string Query { get; set; }
        public string XQuery { get; set; }
    }
}
