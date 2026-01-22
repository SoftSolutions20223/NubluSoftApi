# INSTRUCCIONES DEL PROYECTO - Sistema de Gestión Documental NubluSoft

**Versión:** 6.0 (CLAUDE CODE - EDICIÓN COMPLETA)
**Última actualización:** Enero 2026
**Entorno:** Claude Code - Acceso local completo al repositorio

---

# ⛔⛔⛔ REGLA SUPREMA - LEER ANTES DE CUALQUIER ACCIÓN ⛔⛔⛔

```
╔═══════════════════════════════════════════════════════════════════════════════╗
║                                                                               ║
║   🚨 NUNCA SUPONER NADA - SIEMPRE VERIFICAR PRIMERO 🚨                       ║
║                                                                               ║
║   Esta es la regla más importante de todo el proyecto.                       ║
║   ANTES de escribir, mencionar o sugerir CUALQUIER código:                   ║
║                                                                               ║
║   ┌─────────────────────────────────────────────────────────────────────┐    ║
║   │                                                                     │    ║
║   │   1. ¿EXISTE? → Usar Read/Glob/Grep para VERIFICAR                 │    ║
║   │      • Si voy a mencionar una clase → leerla primero               │    ║
║   │      • Si voy a usar un método → verificar su firma exacta         │    ║
║   │      • Si voy a crear un archivo → verificar que NO exista         │    ║
║   │                                                                     │    ║
║   │   2. ¿CÓMO ESTÁ HECHO? → Revisar TODO lo relacionado               │    ║
║   │      • La clase que voy a modificar                                │    ║
║   │      • Su interface (si existe)                                    │    ║
║   │      • Los DTOs que usa                                            │    ║
║   │      • El Controller que la consume                                │    ║
║   │      • El registro en Program.cs                                   │    ║
║   │      • Las tablas/funciones de BD que usa                          │    ║
║   │                                                                     │    ║
║   │   3. ¿SIGO EL PATRÓN? → Copiar EXACTAMENTE el estilo existente     │    ║
║   │      • Mismo formato de namespace                                  │    ║
║   │      • Misma estructura de código                                  │    ║
║   │      • Misma nomenclatura de variables                             │    ║
║   │      • Mismos patrones de error handling                           │    ║
║   │      • Mismas convenciones de nombres                              │    ║
║   │                                                                     │    ║
║   └─────────────────────────────────────────────────────────────────────┘    ║
║                                                                               ║
║   ❌ PROHIBIDO:                                                              ║
║   • Decir "probablemente existe un método X" → VERIFICAR                     ║
║   • Decir "el DTO debería tener Y" → LEER EL DTO                             ║
║   • Asumir que una tabla existe → BUSCAR EN BdNubluSoft.sql                  ║
║   • Inventar nombres de propiedades → COPIAR DEL CÓDIGO REAL                 ║
║   • Suponer la firma de un método → LEER LA INTERFACE/CLASE                  ║
║   • Crear código sin ver ejemplos similares → BUSCAR PATRÓN PRIMERO          ║
║                                                                               ║
║   ✅ OBLIGATORIO:                                                            ║
║   • Read() el archivo ANTES de mencionarlo                                   ║
║   • Grep() para encontrar definiciones                                       ║
║   • Copiar nombres EXACTAMENTE como están (case-sensitive)                   ║
║   • Mantener la misma estructura del código existente                        ║
║   • Si no lo verifiqué, NO lo menciono                                       ║
║                                                                               ║
║   📌 EJEMPLO DE LO QUE DEBO HACER:                                           ║
║                                                                               ║
║   Si me piden agregar un endpoint a RadicadosController:                     ║
║   1. Read RadicadosController.cs → ver estructura y patrones                 ║
║   2. Read IRadicadosService.cs → ver métodos existentes                      ║
║   3. Read RadicadosService.cs → ver implementación                           ║
║   4. Read RadicadoDto.cs → ver propiedades del DTO                           ║
║   5. Grep en BdNubluSoft.sql → verificar tablas/funciones                    ║
║   6. Read Program.cs → verificar inyección de dependencias                   ║
║   7. ENTONCES crear código siguiendo exactamente el patrón                   ║
║                                                                               ║
╚═══════════════════════════════════════════════════════════════════════════════╝
```

---

# 🎯 SECCIÓN 0: ESTADO ACTUAL Y VISIÓN DEL PROYECTO

## 0.1 Resumen Ejecutivo

NubluSoft es un **Sistema de Gestión Documental Electrónica (SGDE)** para **entidades gubernamentales colombianas**. Es una plataforma SaaS multi-tenant que gestiona el ciclo de vida documental completo: desde la radicación de PQRSD hasta la disposición final según normativa archivística.

### Estado Actual del Backend (Enero 2026)

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                        ESTADO DE IMPLEMENTACIÓN                             │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ✅ COMPLETADO (100%)                                                       │
│  ├── Gateway (Autenticación JWT + Redis + Proxy)                            │
│  ├── Core (Usuarios, Carpetas, Archivos, Radicados, TRD, Oficinas)          │
│  ├── Storage (Google Cloud Storage + Versionamiento)                        │
│  ├── NavIndex (Navegación + WebSocket + Redis Cache)                        │
│  ├── Signature (Firma Simple OTP + Firma Avanzada Certificados)             │
│  ├── Base de Datos PostgreSQL (55+ tablas, 45+ funciones)                   │
│  └── Transferencias, Préstamos, Auditoría, Días Festivos                    │
│                                                                              │
│  🔄 EN DESARROLLO (Frontend Angular)                                        │
│  └── Interfaz de usuario - desarrollo en paralelo                           │
│                                                                              │
│  📋 PENDIENTE (Roadmap 2026)                                                │
│  ├── Q1: Integración SIGEP II, SECOP II                                     │
│  ├── Q2: Preservación digital largo plazo                                   │
│  ├── Q3: OCR + Clasificación IA                                             │
│  └── Q4: Portal ciudadano (Ventanilla Única Digital)                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Último Hito Completado
- **Firma Electrónica Completa** (NubluSoft_Signature) - Enero 2026
  - Firma simple con OTP por correo
  - Firma avanzada con certificados X509
  - Verificación pública de documentos firmados
  - Estampado cronológico

