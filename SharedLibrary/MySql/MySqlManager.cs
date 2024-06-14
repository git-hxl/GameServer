using Dapper;
using MySqlConnector;
using Serilog;

namespace SharedLibrary
{
    public class MySqlManager
    {
        public static MySqlManager Instance { get; private set; } = new MySqlManager();

        public string SQLConnectionStr { get; private set; } = "";

        public void Init(string connectStr)
        {
            SQLConnectionStr = connectStr;
        }

        public async Task<MySqlConnection> GetConnection()
        {
            MySqlConnection conn = new MySqlConnection(SQLConnectionStr);

            await conn.OpenAsync();

            return conn;
        }

        public async Task<List<T>> QueryAsync<T>(string sql)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(SQLConnectionStr))
                {
                    await conn.OpenAsync();
                    var result = await conn.QueryAsync<T>(sql);
                    return result.ToList();
                }
            }
            catch (Exception ex)
            {
                Log.Information(ex.Message);
                return null;
            }
        }

        public async Task<T> QueryFirstOrDefaultAsync<T>(string sql)
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
                return default;
            }
        }

        public async Task<int> ExecuteAsync(string sql)
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
            catch (Exception ex)
            {
                Log.Information(ex.Message);
                return -1;
            }
        }
    }
}