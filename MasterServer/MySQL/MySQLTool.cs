using Dapper;
using MySqlConnector;
using Serilog;

namespace MasterServer.MySQL
{
    internal class MySQLTool
    {
        public static string SQLConnectionStr = "";
        public static async Task<IEnumerable<T>> QueryAsync<T>(string sql)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(SQLConnectionStr))
                {
                    await conn.OpenAsync();
                    var result = await conn.QueryAsync<T>(sql);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
                return null;
            }
        }

        public static async Task<T> QueryFirstOrDefaultAsync<T>(string sql)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(SQLConnectionStr))
                {
                    await conn.OpenAsync();
                    var result = await conn.QueryFirstOrDefaultAsync<T>(sql);
                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
                return default(T);
            }
        }

        public static async Task<int> ExecuteAsync(string sql)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(SQLConnectionStr))
                {
                    await conn.OpenAsync();
                    var result = await conn.ExecuteAsync(sql);
                    return result;
                }
            }
            catch(Exception ex)
            {
                Log.Information(ex.Message);
                return -1;
            }
        }
    }
}