using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MySqlManager.TableInteractions
{
    public sealed class TableManager<TEntity> : IEnumerable<TEntity>, IQueryable<TEntity>, IQueryProvider, IOrderedQueryable<TEntity>
    {
        private readonly MySqlDataContext m_MySqlDataContext;
        private Expression m_Expression = null;

        public TableManager(MySqlDataContext dataContext)
        {
            m_MySqlDataContext = dataContext;
            m_Expression = Expression.Constant(this);
        }

        public TableManager(MySqlDataContext dataContext, Expression expression)
        {
            m_MySqlDataContext = dataContext;
            m_Expression = expression ?? Expression.Constant(this);
        }

        public Type ElementType => typeof(TEntity);

        public Expression Expression => m_Expression;

        public IQueryProvider Provider => this;

        public IQueryable CreateQuery(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(TableManager<>).MakeGenericType(expression.Type.GenericTypeArguments[0]), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return new TableManager<TElement>(m_MySqlDataContext, expression);
        }

        public void Add(TEntity table)
        {
            if (table is null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            TableAttribute tableAttribute = table.GetType().GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;

            IDbCommand dbCommand = m_MySqlDataContext.Provider.Connection.CreateCommand();
            dbCommand.CommandText = $"INSERT INTO {(tableAttribute is null ? table.GetType().Name : tableAttribute.Name)} VALUES ({ConvertTypeToString(table)})";
            dbCommand.ExecuteNonQuery();
        }

        public void AddRange(TEntity[] tables)
        {
            TableAttribute tableAttribute = tables.First().GetType().GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;

            IDbCommand dbCommand = m_MySqlDataContext.Provider.Connection.CreateCommand();
            dbCommand.CommandText = $"INSERT INTO {(tableAttribute is null ? tables.First().GetType().Name : tableAttribute.Name)} VALUES {ConvertTypesToString(tables)}";
            dbCommand.ExecuteNonQuery();
        }

        public void AddRange(TEntity[] tables, Func<TEntity, bool> predicate) => AddRange(tables.Where(predicate).ToArray());

        public void Update(TEntity table)
        {
            string primaryKey = PrimaryKeyToString(table);

            StringBuilder value = new StringBuilder();

            Type tableType = table.GetType();

            PropertyInfo[] tableProperties = tableType.GetProperties();

            foreach (var property in tableProperties)
            {
                ColumnAttribute columnAttribute = property.GetCustomAttribute(typeof(ColumnAttribute)) as ColumnAttribute;

                if (columnAttribute == null || columnAttribute.IsDbGenerated)
                {
                    continue;
                }

                string propertyName = string.IsNullOrWhiteSpace(columnAttribute.Name) ? property.Name : columnAttribute.Name;

                if (property.PropertyType == typeof(string))
                {
                    value.Append($"{propertyName}='{property.GetValue(table)}',");
                }
                else if (property.PropertyType == typeof(DateTime))
                {
                    value.Append($"{propertyName}='{property.GetValue(table):yyyy-MM-dd}',");
                }
                else if (property.PropertyType == typeof(int))
                {
                    value.Append($"{propertyName}={property.GetValue(table)},");
                }
                else if (property.PropertyType == typeof(bool))
                {
                    value.Append($"{propertyName}={Convert.ToInt16(property.GetValue(table))},");
                }
            }

            value.Remove(value.Length - 1, 1);

            TableAttribute tableAttribute = table.GetType().GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;

            IDbCommand dbCommand = m_MySqlDataContext.Provider.Connection.CreateCommand();
            dbCommand.CommandText = $"UPDATE {(tableAttribute is null ? table.GetType().Name : tableAttribute.Name)} SET {value} {(primaryKey is null ? string.Empty : $"WHERE {primaryKey}")}";
            dbCommand.ExecuteNonQuery();
        }

        public void Delete(TEntity table)
        {
            if (table is null)
            {
                //dbCommand.CommandText = $"DELETE FROM {(tableAttribute is null ? table.GetType().Name : tableAttribute.Name)}";

                return;
            }

            TableAttribute tableAttribute = table.GetType().GetCustomAttribute(typeof(TableAttribute)) as TableAttribute;
            IDbCommand dbCommand = m_MySqlDataContext.Provider.Connection.CreateCommand();

            string primaryKey = PrimaryKeyToString(table);

            if (primaryKey is null)
            {
                throw new ArgumentException("The primary key is not specified in the table");
            }

            dbCommand.CommandText = $"DELETE FROM {(tableAttribute is null ? table.GetType().Name : tableAttribute.Name)} WHERE {primaryKey}";

            dbCommand.ExecuteNonQuery();
        }

        private string ConvertTypeToString(TEntity table)
        {
            StringBuilder value = new StringBuilder();

            Type tableType = table.GetType();

            PropertyInfo[] tableProperties = tableType.GetProperties();

            foreach (var property in tableProperties)
            {
                ColumnAttribute columnAttribute = property.GetCustomAttribute(typeof(ColumnAttribute)) as ColumnAttribute;

                if (columnAttribute == null)
                {
                    continue;
                }

                if (columnAttribute.IsDbGenerated)
                {
                    value.Append("NULL,");

                    continue;
                }

                switch (Type.GetTypeCode(property.PropertyType))
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
                        value.Append($"{property.GetValue(table)},");
                        break;

                    case TypeCode.String:
                    case TypeCode.Char:
                        value.Append($"'{property.GetValue(table)}',");
                        break;

                    case TypeCode.DateTime:
                        value.Append($"'{property.GetValue(table):yyyy-MM-dd HH:mm}',");
                        break;
                }
            }

            value.Remove(value.Length - 1, 1);

            return value.ToString();
        }

        private string ConvertTypesToString(TEntity[] tables)
        {
            StringBuilder tablesValue = new StringBuilder();

            foreach (var table in tables)
            {
                tablesValue.Append($"({ConvertTypeToString(table)}),");
            }

            tablesValue.Remove(tablesValue.Length - 1, 1);

            return tablesValue.ToString();
        }

        private string PrimaryKeyToString(TEntity table)
        {
            StringBuilder value = new StringBuilder();

            Type tableType = table.GetType();

            PropertyInfo[] tableProperties = tableType.GetProperties();

            foreach (var property in tableProperties)
            {
                string propertyName = string.Empty;

                ColumnAttribute columnAttribute = property.GetCustomAttributes(false).Where(X => X.GetType() == typeof(ColumnAttribute)).Cast<ColumnAttribute>().FirstOrDefault();

                if (columnAttribute == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(columnAttribute.Name))
                {
                    propertyName = property.Name;
                }
                else
                {
                    propertyName = columnAttribute.Name;
                }

                if (columnAttribute.IsPrimaryKey)
                {
                    if (property.PropertyType == typeof(string) || property.PropertyType == typeof(DateTime))
                    {
                        value.Append($"{propertyName}='{property.GetValue(table)}'");
                    }
                    else if (property.PropertyType == typeof(int))
                    {
                        value.Append($"{propertyName}={property.GetValue(table)}");
                    }
                    else if (property.PropertyType == typeof(bool))
                    {
                        value.Append($"{propertyName}={Convert.ToInt16(property.GetValue(table))}");
                    }

                    return value.ToString();
                }
            }

            return null;
        }

        public object Execute(Expression expression)
        {
            if (expression is null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            return m_MySqlDataContext.Provider.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression) => (TResult)Execute(expression);

        public IEnumerator<TEntity> GetEnumerator() => ((IEnumerable<TEntity>)Execute(Expression)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Execute(m_Expression)).GetEnumerator();
    }
}