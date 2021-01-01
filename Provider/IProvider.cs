using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

namespace MySqlManager.Provider
{
    public interface IProvider : IDisposable
    {
        MySqlConnection Connection { get; }

        void CreateDatabase();

        void DeleteDatabase();

        bool DatabaseExists();

        object Execute(Expression query);

        TResult Execute<TResult>(Expression query);
    }
}