using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace IMatch.DataAccess.Base
{
    //public static class ExpressionExtensions
    //{
    //    public static string ToSqlFilter<TEntity>(this Expression<Func<TEntity, bool>> predicate)
    //    {
    //        var body = predicate.Body;
    //        if (body is BinaryExpression binary)
    //        {
    //            var left = Visit(binary.Left);
    //            var right = Visit(binary.Right);
    //            var operation = GetSqlOperator(binary.NodeType);
    //            return $"{left} {operation} {right}";
    //        }
    //        throw new NotSupportedException("Expression type not supported: " + body.NodeType);
    //    }

    //    private static string Visit(Expression expression)
    //    {
    //        if (expression is MemberExpression member)
    //        {
    //            return member.Member.Name;
    //        }
    //        if (expression is ConstantExpression constant)
    //        {
    //            return GetValue(constant.Value);
    //        }
    //        if (expression is BinaryExpression binary)
    //        {
    //            var left = Visit(binary.Left);
    //            var right = Visit(binary.Right);
    //            var operation = GetSqlOperator(binary.NodeType);
    //            return $"({left} {operation} {right})";
    //        }
    //        throw new NotSupportedException("Expression type not supported: " + expression.NodeType);
    //    }

    //    private static string GetValue(object value)
    //    {
    //        // Handle different value types (e.g., strings, numbers, dates) appropriately.
    //        if (value is string str)
    //        {
    //            return $"'{str}'";
    //        }
    //        return value.ToString();
    //    }

    //    private static string GetSqlOperator(ExpressionType nodeType)
    //    {
    //        // Map C# expression types to SQL operators (you can expand this as needed).
    //        switch (nodeType)
    //        {
    //            case ExpressionType.Equal:
    //                return "=";
    //            case ExpressionType.NotEqual:
    //                return "<>";
    //            case ExpressionType.LessThan:
    //                return "<";
    //            case ExpressionType.LessThanOrEqual:
    //                return "<=";
    //            case ExpressionType.GreaterThan:
    //                return ">";
    //            case ExpressionType.GreaterThanOrEqual:
    //                return ">=";
    //            case ExpressionType.AndAlso:
    //                return "AND";
    //            case ExpressionType.OrElse:
    //                return "OR";
    //            default:
    //                throw new NotSupportedException("Operator not supported: " + nodeType);
    //        }
    //    }
    //}

    public static class ExpressionExtensions
    {
        public static string ToSqlFilter<TEntity>(this Expression<Func<TEntity, bool>> predicate)
        {
            var body = predicate.Body;
            if (body is BinaryExpression binary)
            {
                return Visit(binary).Replace("1 AND ", string.Empty);
            }
            if (body is UnaryExpression unary)
            {
                return string.Empty;
            }
            return string.Empty;
        }

        private static string Visit(BinaryExpression binary)
        {
            var left = Visit(binary.Left);
            var right = Visit(binary.Right);
            var operation = GetSqlOperator(binary.NodeType, binary);
         
            return $"{left} {operation} {right}";
        }

        private static string Visit(Expression expression)
        {
            if (expression is MemberExpression member)
            {
                // Replace property access expressions with constant values
                if (member.Expression is ConstantExpression constant1)
                {
                    var propertyName = member.Member.Name;
                    var propertyValue = GetValue(constant1.Value, propertyName);
                    return propertyValue;
                }
                return member.Member.Name;
            }
            if (expression is ConstantExpression constant)
            {
                return GetValue(constant.Value, null);
            }
            if (expression is BinaryExpression binary)
            {
                // Handle nested expressions
                var left = Visit(binary.Left);
                var right = Visit(binary.Right);
                var operation = GetSqlOperator(binary.NodeType, expression);                
                return $"({left} {operation} {right})";
            }
            if (expression is UnaryExpression unary)
            {
                var operand = Visit(unary.Operand);
                var operation = GetSqlOperator(unary.NodeType, unary);
                return $"({operation} {operand})";
            }
            if (expression is ConditionalExpression conditional)
            {
                var test = Visit(conditional.Test);
                var ifTrue = Visit(conditional.IfTrue);
                var ifFalse = Visit(conditional.IfFalse);
                return $"(CASE WHEN {test} THEN {ifTrue} ELSE {ifFalse} END)";
            }
            if (expression is MethodCallExpression methodCall)
            {
                if (methodCall.Method.Name == "Equals" && methodCall.Arguments.Count == 1)
                {
                    // Assuming this is the pattern "x.Property.Equals(value)"
                    var propertyAccess = Visit(methodCall.Object);
                    var value = Visit(methodCall.Arguments[0]);

                    return $"{propertyAccess} = {value}";
                }
                if (methodCall.Method.Name == "Contains" && methodCall.Arguments.Count == 2)
                {
                    var propertyAccess = Visit(methodCall.Arguments[1]);
                    var propertyType = methodCall.Arguments[0].Type;

                    if (propertyType == typeof(List<string>) ||
                        propertyType == typeof(List<DateTime>))
                    {
                        // Assuming the first argument is an IEnumerable<string>
                        var filterListExpression =  (List<string>)GetValueFromExpression(methodCall.Arguments[0]);

                        if (filterListExpression.Count() == 0) 
                        {
                            return string.Empty;
                        }
                        // Convert the list of strings into a comma-separated string
                        var filterList = string.Join("',' ", filterListExpression);

                        return $"{propertyAccess} IN ('{filterList}')";
                    }
                    else 
                    {
                        var filterListExpression = (List<object>)GetValueFromExpression(methodCall.Arguments[0]);
                        if (filterListExpression.Count() == 0)
                        {
                            return string.Empty;
                        }
                        // Convert the list of strings into a comma-separated string
                        var filterList = string.Join(",", filterListExpression);

                        return $"{propertyAccess} IN ({filterList})";
                    }
                }
                else
                {
                    // Handle other method calls as needed
                }
            }

            throw new NotSupportedException("Expression type not supported: " + expression.NodeType);
        }
        private static object GetValueFromExpression(Expression expression)
        {
            var constantExpression = expression as ConstantExpression;
            if (constantExpression != null)
            {
                return constantExpression.Value;
            }
            else if (expression is MemberExpression memberExpression)
            {
               
            }
            // Handle other cases as needed
            throw new NotSupportedException("Unsupported expression type");
        }
        private static string GetValue(object value, string propertyName)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "1" : "0";
            }

            // Handle different value types (e.g., strings, numbers, dates) appropriately.
            if (value is string str)
            {
                return $"'{str}'";
            }
            if (value is DateTime dateTime)
            {
                // Format DateTime values appropriately for SQL.
                return $"CAST('{dateTime.ToString("yyyy-MM-dd HH:mm:ss")}' AS DATE)";
            }
            if (value == null)
            { 
                return "NULL";
            }
            if (!string.IsNullOrEmpty(propertyName))
            {
                return $"{propertyName} = {value.ToString()}";
            }
            return value.ToString();
        }

        private static string GetSqlOperator(ExpressionType nodeType, Expression expression)
        {
            // Map C# expression types to SQL operators (you can expand this as needed).
            switch (nodeType)
            {
                case ExpressionType.Equal:
                    if (expression is BinaryExpression binary) 
                    {
                        if (binary.Right.ToString() == "null")
                        {
                            return " IS ";
                        }
                    }
                    return  "=";
                case ExpressionType.Not:
                    return " IS NOT ";
                case ExpressionType.NotEqual:
                    if (expression is BinaryExpression binary1)
                    {
                        if (binary1.Right.ToString() == "null")
                        {
                            return " IS NOT ";
                        }
                    }
                    return "<>";
                case ExpressionType.LessThan:
                    return "<";
                case ExpressionType.LessThanOrEqual:
                    return "<=";
                case ExpressionType.GreaterThan:
                    return ">";
                case ExpressionType.GreaterThanOrEqual:
                    return ">=";
                case ExpressionType.AndAlso:
                case ExpressionType.And:
                    return "AND";            
                case ExpressionType.OrElse:
                case ExpressionType.Or:
                    return "OR";
                case ExpressionType.IsFalse:
                    return "0"; // Or

                case ExpressionType.Convert:
                    return HandleConvertExpression((UnaryExpression)expression);
                case ExpressionType.Call:
                   return HandleCallExpression((MethodCallExpression)expression);
                default:
                    throw new NotSupportedException("Operator not supported: " + nodeType);
            }
        }
        private static string HandleConvertExpression(UnaryExpression expression)
        {
            var operand = Visit(expression.Operand); // Visit the operand of the conversion
                                                     // Your logic to handle the conversion, e.g., casting in SQL
            return string.Empty; // Replace [TargetType] with the target data type you want to convert to
        }

        private static string HandleCallExpression(MethodCallExpression expression) 
        {
            MethodCallExpression methodCallExpression = expression;

            if (methodCallExpression.Method.Name == "Equals" && methodCallExpression.Arguments.Count == 1)
            {
                string columnName = ((MemberExpression)methodCallExpression.Object).Member.Name;
                string value = ((ConstantExpression)methodCallExpression.Arguments[0]).Value.ToString();

                return $"{columnName} = '{value}'";
            }

            // Handle other method calls or throw an exception for unsupported methods.
            throw new NotSupportedException("Method not supported: " + methodCallExpression.Method.Name);
        }
    }
}
