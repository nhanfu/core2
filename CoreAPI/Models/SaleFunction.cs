namespace CoreAPI.Models
{
    public class SaleFunction
    {
        public string Id { get; set; } // varchar(50), primary key
        public string Name { get; set; } // nvarchar(250), nullable
        public string Code { get; set; } // nvarchar(250), nullable
        public string Description { get; set; } // nvarchar(500), nullable
        public string Value { get; set; } // nvarchar(500), nullable
        public bool IsYes { get; set; } // bit, not null, with default of 0
        public bool Active { get; set; } // bit, not null, with default of 0
        public string InsertedBy { get; set; } // varchar(50), nullable
        public DateTime? InsertedDate { get; set; } // datetimeoffset(7), nullable
        public string UpdatedBy { get; set; } // varchar(50), nullable
        public DateTime? UpdatedDate { get; set; } // datetimeoffset(7), nullable
    }
}
