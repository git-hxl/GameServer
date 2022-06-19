using Dapper;
using MySqlConnector;

namespace MasterServer.DB
{
    public static class DBHelper
    {
        public static async Task<MySqlConnection?> CreateConnection()
        {
            if (MasterApplication.Instance.ServerConfig == null)
                return null;
            string dbConnectStr = MasterApplication.Instance.ServerConfig.DBConnectStr;
            var connection = new MySqlConnection(dbConnectStr);
            await connection.OpenAsync();
            return connection;
        }

        public static async Task<List<T>?> SqlSelect<T>(MySqlConnection sqlConnection, string sql)
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

        public static async Task<int> SqlQuery(MySqlConnection sqlConnection, string sql)
        {
            using (MySqlCommand cmd = sqlConnection.CreateCommand())
            {
                cmd.CommandText = sql;
                int result = await cmd.ExecuteNonQueryAsync();
                return result;
            }
        }
    }
}
