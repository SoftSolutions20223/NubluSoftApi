using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class OficinasService : IOficinasService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<OficinasService> _logger;

        public OficinasService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<OficinasService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Oficina>> ObtenerPorEntidadAsync(long entidadId)
        {
            const string sql = @"
                SELECT 
                    o.""Cod"",
                    o.""Nombre"",
                    o.""Estado"",
                    o.""Entidad"",
                    o.""Codigo"",
                    o.""OficinaPadre"",
                    o.""Responsable"",
                    o.""NivelJerarquico"",
                    o.""FechaCreacion"",
                    o.""Sigla"",
                    o.""Telefono"",
                    o.""Correo"",
                    o.""Ubicacion"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreResponsable"",
                    op.""Nombre"" AS ""NombreOficinaPadre""
                FROM documentos.""Oficinas"" o
                LEFT JOIN usuarios.""Usuarios"" u ON o.""Responsable"" = u.""Cod"" AND o.""Entidad"" = u.""Entidad""
                LEFT JOIN documentos.""Oficinas"" op ON o.""OficinaPadre"" = op.""Cod"" AND o.""Entidad"" = op.""Entidad""
                WHERE o.""Entidad"" = @EntidadId AND o.""Estado"" = true
                ORDER BY o.""NivelJerarquico"", o.""Codigo"", o.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Oficina>(sql, new { EntidadId = entidadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo oficinas de entidad {EntidadId}", entidadId);
                return Enumerable.Empty<Oficina>();
            }
        }

        public async Task<Oficina?> ObtenerPorIdAsync(long entidadId, long oficinaId)
        {
            const string sql = @"
                SELECT 
                    o.""Cod"",
                    o.""Nombre"",
                    o.""Estado"",
                    o.""Entidad"",
                    o.""Codigo"",
                    o.""OficinaPadre"",
                    o.""Responsable"",
                    o.""NivelJerarquico"",
                    o.""FechaCreacion"",
                    o.""Sigla"",
                    o.""Telefono"",
                    o.""Correo"",
                    o.""Ubicacion"",
                    u.""Nombres"" || ' ' || u.""Apellidos"" AS ""NombreResponsable"",
                    op.""Nombre"" AS ""NombreOficinaPadre""
                FROM documentos.""Oficinas"" o
                LEFT JOIN usuarios.""Usuarios"" u ON o.""Responsable"" = u.""Cod"" AND o.""Entidad"" = u.""Entidad""
                LEFT JOIN documentos.""Oficinas"" op ON o.""OficinaPadre"" = op.""Cod"" AND o.""Entidad"" = op.""Entidad""
                WHERE o.""Entidad"" = @EntidadId AND o.""Cod"" = @OficinaId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryFirstOrDefaultAsync<Oficina>(sql, new { EntidadId = entidadId, OficinaId = oficinaId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo oficina {OficinaId} de entidad {EntidadId}", oficinaId, entidadId);
                return null;
            }
        }

        public async Task<IEnumerable<OficinaArbol>> ObtenerArbolAsync(long entidadId)
        {
            var oficinas = await ObtenerPorEntidadAsync(entidadId);
            return ConstruirArbol(oficinas.ToList());
        }

        public async Task<(Oficina? Oficina, string? Error)> CrearAsync(long entidadId, CrearOficinaRequest request)
        {
            const string sqlNextCod = @"
                SELECT COALESCE(MAX(""Cod""), 0) + 1 
                FROM documentos.""Oficinas"" 
                WHERE ""Entidad"" = @EntidadId";

            const string sqlInsert = @"
                INSERT INTO documentos.""Oficinas"" 
                    (""Cod"", ""Nombre"", ""Estado"", ""Entidad"", ""Codigo"", ""OficinaPadre"", 
                     ""Responsable"", ""NivelJerarquico"", ""FechaCreacion"", ""Sigla"", 
                     ""Telefono"", ""Correo"", ""Ubicacion"")
                VALUES 
                    (@Cod, @Nombre, true, @Entidad, @Codigo, @OficinaPadre, 
                     @Responsable, @NivelJerarquico, @FechaCreacion, @Sigla,
                     @Telefono, @Correo, @Ubicacion)
                RETURNING ""Cod""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Validar oficina padre si se especifica
                if (request.OficinaPadre.HasValue)
                {
                    var padreExiste = await connection.QueryFirstOrDefaultAsync<long?>(
                        @"SELECT ""Cod"" FROM documentos.""Oficinas"" 
                          WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @PadreId AND ""Estado"" = true",
                        new { EntidadId = entidadId, PadreId = request.OficinaPadre.Value });

                    if (!padreExiste.HasValue)
                        return (null, "La oficina padre especificada no existe");
                }

                // Obtener siguiente código
                var nextCod = await connection.QueryFirstAsync<long>(sqlNextCod, new { EntidadId = entidadId });

                // Calcular nivel jerárquico
                int nivelJerarquico = request.NivelJerarquico ?? 1;
                if (request.OficinaPadre.HasValue && !request.NivelJerarquico.HasValue)
                {
                    var nivelPadre = await connection.QueryFirstOrDefaultAsync<int?>(
                        @"SELECT ""NivelJerarquico"" FROM documentos.""Oficinas"" 
                          WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @PadreId",
                        new { EntidadId = entidadId, PadreId = request.OficinaPadre.Value });
                    nivelJerarquico = (nivelPadre ?? 0) + 1;
                }

                var parameters = new
                {
                    Cod = nextCod,
                    request.Nombre,
                    Entidad = entidadId,
                    request.Codigo,
                    request.OficinaPadre,
                    request.Responsable,
                    NivelJerarquico = nivelJerarquico,
                    FechaCreacion = DateTime.Now,
                    request.Sigla,
                    request.Telefono,
                    request.Correo,
                    request.Ubicacion
                };

                await connection.ExecuteAsync(sqlInsert, parameters);

                var oficina = await ObtenerPorIdAsync(entidadId, nextCod);
                return (oficina, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando oficina en entidad {EntidadId}", entidadId);
                return (null, "Error al crear la oficina: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> ActualizarAsync(long entidadId, long oficinaId, ActualizarOficinaRequest request)
        {
            const string sql = @"
                UPDATE documentos.""Oficinas"" SET
                    ""Nombre"" = @Nombre,
                    ""Codigo"" = @Codigo,
                    ""OficinaPadre"" = @OficinaPadre,
                    ""Responsable"" = @Responsable,
                    ""NivelJerarquico"" = @NivelJerarquico,
                    ""Sigla"" = @Sigla,
                    ""Telefono"" = @Telefono,
                    ""Correo"" = @Correo,
                    ""Ubicacion"" = @Ubicacion,
                    ""Estado"" = @Estado
                WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @OficinaId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar que existe
                var existe = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""Cod"" FROM documentos.""Oficinas"" WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @OficinaId",
                    new { EntidadId = entidadId, OficinaId = oficinaId });

                if (!existe.HasValue)
                    return (false, "La oficina no existe");

                // Validar que no se asigne como padre a sí misma o a un descendiente
                if (request.OficinaPadre.HasValue)
                {
                    if (request.OficinaPadre.Value == oficinaId)
                        return (false, "Una oficina no puede ser padre de sí misma");

                    // Verificar que el padre no sea un descendiente
                    var esDescendiente = await EsDescendienteAsync(connection, entidadId, oficinaId, request.OficinaPadre.Value);
                    if (esDescendiente)
                        return (false, "No se puede asignar un descendiente como oficina padre");
                }

                var parameters = new
                {
                    EntidadId = entidadId,
                    OficinaId = oficinaId,
                    request.Nombre,
                    request.Codigo,
                    request.OficinaPadre,
                    request.Responsable,
                    request.NivelJerarquico,
                    request.Sigla,
                    request.Telefono,
                    request.Correo,
                    request.Ubicacion,
                    request.Estado
                };

                await connection.ExecuteAsync(sql, parameters);
                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando oficina {OficinaId} en entidad {EntidadId}", oficinaId, entidadId);
                return (false, "Error al actualizar la oficina: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> EliminarAsync(long entidadId, long oficinaId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar que existe
                var existe = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""Cod"" FROM documentos.""Oficinas"" WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @OficinaId",
                    new { EntidadId = entidadId, OficinaId = oficinaId });

                if (!existe.HasValue)
                    return (false, "La oficina no existe");

                // Verificar que no tenga hijos activos
                var tieneHijos = await connection.QueryFirstOrDefaultAsync<long?>(
                    @"SELECT ""Cod"" FROM documentos.""Oficinas"" 
                      WHERE ""Entidad"" = @EntidadId AND ""OficinaPadre"" = @OficinaId AND ""Estado"" = true
                      LIMIT 1",
                    new { EntidadId = entidadId, OficinaId = oficinaId });

                if (tieneHijos.HasValue)
                    return (false, "No se puede eliminar una oficina que tiene oficinas dependientes activas");

                // Soft delete
                await connection.ExecuteAsync(
                    @"UPDATE documentos.""Oficinas"" SET ""Estado"" = false 
                      WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @OficinaId",
                    new { EntidadId = entidadId, OficinaId = oficinaId });

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando oficina {OficinaId} en entidad {EntidadId}", oficinaId, entidadId);
                return (false, "Error al eliminar la oficina: " + ex.Message);
            }
        }

        // ==================== HELPERS ====================

        private List<OficinaArbol> ConstruirArbol(List<Oficina> oficinas)
        {
            var lookup = oficinas.ToDictionary(o => o.Cod, o => new OficinaArbol
            {
                Cod = o.Cod,
                Nombre = o.Nombre,
                Estado = o.Estado,
                Entidad = o.Entidad,
                Codigo = o.Codigo,
                OficinaPadre = o.OficinaPadre,
                Responsable = o.Responsable,
                NivelJerarquico = o.NivelJerarquico,
                FechaCreacion = o.FechaCreacion,
                Sigla = o.Sigla,
                Telefono = o.Telefono,
                Correo = o.Correo,
                Ubicacion = o.Ubicacion,
                NombreResponsable = o.NombreResponsable,
                NombreOficinaPadre = o.NombreOficinaPadre
            });

            var raices = new List<OficinaArbol>();

            foreach (var oficina in lookup.Values)
            {
                if (oficina.OficinaPadre.HasValue && lookup.ContainsKey(oficina.OficinaPadre.Value))
                {
                    lookup[oficina.OficinaPadre.Value].Hijos.Add(oficina);
                }
                else
                {
                    raices.Add(oficina);
                }
            }

            return raices;
        }

        private async Task<bool> EsDescendienteAsync(Npgsql.NpgsqlConnection connection, long entidadId, long oficinaId, long posibleDescendienteId)
        {
            const string sql = @"
                WITH RECURSIVE descendientes AS (
                    SELECT ""Cod"" FROM documentos.""Oficinas"" 
                    WHERE ""Entidad"" = @EntidadId AND ""OficinaPadre"" = @OficinaId AND ""Estado"" = true
                    UNION ALL
                    SELECT o.""Cod"" FROM documentos.""Oficinas"" o
                    INNER JOIN descendientes d ON o.""OficinaPadre"" = d.""Cod""
                    WHERE o.""Entidad"" = @EntidadId AND o.""Estado"" = true
                )
                SELECT EXISTS(SELECT 1 FROM descendientes WHERE ""Cod"" = @PosibleDescendienteId)";

            return await connection.QueryFirstAsync<bool>(sql, new { EntidadId = entidadId, OficinaId = oficinaId, PosibleDescendienteId = posibleDescendienteId });
        }
    }
}