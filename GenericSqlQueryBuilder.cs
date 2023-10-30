using IMatch.Common.DTO;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;

namespace IMatch.DataAccess.Base
{
    /// <summary>
    /// GenericSqlQueryBuilder
    /// </summary>
    public class GenericSqlQueryBuilder
    {
        #region Private Fields
        private readonly StringBuilder queryBuilder;
        private readonly List<SqlParameter> parameters;
        private readonly List<CTE> ctes;
        private List<string> _columns;
        private readonly List<string> orderBys;
        private bool includeRowCount;
        private int pageSize;
        private int offset;
        private bool _includePagination;
        private StringBuilder fromClause;
        private StringBuilder whereClause;
        #endregion

        #region Constructor
        public GenericSqlQueryBuilder()
        {
            queryBuilder = new StringBuilder();
            parameters = new List<SqlParameter>();
            ctes = new List<CTE>();
            _columns = new List<string>();
            orderBys = new List<string>();
            whereClause = new StringBuilder();
            fromClause = new StringBuilder();
        } 
        #endregion

        public GenericSqlQueryBuilder Select(string columns = "*")
        {
            if (string.IsNullOrWhiteSpace(columns) || columns.Equals("*"))
                 return this;

            _columns = columns.Split(',').ToList();
            return this;
        }
        public GenericSqlQueryBuilder AddRowCount()
        {
            includeRowCount = true;
            return this;
        }
        public GenericSqlQueryBuilder AddColumn(string column)
        {
            if (_columns.Any(x=>x.Equals(column))) return this;

            _columns.Add(column);
            return this;
        }      
        public GenericSqlQueryBuilder From(string tableName)
        {
            fromClause.AppendLine($"FROM {tableName} ");
            return this;
        }
        public GenericSqlQueryBuilder Where(string condition)
        {
            whereClause.AppendLine($" WHERE {condition} ");
            return this;
        }
        public GenericSqlQueryBuilder And(string condition)
        {
            whereClause.Append($"AND {condition} ");
            return this;
        }
        public GenericSqlQueryBuilder Or(string condition)
        {
            whereClause.Append($"OR {condition} ");
            return this;
        }
        public GenericSqlQueryBuilder In(string column, string values)
        {
            whereClause.Append($"AND {column} IN ({values}) ");
            return this;
        }
        public GenericSqlQueryBuilder Between(string column, string minValue, string maxValue)
        {
            whereClause.Append($"AND {column} BETWEEN {minValue} AND {maxValue} ");
            return this;
        }
        public GenericSqlQueryBuilder Not(string condition)
        {
            whereClause.Append($"AND NOT ({condition}) ");
            return this;
        }
        public GenericSqlQueryBuilder NotIn(string column, string values)
        {
            whereClause.Append($"AND {column} NOT IN ({values}) ");
            return this;
        }
        public GenericSqlQueryBuilder GreaterThan(string column, string value)
        {
            whereClause.Append($"AND {column} > {value} ");
            return this;
        }
        public GenericSqlQueryBuilder GreaterThanEqualTo(string column, string value)
        {
            whereClause.Append($"AND {column} >= {value} ");
            return this;
        }
        public GenericSqlQueryBuilder LessThanEqualTo(string column, string value)
        {
            whereClause.Append($"AND {column} <= {value} ");
            return this;
        }
        public GenericSqlQueryBuilder EqualTo(string column, string value)
        {
            whereClause.Append($"AND {column} = {value} ");
            return this;
        }
        public GenericSqlQueryBuilder NotEqualTo(string column, string value)
        {
            whereClause.Append($"AND {column} <> {value} ");
            return this;
        }
        public GenericSqlQueryBuilder IsNull(string column)
        {
            whereClause.Append($"AND {column} IS NULL ");
            return this;
        }
        public GenericSqlQueryBuilder IsNotNull(string column)
        {
            whereClause.Append($"AND {column} IS NOT NULL ");
            return this;
        }
        public GenericSqlQueryBuilder OrderBy(string column, string direction = "ASC")
        {
            orderBys.Add($" {column} {direction} ");
            return this;
        }
        public GenericSqlQueryBuilder OrderByCustomWithCase(string condition, string defaultValue, string direction)
        {
            string orderExpression = $"CASE {condition} END";
            orderBys.Add($"{orderExpression} {direction}, {defaultValue} {direction}");
            return this;
        }
        public GenericSqlQueryBuilder Offset(int offset, int pageSize)
        {
            this.offset = offset;
            this.pageSize = pageSize;     
            _includePagination = true;
            return this;
        }
        public GenericSqlQueryBuilder AddParameter(string name, object value, SqlDbType type)
        {
            var parameter = new SqlParameter(name, type) { Value = value };
            parameters.Add(parameter);
            return this;
        }       
        public GenericSqlQueryBuilder WithCte(string cteName, string cteQuery)
        {
            ctes.Add(new CTE(cteName, cteQuery));
            return this;
        }      
        public void AddFilter(string condition, string parameterName, object parameterValue)
        {
            if (!string.IsNullOrEmpty(condition) && !string.IsNullOrEmpty(parameterName))
            {
                queryBuilder.Append($" {condition} @{parameterName}");
                parameters.Add(new SqlParameter(parameterName, parameterValue));
            }
        }
        public GenericSqlQueryBuilder Join(string tableName, string alias, string onCondition)
        {
            fromClause.Append($"JOIN {tableName} AS {alias} ON {onCondition} ");
            return this;
        }
        public GenericSqlQueryBuilder LeftJoin(string tableName, string alias, string onCondition)
        {
            fromClause.Append($"LEFT JOIN {tableName} AS {alias} ON {onCondition} ");
            return this;
        }
        public GenericSqlQueryBuilder RightJoin(string tableName, string alias, string onCondition)
        {
            fromClause.Append($"RIGHT JOIN {tableName} AS {alias} ON {onCondition} ");
            return this;
        }       
        public GenericSqlQueryBuilder LeftOuterJoin(string tableName, string alias, string onCondition)
        {
            fromClause.Append($"LEFT OUTER JOIN {tableName} AS {alias} ON {onCondition} ");
            return this;
        }
        public GenericSqlQueryBuilder RightOuterJoin(string tableName, string alias, string onCondition)
        {
            fromClause.Append($"RIGHT OUTER JOIN {tableName} AS {alias} ON {onCondition} ");
            return this;
        }
        public GenericSqlQueryBuilder GroupBy(string columns)
        {
            whereClause.AppendLine($"GROUP BY {columns} ");
            return this;
        }
        public GenericSqlQueryBuilder Having(string condition)
        {
            whereClause.AppendLine($"HAVING {condition} ");
            return this;
        }
        public SqlCommand Build(SqlConnection connection)
        {
            var query = PrepareQuery();
            var sqlCommand = new SqlCommand(query, connection);
            sqlCommand.Parameters.AddRange(parameters.ToArray());
            return sqlCommand;
        }
        public string BuildQuery()
        {
            return PrepareQuery();
        }

