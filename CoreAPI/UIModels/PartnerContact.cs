using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class PartnerContact
{
    public string Id { get; set; }

    public string GenderId { get; set; }

    public string ContactName { get; set; }

    public string ContactPhoneNumber { get; set; }

    public string ContactEmail { get; set; }

    public string JobTitles { get; set; }

    public DateTime? Birthday { get; set; }

    public string Note { get; set; }

    public string PartnerId { get; set; }

    public bool IsMain { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Partner Partner { get; set; }
}
