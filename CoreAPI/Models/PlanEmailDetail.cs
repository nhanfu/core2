namespace CoreAPI.Models
{
    public class PlanEmailDetail
    {
        public string Id { get; set; }
        public string PlanEmailId { get; set; } // varchar(50), Checked
        public string TableName { get; set; } // nvarchar(250), Checked
        public string Email { get; set; } // nvarchar(250), Checked
        public string Template { get; set; } // nvarchar(250), Checked
        public string RecordId { get; set; } // varchar(50), Checked
        public DateTime? DailyDate { get; set; } // datetime2(7), Checked
        public DateTime? NextStartDate { get; set; } // datetime2(7), Checked
        public bool Active { get; set; } // bit, Unchecked
        public bool IsPause { get; set; } // bit, Unchecked
        public bool IsStart { get; set; } // bit, Unchecked
        public string InsertedBy { get; set; } // varchar(50), Checked
        public DateTime? InsertedDate { get; set; } // datetime2(7), Checked
        public string UpdatedBy { get; set; } // varchar(50), Checked
        public DateTime? UpdatedDate { get; set; } // datetime
    }
}
