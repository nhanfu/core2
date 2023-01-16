using System.ComponentModel;

namespace Core.Enums
{
    public enum ResponseApproveEnum
    {
        [Description("Bạn không có quyền thực hiện chức năng này")]
        NonRole = 414,
        [Description("Hãy phân quyền cho người nhận chức năng này")]
        NonUser = 415,
        [Description("Yêu cầu đã được duyệt")]
        Approved = 416,
        [Description("Thao tác thành công")]
        Success = 200,
        [Description("Đã có lỗi xảy ra trong quá trình xử lý")]
        Fail = 300,
    }

    public enum DebtStatusEnum
    {
        [Description("Đã duyệt")]
        Approved = 1,
        [Description("Tạo mới")]
        New = 2,
        [Description("Không duyệt")]
        Rejected = 3,
        [Description("Chờ duyệt")]
        Approving = 4,

        [Description("Đang thanh toán")]
        Paying = 5,
        [Description("Đã thanh toán")]
        Paid = 6,
        [Description("Nợ quá hạn")]
        Overdue = 7,
        [Description("Nợ xấu")]
        BadDebt = 8,
    }

    public enum ApprovalStatusEnum
    {
        [Description("Đã duyệt")]
        Approved = 1,
        [Description("Tạo mới")]
        New = 2,
        [Description("Không duyệt")]
        Rejected = 3,
        [Description("Chờ duyệt")]
        Approving = 4,
    }

    public enum EntityActionEnum
    {
        Create = 1,
        Update = 2,
        Deactivate = 3,
        Delete = 4,
    }

    public enum HttpMethod
    {
        GET,
        POST,
        PUT,
        PATCH,
        DELETE
    }

    public enum AuthVerEnum
    {
        None = 100,
        Simple = 0,
        OAuth1 = 1,
        OAuth2 = 2,
        ApiKey = 3
    }

    public enum PaybackPaymentTypeEnum
    {
        [Description("Sửa chữa")]
        TruckMaintenance = 1,
        [Description("Hoàn ứng")]
        AdvPayment = 2
    }

    public enum ReceiptStatusEnum
    {
        [Description("Đã duyệt")]
        Approved = 1,
        [Description("Tạo mới")]
        New = 2,
        [Description("Không duyệt")]
        Rejected = 3,
        [Description("Chờ duyệt")]
        Approving = 4,

        [Description("Hoàn thành")]
        Finished = 5,
    }

    public enum SystemType
    {
        EDO = 1,
        TMS = 2,
        CD = 3,
        FastDemo = 4,
        FastNew = 5,
        FastExsiting = 6,
    }
}
