using System.Collections.Generic;
using System.Linq;

namespace Core.Extensions
{
    public static class OdataExt
    {
        public const string TopKeyword = "$top=";
        public const string FilterKeyword = "$filter=";
        public const string OrderByKeyword = "$orderby=";
        private const string QuestionMark = "?";

        public static string RemoveClause(string DataSourceFilter, string clauseType = FilterKeyword, bool removeKeyword = false)
        {
            if (DataSourceFilter.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var noClauseQuery = DataSourceFilter;
            var clauseIndex = DataSourceFilter.LastIndexOf(clauseType);

            if (clauseIndex >= 0)
            {
                var fromFilter = DataSourceFilter.Substring(clauseIndex);
                var endClauseIndex = fromFilter.IndexOf("&");
                endClauseIndex = endClauseIndex == -1 ? fromFilter.Length : endClauseIndex;
                noClauseQuery = DataSourceFilter.Substring(0, clauseIndex) +
                    fromFilter.Substring(endClauseIndex, fromFilter.Length);
            }
            var endChar = noClauseQuery[noClauseQuery.Length - 1];
            if (noClauseQuery.Length > 0 && endChar == '&' || endChar == '?')
            {
                noClauseQuery = noClauseQuery.Substring(0, noClauseQuery.Length - 1);
            }
            return removeKeyword ? noClauseQuery.Replace(clauseType, "") : noClauseQuery;
        }

        public static string GetClausePart(string DataSourceFilter, string clauseKeyword = FilterKeyword)
        {
            var clauseIndex = DataSourceFilter.LastIndexOf(clauseKeyword);
            if (clauseIndex >= 0)
            {
                var clause = DataSourceFilter.Substring(clauseIndex);
                var endClauseIndex = clause.IndexOf("&");
                endClauseIndex = endClauseIndex == -1 ? clause.Length : endClauseIndex;
                return clause.Substring(clauseKeyword.Length, endClauseIndex - clauseKeyword.Length).Trim();
            }
            return string.Empty;
        }

        public static string GetOrderByPart(string dataSourceFilter)
        {
            var filterIndex = dataSourceFilter.LastIndexOf(OrderByKeyword);
            if (filterIndex >= 0)
            {
                var filter = dataSourceFilter.Substring(filterIndex);
                var endFilterIndex = filter.IndexOf("&");
                endFilterIndex = endFilterIndex == -1 ? filter.Length : endFilterIndex;
                return filter.Substring(OrderByKeyword.Length, endFilterIndex - OrderByKeyword.Length).Trim();
            }
            return string.Empty;
        }

        public static string AppendClause(string DataSourceFilter, string clauseValue, string clauseKeyword = FilterKeyword)
        {
            if (clauseValue.IsNullOrWhiteSpace())
            {
                return DataSourceFilter;
            }

            if (DataSourceFilter.IsNullOrWhiteSpace())
            {
                DataSourceFilter = string.Empty;
            }

            if (!DataSourceFilter.Contains(QuestionMark))
            {
                DataSourceFilter += QuestionMark;
            }

            var originalFilter = GetClausePart(DataSourceFilter, clauseKeyword);
            int index;
            if (originalFilter.IsNullOrEmpty())
            {
                DataSourceFilter += DataSourceFilter.IndexOf("?") < 0 ? clauseKeyword : "&"  + clauseKeyword;
                index = DataSourceFilter.Length;
            }
            else
            {
                index = DataSourceFilter.IndexOf(originalFilter) + originalFilter.Length;
            }
            var finalStatement = DataSourceFilter.Substring(0, index) + clauseValue + DataSourceFilter.Substring(index);
            return finalStatement;
        }

        public static string ApplyClause(string DataSourceFilter, string clauseValue, string clauseKeyword = FilterKeyword)
        {
            var statement = RemoveClause(DataSourceFilter, clauseKeyword, true);
            return AppendClause(statement, clauseValue, clauseKeyword);
        }
    }
}