### Próximo Hito
- **Integración con SIGEP II** - Sincronización de funcionarios públicos

## 0.2 Arquitectura de Alto Nivel

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              ARQUITECTURA                                   │
│                                                                              │
│  CLIENTES                                                                    │
│  ├── Angular SPA (Puerto 4200)                                              │
│  └── Apps Móviles (futuro)                                                  │
│          │                                                                   │
│          ▼                                                                   │
│  ┌─────────────────────────────────────────────────────────────┐            │
│  │                    GATEWAY (5008)                           │            │
│  │  • JWT Auth + Redis Sessions                                │            │
│  │  • Proxy HTTP a microservicios                              │            │
│  │  • Proxy WebSocket (SignalR)                                │            │
│  └─────────────────────────────────────────────────────────────┘            │
│          │                                                                   │
│          ├──────────────┬──────────────┬──────────────┐                     │
│          ▼              ▼              ▼              ▼                     │
│    ┌──────────┐   ┌──────────┐   ┌──────────┐   ┌──────────┐               │
│    │   CORE   │   │ NAVINDEX │   │SIGNATURE │   │ STORAGE  │               │
│    │  (5001)  │   │  (5003)  │   │  (5004)  │   │  (5002)  │               │
│    │          │   │          │   │          │   │          │               │
│    │ Negocio  │   │ Navegac. │   │  Firma   │   │  GCS     │               │
│    │ PQRSD    │   │ Redis    │   │  OTP     │   │ Archivos │               │
│    │ TRD      │   │ WebSocket│   │  X509    │   │ Versiones│               │
│    └──────────┘   └──────────┘   └──────────┘   └──────────┘               │
│          │                                            │                     │
│          └────────────────────────────────────────────┘                     │
│                              │                                               │
│          ┌───────────────────┼───────────────────┐                          │
│          ▼                   ▼                   ▼                          │
│    ┌──────────┐        ┌──────────┐        ┌──────────┐                    │
│    │PostgreSQL│        │  Redis   │        │   GCS    │                    │
│    │  17.x    │        │   7.x    │        │  Bucket  │                    │
│    └──────────┘        └──────────┘        └──────────┘                    │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 0.3 Flujos de Negocio Principales

### Flujo 1: Radicación PQRSD (Ley 1755/2015)
```
Ciudadano presenta → Ventanilla radica → Sistema asigna consecutivo
       │                    │                      │
       ▼                    ▼                      ▼
Tercero creado/     Radicado creado      Notificación a
  actualizado       con vencimiento        funcionario
       │                    │                      │
       └────────────────────┼──────────────────────┘
                            │
       ┌────────────────────┼────────────────────┐
       ▼                    ▼                    ▼
 Funcionario          Sistema calcula      Alertas automáticas
   responde           días hábiles         (3 días antes)
       │                    │                      │
       ▼                    ▼                      ▼
 Documento de         Fecha vencimiento    Notificaciones
  respuesta           según tipo PQRSD      WebSocket
       │
       ▼
 Firma electrónica → Archivo en expediente → Radicado cerrado
```

### Flujo 2: Gestión Documental (Ley 594/2000)
```
                    ESTRUCTURA TRD
                         │
     ┌───────────────────┼───────────────────┐
     ▼                   ▼                   ▼
   SERIE           SUBSERIE            EXPEDIENTE
(100 Actas)    (100.01 Comité)      (Acta 001-2026)
     │                   │                   │
     │                   │                   ▼
     │                   │              DOCUMENTOS
     │                   │           (PDF, Word, etc.)
     │                   │                   │
     └───────────────────┼───────────────────┘
                         │
                         ▼
              CICLO DE VIDA DOCUMENTAL
     ┌───────────────────┼───────────────────┐
     ▼                   ▼                   ▼
 GESTIÓN (AG)      CENTRAL (AC)        HISTÓRICO
  1-5 años          5-20 años         Permanente
     │                   │                   │
     └───────Transferencia Primaria──────────┘
                         │
                         └───────Transferencia Secundaria───→
```

### Flujo 3: Firma Electrónica (Ley 527/1999)
```
Usuario sube documento → Solicita firma → Sistema genera solicitud
          │                    │                    │
          ▼                    ▼                    ▼
     Archivo en GCS      Tipo: Simple         Firmantes
     Hash SHA-256         o Avanzada           asignados
          │                    │                    │
          └────────────────────┼────────────────────┘
                               │
       ┌───────────────────────┴───────────────────────┐
       │                                               │
       ▼ FIRMA SIMPLE                      FIRMA AVANZADA ▼
   Envío código OTP                    Usuario usa certificado
   por correo/SMS                      X509 de su navegador
       │                                               │
       ▼                                               ▼
   Validación OTP                      Firma criptográfica
   (5 minutos)                         con clave privada
       │                                               │
       └───────────────────────┬───────────────────────┘
                               │
                               ▼
                     Documento firmado
                   + Constancia de firma
                   + Código verificación
                               │
                               ▼
                     Verificación pública
                   (/api/verificar/{hash})
```

---

# 🚀 SECCIÓN 1: INSTRUCCIONES PARA CLAUDE CODE

## 1.1 Contexto de Ejecución

Este proyecto se ejecuta con **Claude Code**, una herramienta de línea de comandos que permite interacción directa con el repositorio. Tienes **acceso completo al sistema de archivos**.

### Estructura del Repositorio
```
NubluSoftApi/                          ← Raíz del repositorio
├── CLAUDE.md                          ← Este archivo (LEER PRIMERO)
├── Files_Claude/                      ← Archivos de referencia
│   ├── BdNubluSoft.sql               ← Script completo PostgreSQL
│   ├── PATRONES_CODIGO.md            ← Patrones y convenciones
│   ├── ENDPOINTS_API.md              ← Referencia de endpoints
│   └── TROUBLESHOOTING.md            ← Solución de problemas
├── NubluSoft/                         ← Gateway (Puerto 5008)
├── NubluSoft_Core/                    ← Servicio Core (Puerto 5001)
├── NubluSoft_Storage/                 ← Servicio Storage (Puerto 5002)
├── NubluSoft_NavIndex/                ← Servicio NavIndex (Puerto 5003)
├── NubluSoft_Signature/               ← Servicio Signature (Puerto 5004)
├── NubluSoft.sln                      ← Solución de Visual Studio
└── test-integration.ps1               ← Script de pruebas
```

