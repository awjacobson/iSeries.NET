using IBM.Data.DB2.iSeries;
using log4net;
using Log4netHelpers;
using Log4netHelpers.Extensions;
using Polly;
using System;
using System.Collections.Generic;
using System.Data;

#nullable enable

namespace iSeries.NET.Services
{
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// 
    /// From the IBM Redbook "Integrating DB2 Universal Universal Database for iSeries with for
    /// iSeries with Microsoft ADO .NET crosoft ADO .NET" (page 103):
    /// https://www.redbooks.ibm.com/redbooks/pdfs/sg246440.pdf
    /// Handling communication errors (iDB2CommErrorException)
    /// When you execute commands using IBM.Data.DB2.iSeries, the provider uses a
    /// communication link to transfer the commands and data back and forth to the iSeries server
    /// job that runs requests on behalf of your application. At times, this communication link may
    /// become unusable for any of several reasons, including:
    ///  The iSeries server is IPLed (for example, to perform nightly maintenance).
    ///  The iSeries server job processing your requests (QZDASOINIT) is ended.
    ///  The communication link experiences some other failure.
    /// Whatever the cause, your application should be prepared to handle communication errors
    /// whenever it executes commands. 
    /// 
    /// Retry 5 times with exponetial backoff using Polly.
    /// https://github.com/App-vNext/Polly
    /// 
    /// </remarks>
    public class BaseAS400Service
    {
        protected static readonly ILog Logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        protected IAS400DbContext _dbContext;

        public BaseAS400Service(IAS400DbContext dbConext)
        {
            _dbContext = dbConext;
        }

        #region Get data

        /// <summary>
        /// Gets the data from the IBM i.
        /// </summary>
        /// <param name="sqlStatement">The SQL statement</param>
        /// <returns>DataTable</returns>
        public DataTable GetData(string sqlStatement)
        {
            return GetData(sqlStatement, null);
        }

        /// <summary>
        /// Gets the data from the IBM i.
        /// </summary>
        /// <param name="sqlStatement">The SQL statement</param>
        /// <param name="parameters">The parameters (iDB2Parameter)</param>
        /// <returns>DataTable</returns>
        public DataTable GetData(string sqlStatement, Action<iDB2ParameterCollection>? parameters)
        {
            if (Logger.IsDebugEnabled)
            {
                Logger.Debug($"Entering GetData (sqlStatement={sqlStatement}, parameters={parameters})");
            }

            if (sqlStatement is null) throw new ArgumentNullException(nameof(sqlStatement));

            DataTable dt = new DataTable();

            try
            {
                return _dbContext.GetData(sqlStatement, parameters);
            }
            catch (iDB2SQLErrorException dbException)
            {
                var msg = $"Message={dbException.Message}, MessageCode={dbException.MessageCode}, MessageDetails={dbException.MessageDetails}";
                Logger.Error($"\n{Banner.Error}\niDB2SQLErrorException caught in GetData (sqlStatement={sqlStatement}, parameters={parameters}): {msg}", dbException);
            }
            catch (Exception ex)
            {
                Logger.Error($"\n{Banner.Error}\nException caught in GetData (qlStatement={sqlStatement}, parameters={parameters}): {ex.Message}", ex);
            }

            return dt;
        }

        /// <summary>
        /// Gets the data from the IBM i.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlStatement"></param>
        /// <returns></returns>
        public IEnumerable<T> GetData<T>(string sqlStatement) where T : IMapper, new()
        {
            return GetData<T>(sqlStatement, null);
        }

        /// <summary>
        /// https://stackoverflow.com/questions/19841120/generic-dbdatareader-to-listt-mapping
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sqlStatement"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        public IEnumerable<T> GetData<T>(string sqlStatement, Action<iDB2ParameterCollection>? parameters)
            where T : IMapper, new()
        {
            if (Logger.IsDebugEnabled)
            {
                Logger.Debug($"Entering GetData (sqlStatement={sqlStatement}, parameters={parameters?.ToDebugString()})");
            }

            if (sqlStatement is null) throw new ArgumentNullException(nameof(sqlStatement));

            var resultList = Policy
                .Handle<Exception>()
                .WaitAndRetry(SleepDurations, (exception, timeSpan, retryCount) =>
                {
                    var msg = exception is iDB2CommErrorException
                        ? $"Message={((iDB2CommErrorException)exception).Message}, MessageCode={((iDB2CommErrorException)exception).MessageCode}, MessageDetails={((iDB2CommErrorException)exception).MessageDetails}"
                        : exception.Message;
                    //var msg = "Error";
                    Logger.Error($"\n{Banner.Error}\n{exception.GetType().Name} caught in GetData (sqlStatement={sqlStatement}, parameters={parameters}, timeSpan={timeSpan}): {msg}", exception);
                })
                .Execute<IEnumerable<T>>(() => { return _dbContext.GetData<T>(sqlStatement, parameters); });

            return resultList;
            //throw new Exception("AS/400 Communication error");
        }

        protected IEnumerable<TimeSpan> SleepDurations => new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4),
            TimeSpan.FromSeconds(8),
            TimeSpan.FromSeconds(16)
        };

        #endregion

        /// <summary>
        /// Executes a statement on the IBM i that doesn't return data (INSERT, UPDATE, DELETE)
        /// </summary>
        /// <param name="connString">The connection string</param>
        /// <param name="sqlStatement">The SQL statement</param>
        /// <param name="parameters">The parameters (iDB2Parameter)</param>
        public static void ExecuteNonQuery(string connString, string sqlStatement, Action<iDB2ParameterCollection>? parameters)
        {
            using (iDB2Connection conn = new iDB2Connection(connString))
            {
                using (iDB2Command cmd = new iDB2Command(sqlStatement, conn))
                {
                    conn.Open();
                    if (parameters is { }) { parameters(cmd.Parameters); }
                    var x = cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
    }
}
