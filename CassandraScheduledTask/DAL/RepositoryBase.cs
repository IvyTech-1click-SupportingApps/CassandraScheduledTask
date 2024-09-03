using System.Data;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace CassandraScheduledTask.DAL
{
    public abstract class RepositoryBase
    {
        protected readonly IConfiguration Configuration;

        protected RepositoryBase(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        protected OracleConnection GetConnection()
        {
            var connectionString = Configuration.GetConnectionString("OracleConnectionString");
            OracleConnection conn = new OracleConnection(connectionString);
            if (conn.State == ConnectionState.Closed)
            {
                conn.Open();
            }
            Console.WriteLine("Connected to DB");
            return conn;
        }

        protected static void CloseConnection(IDbConnection conn)
        {
            if (conn.State is ConnectionState.Open or ConnectionState.Broken)
            {
                conn.Close();
            }
        }
    }
}