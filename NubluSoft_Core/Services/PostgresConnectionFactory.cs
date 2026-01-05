using Npgsql;

namespace NubluSoft_Core.Services
{
    public interface IPostgresConnectionFactory
    {
        NpgsqlConnection CreateConnection();
    }

    public class PostgresConnectionFactory : IPostgresConnectionFactory
    {
        private readonly string _connectionString;

        public PostgresConnectionFactory(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionString 'PostgreSQL' no configurado");
        }

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}