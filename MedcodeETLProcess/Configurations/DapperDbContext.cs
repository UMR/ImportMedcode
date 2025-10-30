using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace MedcodeETLProcess.Configurations
{
    public class DapperDbContext
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _historyConnectionString;

        public DapperDbContext(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _historyConnectionString = _configuration.GetConnectionString("MedcodepediaHistoryConnection");
        }

        public IDbConnection CreateConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public IDbConnection CreateHistoryConnection()
        {
            return new SqlConnection(_historyConnectionString);
        }
    }
}
