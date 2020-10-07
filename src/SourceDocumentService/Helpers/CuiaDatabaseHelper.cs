using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using SourceDocumentService.Configuration;

namespace SourceDocumentService.Helpers
{
    public class CuiaDatabaseHelper : ICuiaDatabaseHelper
    {
        private readonly IConfigurationManager _configurationManager;

        public CuiaDatabaseHelper(IConfigurationManager configurationManager)
        {
            _configurationManager = configurationManager;
        }

        public async Task<int> GetNextWreckIdAsync()
        {
            var connectionString = _configurationManager.GetConnectionString("CuiaDatabase");
            var timeoutInSeconds = int.Parse(_configurationManager.GetAppSetting("CuiaDatabaseTimeoutInSeconds"));

            await using var connection = new SqlConnection(connectionString);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandTimeout = timeoutInSeconds;

            var parameter = new SqlParameter(parameterName: "@nextValue", dbType: SqlDbType.Int);
            parameter.Direction = ParameterDirection.Output;

            command.Parameters.Add(parameter);

            command.CommandText = @"select @nextValue = next value for [dbo].[CarisWreckIdSequence]";

            await connection.OpenAsync();
            await command.ExecuteScalarAsync();

            return (int)parameter.Value;
        }
    }
}
