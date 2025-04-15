using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class ConversationRead
{
    public string Id { get; set; }

    public string ConversationId { get; set; }

    public string UserId { get; set; }

    public bool Read { get; set; }

    public bool Active { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string UpdatedBy { get; set; }
}
