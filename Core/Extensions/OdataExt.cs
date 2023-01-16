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

        public static string RemoveClause(string dataSource, string clauseType = FilterKeyword, bool removeKeyword = false)
        {
            if (dataSource.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            var noClauseQuery = dataSource;
            var clauseIndex = dataSource.LastIndexOf(clauseType);

            if (clauseIndex >= 0)
            {
                var fromFilter = dataSource.Substring(clauseIndex);
                var endClauseIndex = fromFilter.IndexOf("&");
                endClauseIndex = endClauseIndex == -1 ? fromFilter.Length : endClauseIndex;
                noClauseQuery = dataSource.Substring(0, clauseIndex) +
                    fromFilter.Substring(endClauseIndex, fromFilter.Length);
            }
            var endChar = noClauseQuery[noClauseQuery.Length - 1];
            if (noClauseQuery.Length > 0 && endChar == '&' || endChar == '?')
            {
                noClauseQuery = noClauseQuery.Substring(0, noClauseQuery.Length - 1);
            }
            return removeKeyword ? noClauseQuery.Replace(clauseType, "") : noClauseQuery;
        }

        public static string GetClausePart(string dataSource, string clauseKeyword = FilterKeyword)
        {
            var clauseIndex = dataSource.LastIndexOf(clauseKeyword);
            if (clauseIndex >= 0)
            {
                var clause = dataSource.Substring(clauseIndex);
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

        public static string AppendClause(string datasource, string clauseValue, string clauseKeyword = FilterKeyword)
        {
            if (clauseValue.IsNullOrWhiteSpace())
            {
                return datasource;
            }

            if (datasource.IsNullOrWhiteSpace())
            {
                datasource = string.Empty;
            }

            if (!datasource.Contains(QuestionMark))
            {
                datasource += QuestionMark;
            }

            var originalFilter = GetClausePart(datasource, clauseKeyword);
            int index;
            if (originalFilter.IsNullOrEmpty())
            {
                datasource += datasource.IndexOf("?") < 0 ? clauseKeyword : "&"  + clauseKeyword;
                index = datasource.Length;
            }
            else
            {
                index = datasource.IndexOf(originalFilter) + originalFilter.Length;
            }
            var finalStatement = datasource.Substring(0, index) + clauseValue + datasource.Substring(index);
            return finalStatement;
        }

        public static string ApplyClause(string dataSource, string clauseValue, string clauseKeyword = FilterKeyword)
        {
            var statement = RemoveClause(dataSource, clauseKeyword, true);
            return AppendClause(statement, clauseValue, clauseKeyword);
        }
    }
}
