using Dapper;
using MCollector.Core.Common;
using MCollector.Core.Contracts;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;
using System.Data;
using System.Data.Common;
using System.Text.Json;
using YamlDotNet.Core.Tokens;

namespace MCollector.Plugins.Sql
{
    public class SqlCollector : ICollector
    {
        public string Type => "sql";

        public async Task<CollectedData> Collect(CollectTarget target)
        {
            var data = new CollectedData(target.Name, target);

            if (target.Contents.Any() == true)
            {
                var args = SerializerHelper.CreateFrom<SqlCollectorArgs>(target.Args) ?? new SqlCollectorArgs();

                using var connection = CreateConnection(target.Target, args);

                connection.Open();
                var length = target.Contents.Length;
                for (var i = 0; i < length; i++)
                {
                    if (i < length - 1)
                    {
                        await connection.ExecuteAsync(target.Contents[i], commandTimeout: args.Timeout);
                    }
                    else
                    {
                        data.Content = JsonSerializer.Serialize(await connection.QueryAsync(target.Contents[i]));
                    }
                }
            }
            else
            {
                //data.IsSuccess = false;
                data.Content = "{}";  
            }

            return data;
        }

        private IDbConnection CreateConnection(string connStr, SqlCollectorArgs args)
        {
            IDbConnection conn = null;

            switch (args.Type)
            {
                case "sqlite":
                    throw new NotImplementedException();//好像没有使用本地库的场景
                    break;
                case "mysql":
                    conn = new MySqlConnection(connStr);
                    break;
                case "pgsql":
                    conn = new Npgsql.NpgsqlConnection(connStr);
                    break;
                default:
                case "mssql":
                    conn = new SqlConnection(connStr);
                    break;
            }

            return conn;
        }
    }

    internal class SqlCollectorArgs
    {
        /// <summary>
        /// 数据库类型 mssql/mysql/pgsql/sqlite
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// 超时时间秒
        /// </summary>
        public int Timeout { get; set; } = 300;
    }
}