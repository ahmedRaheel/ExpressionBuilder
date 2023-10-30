using IMatch.Common.DTO;
using System;
using System.Linq.Expressions;

namespace IMatch.DataAccess.Base
{
    public class FilterBuilder<TEntity>
    {
        private ParameterExpression parameter;
        private Expression currentExpression;

        public FilterBuilder()
        {
            parameter = Expression.Parameter(typeof(TEntity), "entity");
            currentExpression = null;
        }

        public FilterBuilder<TEntity> Where(Expression<Func<TEntity, bool>> predicate)
        {
            var lambda = predicate as LambdaExpression;
            var body = lambda.Body;

            if (currentExpression == null)
            {
                currentExpression = body;
            }
            else
            {
                var andAlso = Expression.AndAlso(currentExpression, body);
                currentExpression = andAlso;
            }

            return this;
        }

        public Expression<Func<TEntity, bool>> Build()
        {
            if (currentExpression == null)
            {
                return null;
            }

            var lambda = Expression.Lambda<Func<TEntity, bool>>(currentExpression, parameter);
            return lambda;
        }
    }
}
