# INSTRUCCIONES DEL PROYECTO - Sistema de Gestión Documental NubluSoft

**Versión:** 7.0 | **Última actualización:** Enero 2026 | **Entorno:** Claude Code

---

# ⛔ REGLA SUPREMA - NUNCA SUPONER, SIEMPRE VERIFICAR

**ANTES de escribir, mencionar o sugerir CUALQUIER código:**

1. **¿EXISTE?** → Usar Read/Glob/Grep para VERIFICAR
2. **¿CÓMO ESTÁ HECHO?** → Leer archivos relacionados (Controller, Service, Interface, DTO, Program.cs)
3. **¿SIGO EL PATRÓN?** → Copiar EXACTAMENTE el estilo existente

**PROHIBIDO:** Suponer que métodos/propiedades/tablas/funciones existen → VERIFICAR PRIMERO
**REGLA DE ORO:** Si no lo verifiqué, NO lo menciono

---

# 🔗 COMUNICACIÓN CON EL FRONTEND

El proyecto frontend Angular está en: `C:\Users\Cristian\Documents\GitHub\NubluSoft`

**Cuando necesite información del frontend** (estructura, interfaces TypeScript, componentes, servicios), ejecutar:

```bash
cd "C:\Users\Cristian\Documents\GitHub\NubluSoft" && claude -p "tu pregunta específica aquí"
```

**Ejemplos de uso:**
| Necesito saber... | Comando |
|-------------------|---------|
| Qué endpoints consume el frontend | `claude -p "¿Qué endpoints de /api/radicados usa el frontend?"` |
| Estructura de una interface TS | `claude -p "¿Qué propiedades tiene la interface RadicadoDto?"` |
| Qué componentes existen | `claude -p "Lista los componentes en src/app/components"` |
| Cómo se usa un servicio | `claude -p "¿Cómo se usa RadicadosService en el frontend?"` |

**Notas:**
- El flag `-p` ejecuta Claude Code de forma no interactiva (pregunta única, sin memoria)
- Hacer preguntas específicas y concretas para obtener respuestas útiles
- Cada llamada es independiente, no hay contexto entre llamadas

**⛔ RESTRICCIÓN CRÍTICA:**
- SOLO usar para CONSULTAR información (lectura)
- NUNCA pedir al Claude del frontend que MODIFIQUE código
- Si necesitas cambios en el frontend → PEDIR AL USUARIO
- El usuario coordinará las modificaciones entre proyectos

---

# 🎯 RESUMEN DEL PROYECTO

**NubluSoft** es un Sistema de Gestión Documental Electrónica (SGDE) para entidades gubernamentales colombianas. Plataforma SaaS multi-tenant que gestiona: radicación PQRSD, TRD, transferencias documentales, firma electrónica.

## Estado Actual (Enero 2026)
- **Backend:** 100% completado (Gateway, Core, Storage, NavIndex, Signature)
- **Base de Datos:** PostgreSQL con 55+ tablas, 45+ funciones
- **Frontend Angular:** En desarrollo paralelo

## Arquitectura

```
Angular (4200) → Gateway (5008) → Core (5001) / Storage (5002) / NavIndex (5003) / Signature (5004)
                     ↓
              PostgreSQL + Redis + GCS
```

## Servicios y Puertos

| Servicio | Puerto | Responsabilidad |
|----------|--------|-----------------|
| Gateway | 5008 | Auth JWT, Proxy HTTP/WebSocket |
| Core | 5001 | Usuarios, Carpetas, Archivos, Radicados, TRD, Transferencias |
| Storage | 5002 | Google Cloud Storage, Versionamiento |
| NavIndex | 5003 | Árbol navegación, Redis Cache, WebSocket |
| Signature | 5004 | Firma OTP, Firma X509, Verificación |

---

# 📜 NORMATIVA COLOMBIANA