## 1.2 Reglas de Verificación OBLIGATORIAS

### ⚡ ANTES de dar CUALQUIER código, DEBES:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│   PASO 1: Verificar existencia                                              │
│   → Usar Read, Glob o Grep para verificar si archivo/clase/método EXISTE   │
│   → Si existe: NO proponer crearlo, usar el existente                       │
│                                                                              │
│   PASO 2: Verificar patrones existentes                                     │
│   → Antes de crear algo nuevo, leer un archivo similar existente            │
│   → Copiar EXACTAMENTE el mismo estilo (namespaces, patrones, formato)      │
│                                                                              │
│   PASO 3: Verificar rutas de comunicación                                   │
│   → Leer NubluSoft/Middleware/ProxyMiddleware.cs para ver rutas proxy       │
│   → Verificar que las rutas estén configuradas en Gateway                   │
│                                                                              │
│   PASO 4: Verificar base de datos                                           │
│   → Usar Grep en Files_Claude/BdNubluSoft.sql para tablas/funciones        │
│   → Verificar que tablas/funciones existan antes de usarlas                 │
│                                                                              │
│   PASO 5: Verificar normativa colombiana                                    │
│   → ¿Qué ley/acuerdo aplica al módulo?                                      │
│   → ¿Los campos obligatorios están presentes?                               │
│                                                                              │
│   ❌ SI NO SE VERIFICAN ESTOS 5 PASOS = NO DAR CÓDIGO                       │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 1.3 Comandos de Verificación con Herramientas Claude Code

| Cuando necesites... | Herramienta a usar |
|---------------------|-------------------|
| Verificar si archivo existe | `Glob` con pattern específico |
| Ver contenido de archivo | `Read` con file_path |
| Buscar definición de clase/método | `Grep` con pattern |
| Ver estructura de directorio | `Glob` con pattern `*` |
| Verificar tabla en BD | `Grep` en BdNubluSoft.sql |
| Verificar función PostgreSQL | `Grep` con "CREATE FUNCTION" |
| Ver rutas del proxy | `Read` ProxyMiddleware.cs |
| Ver servicios registrados | `Read` Program.cs + buscar "AddScoped" |

---

# 🔴 SECCIÓN 2: REGLA ANTI-SUPOSICIONES (CRÍTICA)

## ⚠️ PROHIBIDO SUPONER - SIEMPRE VERIFICAR

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│   🚫 CLAUDE NUNCA DEBE:                                                     │
│                                                                              │
│   1. SUPONER que un método existe → DEBE verificar con Grep/Read            │
│   2. SUPONER la firma de un método → DEBE verificar parámetros exactos      │
│   3. SUPONER el nombre de una propiedad → DEBE verificar en el DTO/Entity   │
│   4. SUPONER el tipo de retorno → DEBE verificar en la interface/clase      │
│   5. SUPONER que un DTO tiene X campo → DEBE buscar la definición real      │
│   6. SUPONER el namespace → DEBE copiar del archivo existente               │
│   7. SUPONER la ruta de un endpoint → DEBE verificar en el Controller       │
│   8. SUPONER que dependencia está inyectada → DEBE verificar Program.cs     │
│   9. SUPONER términos legales → DEBE verificar Ley 1755/2015                │
│   10. SUPONER estructura TRD → DEBE verificar Acuerdo 004/2019              │
│   11. SUPONER que una tabla existe → DEBE verificar en BdNubluSoft.sql      │
│   12. SUPONER función PostgreSQL existe → DEBE verificar en BD              │
│                                                                              │
│   🔴 REGLA DE ORO: Si no lo verifiqué, NO lo menciono                       │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 📋 CHECKLIST OBLIGATORIO

```
Antes de escribir CUALQUIER nombre de clase/método/propiedad:

□ ¿Verifiqué este elemento con Read/Grep en el archivo correspondiente?
□ ¿Encontré la definición EXACTA?
□ ¿Copié el nombre EXACTAMENTE como está (case-sensitive)?
□ ¿Verifiqué todos los parámetros del método?
□ ¿Verifiqué el tipo de retorno?
□ ¿Verifiqué el namespace correcto?
□ ¿El módulo cumple con la normativa aplicable?

⚠️ Si la respuesta a CUALQUIERA es NO → VERIFICAR PRIMERO
```

## 🔍 REVISAR TODO LO RELACIONADO

Cuando voy a modificar o agregar algo a una clase, NO basta con leer solo esa clase.
DEBO revisar TODO el ecosistema relacionado:

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│   Ejemplo: Voy a agregar un método a RadicadosService                       │
│                                                                              │
│   DEBO LEER (en este orden):                                                │
│                                                                              │
│   1. RadicadosController.cs                                                 │
│      → Para ver cómo se llaman los métodos del service                      │
│      → Para ver el patrón de validación de claims                           │
│      → Para ver cómo se retornan las respuestas                             │
│                                                                              │
│   2. IRadicadosService.cs                                                   │
│      → Para ver la firma exacta de métodos existentes                       │
│      → Para agregar mi nuevo método con el mismo estilo                     │
│                                                                              │
│   3. RadicadosService.cs                                                    │
│      → Para ver la implementación actual                                    │
│      → Para copiar el patrón de conexión a BD                               │
│      → Para copiar el patrón de manejo de errores                           │
│                                                                              │
│   4. RadicadoDto.cs (y DTOs relacionados)                                   │
│      → Para ver qué propiedades tienen                                      │
│      → Para no inventar nombres de campos                                   │
│                                                                              │
│   5. BdNubluSoft.sql (Grep)                                                 │
│      → Para verificar que las tablas existen                                │
│      → Para verificar que las funciones PostgreSQL existen                  │
│      → Para ver los parámetros exactos de las funciones                     │
│                                                                              │
│   6. Program.cs del servicio                                                │
│      → Para verificar que el servicio está registrado                       │
│      → Para ver si hay dependencias adicionales                             │
│                                                                              │
│   7. ProxyMiddleware.cs (si es endpoint nuevo)                              │
│      → Para verificar que la ruta está configurada                          │
│                                                                              │
│   SOLO DESPUÉS de leer TODO esto → escribo el código                        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

