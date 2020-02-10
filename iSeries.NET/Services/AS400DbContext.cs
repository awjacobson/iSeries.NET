using IBM.Data.DB2.iSeries;
using System;
using System.Collections.Generic;
using System.Data;

#nullable enable

namespace iSeries.NET.Services
{
    public interface IAS400DbContext
    {
        IEnumerable<T> GetData<T>(string sqlStatement, Action<iDB2ParameterCollection>? parameters) where T : IMapper, new();
        DataTable GetData(string sqlStatement, Action<iDB2ParameterCollection>? parameters);
    }

    public interface IMapper
    {
        void FromDb(iDB2DataReader reader);
    }

    public class AS400DbContext : IAS400DbContext, IDisposable
    {
        private readonly string _connString;
        private iDB2Connection? _connection;

        public AS400DbContext(string connString)
        {
            _connString = connString;
        }

        public IEnumerable<T> GetData<T>(string sqlStatement, Action<iDB2ParameterCollection>? parameters)
            where T : IMapper, new()
        {
            if (sqlStatement is null) throw new ArgumentNullException(nameof(sqlStatement));

            var resultList = new List<T>();

            using (iDB2Command cmd = new iDB2Command(sqlStatement, Connection))
            {
                if (parameters is { }) { parameters(cmd.Parameters); }

                using (iDB2DataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var resultItem = new T();
                        resultItem.FromDb(reader);
                        resultList.Add(resultItem);
                    }
                    reader.Close();
                }
            }

            return resultList;
        }

        public DataTable GetData(string sqlStatement, Action<iDB2ParameterCollection>? parameters)
        {
            if (sqlStatement is null) throw new ArgumentNullException(nameof(sqlStatement));

            DataTable dt = new DataTable();

            using (iDB2Command cmd = new iDB2Command(sqlStatement, Connection))
            {
                if (parameters is { }) { parameters(cmd.Parameters); }
                using (iDB2DataAdapter da = new iDB2DataAdapter(cmd)) { da.Fill(dt); }
            }

            return dt;
        }

        private iDB2Connection Connection
        {
            get
            {
                if (_connection is null)
                {
                    _connection = new iDB2Connection(_connString);
                }

                if (_connection.State != ConnectionState.Open)
                {
                    _connection.Open();
                }

                return _connection;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (_connection is { })
                    {
                        _connection.Dispose();
                    }
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
