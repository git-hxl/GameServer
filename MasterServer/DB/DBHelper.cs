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

        public static async Task<List<T>?> SqlSelect<T>(MySqlConnection sqlConnection, string sql)
        {
            try
            {
                using (MySqlCommand cmd = sqlConnection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    MySqlDataReader reader = await cmd.ExecuteReaderAsync();
                    List<T> list = reader.Parse<T>().ToList();
                    reader.Close();
                    return list;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return null;
            }
        }

        public static async Task<int> SqlQuery(MySqlConnection sqlConnection, string sql)
        {
            try
            {
                using (MySqlCommand cmd = sqlConnection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    int result = await cmd.ExecuteNonQueryAsync();
                    return result;
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                return -1;
            }
        }
    }
}