## 📐 MANTENER LA MISMA ESTRUCTURA

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                                                                              │
│   Cuando agregue código nuevo, DEBO mantener:                               │
│                                                                              │
│   ✓ El mismo formato de indentación                                         │
│   ✓ El mismo estilo de comentarios                                          │
│   ✓ El mismo patrón de nombres (GetXxxAsync, CrearXxxAsync, etc.)           │
│   ✓ El mismo patrón de manejo de errores (try/catch)                        │
│   ✓ El mismo patrón de logging                                              │
│   ✓ El mismo patrón de retorno de resultados                                │
│   ✓ El mismo uso de DynamicParameters en Dapper                             │
│   ✓ Las mismas convenciones de SQL (@param, "NombreColumna")                │
│   ✓ El mismo patrón de validación de claims (User.GetEntidadId())           │
│                                                                              │
│   ❌ NO DEBO:                                                                │
│   • Cambiar el estilo de código existente                                   │
│   • Introducir nuevos patrones sin necesidad                                │
│   • Usar nombres diferentes a los establecidos                              │
│   • Ignorar las convenciones del proyecto                                   │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

# 📜 SECCIÓN 3: MARCO NORMATIVO COLOMBIANO

## 3.1 Legislación Principal

| Ley/Norma | Tema | Impacto en NubluSoft |
|-----------|------|----------------------|
| **Ley 594/2000** | Ley General de Archivos | TRD, Ciclo vital, Transferencias |
| **Ley 1755/2015** | Derecho de Petición | Tiempos PQRSD, Días hábiles |
| **Ley 527/1999** | Comercio Electrónico | Firma electrónica, Hash SHA-256 |
| **Ley 1712/2014** | Transparencia | Clasificación información |
| **Ley 1581/2012** | Habeas Data | Protección datos personales |
| **Decreto 1080/2015** | Sector Cultura | PGD, requisitos documentos |

## 3.2 Acuerdos AGN

| Acuerdo | Tema | Módulo Relacionado |
|---------|------|-------------------|
| **060/2001** | Comunicaciones oficiales | Radicados, Consecutivos |
| **042/2002** | Archivos de gestión | Expedientes |
| **004/2019** | TRD y TVD | Módulo TRD |
| **003/2015** | Transferencias secundarias | Transferencias |
| **006/2014** | Organización archivos | Principio procedencia |

## 3.3 Tiempos de Respuesta PQRSD (Ley 1755/2015)

| Tipo de Solicitud | Días Hábiles | Código en BD |
|-------------------|--------------|--------------|
| Petición general | 15 | PETICION |
| Petición información | 10 | INFORMACION |
| Petición documentos | 10 | DOCUMENTOS |
| Consultas | 30 | CONSULTA |
| Quejas | 15 | QUEJA |
| Reclamos | 15 | RECLAMO |
| Denuncias | 15 | DENUNCIA |
| Tutelas | 10 | TUTELA |

## 3.4 Disposiciones Finales TRD

| Código | Disposición | Acción |
|--------|-------------|--------|
| **CT** | Conservación Total | Archivo histórico permanente |
| **E** | Eliminación | Destrucción controlada |
| **S** | Selección | Muestra representativa |
| **M** | Microfilmación/Digitalización | Cambio de soporte |

---

# 🏗️ SECCIÓN 4: ARQUITECTURA TÉCNICA

## 4.1 Stack Tecnológico

| Componente | Tecnología | Versión |
|------------|------------|---------|
| Backend | .NET Core | 8.0 |
| Base de datos | PostgreSQL | 17.x |
| Caché/Sesiones | Redis | 7.x |
| Almacenamiento | Google Cloud Storage | - |
| Frontend | Angular | 18.x |
| Autenticación | JWT + Redis | - |
| WebSockets | SignalR | - |
| ORM | Dapper | - |
| PDF Signing | iText7 + BouncyCastle | - |
| Email | MailKit | - |

## 4.2 Puertos y Servicios

| Servicio | Puerto | URL Base | Descripción |
|----------|--------|----------|-------------|
| **Gateway** | 5008 | `http://localhost:5008` | Autenticación, Proxy |
| **Core** | 5001 | `http://localhost:5001` | Lógica de negocio |
| **Storage** | 5002 | `http://localhost:5002` | Almacenamiento GCS |
| **NavIndex** | 5003 | `http://localhost:5003` | Navegación, Redis |
| **Signature** | 5004 | `http://localhost:5004` | Firma electrónica |

## 4.3 Modelo Multi-Tenant

```sql
-- TODAS las consultas incluyen filtro por entidad
SELECT * FROM documentos."Archivos"
WHERE "Entidad" = @EntidadId  -- Siempre presente (viene del JWT)
  AND "Cod" = @ArchivoId;

-- El EntidadId viene del claim JWT, NO del request del usuario
-- Esto garantiza aislamiento total entre entidades
```

## 4.4 Comunicación entre Servicios

