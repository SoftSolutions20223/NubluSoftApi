# TROUBLESHOOTING - NubluSoft

## 1. Problemas de Conexión

### Error: "Servicio no disponible" (503)

**Causa:** El microservicio destino no está corriendo.

**Solución:**
1. Verificar que el servicio esté corriendo:
   ```bash
   curl http://localhost:5001/health  # Core
   curl http://localhost:5002/health  # Storage
   curl http://localhost:5003/health  # NavIndex
   curl http://localhost:5004/health  # Signature
   ```
2. Iniciar el servicio faltante:
   ```bash
   cd NubluSoft_Core && dotnet run
   ```

### Error: "Connection refused" a PostgreSQL

**Causa:** PostgreSQL no está corriendo o credenciales incorrectas.

**Solución:**
1. Verificar servicio PostgreSQL:
   ```bash
   # Windows
   net start postgresql-x64-17

   # O verificar en pgAdmin
   ```
2. Verificar ConnectionString en appsettings.json:
   ```json
   "ConnectionStrings": {
     "PostgreSQL": "Host=localhost;Port=5432;Database=nublusoft;Username=postgres;Password=8213;SearchPath=usuarios,documentos,public"
   }
   ```

### Error: "Connection refused" a Redis

**Causa:** Redis no está corriendo.

**Solución:**
1. Iniciar Redis:
   ```bash
   # Windows con Redis instalado
   redis-server

   # O verificar si está corriendo
   redis-cli ping  # Debe responder PONG
   ```
2. Verificar configuración en Gateway/NavIndex:
   ```json
   "Redis": {
     "ConnectionString": "localhost:6379"
   }
   ```

---

## 2. Problemas de Autenticación

### Error: "Token inválido o expirado" (401)

**Causas posibles:**
1. Token expirado (por defecto expira en 8 horas)
2. Sesión eliminada de Redis
3. Secret JWT diferente entre servicios

**Solución:**
1. Renovar token con refresh:
   ```
   POST /api/auth/refresh
   ```
2. Verificar que todos los servicios tengan el MISMO JWT Secret:
   ```json
   "Jwt": {
     "Secret": "NubluSoft-SecretKey-2024-MinimoTreintaYDosCaracteres!!"
   }
   ```

### Error: "No se pudo determinar la entidad del usuario"

**Causa:** El claim "Entidad" no está en el token.

**Solución:**
1. Verificar que AuthService.cs genere el claim:
   ```csharp
   new Claim("Entidad", usuario.Entidad.ToString()),
   ```
2. Verificar que ClaimsPrincipalExtensions busque correctamente:
   ```csharp
   var claim = principal.FindFirst("Entidad");
   ```

---

## 3. Problemas de Proxy

### Error: Las rutas no llegan al microservicio

**Causa:** Ruta no configurada en ProxyMiddleware.

**Solución:**
1. Verificar que la ruta esté en el mapeo:
   ```csharp
   // NubluSoft/Middleware/ProxyMiddleware.cs
   private static readonly Dictionary<string, string> RouteMapping = new()
   {
       { "/api/tu-nueva-ruta", "CoreService" },
       // ...
   };
   ```
2. Verificar que ServiceEndpoints tenga la URL:
   ```json
   "ServiceEndpoints": {
     "CoreService": "http://localhost:5001",
     "NavIndexService": "http://localhost:5003",
     "SignatureService": "http://localhost:5004"
   }
   ```

### WebSocket no conecta

**Causa:** WebSocketProxyMiddleware no configurado.

**Solución:**
1. Verificar que la ruta WS esté configurada:
   ```csharp
   // WebSocketProxyMiddleware.cs
   if (path.StartsWithSegments("/ws/navegacion"))
   {
       await ProxyWebSocket(context, _endpoints.NavIndexService);
   }
   ```

---

## 4. Problemas de Base de Datos

### Error: "relation does not exist"

**Causa:** Tabla no existe o SearchPath incorrecto.

**Solución:**
1. Verificar SearchPath en ConnectionString:
   ```
   SearchPath=usuarios,documentos,public
   ```
2. Verificar que la tabla existe:
   ```sql
   SELECT * FROM documentos."NombreTabla" LIMIT 1;
   ```

### Error: "function does not exist"

**Causa:** Función PostgreSQL no existe.

**Solución:**
1. Verificar en BdNubluSoft.sql que la función existe
2. Restaurar la base de datos:
   ```bash
   psql -U postgres -d nublusoft -f Files_Claude/BdNubluSoft.sql
   ```

### Error: Violación de FK (23503)

**Causa:** Referencia a registro que no existe.

**Solución:**
1. Verificar que el registro padre existe:
   ```sql
   SELECT * FROM documentos."TablaPadre" WHERE "Cod" = @Id;
   ```

