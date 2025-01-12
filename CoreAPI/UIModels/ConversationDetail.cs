using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ConversationDetail
{
    public string Id { get; set; }

    public string ConversationId { get; set; }

    public string FromName { get; set; }

    public string FromId { get; set; }

    public string Avatar { get; set; }

    public string Message { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
