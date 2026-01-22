# REFERENCIA DE ENDPOINTS API - NubluSoft

Todos los endpoints requieren autenticación JWT excepto donde se indique.

Base URL: `http://localhost:5008` (Gateway)

---

## AUTENTICACIÓN (Gateway)

| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| POST | `/api/auth/login` | Iniciar sesión | No |
| POST | `/api/auth/logout` | Cerrar sesión | Sí |
| POST | `/api/auth/refresh` | Refrescar token | Sí |
| GET | `/health` | Estado del servicio | No |

### POST /api/auth/login
```json
// Request
{
  "usuario": "string",
  "contrasena": "string"
}

// Response 200
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2026-01-16T12:00:00Z",
  "usuario": {
    "cod": 1,
    "nombres": "Juan",
    "apellidos": "Pérez",
    "entidad": 1,
    "nombreEntidad": "Alcaldía de Neiva"
  }
}
```

---

## USUARIOS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/usuarios` | Lista usuarios de la entidad |
| GET | `/api/usuarios/{id}` | Obtener usuario por ID |
| GET | `/api/usuarios/mi-perfil` | Perfil del usuario autenticado |
| POST | `/api/usuarios` | Crear usuario |
| PUT | `/api/usuarios/{id}` | Actualizar usuario |
| DELETE | `/api/usuarios/{id}` | Desactivar usuario |

---

## CARPETAS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/carpetas` | Lista carpetas con filtros |
| GET | `/api/carpetas/{id}` | Obtener carpeta por ID |
| GET | `/api/carpetas/arbol` | Árbol de carpetas |
| GET | `/api/carpetas/{id}/contenido` | Contenido de carpeta |
| GET | `/api/carpetas/{id}/ruta` | Ruta completa (breadcrumb) |
| POST | `/api/carpetas` | Crear carpeta |
| PUT | `/api/carpetas/{id}` | Actualizar carpeta |
| POST | `/api/carpetas/{id}/mover` | Mover carpeta |
| POST | `/api/carpetas/{id}/copiar` | Copiar carpeta |
| POST | `/api/carpetas/{id}/cerrar` | Cerrar expediente |
| POST | `/api/carpetas/{id}/reabrir` | Reabrir expediente |
| DELETE | `/api/carpetas/{id}` | Eliminar carpeta (papelera) |

### Tipos de Carpeta
- `1` = Serie
- `2` = Subserie
- `3` = Expediente
- `4` = Genérica

---

## ARCHIVOS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/archivos` | Lista archivos con filtros |
| GET | `/api/archivos/{id}` | Obtener archivo por ID |
| GET | `/api/archivos/{id}/versiones` | Historial de versiones |
| GET | `/api/archivos/{id}/download` | URL de descarga |
| POST | `/api/archivos/upload-url` | URL para subir archivo |
| POST | `/api/archivos/registrar` | Registrar archivo subido |
| PUT | `/api/archivos/{id}` | Actualizar metadatos |
| POST | `/api/archivos/{id}/mover` | Mover archivo |
| POST | `/api/archivos/{id}/copiar` | Copiar archivo |
| POST | `/api/archivos/{id}/nueva-version` | Subir nueva versión |
| POST | `/api/archivos/{id}/restaurar-version` | Restaurar versión anterior |
| DELETE | `/api/archivos/{id}` | Eliminar archivo (papelera) |

---

## RADICADOS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/radicados` | Lista radicados con filtros |
| GET | `/api/radicados/{id}` | Obtener radicado por ID |
| GET | `/api/radicados/buscar/{numero}` | Buscar por número |
| GET | `/api/radicados/{id}/trazabilidad` | Historial de acciones |
| GET | `/api/radicados/{id}/anexos` | Anexos del radicado |
| GET | `/api/radicados/por-vencer` | Radicados próximos a vencer |
| GET | `/api/radicados/vencidos` | Radicados vencidos |
| POST | `/api/radicados/entrada` | Radicar entrada |
| POST | `/api/radicados/salida` | Radicar salida |
| POST | `/api/radicados/interna` | Radicar interna |
| POST | `/api/radicados/{id}/asignar` | Asignar a funcionario |
| POST | `/api/radicados/{id}/trasladar` | Trasladar por competencia |
| POST | `/api/radicados/{id}/archivar` | Archivar en expediente |
| POST | `/api/radicados/{id}/prorroga` | Solicitar prórroga |
| POST | `/api/radicados/{id}/anular` | Anular radicado |
| POST | `/api/radicados/{id}/anexos` | Agregar anexo |
| DELETE | `/api/radicados/anexos/{anexoId}` | Eliminar anexo |

