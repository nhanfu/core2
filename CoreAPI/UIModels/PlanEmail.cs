using System;
using System.Collections.Generic;

namespace CoreAPI.UIModels;

public partial class PlanEmail
{
    public string Id { get; set; }

    public string ComponentId { get; set; }

    public string FromName { get; set; }

    public string FromEmail { get; set; }

    public string PassEmail { get; set; }

    public string ToEmail { get; set; }

    public string SubjectMail { get; set; }

    public string FeatureId { get; set; }

    public DateTime? DailyDate { get; set; }

    public int? ReminderSettingId { get; set; }

    public int? NotificationNumber { get; set; }

    public string UserId { get; set; }

    public string Name { get; set; }

    public string Template { get; set; }

    public DateTime? LastNotificationDate { get; set; }

    public bool IsCompany { get; set; }

    public bool IsPause { get; set; }

    public bool IsStart { get; set; }

    public bool IsToId { get; set; }

    public bool Active { get; set; }

    public string InsertedBy { get; set; }

    public DateTime? InsertedDate { get; set; }

    public string UpdatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? LastStartDate { get; set; }

    public DateTime? NextStartDate { get; set; }

    public string EmailFieldId { get; set; }

    public string Field1Id { get; set; }

    public string Value1 { get; set; }

    public string Field2Id { get; set; }

    public string Value2 { get; set; }

    public string Field3Id { get; set; }

    public string Value3 { get; set; }
}
