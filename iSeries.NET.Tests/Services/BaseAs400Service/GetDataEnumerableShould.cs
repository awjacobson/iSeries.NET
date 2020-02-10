using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using iSeries.NET.Services;
using IBM.Data.DB2.iSeries;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CassCounty.Data.Tests.Services.BaseAs400Service
{
    [TestClass]
    public class GetDataEnumerableShould
    {
        [TestMethod]
        public void TryUpToFiveTimes()
        {
            // Arrange
            var dbContext = new MockDbContext();
            var service = new BaseAS400Service(dbContext);

            // Act
            var actual = service.GetData<MockObject>("SELECT Id FROM Table");

            // Assert
            Assert.AreEqual(5, dbContext.Count);
        }
    }

    public class MockDbContext : IAS400DbContext
    {
        public int Count { get; private set; }

        public IEnumerable<T> GetData<T>(string sqlStatement, Action<iDB2ParameterCollection> parameters) where T : IMapper, new()
        {
            Count += 1;
            if (Count == 5)
            {
                return new List<T> { new T() };
            }

            // iDB2CommErrorException has only internal constructors, methods, etc. and
            // MpDcErrorInfo is an internal struct.
            // Using reflection to test all this.
            var mpEIType = typeof(iDB2CommErrorException).Assembly.GetType("IBM.Data.DB2.iSeries.MpDcErrorInfo");

            var mpEI = Activator.CreateInstance(mpEIType);

            var property = mpEIType.GetField("firstLevelMessageTextAsManagedString", BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(mpEI, "Aaron was here");

            property = mpEIType.GetField("returnCode", BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(mpEI, 33);

            property = mpEIType.GetField("errorType", BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(mpEI, 2);

            property = mpEIType.GetField("errorCode", BindingFlags.NonPublic | BindingFlags.Instance);
            property.SetValue(mpEI, 33);

            var throwDcException = typeof(iDB2Exception).GetMethod("throwDcException", BindingFlags.NonPublic | BindingFlags.Static, null, new Type[] { mpEIType }, null);
            throwDcException.Invoke(null, new object[] { mpEI });

            // have to return something in order to compile
            return null;
        }

        public DataTable GetData(string sqlStatement, Action<iDB2ParameterCollection> parameters)
        {
            throw new NotImplementedException();
        }
    }

    public class MockObject : IMapper
    {
        public void FromDb(iDB2DataReader reader)
        {
            // Do nothing
        }
    }

}
