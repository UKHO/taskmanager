using System.Data.SqlClient;

namespace Common.Helpers
{
    public static class DatabasesHelpers
    {
        public static string
            BuildSqlConnectionString(bool isLocalDebugging, string dataSource, string initialCatalog = "") =>
            new SqlConnectionStringBuilder
            {
                DataSource = dataSource,
                InitialCatalog = initialCatalog,
                IntegratedSecurity = isLocalDebugging,
                Encrypt = isLocalDebugging ? false : true,
                ConnectTimeout = 20
            }.ToString();
    }
}