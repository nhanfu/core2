using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class MasterData
    {
        public MasterData()
        {
            InverseParent = new HashSet<MasterData>();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ParentId { get; set; }
        public string Path { get; set; }
        public string Additional { get; set; }
        public string Order { get; set; }
        public string Enum { get; set; }
        public string Level { get; set; }
        public bool Active { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
        public string UpdatedBy { get; set; }
        public string InterDesc { get; set; }
        public string CostCenterId { get; set; }
        public string Code { get; set; }
        public string Click { get; set; }
        public string Length { get; set; }
        public string Additional2 { get; set; }
        public string ActId { get; set; }
        public string NameEnglish { get; set; }
        public string DescriptionEnglish { get; set; }

        public virtual MasterData Parent { get; set; }
        public virtual ICollection<MasterData> InverseParent { get; set; }
    }
}