| Ley/Acuerdo | Tema | Módulo |
|-------------|------|--------|
| Ley 594/2000 | Ley General de Archivos | TRD, Ciclo vital, Transferencias |
| Ley 1755/2015 | Derecho de Petición | Tiempos PQRSD, Días hábiles |
| Ley 527/1999 | Comercio Electrónico | Firma electrónica, Hash SHA-256 |
| Ley 1712/2014 | Transparencia | Clasificación información |
| Ley 1581/2012 | Habeas Data | Protección datos personales (Terceros) |
| Acuerdo 060/2001 | Comunicaciones oficiales | Radicados, Consecutivos |
| Acuerdo 004/2019 | TRD y TVD | Módulo TRD |
| Acuerdo 003/2015 | Transferencias secundarias | Transferencias |

**Tiempos de Respuesta PQRSD (días hábiles):**
| Tipo | Días | Código BD |
|------|------|-----------|
| Petición general | 15 | PETICION |
| Petición información | 10 | INFORMACION |
| Consultas | 30 | CONSULTA |
| Quejas/Reclamos/Denuncias | 15 | QUEJA/RECLAMO/DENUNCIA |
| Tutelas | 10 | TUTELA |

**Disposiciones Finales TRD:** CT (Conservación Total), E (Eliminación), S (Selección), M (Microfilmación)

---

# 🏗️ ESTRUCTURA DEL REPOSITORIO

```
NubluSoftApi/
├── CLAUDE.md                    ← Este archivo
├── Files_Claude/
│   └── BdNubluSoft.sql         ← Script PostgreSQL (verificar tablas/funciones aquí)
├── NubluSoft/                   ← Gateway (5008)
├── NubluSoft_Core/              ← Core (5001)
├── NubluSoft_Storage/           ← Storage (5002)
├── NubluSoft_NavIndex/          ← NavIndex (5003)
└── NubluSoft_Signature/         ← Signature (5004)
```

---

# 🔧 STACK TECNOLÓGICO

| Componente | Tecnología | Versión |
|------------|------------|---------|
| Backend | .NET Core | 8.0 |
| Base de datos | PostgreSQL | 17.x |
| Caché/Sesiones | Redis | 7.x |
| Almacenamiento | Google Cloud Storage | - |
| Frontend | Angular | 18.x |
| ORM | Dapper | - |
| WebSocket | SignalR | - |
| PDF Signing | iText7 + BouncyCastle | - |
| Email | MailKit | - |

---

# 📁 INVENTARIO DE ARCHIVOS POR SERVICIO

## Gateway (NubluSoft)
```
Controllers/    AuthController.cs, HealthController.cs
Services/       AuthService.cs, JwtService.cs, RedisSessionService.cs (+ interfaces)
Middleware/     JwtMiddleware.cs, ProxyMiddleware.cs, WebSocketProxyMiddleware.cs
Configuration/  JwtSettings.cs, RedisSettings.cs, ServiceEndpoints.cs
Extensions/     ClaimsPrincipalExtensions.cs (retorna long)
Models/DTOs/    LoginRequest.cs, LoginResponse.cs, RefreshTokenRequest.cs, UserSessionDto.cs
```

## Core (NubluSoft_Core)
```
Controllers/    ArchivosController, AuditoriaController, CarpetasController,
                DatosEstaticosController, DiasFestivosController, EntidadesController,
                NotificacionesController, OficinasController, OficinaTRDController,
                PrestamosController, RadicadosController, TercerosController,
                TransferenciasController, TRDController, UsuariosController

Services/       ArchivosService, AuditoriaService, CarpetasService, DatosEstaticosService,
                DiasFestivosService, EntidadesService, NotificacionService, OficinasService,
                OficinaTRDService, PrestamosService, RadicadosService, StorageClientService,
                TercerosService, TransferenciasService, TRDService, UsuariosService
                (todos con interfaces I*Service.cs)

Models/DTOs/    ArchivoDto, ArchivoUploadDto, AuditoriaDtos, CarpetaDto, DiaFestivoDtos,
                EntidadDtos, NotificacionDtos, OficinaDto, OficinaTRDDtos, PrestamoDtos,
                RadicadoDto, TerceroDto, TransferenciaDto, TRDDto, UsuarioDto

Hubs/           NotificacionesHub.cs
Listeners/      PostgresNotificacionListener.cs
Extensions/     ClaimsPrincipalExtensions.cs (retorna long)
```

