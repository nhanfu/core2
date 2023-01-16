using Core.Enums;
using System.Collections.Generic;

namespace Core.Extensions
{
    public class AdvOptionExt
    {
        public static Dictionary<AdvSearchOperation, string> OperationToOdata = new Dictionary<AdvSearchOperation, string>
        {
            { AdvSearchOperation.Equal, "{0} eq {1}" },
            { AdvSearchOperation.NotEqual, "{0} ne {1}" },
            { AdvSearchOperation.GreaterThan, "{0} gt {1}" },
            { AdvSearchOperation.GreaterThanOrEqual, "{0} ge {1}" },
            { AdvSearchOperation.LessThan, "{0} lt {1}" },
            { AdvSearchOperation.LessThanOrEqual, "{0} le {1}" },
            { AdvSearchOperation.Contains, "contains({0}, {1})" },
            { AdvSearchOperation.NotContains, "contains({0}, {1}) eq false" },
            { AdvSearchOperation.StartWith, "startswith({0}, {1})" },
            { AdvSearchOperation.NotStartWith, "indexof({0}, {1}) ne 0" },
            { AdvSearchOperation.EndWidth, "endswith({0}, {1})" },
            { AdvSearchOperation.NotEndWidth, "endswith({0}, {1}) eq false" },
            { AdvSearchOperation.In, "{0} in ({1})" },
            { AdvSearchOperation.Like, "contains({0},{1})" },
            { AdvSearchOperation.NotLike, "contains({0},{1}) eq false" },
            { AdvSearchOperation.NotIn, "{0} in ({1}) eq false" },
            { AdvSearchOperation.EqualDatime, "cast({0},Edm.DateTimeOffset) eq {1}" },
            { AdvSearchOperation.NotEqualDatime, "cast({0},Edm.DateTimeOffset) ne {1}" },
            { AdvSearchOperation.EqualNull, "{0} eq null" },
            { AdvSearchOperation.NotEqualNull, "{0} ne null" },
            { AdvSearchOperation.GreaterThanDatime, "cast({0},Edm.DateTimeOffset) gt {1}" },
            { AdvSearchOperation.GreaterEqualDatime, "cast({0},Edm.DateTimeOffset) ge {1}" },
            { AdvSearchOperation.LessThanDatime, "cast({0},Edm.DateTimeOffset) lt {1}" },
            { AdvSearchOperation.LessEqualDatime, "cast({0},Edm.DateTimeOffset) le {1}" },
        };
    }
}
