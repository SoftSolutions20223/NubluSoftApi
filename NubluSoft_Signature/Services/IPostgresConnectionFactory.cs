using Npgsql;

namespace NubluSoft_Signature.Services
{
    /// <summary>
    /// Factory para crear conexiones a PostgreSQL
    /// </summary>
    public interface IPostgresConnectionFactory
    {
        /// <summary>
        /// Crea una nueva conexión a PostgreSQL
        /// </summary>
        NpgsqlConnection CreateConnection();

        /// <summary>
        /// Obtiene el connection string
        /// </summary>
        string ConnectionString { get; }
    }
}