### Rutas Proxy HTTP (ProxyMiddleware.cs)
```csharp
// NubluSoft_Core (Puerto 5001) - 15 rutas
"/api/usuarios"        → CoreService
"/api/carpetas"        → CoreService
"/api/archivos"        → CoreService
"/api/radicados"       → CoreService
"/api/oficinas"        → CoreService
"/api/trd"             → CoreService
"/api/terceros"        → CoreService
"/api/transferencias"  → CoreService
"/api/datosestaticos"  → CoreService
"/api/notificaciones"  → CoreService
"/api/auditoria"       → CoreService
"/api/diasfestivos"    → CoreService
"/api/oficinas-trd"    → CoreService
"/api/prestamos"       → CoreService
"/api/entidades"       → CoreService

// NubluSoft_NavIndex (Puerto 5003)
"/api/navegacion"      → NavIndexService

// NubluSoft_Signature (Puerto 5004)
"/api/solicitudes"     → SignatureService
"/api/firma"           → SignatureService
"/api/certificados"    → SignatureService
"/api/verificar"       → SignatureService
"/api/configuracion"   → SignatureService
```

### Rutas Proxy WebSocket
```csharp
"/ws/navegacion"       → NavIndexService (Puerto 5003)
"/ws/notificaciones"   → CoreService (Puerto 5001)
```

---

# 🎯 SECCIÓN 5: MÓDULOS FUNCIONALES

## 5.1 Gateway (NubluSoft - Puerto 5008)

**Responsabilidades:**
- Autenticación JWT con validación Redis
- Generación y refresh de tokens
- Proxy HTTP a microservicios internos
- Proxy WebSocket para SignalR
- Bloqueo de rutas `/internal`

**Archivos clave:**
- `Services/AuthService.cs` - Validación credenciales PostgreSQL
- `Services/JwtService.cs` - Generación tokens JWT
- `Services/RedisSessionService.cs` - Gestión sesiones
- `Middleware/JwtMiddleware.cs` - Validación cada request
- `Middleware/ProxyMiddleware.cs` - Enrutamiento a microservicios

## 5.2 Core (NubluSoft_Core - Puerto 5001)

**Responsabilidades:**
- Gestión de usuarios y oficinas
- Gestión documental (Carpetas, Archivos)
- Ventanilla Única (Radicados, Terceros)
- TRD y transferencias documentales
- Préstamos de expedientes
- Auditoría y días festivos
- Notificaciones WebSocket

**Módulos implementados:**

| Módulo | Controller | Service | Cumple |
|--------|------------|---------|--------|
| Usuarios | UsuariosController | UsuariosService | - |
| Carpetas | CarpetasController | CarpetasService | Ley 594 |
| Archivos | ArchivosController | ArchivosService | Ley 594 |
| Radicados | RadicadosController | RadicadosService | Acuerdo 060, Ley 1755 |
| TRD | TRDController | TRDService | Acuerdo 004 |
| Oficinas | OficinasController | OficinasService | - |
| Terceros | TercerosController | TercerosService | Ley 1581 |
| Transferencias | TransferenciasController | TransferenciasService | Acuerdo 003 |
| Préstamos | PrestamosController | PrestamosService | Ley 594 Art.16 |
| Auditoría | AuditoriaController | AuditoriaService | Ley 1712 |
| Días Festivos | DiasFestivosController | DiasFestivosService | Ley 1755 |
| Oficinas TRD | OficinaTRDController | OficinaTRDService | - |
| Entidades | EntidadesController | EntidadesService | - |

## 5.3 Storage (NubluSoft_Storage - Puerto 5002)

**Responsabilidades:**
- Integración con Google Cloud Storage
- URLs firmadas para upload/download seguro
- Versionamiento de archivos
- Cálculo de hash SHA-256
- Detección de MIME types

**Archivos clave:**
- `Services/GcsStorageService.cs` - Operaciones con GCS
- `Services/StorageService.cs` - Lógica de negocio
- `Controllers/StorageController.cs` - Endpoints públicos
- `Controllers/InternalController.cs` - Endpoints servicio-a-servicio

## 5.4 NavIndex (NubluSoft_NavIndex - Puerto 5003)

**Responsabilidades:**
- Árbol de navegación documental
- Cache Redis para rendimiento
- WebSocket para actualizaciones en tiempo real
- Compresión gzip de estructuras grandes
- PostgreSQL NOTIFY listener

**Archivos clave:**
- `Services/NavIndexService.cs` - Construcción del árbol
- `Services/RedisCacheService.cs` - Gestión de cache
- `Hubs/NavIndexHub.cs` - WebSocket SignalR
- `Services/PostgresNotifyListener.cs` - Escucha cambios BD

## 5.5 Signature (NubluSoft_Signature - Puerto 5004)

**Responsabilidades:**
- Firma simple con OTP (correo)
- Firma avanzada con certificados X509
- Gestión de certificados de usuario
- Verificación pública de firmas
- Constancias de firma PDF
- Configuración por entidad

**Archivos clave:**
- `Services/SolicitudFirmaService.cs` - Gestión solicitudes
- `Services/OtpService.cs` - Generación/validación OTP
- `Services/FirmaService.cs` - Proceso de firmado
- `Services/PdfSignatureService.cs` - Firma PDF con iText7
- `Services/CertificadoService.cs` - Gestión certificados
- `Services/VerificacionService.cs` - Verificación pública

**Nota importante:** Este servicio usa `long?` (nullable) en ClaimsPrincipalExtensions, diferente a los demás servicios que usan `long`.

---

# 🗄️ SECCIÓN 6: ESQUEMA DE BASE DE DATOS

## 6.1 Esquemas PostgreSQL

| Esquema | Propósito |
|---------|-----------|
| `documentos` | Gestión documental, TRD, Ventanilla Única, Firma |
| `usuarios` | Gestión de usuarios, entidades y planes |
| `public` | Extensiones (pgcrypto, unaccent, uuid-ossp) |

## 6.2 Tablas Principales (55 tablas)

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
- `Configuracion_Firma` - Config de firma por entidad
- `Evidencias_Firma` - Evidencias inmutables

### Transferencias y Préstamos
- `Transferencias_Documentales` - Transferencias primarias/secundarias
- `Transferencias_Detalle` - Expedientes en cada transferencia
- `Prestamos_Expedientes` - Préstamos de documentos

### Auditoría
- `Historial_Acciones` - Log de acciones del sistema
- `Archivos_Eliminados` - Papelera de archivos
- `Carpetas_Eliminadas` - Papelera de carpetas
- `ErrorLog` - Log de errores
- `Notificaciones` - Notificaciones del sistema

