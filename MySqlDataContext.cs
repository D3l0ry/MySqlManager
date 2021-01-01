using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

namespace MySqlManager
{
    public sealed class MySqlDataContext
    {
        private readonly MySqlProvider m_MySqlProvider;

        public MySqlDataContext(MySqlConnection connection)
        {
            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            m_MySqlProvider = new MySqlProvider(connection);
        }

        public MySqlDataContext(string server, string user, string password, string database, bool open = true)
        {
            try
            {
                string[] serverSplit = server.Split(':');
                string connectionString = string.Empty;

                if (serverSplit.Length > 1)
                {
                    connectionString = $"server={serverSplit[0]};port={serverSplit[1]};userid={user};password={password};database={database}";
                }
                else
                {
                    connectionString = $"server={serverSplit[0]};userid={user};password={password};database={database}";
                }

                MySqlConnection sqlConnection = new MySqlConnection(connectionString);
                m_MySqlProvider = new MySqlProvider(sqlConnection);

                if (open)
                {
                    Open();
                }
            }
            catch
            {
                m_MySqlProvider.Connection.Close();
            }
        }

        public void Open()
        {
            if (m_MySqlProvider.Connection.State == ConnectionState.Open)
            {
                return;
            }

            m_MySqlProvider.Connection.Open();
        }

        public void Close()
        {
            m_MySqlProvider.Connection.Close();
        }

        public IProvider Provider => m_MySqlProvider;

        public void CreateDatabase() => Provider.CreateDatabase();

        public void DeleteDatabase() => Provider.DeleteDatabase();

        public bool DatabaseExists => Provider.DatabaseExists();

        public void Dispose() => Provider.Dispose();

        public TableManager<T> GetTable<T>() => new TableManager<T>(this);
    }
}