        private string PrepareQuery()
        {
            if (ctes.Count > 0)
            {
                var cteString = string.Join(", ", ctes.Select(cte => $"{cte.Name} AS ({cte.Query})"));
                queryBuilder.AppendLine($"WITH {cteString}");
            }
            queryBuilder.AppendLine("\rSELECT ");
            if (_columns.Count > 0)
            {
                for (int index = 0; index < _columns.Count; index++)
                {
                    if (index == 0)
                    {
                        queryBuilder.AppendLine($"\t{_columns[index]}");
                    }
                    else
                    {
                        queryBuilder.AppendLine($"\t,{_columns[index]}");
                    }
                }
            }
            if (includeRowCount)
            {
                queryBuilder.AppendLine(_columns.Count > 0 ? ", COUNT(*) OVER() AS TotalRowCount" : " *, COUNT(*) OVER() AS TotalRowCount");
            }

            if (!includeRowCount && _columns.Count == 0)
            {
                queryBuilder.AppendLine(" * ");
            }
            
            if (!string.IsNullOrWhiteSpace(fromClause.ToString()))
            {
                queryBuilder.AppendLine($" {fromClause.ToString()}");
            }
        
            if (!string.IsNullOrWhiteSpace(whereClause.ToString())) 
            {
                queryBuilder.AppendLine($"{whereClause.ToString()}");
            }
            if (orderBys.Count > 0)
            {
                queryBuilder.AppendLine("ORDER BY " + string.Join(", ", orderBys));
            }
            if (_includePagination)
            {
                queryBuilder.AppendLine($"OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY  OPTION (RECOMPILE)");
            }
            return $"{queryBuilder}";
        }

        #region Private class
        private class CTE
        {
            public string Name { get; }
            public string Query { get; }

            public CTE(string name, string query)
            {
                Name = name;
                Query = query;
            }
        } 

        #endregion
    }    
}