### Tipos de Comunicación
- `E` = Entrada
- `S` = Salida
- `I` = Interna

---

## TRD (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/trd` | Lista TRDs de la entidad |
| GET | `/api/trd/arbol` | Árbol de TRD |
| GET | `/api/trd/{id}` | Obtener TRD por ID |
| GET | `/api/trd/series` | Solo series (nivel 1) |
| GET | `/api/trd/{id}/subseries` | Subseries de una serie |
| POST | `/api/trd` | Crear serie o subserie |
| PUT | `/api/trd/{id}` | Actualizar TRD |
| DELETE | `/api/trd/{id}` | Eliminar TRD |

---

## OFICINAS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/oficinas` | Lista oficinas |
| GET | `/api/oficinas/arbol` | Árbol jerárquico |
| GET | `/api/oficinas/{id}` | Obtener oficina |
| GET | `/api/oficinas/{id}/usuarios` | Usuarios de la oficina |
| POST | `/api/oficinas` | Crear oficina |
| PUT | `/api/oficinas/{id}` | Actualizar oficina |
| DELETE | `/api/oficinas/{id}` | Desactivar oficina |

---

## OFICINAS-TRD (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/oficinas-trd/oficina/{oficinaId}` | TRDs asignadas a oficina |
| GET | `/api/oficinas-trd/trd/{trdId}` | Oficinas con acceso a TRD |
| POST | `/api/oficinas-trd/asignar` | Asignar TRD a oficina |
| DELETE | `/api/oficinas-trd/revocar` | Revocar asignación |

---

## TERCEROS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/terceros` | Lista terceros |
| GET | `/api/terceros/{id}` | Obtener tercero |
| GET | `/api/terceros/buscar` | Buscar por documento o nombre |
| POST | `/api/terceros` | Crear tercero |
| PUT | `/api/terceros/{id}` | Actualizar tercero |
| DELETE | `/api/terceros/{id}` | Desactivar tercero |

---

## TRANSFERENCIAS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/transferencias` | Lista transferencias |
| GET | `/api/transferencias/{id}` | Obtener transferencia |
| GET | `/api/transferencias/{id}/expedientes` | Expedientes en transferencia |
| POST | `/api/transferencias` | Crear transferencia |
| POST | `/api/transferencias/{id}/agregar-expediente` | Agregar expediente |
| POST | `/api/transferencias/{id}/quitar-expediente` | Quitar expediente |
| POST | `/api/transferencias/{id}/enviar` | Enviar para aprobación |
| POST | `/api/transferencias/{id}/aprobar` | Aprobar transferencia |
| POST | `/api/transferencias/{id}/rechazar` | Rechazar transferencia |
| POST | `/api/transferencias/{id}/ejecutar` | Ejecutar transferencia |

---

## PRÉSTAMOS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/prestamos` | Lista préstamos |
| GET | `/api/prestamos/{id}` | Obtener préstamo |
| GET | `/api/prestamos/mis-solicitudes` | Mis solicitudes |
| GET | `/api/prestamos/pendientes-aprobacion` | Pendientes de aprobar |
| POST | `/api/prestamos` | Solicitar préstamo |
| POST | `/api/prestamos/{id}/aprobar` | Aprobar préstamo |
| POST | `/api/prestamos/{id}/rechazar` | Rechazar préstamo |
| POST | `/api/prestamos/{id}/entregar` | Registrar entrega |
| POST | `/api/prestamos/{id}/devolver` | Registrar devolución |

---

## AUDITORÍA (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/auditoria/historial` | Historial de acciones |
| GET | `/api/auditoria/errores` | Log de errores |
| GET | `/api/auditoria/papelera/archivos` | Archivos eliminados |
| GET | `/api/auditoria/papelera/carpetas` | Carpetas eliminadas |
| POST | `/api/auditoria/papelera/restaurar-archivo/{id}` | Restaurar archivo |
| POST | `/api/auditoria/papelera/restaurar-carpeta/{id}` | Restaurar carpeta |

---

## DÍAS FESTIVOS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/diasfestivos` | Lista días festivos |
| GET | `/api/diasfestivos/año/{año}` | Festivos de un año |
| GET | `/api/diasfestivos/es-habil/{fecha}` | Verificar si es hábil |
| GET | `/api/diasfestivos/calcular` | Calcular días hábiles |
| POST | `/api/diasfestivos` | Agregar festivo |
| DELETE | `/api/diasfestivos/{id}` | Eliminar festivo |

### GET /api/diasfestivos/calcular
```
?fechaInicio=2026-01-01&fechaFin=2026-01-31
```

