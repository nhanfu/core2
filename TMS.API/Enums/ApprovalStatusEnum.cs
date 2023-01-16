using System.ComponentModel;

namespace TMS.API.Enums
{
    public enum ApprovalEnum
    {
        [Description("Approved")]
        Approved = 1,
        [Description("New")]
        New = 2,
        [Description("Rejected")]
        Rejected = 3,
        [Description("Approving")]
        Approving = 4,
    }
}
