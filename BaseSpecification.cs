using IMatch.Common.Interface;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace IMatch.DataAccess.Base
{
    /// <summary>
    /// BaseSpecification
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BaseSpecification<T> : ISpecification<T>
    {
        #region Constructor
        public BaseSpecification()
        {
        }
        public BaseSpecification(Expression<Func<T, bool>> criteria)
        {
            Criteria = criteria;
        }
        #endregion

        #region Properties
        public Expression<Func<T, bool>> Criteria { get; }
        public List<Expression<Func<T, object>>> Includes { get; } = new List<Expression<Func<T, object>>>();
        public Expression<Func<T, object>> OrderBy { get; private set; }
        public Expression<Func<T, object>> OrderByDescending { get; private set; }

        #endregion

        #region Methods
        protected void AddInclude(Expression<Func<T, object>> includeExpression)
        {
            Includes.Add(includeExpression);
        }

        protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
        {
            OrderBy = orderByExpression;
        }

        protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
        {
            OrderByDescending = orderByDescExpression;
        }
        #endregion
    }
}
