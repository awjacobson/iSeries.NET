using IBM.Data.DB2.iSeries;
using System;

namespace iSeries.NET.Extensions
{
    public static class DataReaderExtensions
    {
        public static bool IsDBNull(this iDB2DataReader reader, string columnName)
        {
            return reader[columnName] == DBNull.Value;
        }

        public static string GetString(this iDB2DataReader reader, string columnName, string defaultValue = null) => GetNullable(reader, columnName, defaultValue);

        /// <summary>
        /// Gets the non-nullable decimal column value by column name.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static decimal GetDecimal(this iDB2DataReader reader, string columnName) => (decimal)reader[columnName];

        /// <summary>
        /// Gets the nullable decimal column value by column name.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal? GetDecimal(this iDB2DataReader reader, string columnName, decimal? defaultValue) => GetNullable(reader, columnName, defaultValue);

        /// <summary>
        /// Gets the non-nullable int column value by column name.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <returns></returns>
        public static int GetInt32(this iDB2DataReader reader, string columnName) => (int)reader[columnName];

        /// <summary>
        /// Gets the nullable int column value by column name.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="columnName"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int? GetInt32(this iDB2DataReader reader, string columnName, int? defaultValue) => GetNullable(reader, columnName, defaultValue);

        public static long GetInt64(this iDB2DataReader reader, string columnName) => (long)reader[columnName];

        public static long? GetInt64(this iDB2DataReader reader, string columnName, long? defaultValue) => GetNullable(reader, columnName, defaultValue);

        public static T GetNullable<T>(this iDB2DataReader reader, string columnName, T defaultValue)
        {
            return reader.IsDBNull(columnName)
                ? defaultValue
                : (T)reader[columnName];
        }
    }
}
