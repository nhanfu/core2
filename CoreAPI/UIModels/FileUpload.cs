using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class FileUpload
{
    public string Id { get; set; }

    public string EntityName { get; set; }

    public string RecordId { get; set; }

    public string SectionId { get; set; }

    public string FieldName { get; set; }

    public string FileName { get; set; }

    public string FilePath { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
