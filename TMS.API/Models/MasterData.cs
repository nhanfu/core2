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

        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int? ParentId { get; set; }
        public string Path { get; set; }
        public string Additional { get; set; }
        public int? Order { get; set; }
        public int? Enum { get; set; }
        public int Level { get; set; }
        public bool Active { get; set; }
        public DateTime InsertedDate { get; set; }
        public int InsertedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public string InterDesc { get; set; }
        public int? CostCenterId { get; set; }
        public string Code { get; set; }
        public int? Click { get; set; }
        public int? Length { get; set; }
        public string Additional2 { get; set; }

        public virtual MasterData Parent { get; set; }
        public virtual ICollection<MasterData> InverseParent { get; set; }
    }
}
