using Npgsql;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Implementación del factory de conexiones PostgreSQL
    /// </summary>
    public class PostgresConnectionFactory : IPostgresConnectionFactory
    {
        private readonly string _connectionString;
        private readonly ILogger<PostgresConnectionFactory> _logger;

        public PostgresConnectionFactory(IConfiguration configuration, ILogger<PostgresConnectionFactory> logger)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionString 'PostgreSQL' no configurado");
            _logger = logger;

            _logger.LogInformation("PostgresConnectionFactory inicializado");
        }

        public string ConnectionString => _connectionString;

        public NpgsqlConnection CreateConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }
    }
}