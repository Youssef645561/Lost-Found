using System;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Common;

namespace LostFound.DAL
{
    public static class clsDataAccess
    {
        private static IConfiguration _Configuration;

        public static void Initialize(IConfiguration Configuration)
        {
            _Configuration = Configuration;
        }

        public static SqlConnection GetConnection()
        {
            return new SqlConnection(_Configuration.GetConnectionString("ConnectionString"));
        }
    }
}
