using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using MySqlManager.QueryableInteractions;

namespace MySqlManager.Provider
{
    internal class MySqlProvider : IProvider
    {
        private readonly MySqlConnection m_MySqlConnection;

        public MySqlConnection Connection => m_MySqlConnection;

        public MySqlProvider(MySqlConnection connection) => m_MySqlConnection = connection;

        public void CreateDatabase()
        {
            throw new NotImplementedException();
        }

        public bool DatabaseExists()
        {
            throw new NotImplementedException();
        }

        public void DeleteDatabase()
        {
            throw new NotImplementedException();
        }

        public object Execute(Expression query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            IDbCommand dbCommand = Connection.CreateCommand();
            dbCommand.CommandText = new QueryTranslator().Translate(query);

            return Activator.CreateInstance(typeof(ObjectReader<>).MakeGenericType(query.Type.GenericTypeArguments.Length > 0 ? query.Type.GenericTypeArguments[0] : query.Type), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { dbCommand.ExecuteReader() }, null);
        }

        public TResult Execute<TResult>(Expression query)
        {
            if (query is null)
            {
                throw new ArgumentNullException(nameof(query));
            }

            IDbCommand dbCommand = Connection.CreateCommand();
            dbCommand.CommandText = new QueryTranslator().Translate(query);

            if (typeof(TResult).IsClass)
            {
                return (TResult)(Activator.CreateInstance(typeof(ObjectReader<>).MakeGenericType(query.Type.GenericTypeArguments.Length > 0 ? query.Type.GenericTypeArguments[0] : typeof(TResult)), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { dbCommand.ExecuteReader() }, null));
            }
            else
            {
                return (TResult)dbCommand.ExecuteScalar();
            }
        }

        public void Dispose()
        {
            m_MySqlConnection.Close();
            m_MySqlConnection.Dispose();
        }
    }
}