using Core.Enums;
using System.Collections.Generic;
using System.Linq;

namespace Core.Extensions
{
    public class AdvOptionExt
    {
        public static Dictionary<AdvSearchOperation, string> OperationToSql = new Dictionary<AdvSearchOperation, string>
        {
            { AdvSearchOperation.Equal, "{0} = N'{1}'" },
            { AdvSearchOperation.NotEqual, "{0} != N'{1}'" },
            { AdvSearchOperation.GreaterThan, "{0} > N'{1}'" },
            { AdvSearchOperation.GreaterThanOrEqual, "{0} >= N'{1}'" },
            { AdvSearchOperation.LessThan, "{0} < N'{1}'" },
            { AdvSearchOperation.LessThanOrEqual, "{0} <= N'{1}'" },
            { AdvSearchOperation.Contains, "charindex({0}, N'{1}')" },
            { AdvSearchOperation.NotContains, "contains({0}, N'{1}') eq false" },
            { AdvSearchOperation.StartWith, "charindex({0}, N'{1}') = 1" },
            { AdvSearchOperation.NotStartWith, "charindex({0}, N'{1}') > 1" },
            { AdvSearchOperation.EndWidth, "{0} like N'%{1}')" },
            { AdvSearchOperation.NotEndWidth, "{0} not like N'%{1}'" },
            { AdvSearchOperation.In, "{0} in ({1})" },
            { AdvSearchOperation.Like, "{0} like N'%{1}%')" },
            { AdvSearchOperation.NotLike, "{0} not like N'{1}')" },
            { AdvSearchOperation.NotIn, "{0} not in ({1})" },
            { AdvSearchOperation.EqualDatime, "cast(date, {0}) = N'{1}'" },
            { AdvSearchOperation.NotEqualDatime, "cast(date, {0}) != N'{1}'" },
            { AdvSearchOperation.EqualNull, "{0} is null" },
            { AdvSearchOperation.NotEqualNull, "{0} is not null" },
            { AdvSearchOperation.GreaterThanDatime, "cast(date, {0}) > N'{1}'" },
            { AdvSearchOperation.GreaterEqualDatime, "cast(date, {0}) >= N'{1}'" },
            { AdvSearchOperation.LessThanDatime, "cast(date, {0}) < N'{1}'" },
            { AdvSearchOperation.LessEqualDatime, "cast(date, {0}) <= N'{1}'" },
        };
    }
}
