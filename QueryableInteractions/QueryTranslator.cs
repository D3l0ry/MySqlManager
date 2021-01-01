using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MySqlManager.TableInteractions;

namespace MySqlManager.QueryableInteractions
{
    internal class QueryTranslator : ExpressionVisitor
    {
        StringBuilder m_TranslatedQuery = null;

        public QueryTranslator()
        {
            m_TranslatedQuery = new StringBuilder("SELECT *");
        }

        internal string Translate(Expression expression)
        {
            Visit(expression);

            return m_TranslatedQuery.ToString();
        }

        private static Expression StripQuotes(Expression node)
        {
            while (node.NodeType == ExpressionType.Quote)
            {
                node = ((UnaryExpression)node).Operand;
            }

            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable))
            {
                switch (node.Method.Name)
                {
                    case "Where":
                        Visit(node.Arguments[0]);

                        m_TranslatedQuery.Append(" WHERE ");

                        LambdaExpression lambdaWhere = (LambdaExpression)StripQuotes(node.Arguments[1]);

                        Visit(lambdaWhere.Body);
                        return node;

                    case "OrderBy":
                        Visit(node.Arguments[0]);

                        m_TranslatedQuery.Append(" ORDER BY ");

                        LambdaExpression lambdaOrder = (LambdaExpression)StripQuotes(node.Arguments[1]);

                        Visit(lambdaOrder.Body);
                        return node;

                    case "OrderByDescending":
                        Visit(node.Arguments[0]);

                        m_TranslatedQuery.Append(" ORDER BY ");

                        LambdaExpression lambdaOrderDesc = (LambdaExpression)StripQuotes(node.Arguments[1]);

                        Visit(lambdaOrderDesc.Body);

                        m_TranslatedQuery.Append(" DESC ");
                        return node;

                    case "Take":
                        Visit(node.Arguments[0]);

                        m_TranslatedQuery.Append(" LIMIT ");

                        Visit(node.Arguments[1]);
                        return node;

                    case "Count":
                        m_TranslatedQuery.Replace("*", "COUNT(*) ");
                        Visit(node.Arguments[0]);
                        return node;
                }
            }

            throw new NotSupportedException(string.Format("The method '{0}' is not supported", node.Method.Name));
        }

        protected override Expression VisitUnary(UnaryExpression unary)
        {
            switch (unary.NodeType)
            {
                case ExpressionType.Not:
                    m_TranslatedQuery.Append(" NOT ");
                    Visit(unary.Operand);
                    break;
                default:
                    throw new NotSupportedException(string.Format("The unary operator '{0}' is not supported", unary.NodeType));
            }
            return unary;
        }

        protected override Expression VisitBinary(BinaryExpression binary)
        {
            m_TranslatedQuery.Append("(");
            Visit(binary.Left);
            switch (binary.NodeType)
            {
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    m_TranslatedQuery.Append(" AND ");
                    break;
                case ExpressionType.Or:
                    m_TranslatedQuery.Append(" OR");
                    break;
                case ExpressionType.Equal:
                    m_TranslatedQuery.Append(" = ");
                    break;
                case ExpressionType.NotEqual:
                    m_TranslatedQuery.Append(" <> ");
                    break;
                case ExpressionType.LessThan:
                    m_TranslatedQuery.Append(" < ");
                    break;
                case ExpressionType.LessThanOrEqual:
                    m_TranslatedQuery.Append(" <= ");
                    break;
                case ExpressionType.GreaterThan:
                    m_TranslatedQuery.Append(" > ");
                    break;
                case ExpressionType.GreaterThanOrEqual:
                    m_TranslatedQuery.Append(" >= ");
                    break;
                default:
                    throw new NotSupportedException(string.Format("The binary operator '{0}' is not supported", binary.NodeType));
            }

            try
            {
                var f = Expression.Lambda(binary.Right).Compile();
                var value = f.DynamicInvoke();

                switch (Type.GetTypeCode(value.GetType()))
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Boolean:
                        m_TranslatedQuery.Append(value);
                        break;
                    case TypeCode.String:
                        m_TranslatedQuery.Append("'");
                        m_TranslatedQuery.Append(value);
                        m_TranslatedQuery.Append("'");
                        break;
                    case TypeCode.DateTime:
                        m_TranslatedQuery.Append($"'{(DateTime)value:yyyy-MM-dd HH:mm}'");
                        break;
                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", value));
                }
            }
            catch
            {

            }

            Visit(binary.Right);
            m_TranslatedQuery.Append(")");
            return binary;
        }

        protected override Expression VisitConstant(ConstantExpression constant)
        {
            if (constant.Value is IQueryable queryable)
            {
                m_TranslatedQuery.Append($" FROM {(queryable.ElementType.GetCustomAttribute(typeof(TableAttribute)) is TableAttribute tableAttribute ? tableAttribute.Name : queryable.ElementType.Name)}");
            }

            else if (constant.Value == null)
            {
                m_TranslatedQuery.Append("NULL");
            }
            else
            {
                switch (Type.GetTypeCode(constant.Value.GetType()))
                {
                    case TypeCode.SByte:
                    case TypeCode.Byte:
                    case TypeCode.Int16:
                    case TypeCode.Int32:
                    case TypeCode.Int64:
                    case TypeCode.UInt16:
                    case TypeCode.UInt32:
                    case TypeCode.UInt64:
                    case TypeCode.Single:
                    case TypeCode.Decimal:
                    case TypeCode.Double:
                        m_TranslatedQuery.Append(constant.Value);
                        break;
                    case TypeCode.String:
                        m_TranslatedQuery.Append("'");
                        m_TranslatedQuery.Append(constant.Value);
                        m_TranslatedQuery.Append("'");
                        break;
                    case TypeCode.DateTime:
                        m_TranslatedQuery.Append($"'{(DateTime)constant.Value:yyyy-MM-dd HH:mm}'");
                        break;
                    case TypeCode.Object:
                        throw new NotSupportedException(string.Format("The constant for '{0}' is not supported", constant));
                }
            }
            return constant;
        }

        protected override Expression VisitMember(MemberExpression member)
        {
            if (member.Expression != null && member.Expression.NodeType == ExpressionType.Parameter)
            {
                PropertyInfo memberProperty = member.Member.ReflectedType.GetProperty(member.Member.Name);
                ColumnAttribute columnProperty = memberProperty.GetCustomAttribute(typeof(ColumnAttribute)) as ColumnAttribute;

                if (columnProperty is null)
                {
                    m_TranslatedQuery.Append(member.Member.Name);
                }
                else
                {
                    m_TranslatedQuery.Append(columnProperty.Name ?? member.Member.Name);
                }

                return member;
            }

            return member;
        }
    }
}