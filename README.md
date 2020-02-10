# iSeries<span>.</span>NET

The IBM implementation of ADO<span>.</span>NET database provider for iSeries is not particularly resilient. In fact, the following is a quote from the IBM Redbook [Integrating DB2 Universal Universal Database for iSeries with for iSeries with Microsoft ADO.NET](https://www.redbooks.ibm.com/redbooks/pdfs/sg246440.pdf) page 103:

> Handling communication errors (iDB2CommErrorException)
> When you execute commands using IBM.Data.DB2.iSeries, the provider uses a
> communication link to transfer the commands and data back and forth to the iSeries server
> job that runs requests on behalf of your application. At times, this communication link may
> become unusable for any of several reasons, including:
>  The iSeries server is IPLed (for example, to perform nightly maintenance).
>  The iSeries server job processing your requests (QZDASOINIT) is ended.
>  The communication link experiences some other failure.
> Whatever the cause, your application should be prepared to handle communication errors
> whenever it executes commands. 

The iSeries<span>.</span>NET package utilizes [Polly](https://github.com/App-vNext/Polly) to overcome these "expected" and random communication errors.

## Install iSeries<span>.</span>NET via Nuget
If you want to include iSeries<span>.</span>NET in your project, you can [install it directly from NuGet](https://www.nuget.org/packages/iSeries.NET/)

To install iSeries<span>.</span>NET, run the following command in the Package Manager Console
```
PM> Install-Package iSeries.NET
```

## Usage

Create a class that extends BaseAS400Service:
```c#
public class MyService : BaseAS400Service
{
    public MyService(IAS400DbContext dbConext) : base(dbConext)
    {
    }
}
```
Here I have added an optional constructor overload that accepts a connection string and an example method to get data:
```c#
public class MyService : BaseAS400Service
{
    public MyService(string connectionString) : base(new AS400DbContext(connectionString))
    {
    }

    public MyService(IAS400DbContext dbConext) : base(dbConext)
    {
    }

    public IEnumerable<PropertyDto> VehicleListing(int year, int account)
    {
        return GetData<PropertyDto>(Sql.VehicleListing,
            parameters =>
            {
                parameters.Add("@year", iDB2DbType.iDB2Integer).Value = year;
                parameters.Add("@account", iDB2DbType.iDB2Integer).Value = account;
            });
    }
}
```
Example code to call the service method:
```C#
var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;
var service = new MyService(connectionString);
var results = service.VehicleListing(2019, 180);
```