### Catálogos
- `Tipos_Carpetas` - Serie(1), Subserie(2), Expediente(3), Genérica(4)
- `Estados_Carpetas` - Abierto(1), Cerrado(2), En Transferencia(3)
- `Tipos_Archivos` - PDF, Word, Excel, etc.
- `Tipos_Documentales` - Acta, Oficio, Resolución, etc.
- `Estados_Radicado` - Pendiente, Asignado, En Trámite, etc.
- `Medios_Recepcion` - Ventanilla, Email, Web, etc.
- `Disposiciones_Finales` - CT, E, S, M
- `Dias_Festivos` - Festivos Colombia
- `Prioridades_Radicado` - Alta, Normal, Baja
- `Tipos_Comunicacion` - Entrada, Salida, Interna
- `Tipos_Solicitud` - PQRSD, Tutela, etc.

## 6.3 Funciones PostgreSQL Principales (45+ funciones)

### Radicados
| Función | Descripción |
|---------|-------------|
| `F_GenerarNumeroRadicado` | Genera consecutivo E-YYYY-NNNN, S-YYYY-NNNN, I-YYYY-NNNN |
| `F_AsignarRadicado` | Asigna/reasigna radicado con trazabilidad |
| `F_TrasladarRadicado` | Traslado por competencia (Ley 1755 Art.21) |
| `F_ArchivarRadicado` | Vincula radicado a expediente |
| `F_SolicitarProrroga` | Prórroga con cálculo días hábiles |
| `F_AnularRadicado` | Anulación con motivo obligatorio |
| `F_CalcularVencimientoRadicado` | Calcula fecha vencimiento según tipo |
| `F_RadicadosPorVencer` | Radicados próximos a vencer |

### Carpetas y Archivos
| Función | Descripción |
|---------|-------------|
| `F_CrearCarpeta` | Crea carpeta validando jerarquía TRD |
| `F_EliminarCarpeta` | Elimina a papelera (soft delete) |
| `F_MoverCarpeta` | Mueve validando permisos |
| `F_CopiarCarpeta` | Copia con contenido opcional |
| `F_EliminarArchivo` | Elimina a papelera |
| `F_RestaurarVersionArchivo` | Restaura versión anterior |

### Firma Electrónica
| Función | Descripción |
|---------|-------------|
| `F_CrearSolicitudFirma` | Crea solicitud con firmantes |
| `F_RegistrarFirma` | Registra firma realizada |
| `F_CancelarSolicitud` | Cancela solicitud pendiente |
| `F_NotificarFirmante` | Notifica siguiente firmante |
| `F_RechazarFirma` | Rechaza y notifica |
| `F_ObtenerHistorialFirma` | Historial de firmas de archivo |

### TRD y Transferencias
| Función | Descripción |
|---------|-------------|
| `F_CrearTRD` | Crea serie/subserie validando códigos |
| `F_AsignarTRDOficina` | Asigna TRD a oficina |
| `F_RevocarTRDOficina` | Revoca asignación |
| `F_ObtenerTRDsOficina` | TRDs disponibles para oficina |
| `F_CrearTransferencia` | Crea transferencia documental |
| `F_AgregarExpedienteTransferencia` | Agrega expediente cerrado |
| `F_EjecutarTransferencia` | Ejecuta transferencia aprobada |

### Utilidades
| Función | Descripción |
|---------|-------------|
| `F_EsDiaHabil` | Verifica si fecha es día hábil |
| `F_AgregarDiasHabiles` | Agrega N días hábiles a fecha |
| `F_DiasHabilesEntre` | Días hábiles entre dos fechas |
| `F_SiguienteCod` | Siguiente código para tabla |
| `F_NotifyNavIndex` | Trigger para actualizar NavIndex |
| `F_GenerarCodigoVerificacion` | Código verificación firmas |

---

# 📁 SECCIÓN 7: INVENTARIO DE ARCHIVOS

## 7.1 NubluSoft (Gateway)

```
NubluSoft/
├── Program.cs
├── appsettings.json
├── Configuration/
│   ├── JwtSettings.cs
│   ├── RedisSettings.cs
│   └── ServiceEndpoints.cs
├── Controllers/
│   ├── AuthController.cs
│   └── HealthController.cs
├── Extensions/
│   ├── ClaimsPrincipalExtensions.cs    ← retorna long
│   └── ServiceCollectionExtensions.cs
├── Middleware/
│   ├── JwtMiddleware.cs
│   ├── ProxyMiddleware.cs
│   └── WebSocketProxyMiddleware.cs
├── Models/
│   ├── DTOs/
│   │   ├── LoginRequest.cs
│   │   ├── LoginResponse.cs
│   │   ├── RefreshTokenRequest.cs
│   │   └── UserSessionDto.cs
│   └── Entities/
│       ├── Entidad.cs
│       └── Usuario.cs
└── Services/
    ├── AuthService.cs / IAuthService.cs
    ├── JwtService.cs / IJwtService.cs
    └── RedisSessionService.cs / IRedisSessionService.cs
```

## 7.2 NubluSoft_Core

