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

    public enum HttpMethod
    {
        GET,
        POST,
        PUT,
        PATCH,
        DELETE
    }
}
