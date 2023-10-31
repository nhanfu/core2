using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class MasterData
    {
        public string Id { get; set; }

        public string TenantCode { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string ParentId { get; set; }

        public string Path { get; set; }

        public string Additional { get; set; }

        public int? Order { get; set; }

        public int? Enum { get; set; }

        public int Level { get; set; }

        public bool Active { get; set; }

        public DateTimeOffset InsertedDate { get; set; }

        public string InsertedBy { get; set; }

        public DateTimeOffset? UpdatedDate { get; set; }

        public string UpdatedBy { get; set; }

        public string InterDesc { get; set; }

        public string CostCenterId { get; set; }

        public string Code { get; set; }

        public int? Click { get; set; }

        public int? Length { get; set; }

        public string Additional2 { get; set; }

        public string ActId { get; set; }

        public string NameEnglish { get; set; }

        public string DescriptionEnglish { get; set; }

        public virtual ICollection<MasterData> InverseParent { get; set; } = new List<MasterData>();

        public virtual MasterData Parent { get; set; }
    }
}