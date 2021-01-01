using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

namespace MySqlManager.TableInteractions
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        private MySqlDbType? m_DbType;

        public string Name;

        public bool IsPrimaryKey;

        public bool IsDbGenerated;

        public bool CanBeNull;

        public MySqlDbType DbType
        {
            get
            {
                if (m_DbType is null)
                {
                    return m_DbType.GetValueOrDefault() - 1;
                }

                return m_DbType.Value;
            }
            set => m_DbType = value;
        }

        public ColumnAttribute() { }
    }
}