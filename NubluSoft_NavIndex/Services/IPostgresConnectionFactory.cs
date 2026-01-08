using Npgsql;

namespace NubluSoft_NavIndex.Services
{
    /// <summary>
    /// Factory para crear conexiones a PostgreSQL
    /// </summary>
    public interface IPostgresConnectionFactory
    {
        NpgsqlConnection CreateConnection();
        string GetConnectionInfo();
    }
}