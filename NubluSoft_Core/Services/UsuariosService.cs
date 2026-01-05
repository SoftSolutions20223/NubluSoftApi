using Dapper;
using NubluSoft_Core.Models.DTOs;
using NubluSoft_Core.Models.Entities;

namespace NubluSoft_Core.Services
{
    public class UsuariosService : IUsuariosService
    {
        private readonly IPostgresConnectionFactory _connectionFactory;
        private readonly ILogger<UsuariosService> _logger;

        public UsuariosService(
            IPostgresConnectionFactory connectionFactory,
            ILogger<UsuariosService> logger)
        {
            _connectionFactory = connectionFactory;
            _logger = logger;
        }

        public async Task<IEnumerable<Usuario>> ObtenerPorEntidadAsync(long entidadId)
        {
            const string sql = @"
                SELECT 
                    u.""Cod"",
                    u.""Nombres"",
                    u.""Apellidos"",
                    u.""Telefono"",
                    u.""Documento"",
                    u.""Usuario"" AS ""Usuario_"",
                    u.""Estado"",
                    u.""Entidad"",
                    u.""Correo"",
                    u.""TipoDocumento"",
                    u.""Cargo"",
                    u.""FechaCreacion"",
                    u.""FechaUltimoAcceso"",
                    u.""OficinaPredeterminada"",
                    u.""NotificacionesCorreo"",
                    o.""Nombre"" AS ""NombreOficinaPredeterminada"",
                    td.""Nombre"" AS ""NombreTipoDocumento""
                FROM documentos.""Usuarios"" u
                LEFT JOIN documentos.""Oficinas"" o ON u.""OficinaPredeterminada"" = o.""Cod"" AND u.""Entidad"" = o.""Entidad""
                LEFT JOIN documentos.""Tipos_Documento_Identidad"" td ON u.""TipoDocumento"" = td.""Cod""
                WHERE u.""Entidad"" = @EntidadId AND u.""Estado"" = true
                ORDER BY u.""Nombres"", u.""Apellidos""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<Usuario>(sql, new { EntidadId = entidadId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuarios de entidad {EntidadId}", entidadId);
                return Enumerable.Empty<Usuario>();
            }
        }

        public async Task<UsuarioConRoles?> ObtenerPorIdAsync(long entidadId, long usuarioId)
        {
            const string sqlUsuario = @"
                SELECT 
                    u.""Cod"",
                    u.""Nombres"",
                    u.""Apellidos"",
                    u.""Telefono"",
                    u.""Documento"",
                    u.""Usuario"" AS ""Usuario_"",
                    u.""Estado"",
                    u.""Entidad"",
                    u.""Correo"",
                    u.""TipoDocumento"",
                    u.""Cargo"",
                    u.""Foto"",
                    u.""FechaCreacion"",
                    u.""FechaUltimoAcceso"",
                    u.""FirmaDigitalRuta"",
                    u.""FirmaDigitalVence"",
                    u.""IntentosFallidos"",
                    u.""FechaBloqueo"",
                    u.""DebeCambiarContrasena"",
                    u.""FechaCambioContrasena"",
                    u.""NotificacionesCorreo"",
                    u.""OficinaPredeterminada"",
                    o.""Nombre"" AS ""NombreOficinaPredeterminada"",
                    td.""Nombre"" AS ""NombreTipoDocumento""
                FROM documentos.""Usuarios"" u
                LEFT JOIN documentos.""Oficinas"" o ON u.""OficinaPredeterminada"" = o.""Cod"" AND u.""Entidad"" = o.""Entidad""
                LEFT JOIN documentos.""Tipos_Documento_Identidad"" td ON u.""TipoDocumento"" = td.""Cod""
                WHERE u.""Entidad"" = @EntidadId AND u.""Cod"" = @UsuarioId";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var usuario = await connection.QueryFirstOrDefaultAsync<UsuarioConRoles>(sqlUsuario,
                    new { EntidadId = entidadId, UsuarioId = usuarioId });

                if (usuario != null)
                {
                    var roles = await ObtenerRolesUsuarioAsync(usuarioId);
                    usuario.Roles = roles.ToList();
                }

                return usuario;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo usuario {UsuarioId} de entidad {EntidadId}", usuarioId, entidadId);
                return null;
            }
        }

