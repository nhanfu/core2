using System.Collections.Generic;

namespace Core.ViewModels
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
    }

    public class SqlResult
    {
        public object DataSet { get; set; }
        public SqlStatusEnum Status { get; set; }
        public string Message { get; set; }
    }

    public class SqlQueryResult
    {
        public string Query { get; set; }
        public string Result { get; set; }
        public SqlTypeEnum SqlType { get; set; }
        public string Message { get; set; }
        public string System { get; set; }
        public string ConnectionStr { get; set; }
    }
}
