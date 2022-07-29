using Dapper;
using MySqlConnector;
using Serilog;

namespace MasterServer.DB
{
    public static class DBHelper
    {
        public static async Task<MySqlConnection?> CreateConnection()
        {
            try
            {
                if (MasterApplication.Instance.ServerConfig == null)
                    return null;
                string dbConnectStr = MasterApplication.Instance.ServerConfig.DBConnectStr;
                var connection = new MySqlConnection(dbConnectStr);
                await connection.OpenAsync();
                return connection;
            }
            catch(Exception e)
            {
                Log.Error(e.ToString());
                return null;
            }
        }
    }
}