## Storage (NubluSoft_Storage)
```
Controllers/    StorageController.cs, InternalController.cs, HealthController.cs
Services/       GcsStorageService.cs, StorageService.cs (+ interfaces)
Helpers/        FileSizeHelper.cs, HashCalculatingStream.cs, MimeTypeHelper.cs
Models/DTOs/    DownloadDtos.cs, StorageResultDtos.cs, UploadDtos.cs, VersionDtos.cs
Extensions/     ClaimsPrincipalExtensions.cs (retorna long)
```

## NavIndex (NubluSoft_NavIndex)
```
Controllers/    NavegacionController.cs, HealthController.cs
Services/       NavIndexService.cs, NotificationService.cs, RedisCacheService.cs,
                NavIndexHubService.cs, PostgresNotifyListener.cs (+ interfaces)
Hubs/           NavIndexHub.cs
Models/DTOs/    NavIndexDtos.cs, WebSocketDtos.cs
Extensions/     ClaimsPrincipalExtensions.cs (retorna long)
```

## Signature (NubluSoft_Signature)
```
Controllers/    CertificadosController, ConfiguracionController, FirmaController,
                SolicitudesController, VerificacionController, HealthController

Services/       CertificadoService, ConfiguracionFirmaService, EmailService, FirmaService,
                OtpService, PdfSignatureService, SolicitudFirmaService, StorageClientService,
                VerificacionService (+ interfaces)

Helpers/        CryptoHelper.cs, OtpHelper.cs
Models/DTOs/    CertificadoDtos, ConfiguracionFirmaDtos, FirmaDtos, SolicitudFirmaDtos, VerificacionDtos
Models/Enums/   EstadoSolicitud.cs
Extensions/     ClaimsPrincipalExtensions.cs (retorna long? ⚠️ DIFERENTE)
```

---

# 🗄️ BASE DE DATOS

**Esquemas:** `documentos`, `usuarios`, `public`

**Para verificar tablas/funciones:** Usar Grep en `Files_Claude/BdNubluSoft.sql`

## Tablas Principales

### Gestión Documental
- `Carpetas` - Series, Subseries, Expedientes, Genéricas
- `Archivos` - Documentos almacenados
- `Versiones_Archivos` - Historial de versiones
- `Tablas_Retencion_Documental` - TRD (Series documentales)
- `Oficinas` - Dependencias de la entidad
- `Oficinas_TRD` - Relación Oficina-TRD

### Ventanilla Única (PQRSD)
- `Radicados` - Correspondencia radicada
- `Radicados_Anexos` - Anexos de radicados
- `Radicados_Trazabilidad` - Historial de acciones
- `Terceros` - Remitentes/Destinatarios
- `Consecutivos_Radicado` - Consecutivos por tipo y año

### Firma Electrónica
- `Solicitudes_Firma` - Solicitudes de firma
- `Firmantes_Solicitud` - Firmantes de cada solicitud
- `Firmas_Archivos` - Firmas realizadas
- `Codigos_OTP` - Códigos OTP para firma simple
- `Certificados_Usuarios` - Certificados digitales
- `Configuracion_Firma` - Config por entidad
- `Evidencias_Firma` - Evidencias inmutables

### Transferencias y Préstamos
- `Transferencias_Documentales` - Transferencias primarias/secundarias
- `Transferencias_Detalle` - Expedientes en cada transferencia
- `Prestamos_Expedientes` - Préstamos de documentos

### Auditoría
- `Historial_Acciones` - Log de acciones
- `Archivos_Eliminados` - Papelera de archivos
- `Carpetas_Eliminadas` - Papelera de carpetas
- `Notificaciones` - Notificaciones del sistema

