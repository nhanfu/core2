using System;
using System.Collections.Generic;

namespace TMS.API.Models
{
    public partial class FileUpload
    {
        public int Id { get; set; }
        public int SectionId { get; set; }
        public int RecordId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int InsertedBy { get; set; }
        public DateTime InsertedDate { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string FieldName { get; set; }
        public string EntityName { get; set; }
    }
}
