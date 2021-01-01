using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using MySql.Data.MySqlClient;

using MySqlManager.TableInteractions;

namespace MySqlManager.QueryableInteractions
{
    internal class ObjectReader<T> : IEnumerable<T>, IEnumerator<T>, IDisposable where T : class, new()
    {
        private readonly IDataReader m_DataReader;
        private readonly PropertyInfo[] m_TypeFields;
        private readonly int m_FieldCount;
        private T m_Current;

        internal ObjectReader(IDataReader dataReader)
        {
            m_DataReader = dataReader;
            m_TypeFields = typeof(T).GetProperties();

            if (dataReader.FieldCount.CompareTo(m_TypeFields.Length) <= 0)
            {
                m_FieldCount = m_DataReader.FieldCount;
            }
            else
            {
                m_FieldCount = m_TypeFields.Length;
            }
        }

        public T Current => m_Current;

        object IEnumerator.Current => m_Current;

        public bool MoveNext()
        {
            if (m_DataReader.Read())
            {
                T currentElement = new T();

                for (int index = 0; index < m_FieldCount; index++)
                {
                    PropertyInfo field = m_TypeFields[index];

                    ColumnAttribute columnAttribute = field.GetCustomAttribute(typeof(ColumnAttribute)) as ColumnAttribute;

                    if (m_DataReader.IsDBNull(index))
                    {
                        field.SetValue(currentElement, null);
                    }
                    else
                    {
                        string fieldName;
                        MySqlDbType fieldType = MySqlDbType.Decimal - 1;

                        if (columnAttribute != null)
                        {
                            fieldName = columnAttribute.Name.ToLower();
                            fieldType = columnAttribute.DbType;
                        }
                        else
                        {
                            fieldName = field.Name.ToLower();
                        }

                        if (m_DataReader.GetName(index) != fieldName)
                        {
                            ConvertProperty(currentElement, index, field, fieldType, null);
                        }
                        else
                        {
                            ConvertProperty(currentElement, index, field, fieldType, fieldName);
                        }
                    }
                }

                m_Current = currentElement;

                return true;
            }

            return false;
        }

        private void ConvertProperty(T currentElement, int index, PropertyInfo field, MySqlDbType? fieldType, string fieldName)
        {
            object fieldValue = fieldName is null ? m_DataReader[index] : m_DataReader[fieldName];

            switch (fieldType)
            {
                case MySqlDbType.Decimal:
                case MySqlDbType.NewDecimal:
                    field.SetValue(currentElement, Convert.ToDecimal(fieldValue));
                    break;

                case MySqlDbType.Bit:
                case MySqlDbType.Byte:
                    field.SetValue(currentElement, Convert.ToSByte(fieldValue));
                    break;

                case MySqlDbType.Int16:
                    field.SetValue(currentElement, Convert.ToInt16(fieldValue));
                    break;
                case MySqlDbType.Int24:
                case MySqlDbType.Int32:
                    field.SetValue(currentElement, Convert.ToInt32(fieldValue));
                    break;
                case MySqlDbType.Int64:
                    field.SetValue(currentElement, Convert.ToInt64(fieldValue));
                    break;
                case MySqlDbType.Float:
                    field.SetValue(currentElement, Convert.ToSingle(fieldValue));
                    break;
                case MySqlDbType.Double:
                    field.SetValue(currentElement, Convert.ToDouble(fieldValue));
                    break;

                case MySqlDbType.Timestamp:
                case MySqlDbType.Date:
                case MySqlDbType.Time:
                case MySqlDbType.DateTime:
                case MySqlDbType.Year:
                case MySqlDbType.Newdate:
                    field.SetValue(currentElement, Convert.ToDateTime(fieldValue));
                    break;

                case MySqlDbType.JSON:
                    break;
                case MySqlDbType.Enum:
                    break;
                case MySqlDbType.Set:
                    break;
                case MySqlDbType.TinyBlob:
                    break;
                case MySqlDbType.MediumBlob:
                    break;
                case MySqlDbType.LongBlob:
                    break;
                case MySqlDbType.Blob:
                    break;
                case MySqlDbType.Geometry:
                    break;
                case MySqlDbType.UByte:
                    field.SetValue(currentElement, Convert.ToByte(fieldValue));
                    break;
                case MySqlDbType.UInt16:
                    field.SetValue(currentElement, Convert.ToUInt16(fieldValue));
                    break;
                case MySqlDbType.UInt24:
                case MySqlDbType.UInt32:
                    field.SetValue(currentElement, Convert.ToUInt32(fieldValue));
                    break;
                case MySqlDbType.UInt64:
                    field.SetValue(currentElement, Convert.ToUInt64(fieldValue));
                    break;

                case MySqlDbType.String:
                case MySqlDbType.VarChar:
                case MySqlDbType.VarString:
                case MySqlDbType.Binary:
                case MySqlDbType.VarBinary:
                case MySqlDbType.TinyText:
                case MySqlDbType.MediumText:
                case MySqlDbType.LongText:
                case MySqlDbType.Text:
                    field.SetValue(currentElement, Convert.ToString(fieldValue));
                    break;

                case MySqlDbType.Guid:
                    break;

                case null:
                default:
                    field.SetValue(currentElement, fieldValue);
                    break;
            }
        }

        public void Reset() { }

        public void Dispose()
        {
            m_DataReader.Dispose();
        }

        public IEnumerator<T> GetEnumerator() => this;

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}