### Catálogos
- `Tipos_Carpetas` - Serie(1), Subserie(2), Expediente(3), Genérica(4)
- `Estados_Carpetas` - Abierto(1), Cerrado(2), En Transferencia(3)
- `Tipos_Archivos`, `Tipos_Documentales`, `Estados_Radicado`
- `Medios_Recepcion`, `Disposiciones_Finales`, `Dias_Festivos`
- `Prioridades_Radicado`, `Tipos_Comunicacion`, `Tipos_Solicitud`

## Funciones PostgreSQL Principales

### Radicados
| Función | Descripción |
|---------|-------------|
| `F_GenerarNumeroRadicado` | Genera consecutivo E-YYYY-NNNN, S-YYYY-NNNN, I-YYYY-NNNN |
| `F_AsignarRadicado` | Asigna/reasigna con trazabilidad |
| `F_TrasladarRadicado` | Traslado por competencia (Ley 1755 Art.21) |
| `F_ArchivarRadicado` | Vincula radicado a expediente |
| `F_SolicitarProrroga` | Prórroga con cálculo días hábiles |
| `F_AnularRadicado` | Anulación con motivo obligatorio |
| `F_CalcularVencimientoRadicado` | Calcula fecha vencimiento según tipo |

### Carpetas y Archivos
| Función | Descripción |
|---------|-------------|
| `F_CrearCarpeta` | Crea carpeta validando jerarquía TRD |
| `F_EliminarCarpeta` | Elimina a papelera (soft delete) |
| `F_MoverCarpeta` | Mueve validando permisos |
| `F_EliminarArchivo` | Elimina a papelera |
| `F_RestaurarVersionArchivo` | Restaura versión anterior |

### Firma Electrónica
| Función | Descripción |
|---------|-------------|
| `F_CrearSolicitudFirma` | Crea solicitud con firmantes |
| `F_RegistrarFirma` | Registra firma realizada |
| `F_CancelarSolicitud` | Cancela solicitud pendiente |
| `F_RechazarFirma` | Rechaza y notifica |

### TRD y Transferencias
| Función | Descripción |
|---------|-------------|
| `F_CrearTRD` | Crea serie/subserie validando códigos |
| `F_AsignarTRDOficina` | Asigna TRD a oficina |
| `F_CrearTransferencia` | Crea transferencia documental |
| `F_EjecutarTransferencia` | Ejecuta transferencia aprobada |

### Utilidades
| Función | Descripción |
|---------|-------------|
| `F_EsDiaHabil` | Verifica si fecha es día hábil |
| `F_AgregarDiasHabiles` | Agrega N días hábiles a fecha |
| `F_DiasHabilesEntre` | Días hábiles entre dos fechas |
| `F_SiguienteCod` | Siguiente código para tabla |

**Modelo Multi-Tenant:** TODAS las consultas filtran por `"Entidad" = @EntidadId` (viene del JWT)

---

# 🔀 RUTAS PROXY (Gateway → Microservicios)

Verificar en: `NubluSoft/Middleware/ProxyMiddleware.cs`

**Core (5001):**
`/api/usuarios`, `/api/carpetas`, `/api/archivos`, `/api/radicados`, `/api/oficinas`, `/api/trd`, `/api/terceros`, `/api/transferencias`, `/api/datosestaticos`, `/api/notificaciones`, `/api/auditoria`, `/api/diasfestivos`, `/api/oficinas-trd`, `/api/prestamos`, `/api/entidades`

**NavIndex (5003):** `/api/navegacion`

**Signature (5004):** `/api/solicitudes`, `/api/firma`, `/api/certificados`, `/api/verificar`, `/api/configuracion`

**WebSocket:** `/ws/navegacion` (5003), `/ws/notificaciones` (5001)

---

# ⚠️ DIFERENCIA CRÍTICA: ClaimsPrincipalExtensions

| Servicio | Tipo retorno GetUsuarioId/GetEntidadId |
|----------|----------------------------------------|
| Gateway, Core, Storage, NavIndex | `long` |
| **Signature** | `long?` (nullable) |

---

# 🔐 JWT Claims

