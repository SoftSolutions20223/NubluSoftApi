using Npgsql;
using NubluSoft_Storage.Services;

namespace NubluSoft_Storage.Services
{
    /// <summary>
    /// Implementación de factory para conexiones PostgreSQL
    /// </summary>
    public class PostgresConnectionFactory : IPostgresConnectionFactory
    {
        private readonly string _connectionString;
        private readonly ILogger<PostgresConnectionFactory> _logger;

        public PostgresConnectionFactory(
            IConfiguration configuration,
            ILogger<PostgresConnectionFactory> logger)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionString 'PostgreSQL' no configurado");
            _logger = logger;

            _logger.LogInformation("PostgresConnectionFactory inicializado");
        }

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }

        public string GetConnectionInfo()
        {
            // Retorna info sin password para logging seguro
            var builder = new NpgsqlConnectionStringBuilder(_connectionString);
            return $"Host={builder.Host};Port={builder.Port};Database={builder.Database};Username={builder.Username}";
        }
    }
}