---

## NOTIFICACIONES (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/notificaciones` | Mis notificaciones |
| GET | `/api/notificaciones/no-leidas` | Solo no leídas |
| GET | `/api/notificaciones/conteo` | Conteo de no leídas |
| POST | `/api/notificaciones/{id}/marcar-leida` | Marcar como leída |
| POST | `/api/notificaciones/marcar-todas-leidas` | Marcar todas leídas |

WebSocket: `/ws/notificaciones`

---

## DATOS ESTÁTICOS (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/datosestaticos/todos` | Todos los catálogos |
| GET | `/api/datosestaticos/tipos-carpetas` | Tipos de carpetas |
| GET | `/api/datosestaticos/estados-carpetas` | Estados de carpetas |
| GET | `/api/datosestaticos/tipos-archivos` | Tipos de archivos |
| GET | `/api/datosestaticos/tipos-documentales` | Tipos documentales |
| GET | `/api/datosestaticos/estados-radicado` | Estados de radicado |
| GET | `/api/datosestaticos/tipos-solicitud` | Tipos de solicitud PQRSD |
| GET | `/api/datosestaticos/medios-recepcion` | Medios de recepción |
| GET | `/api/datosestaticos/disposiciones-finales` | Disposiciones TRD |
| GET | `/api/datosestaticos/sectores` | Sectores gubernamentales |
| GET | `/api/datosestaticos/tipos-entidad` | Tipos de entidad |

---

## ENTIDADES (Core)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/entidades/{id}` | Obtener entidad |
| PUT | `/api/entidades/{id}` | Actualizar entidad |

---

## NAVEGACIÓN (NavIndex)

| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/navegacion/estructura` | Árbol completo |
| GET | `/api/navegacion/estructura/{carpetaId}` | Subárbol desde carpeta |
| GET | `/api/navegacion/indice/{carpetaId}` | Índice de expediente |
| POST | `/api/navegacion/invalidar-cache` | Invalidar cache Redis |

WebSocket: `/ws/navegacion`

---

## FIRMA ELECTRÓNICA (Signature)

### Solicitudes
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/solicitudes` | Mis solicitudes |
| GET | `/api/solicitudes/{id}` | Obtener solicitud |
| GET | `/api/solicitudes/pendientes` | Pendientes de firmar |
| GET | `/api/solicitudes/archivo/{archivoId}` | Solicitudes de un archivo |
| POST | `/api/solicitudes` | Crear solicitud |
| POST | `/api/solicitudes/{id}/cancelar` | Cancelar solicitud |

### Firma
| Método | Ruta | Descripción |
|--------|------|-------------|
| POST | `/api/firma/simple/iniciar` | Iniciar firma simple (envía OTP) |
| POST | `/api/firma/simple/validar` | Validar OTP y firmar |
| POST | `/api/firma/avanzada` | Firmar con certificado |
| POST | `/api/firma/{firmanteCod}/rechazar` | Rechazar firma |

### Certificados
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/certificados/mis-certificados` | Mis certificados |
| GET | `/api/certificados/{id}` | Obtener certificado |
| POST | `/api/certificados/registrar` | Registrar certificado |
| DELETE | `/api/certificados/{id}` | Revocar certificado |

### Verificación
| Método | Ruta | Descripción | Auth |
|--------|------|-------------|------|
| GET | `/api/verificar/{hash}` | Verificar documento | No |
| GET | `/api/verificar/codigo/{codigo}` | Verificar por código | No |
| GET | `/api/verificar/{hash}/constancia` | Descargar constancia | No |

### Configuración
| Método | Ruta | Descripción |
|--------|------|-------------|
| GET | `/api/configuracion/entidad` | Configuración de firma |
| PUT | `/api/configuracion/entidad` | Actualizar configuración |

---

## Códigos de Estado HTTP

| Código | Significado |
|--------|-------------|
| 200 | OK - Operación exitosa |
| 201 | Created - Recurso creado |
| 204 | No Content - Eliminado exitosamente |
| 400 | Bad Request - Datos inválidos |
| 401 | Unauthorized - No autenticado |
| 403 | Forbidden - Sin permisos |
| 404 | Not Found - Recurso no encontrado |
| 409 | Conflict - Conflicto (duplicado, etc.) |
| 500 | Internal Server Error - Error del servidor |
| 503 | Service Unavailable - Servicio no disponible |

---

## Formato de Errores

```json
{
  "message": "Descripción del error",
  "errors": {
    "campo1": ["Error de validación 1"],
    "campo2": ["Error de validación 2"]
  }
}
```