```
NubluSoft_Core/
├── Program.cs
├── Configuration/
│   ├── JwtSettings.cs
│   └── ServiceSettings.cs
├── Controllers/
│   ├── ArchivosController.cs
│   ├── AuditoriaController.cs
│   ├── CarpetasController.cs
│   ├── DatosEstaticosController.cs
│   ├── DiasFestivosController.cs
│   ├── EntidadesController.cs
│   ├── HealthController.cs
│   ├── NotificacionesController.cs
│   ├── OficinasController.cs
│   ├── OficinaTRDController.cs
│   ├── PrestamosController.cs
│   ├── RadicadosController.cs
│   ├── TercerosController.cs
│   ├── TransferenciasController.cs
│   ├── TRDController.cs
│   └── UsuariosController.cs
├── Extensions/
│   ├── ClaimsPrincipalExtensions.cs    ← retorna long
│   └── ServiceCollectionExtensions.cs
├── Hubs/
│   └── NotificacionesHub.cs
├── Listeners/
│   └── PostgresNotificacionListener.cs
├── Models/
│   ├── DTOs/
│   │   ├── ArchivoDto.cs, ArchivoUploadDto.cs
│   │   ├── AuditoriaDtos.cs
│   │   ├── CarpetaDto.cs
│   │   ├── DiaFestivoDtos.cs
│   │   ├── EntidadDtos.cs
│   │   ├── NotificacionDtos.cs
│   │   ├── OficinaDto.cs, OficinaTRDDtos.cs
│   │   ├── PrestamoDtos.cs
│   │   ├── RadicadoDto.cs
│   │   ├── TerceroDto.cs
│   │   ├── TransferenciaDto.cs
│   │   ├── TRDDto.cs
│   │   └── UsuarioDto.cs
│   └── Entities/
│       ├── Archivo.cs, Carpeta.cs, Catalogo.cs
│       ├── Oficina.cs, Radicado.cs, Tercero.cs
│       ├── Transferencia.cs, TRD.cs, Usuario.cs
└── Services/
    ├── ArchivosService.cs / IArchivosService.cs
    ├── AuditoriaService.cs / IAuditoriaService.cs
    ├── CarpetasService.cs / ICarpetasService.cs
    ├── DatosEstaticosService.cs / IDatosEstaticosService.cs
    ├── DiasFestivosService.cs / IDiasFestivosService.cs
    ├── EntidadesService.cs / IEntidadesService.cs
    ├── NotificacionService.cs / INotificacionService.cs
    ├── OficinasService.cs / IOficinasService.cs
    ├── OficinaTRDService.cs / IOficinaTRDService.cs
    ├── PostgresConnectionFactory.cs
    ├── PrestamosService.cs / IPrestamosService.cs
    ├── RadicadosService.cs / IRadicadosService.cs
    ├── StorageClientService.cs / IStorageClientService.cs
    ├── TercerosService.cs / ITercerosService.cs
    ├── TransferenciasService.cs / ITransferenciasService.cs
    ├── TRDService.cs / ITRDService.cs
    └── UsuariosService.cs / IUsuariosService.cs
```

## 7.3 NubluSoft_NavIndex

```
NubluSoft_NavIndex/
├── Program.cs
├── Configuration/
│   ├── JwtSettings.cs
│   └── RedisSettings.cs
├── Controllers/
│   ├── HealthController.cs
│   └── NavegacionController.cs
├── Extensions/
│   ├── ClaimsPrincipalExtensions.cs    ← retorna long
│   └── ServiceCollectionExtensions.cs
├── Hubs/
│   └── NavIndexHub.cs
├── Models/DTOs/
│   ├── NavIndexDtos.cs
│   └── WebSocketDtos.cs
└── Services/
    ├── INavIndexService.cs / NavIndexService.cs
    ├── INotificationService.cs / NotificationService.cs
    ├── IPostgresConnectionFactory.cs / PostgresConnectionFactory.cs
    ├── IRedisCacheService.cs / RedisCacheService.cs
    ├── NavIndexHubService.cs
    └── PostgresNotifyListener.cs
```

## 7.4 NubluSoft_Signature

```
NubluSoft_Signature/
├── Program.cs
├── Configuration/
│   ├── JwtSettings.cs
│   ├── ServiceSettings.cs
│   ├── SignatureSettings.cs
│   └── SmtpSettings.cs
├── Controllers/
│   ├── CertificadosController.cs
│   ├── ConfiguracionController.cs
│   ├── FirmaController.cs
│   ├── HealthController.cs
│   ├── SolicitudesController.cs
│   └── VerificacionController.cs
├── Extensions/
│   ├── ClaimsPrincipalExtensions.cs    ← retorna long? (DIFERENTE!)
│   └── ServiceCollectionExtensions.cs
├── Helpers/
│   ├── CryptoHelper.cs
│   └── OtpHelper.cs
├── Models/
│   ├── DTOs/
│   │   ├── CertificadoDtos.cs
│   │   ├── ConfiguracionFirmaDtos.cs
│   │   ├── FirmaDtos.cs
│   │   ├── SolicitudFirmaDtos.cs
│   │   └── VerificacionDtos.cs
│   └── Enums/
│       └── EstadoSolicitud.cs
└── Services/
    ├── CertificadoService.cs / ICertificadoService.cs
    ├── ConfiguracionFirmaService.cs / IConfiguracionFirmaService.cs
    ├── EmailService.cs / IEmailService.cs
    ├── FirmaService.cs / IFirmaService.cs
    ├── OtpService.cs / IOtpService.cs
    ├── PdfSignatureService.cs / IPdfSignatureService.cs
    ├── PostgresConnectionFactory.cs / IPostgresConnectionFactory.cs
    ├── SolicitudFirmaService.cs / ISolicitudFirmaService.cs
    ├── StorageClientService.cs / IStorageClientService.cs
    └── VerificacionService.cs / IVerificacionService.cs
```

## 7.5 NubluSoft_Storage

```
NubluSoft_Storage/
├── Program.cs
├── gcs-credentials.json
├── Configuration/
│   ├── GcsSettings.cs
│   └── JwtSettings.cs
├── Controllers/
│   ├── HealthController.cs
│   ├── InternalController.cs
│   └── StorageController.cs
├── Extensions/
│   └── ClaimsPrincipalExtensions.cs    ← retorna long
├── Helpers/
│   ├── FileSizeHelper.cs
│   ├── HashCalculatingStream.cs
│   └── MimeTypeHelper.cs
├── Models/DTOs/
│   ├── DownloadDtos.cs
│   ├── StorageResultDtos.cs
│   ├── UploadDtos.cs
│   └── VersionDtos.cs
└── Services/
    ├── GcsStorageService.cs / IGcsStorageService.cs
    ├── IPostgresConnectionFactory.cs / PostgresConnectionFactory.cs
    └── StorageService.cs / IStorageService.cs
```

---

# ⚙️ SECCIÓN 8: CONFIGURACIONES CRÍTICAS

## 8.1 JWT (Idéntico en todos los servicios)