---

## 5. Problemas de Storage (GCS)

### Error: "Could not load credentials"

**Causa:** Archivo gcs-credentials.json no encontrado o inválido.

**Solución:**
1. Verificar que existe el archivo:
   ```
   NubluSoft_Storage/gcs-credentials.json
   ```
2. Verificar configuración:
   ```json
   "Gcs": {
     "CredentialsPath": "gcs-credentials.json"
   }
   ```

### Error: URL de descarga expirada

**Causa:** URLs firmadas tienen tiempo de expiración (15 minutos por defecto).

**Solución:**
1. Solicitar nueva URL de descarga
2. Aumentar tiempo de expiración en GcsStorageService si es necesario

---

## 6. Problemas de Firma Electrónica

### Error: "Código OTP inválido o expirado"

**Causa:** Código expirado (5 minutos) o ya utilizado.

**Solución:**
1. Solicitar nuevo código OTP
2. Verificar que el reloj del servidor esté sincronizado

### Error: "No se pudo enviar el correo OTP"

**Causa:** Configuración SMTP incorrecta.

**Solución:**
1. Verificar configuración SMTP:
   ```json
   "Smtp": {
     "Host": "smtp.gmail.com",
     "Port": 587,
     "Username": "tu-email@gmail.com",
     "Password": "tu-app-password"  // App Password, no contraseña normal
   }
   ```
2. Para Gmail, habilitar "Contraseñas de aplicaciones"

### Error: "Certificado no válido"

**Causa:** Certificado X509 expirado o inválido.

**Solución:**
1. Verificar fecha de expiración del certificado
2. Verificar que el certificado esté en formato PEM o PFX válido

---

## 7. Problemas de Compilación

### Error: "The type or namespace could not be found"

**Causa:** Referencia faltante o namespace incorrecto.

**Solución:**
1. Verificar el namespace correcto del proyecto:
   - Gateway: `NubluSoft`
   - Core: `NubluSoft_Core`
   - Storage: `NubluSoft_Storage`
   - NavIndex: `NubluSoft_NavIndex`
   - Signature: `NubluSoft_Signature`
2. Restaurar paquetes:
   ```bash
   dotnet restore NubluSoft.sln
   ```

### Error: "Service not registered"

**Causa:** Servicio no agregado en Program.cs.

**Solución:**
```csharp
// En Program.cs del servicio correspondiente
builder.Services.AddScoped<INuevoService, NuevoService>();
```

---

## 8. Problemas de Rendimiento

### Consultas lentas

**Diagnóstico:**
```sql
EXPLAIN ANALYZE SELECT * FROM tu_consulta;
```

**Solución:**
1. Agregar índices:
   ```sql
   CREATE INDEX idx_tabla_columna ON documentos."Tabla"("Columna");
   ```
2. Usar paginación en consultas grandes:
   ```sql
   LIMIT @Limite OFFSET @Offset
   ```

### Cache Redis no funciona

**Verificar:**
```bash
redis-cli
> KEYS NubluSoft_*
> GET "NubluSoft_nav_1"
```

---

## 9. Logs y Diagnóstico

### Ver logs de un servicio

```bash
# En desarrollo
dotnet run --verbosity detailed

# O revisar la consola del servicio
```

### Habilitar logs detallados

```json
// appsettings.Development.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  }
}
```

### Verificar errores en BD

```sql
SELECT * FROM documentos."ErrorLog"
ORDER BY "Fecha" DESC
LIMIT 20;
```

---

## 10. Checklist de Verificación Rápida

```
□ PostgreSQL corriendo (puerto 5432)
□ Redis corriendo (puerto 6379)
□ Gateway corriendo (puerto 5008)
□ Core corriendo (puerto 5001)
□ Storage corriendo (puerto 5002)
□ NavIndex corriendo (puerto 5003)
□ Signature corriendo (puerto 5004)
□ JWT Secret idéntico en todos los servicios
□ ConnectionString correcta
□ gcs-credentials.json presente (para Storage)
□ SMTP configurado (para Signature)
```

---

## 11. Comandos Útiles

```bash
# Compilar todo
dotnet build NubluSoft.sln

# Limpiar y recompilar
dotnet clean && dotnet build

# Ver paquetes desactualizados
dotnet list package --outdated

# Verificar health de todos los servicios
for port in 5008 5001 5002 5003 5004; do
  echo "Puerto $port:"; curl -s http://localhost:$port/health; echo
done

# Conexión a PostgreSQL
psql -U postgres -d nublusoft

# Conexión a Redis
redis-cli

# Ver procesos en puertos
netstat -ano | findstr :5008
```
