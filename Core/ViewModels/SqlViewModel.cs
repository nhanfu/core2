namespace Core.ViewModels
{
    public class SqlViewModel
    {
        public string SvcId { get; set; }
        public string ComId { get; set; }
        public string Action { get; set; }
        public string Entity { get; set; }
        public string[] Ids { get; set; }
        public string AnnonymousTenant { get; set; }
        public string Paging { get; set; }
        public string Select { get; set; }
        public string Where { get; set; }
        public string OrderBy { get; set; }
        public string GroupBy { get; set; }
        public string Having { get; set; }
        public bool Count { get; set; }
        public bool RawQuery { get; set; }
        public string[] FieldName { get; set; }
        public bool SkipXQuery { get; set; }
        public string ConnKey { get; internal set; }
        public string Table { get; set; }
    }

    public class SignedCom
    {
        public string CmdId { get; set; }
        public string Query { get; set; }
        public string Signed { get; set; }
    }
}
