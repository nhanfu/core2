using System;

namespace Core.ViewModels
{
    public class FileUpload
    {
        public string Id { get; set; }
        public string EntityName { get; set; }
        public string RecordId { get; set; }
        public string FieldName { get; set; }
        public string SectionId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string InsertedBy { get; set; }
        public DateTimeOffset InsertedDate { get; set; }
        public string UpdatedBy { get; set; }
        public DateTimeOffset? UpdatedDate { get; set; }
    }
}