        public async Task<(Usuario? Usuario, string? Error)> CrearAsync(long entidadId, CrearUsuarioRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Validar que el usuario no exista
                    var usuarioExiste = await connection.QueryFirstOrDefaultAsync<long?>(
                        @"SELECT ""Cod"" FROM documentos.""Usuarios"" 
                          WHERE ""Entidad"" = @EntidadId AND ""Usuario"" = @Usuario",
                        new { EntidadId = entidadId, request.Usuario }, transaction);

                    if (usuarioExiste.HasValue)
                        return (null, "El nombre de usuario ya existe");

                    // Validar documento único
                    var documentoExiste = await connection.QueryFirstOrDefaultAsync<long?>(
                        @"SELECT ""Cod"" FROM documentos.""Usuarios"" 
                          WHERE ""Entidad"" = @EntidadId AND ""Documento"" = @Documento",
                        new { EntidadId = entidadId, request.Documento }, transaction);

                    if (documentoExiste.HasValue)
                        return (null, "El número de documento ya está registrado");

                    // Obtener siguiente código
                    var nextCod = await connection.QueryFirstAsync<long>(
                        @"SELECT COALESCE(MAX(""Cod""), 0) + 1 FROM documentos.""Usuarios"" WHERE ""Entidad"" = @EntidadId",
                        new { EntidadId = entidadId }, transaction);

                    // Insertar en documentos.Usuarios
                    const string sqlInsertDoc = @"
                        INSERT INTO documentos.""Usuarios"" 
                            (""Cod"", ""Nombres"", ""Apellidos"", ""Telefono"", ""Documento"", ""Usuario"", 
                             ""Contraseña"", ""Estado"", ""Entidad"", ""Correo"", ""TipoDocumento"", ""Cargo"",
                             ""FechaCreacion"", ""NotificacionesCorreo"", ""OficinaPredeterminada"", ""DebeCambiarContrasena"")
                        VALUES 
                            (@Cod, @Nombres, @Apellidos, @Telefono, @Documento, @Usuario, 
                             @Contrasena, true, @Entidad, @Correo, @TipoDocumento, @Cargo,
                             @FechaCreacion, @NotificacionesCorreo, @OficinaPredeterminada, false)";

                    await connection.ExecuteAsync(sqlInsertDoc, new
                    {
                        Cod = nextCod,
                        request.Nombres,
                        request.Apellidos,
                        request.Telefono,
                        request.Documento,
                        request.Usuario,
                        request.Contrasena,
                        Entidad = entidadId,
                        request.Correo,
                        request.TipoDocumento,
                        request.Cargo,
                        FechaCreacion = DateTime.Now,
                        request.NotificacionesCorreo,
                        request.OficinaPredeterminada
                    }, transaction);

                    // Insertar en usuarios.Usuarios (para autenticación)
                    const string sqlInsertAuth = @"
                        INSERT INTO usuarios.""Usuarios"" 
                            (""Cod"", ""Nombres"", ""Apellidos"", ""Telefono"", ""Documento"", ""Usuario"", 
                             ""Contraseña"", ""Estado"", ""Entidad"", ""SesionActiva"", ""Token"")
                        VALUES 
                            (@Cod, @Nombres, @Apellidos, @Telefono, @Documento, @Usuario, 
                             @Contrasena, true, @Entidad, false, NULL)";

                    await connection.ExecuteAsync(sqlInsertAuth, new
                    {
                        Cod = nextCod,
                        request.Nombres,
                        request.Apellidos,
                        request.Telefono,
                        request.Documento,
                        request.Usuario,
                        request.Contrasena,
                        Entidad = entidadId
                    }, transaction);

                    // Asignar roles si se especificaron
                    if (request.Roles != null && request.Roles.Any())
                    {
                        foreach (var rol in request.Roles)
                        {
                            await AsignarRolInternoAsync(connection, transaction, entidadId, nextCod, rol);
                        }
                    }

                    await transaction.CommitAsync();

                    var usuario = await ObtenerPorIdAsync(entidadId, nextCod);
                    return (usuario, null);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando usuario en entidad {EntidadId}", entidadId);
                return (null, "Error al crear el usuario: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> ActualizarAsync(long entidadId, long usuarioId, ActualizarUsuarioRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Verificar que existe
                    var existe = await connection.QueryFirstOrDefaultAsync<long?>(
                        @"SELECT ""Cod"" FROM documentos.""Usuarios"" WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId",
                        new { EntidadId = entidadId, UsuarioId = usuarioId }, transaction);

                    if (!existe.HasValue)
                        return (false, "El usuario no existe");

                    // Actualizar documentos.Usuarios
                    const string sqlUpdateDoc = @"
                        UPDATE documentos.""Usuarios"" SET
                            ""Nombres"" = @Nombres,
                            ""Apellidos"" = @Apellidos,
                            ""Telefono"" = @Telefono,
                            ""Documento"" = @Documento,
                            ""Correo"" = @Correo,
                            ""TipoDocumento"" = @TipoDocumento,
                            ""Cargo"" = @Cargo,
                            ""OficinaPredeterminada"" = @OficinaPredeterminada,
                            ""NotificacionesCorreo"" = @NotificacionesCorreo,
                            ""Estado"" = @Estado
                        WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId";

                    await connection.ExecuteAsync(sqlUpdateDoc, new
                    {
                        EntidadId = entidadId,
                        UsuarioId = usuarioId,
                        request.Nombres,
                        request.Apellidos,
                        request.Telefono,
                        request.Documento,
                        request.Correo,
                        request.TipoDocumento,
                        request.Cargo,
                        request.OficinaPredeterminada,
                        request.NotificacionesCorreo,
                        request.Estado
                    }, transaction);

                    // Actualizar usuarios.Usuarios (para autenticación)
                    const string sqlUpdateAuth = @"
                        UPDATE usuarios.""Usuarios"" SET
                            ""Nombres"" = @Nombres,
                            ""Apellidos"" = @Apellidos,
                            ""Telefono"" = @Telefono,
                            ""Documento"" = @Documento,
                            ""Estado"" = @Estado
                        WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId";

                    await connection.ExecuteAsync(sqlUpdateAuth, new
                    {
                        EntidadId = entidadId,
                        UsuarioId = usuarioId,
                        request.Nombres,
                        request.Apellidos,
                        request.Telefono,
                        request.Documento,
                        request.Estado
                    }, transaction);

                    await transaction.CommitAsync();
                    return (true, null);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error actualizando usuario {UsuarioId} en entidad {EntidadId}", usuarioId, entidadId);
                return (false, "Error al actualizar el usuario: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> EliminarAsync(long entidadId, long usuarioId)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    // Verificar que existe
                    var existe = await connection.QueryFirstOrDefaultAsync<long?>(
                        @"SELECT ""Cod"" FROM documentos.""Usuarios"" WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId",
                        new { EntidadId = entidadId, UsuarioId = usuarioId }, transaction);

                    if (!existe.HasValue)
                        return (false, "El usuario no existe");

                    // Soft delete en documentos.Usuarios
                    await connection.ExecuteAsync(
                        @"UPDATE documentos.""Usuarios"" SET ""Estado"" = false WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId",
                        new { EntidadId = entidadId, UsuarioId = usuarioId }, transaction);

                    // Soft delete en usuarios.Usuarios
                    await connection.ExecuteAsync(
                        @"UPDATE usuarios.""Usuarios"" SET ""Estado"" = false WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId",
                        new { EntidadId = entidadId, UsuarioId = usuarioId }, transaction);

                    // Desactivar roles
                    await connection.ExecuteAsync(
                        @"UPDATE documentos.""Roles_Usuarios"" SET ""Estado"" = false WHERE ""Usuario"" = @UsuarioId",
                        new { UsuarioId = usuarioId }, transaction);

                    await transaction.CommitAsync();
                    return (true, null);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error eliminando usuario {UsuarioId} en entidad {EntidadId}", usuarioId, entidadId);
                return (false, "Error al eliminar el usuario: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> CambiarContrasenaAsync(long entidadId, long usuarioId, CambiarContrasenaRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                // Verificar contraseña actual
                var contrasenaActual = await connection.QueryFirstOrDefaultAsync<string>(
                    @"SELECT ""Contraseña"" FROM usuarios.""Usuarios"" WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId",
                    new { EntidadId = entidadId, UsuarioId = usuarioId });

                if (contrasenaActual == null)
                    return (false, "Usuario no encontrado");

                if (contrasenaActual != request.ContrasenaActual)
                    return (false, "La contraseña actual es incorrecta");

                // Actualizar en ambas tablas
                using var transaction = await connection.BeginTransactionAsync();
                try
                {
                    await connection.ExecuteAsync(
                        @"UPDATE usuarios.""Usuarios"" SET ""Contraseña"" = @Nueva WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId",
                        new { EntidadId = entidadId, UsuarioId = usuarioId, Nueva = request.ContrasenaNueva }, transaction);

                    await connection.ExecuteAsync(
                        @"UPDATE documentos.""Usuarios"" SET ""Contraseña"" = @Nueva, ""FechaCambioContrasena"" = @Fecha, ""DebeCambiarContrasena"" = false 
                          WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId",
                        new { EntidadId = entidadId, UsuarioId = usuarioId, Nueva = request.ContrasenaNueva, Fecha = DateTime.Now }, transaction);

                    await transaction.CommitAsync();
                    return (true, null);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cambiando contraseña de usuario {UsuarioId}", usuarioId);
                return (false, "Error al cambiar la contraseña: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> ResetearContrasenaAsync(long entidadId, long usuarioId, ResetearContrasenaRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                try
                {
                    await connection.ExecuteAsync(
                        @"UPDATE usuarios.""Usuarios"" SET ""Contraseña"" = @Nueva WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId",
                        new { EntidadId = entidadId, UsuarioId = usuarioId, Nueva = request.ContrasenaNueva }, transaction);

                    await connection.ExecuteAsync(
                        @"UPDATE documentos.""Usuarios"" SET ""Contraseña"" = @Nueva, ""FechaCambioContrasena"" = @Fecha, ""DebeCambiarContrasena"" = @Debe, ""IntentosFallidos"" = 0, ""FechaBloqueo"" = NULL 
                          WHERE ""Entidad"" = @EntidadId AND ""Cod"" = @UsuarioId",
                        new { EntidadId = entidadId, UsuarioId = usuarioId, Nueva = request.ContrasenaNueva, Fecha = DateTime.Now, Debe = request.DebeCambiarContrasena }, transaction);

                    await transaction.CommitAsync();
                    return (true, null);
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reseteando contraseña de usuario {UsuarioId}", usuarioId);
                return (false, "Error al resetear la contraseña: " + ex.Message);
            }
        }

        public async Task<IEnumerable<RolUsuario>> ObtenerRolesUsuarioAsync(long usuarioId)
        {
            const string sql = @"
                SELECT 
                    ru.""Cod"",
                    ru.""Oficina"",
                    o.""Nombre"" AS ""NombreOficina"",
                    ru.""Rol"",
                    r.""Nombre"" AS ""NombreRol"",
                    ru.""Estado"",
                    ru.""FechaInicio"",
                    ru.""FechaFin""
                FROM documentos.""Roles_Usuarios"" ru
                INNER JOIN documentos.""Oficinas"" o ON ru.""Oficina"" = o.""Cod""
                INNER JOIN documentos.""Roles"" r ON ru.""Rol"" = r.""Cod""
                WHERE ru.""Usuario"" = @UsuarioId AND ru.""Estado"" = true
                ORDER BY o.""Nombre"", r.""Nombre""";

            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await connection.QueryAsync<RolUsuario>(sql, new { UsuarioId = usuarioId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo roles de usuario {UsuarioId}", usuarioId);
                return Enumerable.Empty<RolUsuario>();
            }
        }

        public async Task<(bool Success, string? Error)> AsignarRolAsync(long entidadId, long usuarioId, AsignarRolRequest request)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();
                return await AsignarRolInternoAsync(connection, null, entidadId, usuarioId, request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error asignando rol a usuario {UsuarioId}", usuarioId);
                return (false, "Error al asignar el rol: " + ex.Message);
            }
        }

        public async Task<(bool Success, string? Error)> RevocarRolAsync(long entidadId, long usuarioId, long oficina, long rol)
        {
            try
            {
                using var connection = _connectionFactory.CreateConnection();
                await connection.OpenAsync();

                var affected = await connection.ExecuteAsync(
                    @"UPDATE documentos.""Roles_Usuarios"" SET ""Estado"" = false, ""FechaFin"" = @Fecha
                      WHERE ""Usuario"" = @UsuarioId AND ""Oficina"" = @Oficina AND ""Rol"" = @Rol AND ""Estado"" = true",
                    new { UsuarioId = usuarioId, Oficina = oficina, Rol = rol, Fecha = DateTime.Now });

                if (affected == 0)
                    return (false, "El rol no está asignado al usuario");

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revocando rol de usuario {UsuarioId}", usuarioId);
                return (false, "Error al revocar el rol: " + ex.Message);
            }
        }

        // ==================== HELPERS ====================

        private async Task<(bool Success, string? Error)> AsignarRolInternoAsync(
            Npgsql.NpgsqlConnection connection,
            Npgsql.NpgsqlTransaction? transaction,
            long entidadId,
            long usuarioId,
            AsignarRolRequest request)
        {
            // Verificar que no exista ya
            var existe = await connection.QueryFirstOrDefaultAsync<long?>(
                @"SELECT ""Cod"" FROM documentos.""Roles_Usuarios"" 
                  WHERE ""Usuario"" = @UsuarioId AND ""Oficina"" = @Oficina AND ""Rol"" = @Rol AND ""Estado"" = true",
                new { UsuarioId = usuarioId, request.Oficina, request.Rol }, transaction);

            if (existe.HasValue)
                return (false, "El rol ya está asignado en esa oficina");

            // Obtener siguiente código
            var nextCod = await connection.QueryFirstAsync<long>(
                @"SELECT COALESCE(MAX(""Cod""), 0) + 1 FROM documentos.""Roles_Usuarios""",
                transaction: transaction);

            await connection.ExecuteAsync(
                @"INSERT INTO documentos.""Roles_Usuarios"" (""Cod"", ""Oficina"", ""Rol"", ""Usuario"", ""Estado"", ""FechaInicio"")
                  VALUES (@Cod, @Oficina, @Rol, @Usuario, true, @Fecha)",
                new { Cod = nextCod, request.Oficina, request.Rol, Usuario = usuarioId, Fecha = DateTime.Now }, transaction);

            return (true, null);
        }
    }
}