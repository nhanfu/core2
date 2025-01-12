using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ChatEntity
{
    public string Id { get; set; }

    public string Icon { get; set; }

    public string Avatar { get; set; }

    public string TableName { get; set; }

    public string Name { get; set; }

    public string RecordId { get; set; }

    public string FromId { get; set; }

    public string ToId { get; set; }

    public string TextContent { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