```csharp
// En AuthService.cs del Gateway:
new Claim(ClaimTypes.NameIdentifier, usuario.Cod.ToString()),
new Claim("Cod", usuario.Cod.ToString()),
new Claim("Usuario", usuario.Usuario_),
new Claim("Entidad", usuario.Entidad.ToString()),    // PascalCase
new Claim("NombreCompleto", usuario.NombreCompleto),
new Claim("NombreEntidad", nombreEntidad),
new Claim("SessionId", sessionId),
```

---

# ✅ CHECKLIST ANTES DE CADA RESPUESTA

```
□ ¿Verifiqué si el archivo ya existe? (Glob/Read)
□ ¿Leí código similar para copiar patrones?
□ ¿Verifiqué métodos/propiedades EXACTOS? (Read Interface/DTO)
□ ¿Verifiqué tablas/funciones en BdNubluSoft.sql? (Grep)
□ ¿La ruta está en ProxyMiddleware.cs? (si es endpoint nuevo)
□ ¿El servicio está registrado en Program.cs?
□ ¿Para Signature, usé long? en lugar de long?
□ ¿El módulo cumple normativa colombiana aplicable?
```

---

# 📋 PATRÓN DE VERIFICACIÓN

**Ejemplo: Agregar método a RadicadosService**

1. `Read RadicadosController.cs` → ver patrones de endpoints y validación de claims
2. `Read IRadicadosService.cs` → ver firmas de métodos existentes
3. `Read RadicadosService.cs` → ver implementación, conexión BD, manejo errores
4. `Read RadicadoDto.cs` → ver propiedades del DTO (no inventar campos)
5. `Grep "Radicados" en BdNubluSoft.sql` → verificar tablas/funciones PostgreSQL
6. `Read Program.cs` → verificar inyección de dependencias
7. **ENTONCES** escribir código siguiendo el patrón exacto

**Mantener siempre:**
- Mismo formato de indentación
- Mismo patrón de nombres (GetXxxAsync, CrearXxxAsync)
- Mismo patrón de manejo de errores (try/catch)
- Mismo uso de DynamicParameters en Dapper
- Mismas convenciones SQL (@param, "NombreColumna")
- Mismo patrón de validación de claims (User.GetEntidadId())

---

# 🛠️ COMANDOS ÚTILES

```bash
# Compilar
dotnet build NubluSoft.sln

# Ejecutar servicio
cd NubluSoft && dotnet run          # Gateway 5008
cd NubluSoft_Core && dotnet run     # Core 5001
cd NubluSoft_Storage && dotnet run  # Storage 5002
cd NubluSoft_NavIndex && dotnet run # NavIndex 5003
cd NubluSoft_Signature && dotnet run # Signature 5004

# Health check
curl http://localhost:500X/health

# Restaurar BD
psql -U postgres -d nublusoft -f Files_Claude/BdNubluSoft.sql
```

---

# ❌ ERRORES COMUNES A EVITAR

| Error | Corrección |
|-------|------------|
| Crear archivo que ya existe | Verificar con Glob primero |
| Nombres incorrectos de propiedades | Leer el DTO/Entity real |
| Ruta no funciona | Verificar ProxyMiddleware.cs |
| Usar `long` en Signature | Signature usa `long?` |
| Suponer función PostgreSQL existe | Grep en BdNubluSoft.sql |
| No registrar servicio | Agregar `builder.Services.AddScoped<>()` en Program.cs |
| Términos radicado incorrectos | Verificar Ley 1755/2015 |
| Suponer estructura TRD | Verificar Acuerdo 004/2019 |

---

# 📄 FUENTES DE VERDAD (en orden de prioridad)

1. **Código fuente** en el repositorio (Read, Glob, Grep)
2. **Files_Claude/BdNubluSoft.sql** para tablas y funciones PostgreSQL
3. **Este archivo CLAUDE.md** para contexto general
4. **Frontend** (consultar con `claude -p`) para interfaces TypeScript y componentes

---

**Versión 7.0 - Enero 2026**
**Claude Code - Backend NubluSoft**
**Frontend en: C:\Users\Cristian\Documents\GitHub\NubluSoft**
