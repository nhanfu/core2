using System.ComponentModel;

namespace Core.Enums
{
    public enum RoleEnum
    {
        [Description("System")]
        System = 8,
    }

    public enum TaskStateEnum
    {
        UnreadStatus = 339,
        Read = 340,
        Processing = 341,
        Proceeded = 342
    }

    public enum ComponentTypeTypeEnum
    {
        Dropdown = 1,
        SearchEntry = 1,
        MultipleSearchEntry = 1,
        Datepicker = 2,
        Number = 3,
        Textbox = 4,
        Checkbox = 5,
    }

    public enum ActiveStateEnum
    {
        [Description("Tất cả trạng thái")]
        All = 2,
        [Description("Hiệu lực")]
        Yes = 1,
        [Description("Không hiệu lực")]
        No = 0,
    }

    public enum AdvSearchOperation
    {
        [Description("=")]
        Equal = 1,
        [Description("<>")]
        NotEqual = 2,
        [Description(">")]
        GreaterThan = 3,
        [Description(">=")]
        GreaterThanOrEqual = 4,
        [Description("<")]
        LessThan = 5,
        [Description("<=")]
        LessThanOrEqual = 6,
        [Description("chứa")]
        Contains = 7,
        [Description("không chứa")]
        NotContains = 8,
        [Description("bắt đầu bằng")]
        StartWith = 9,
        [Description("không bắt đầu bằng")]
        NotStartWith = 10,
        [Description("Kết thúc bằng")]
        EndWidth = 11,
        [Description("Không kết thúc bằng")]
        NotEndWidth = 12,
        [Description("Trong tập")]
        In = 13,
        [Description("Khác")]
        NotIn = 14,
        [Description("= Ngày")]
        EqualDatime = 15,
        [Description("> Ngày")]
        GreaterThanDatime = 21,
        [Description("< Ngày")]
        LessThanDatime = 22,
        [Description("<> Ngày")]
        NotEqualDatime = 16,
        [Description("= null")]
        EqualNull = 17,
        [Description("<> null")]
        NotEqualNull = 18,
        Like = 19,
        NotLike = 20,
        [Description(">= Ngày")]
        GreaterEqualDatime = 23,
        [Description("<= Ngày")]
        LessEqualDatime = 24,
    }

    public enum LogicOperation
    {
        [Description("Và")]
        And = 0,
        [Description("Hoặc")]
        Or = 1,
    }

    public enum OrderbyDirection
    {
        [Description("Tăng dần")]
        ASC = 1,
        [Description("Giảm dần")]
        DESC = 2,
    }

    public enum RoleSelection
    {
        [Description("Top first")]
        TopFirst = 1,
        [Description("Bottom first")]
        BottomFirst = 2,
    }
}