```json
"Jwt": {
  "Secret": "NubluSoft-SecretKey-2024-MinimoTreintaYDosCaracteres!!",
  "Issuer": "NubluSoft",
  "Audience": "NubluSoftClients"
}
```

## 8.2 ConnectionStrings

```json
"ConnectionStrings": {
  "PostgreSQL": "Host=localhost;Port=5432;Database=nublusoft;Username=postgres;Password=8213;SearchPath=usuarios,documentos,public"
}
```

## 8.3 Redis (Gateway y NavIndex)

```json
"Redis": {
  "ConnectionString": "localhost:6379",
  "InstanceName": "NubluSoft_"
}
```

## 8.4 Google Cloud Storage (Storage)

```json
"Gcs": {
  "ProjectId": "tu-proyecto-gcs",
  "BucketName": "nublusoft-documents",
  "CredentialsPath": "gcs-credentials.json"
}
```

## 8.5 SMTP (Signature)

```json
"Smtp": {
  "Host": "smtp.gmail.com",
  "Port": 587,
  "Username": "tu-email@gmail.com",
  "Password": "tu-app-password",
  "FromEmail": "noreply@nublusoft.com",
  "FromName": "NubluSoft"
}
```

## 8.6 Claims JWT Generados

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

# 🛠️ SECCIÓN 9: COMANDOS DE DESARROLLO

## 9.1 Compilar y Ejecutar

```bash
# Compilar toda la solución
dotnet build NubluSoft.sln

# Ejecutar servicio individual
cd NubluSoft && dotnet run          # Gateway en 5008
cd NubluSoft_Core && dotnet run     # Core en 5001
cd NubluSoft_Storage && dotnet run  # Storage en 5002
cd NubluSoft_NavIndex && dotnet run # NavIndex en 5003
cd NubluSoft_Signature && dotnet run # Signature en 5004
```

## 9.2 Verificar Health

```bash
curl http://localhost:5008/health  # Gateway
curl http://localhost:5001/health  # Core
curl http://localhost:5002/health  # Storage
curl http://localhost:5003/health  # NavIndex
curl http://localhost:5004/health  # Signature
```

## 9.3 Pruebas de Integración

```powershell
./test-integration.ps1
```

## 9.4 Restaurar Base de Datos

```bash
psql -U postgres -d nublusoft -f Files_Claude/BdNubluSoft.sql
```

---

# 📋 SECCIÓN 10: CHECKLIST Y ERRORES COMUNES

## Checklist Antes de Cada Respuesta

```
□ 1. ¿Verifiqué si el archivo ya existe?
□ 2. ¿Leí código similar para copiar patrones?
□ 3. ¿El código sigue el MISMO patrón existente?
□ 4. ¿Los nombres siguen la NOMENCLATURA establecida?
□ 5. ¿La ruta está configurada en Gateway (si aplica)?
□ 6. ¿Verifiqué que los métodos/propiedades EXISTEN?
□ 7. ¿El namespace es correcto para el proyecto?
□ 8. ¿Verifiqué la FIRMA EXACTA de los métodos?
□ 9. ¿El servicio está registrado en Program.cs?
□ 10. ¿Para Signature, usé long? en lugar de long?
□ 11. ¿El módulo cumple normativa colombiana?
□ 12. ¿Verifiqué tablas/funciones en BdNubluSoft.sql?

❌ Si alguno es NO → VERIFICAR PRIMERO
```

## Errores Comunes a Evitar

| ❌ Error | ✅ Corrección |
|----------|---------------|
| Crear archivo que ya existe | Verificar con Glob/Read primero |
| Nombres incorrectos | Copiar nombre EXACTO del existente |
| Rutas no configuradas | Verificar ProxyMiddleware.cs |
| Términos radicado incorrectos | Verificar Ley 1755/2015 |
| Usar `long` en Signature | Signature usa `long?` |
| No registrar servicio | Agregar `builder.Services.AddScoped<>()` |
| Suponer tabla existe | Verificar en BdNubluSoft.sql |
| Suponer función existe | Verificar en BdNubluSoft.sql |

---

# 🎯 SECCIÓN 11: RECORDATORIO FINAL

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                  │
│   ⛔ ANTES DE DAR CÓDIGO:                                       │
│                                                                  │
│   1. VERIFICAR que no existe ya (Read, Glob, Grep)              │
│   2. VER código similar existente                               │
│   3. COPIAR patrón exacto                                       │
│   4. CONFIRMAR rutas de comunicación                            │
│   5. VERIFICAR firmas de métodos (NO SUPONER)                   │
│   6. VERIFICAR normativa colombiana aplicable                   │
│   7. VERIFICAR tablas/funciones en BdNubluSoft.sql              │
│                                                                  │
│   📜 NORMATIVA CLAVE:                                           │
│   • Ley 594/2000 - Archivos                                     │
│   • Ley 1755/2015 - Tiempos PQRSD                               │
│   • Ley 527/1999 - Firma electrónica                            │
│   • Ley 1581/2012 - Habeas Data                                 │
│   • Ley 1712/2014 - Transparencia                               │
│   • Acuerdo 060/2001 - Radicación                               │
│   • Acuerdo 004/2019 - TRD                                      │
│                                                                  │
│   🔴 REGLA DE ORO: Si no lo verifiqué, NO lo menciono           │
│                                                                  │
│   📄 Fuentes de verdad (en orden de prioridad):                 │
│   1. Código fuente en el repositorio (Read, Glob, Grep)         │
│   2. Files_Claude/BdNubluSoft.sql                               │
│   3. Este archivo CLAUDE.md                                     │
│   4. Files_Claude/PATRONES_CODIGO.md                            │
│                                                                  │
│   🚀 Ventaja de Claude Code:                                    │
│   Tienes acceso DIRECTO al repositorio. ÚSALO para verificar    │
│   antes de dar cualquier respuesta con código.                  │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

**Versión 6.0 - Enero 2026**
**Claude Code - Acceso Local Completo**
**Sistema de Gestión Documental para Entidades del Estado Colombiano**
