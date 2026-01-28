-- =====================================================
-- DATOS DE CATÁLOGOS - NubluSoft
-- Ejecutar DESPUÉS de BdNubluSoft.sql
-- =====================================================

-- =====================================================
-- TIPOS DE CARPETAS (TRD)
-- =====================================================
INSERT INTO documentos."Tipos_Carpetas" ("Cod", "Nombre", "Estado") VALUES
(1, 'Serie', true),
(2, 'Subserie', true),
(3, 'Expediente', true),
(4, 'Genérica', true);

-- =====================================================
-- ESTADOS DE CARPETAS
-- =====================================================
INSERT INTO documentos."Estados_Carpetas" ("Cod", "Nombre") VALUES
(1, 'Abierto'),
(2, 'Cerrado'),
(3, 'En Transferencia');

-- =====================================================
-- NIVELES DE VISUALIZACIÓN
-- =====================================================
INSERT INTO documentos."Niveles_Visualizacion_Carpetas" ("Cod", "Nombre") VALUES
(1, 'Público'),
(2, 'Privado'),
(3, 'Confidencial');

-- =====================================================
-- ROLES DEL SISTEMA
-- =====================================================
INSERT INTO documentos."Roles" ("Cod", "Nombre") VALUES
(1, 'Administrador'),
(2, 'Jefe de Oficina'),
(3, 'Radicador'),
(4, 'Funcionario'),
(5, 'Consulta');

-- =====================================================
-- TIPOS DE COMUNICACIÓN (Acuerdo 060 AGN)
-- =====================================================
INSERT INTO documentos."Tipos_Comunicacion" ("Cod", "Nombre", "Descripcion", "RequiereRespuesta", "Prefijo") VALUES
('E', 'Entrada', 'Comunicaciones recibidas de externos', true, 'E'),
('S', 'Salida', 'Comunicaciones enviadas a externos', false, 'S'),
('I', 'Interna', 'Comunicaciones entre dependencias', true, 'I');

-- =====================================================
-- TIPOS DE SOLICITUD - PQRSD (Ley 1755/2015)
-- =====================================================
INSERT INTO documentos."Tipos_Solicitud" ("Cod", "Nombre", "Descripcion", "DiasHabilesRespuesta", "DiasHabilesProrroga", "EsCalendario", "RequiereRespuestaEscrita", "Normativa", "Estado") VALUES
('PETICION', 'Petición General', 'Derecho de petición de interés general o particular', 15, 15, false, true, 'Ley 1755/2015 Art. 14', true),
('INFORMACION', 'Petición de Información', 'Solicitud de acceso a información pública', 10, 10, false, true, 'Ley 1755/2015 Art. 14', true),
('CONSULTA', 'Consulta', 'Consulta sobre materias a cargo de la entidad', 30, 0, false, true, 'Ley 1755/2015 Art. 14', true),
('QUEJA', 'Queja', 'Manifestación de inconformidad con la conducta de un servidor', 15, 0, false, true, 'Ley 1755/2015 Art. 14', true),
('RECLAMO', 'Reclamo', 'Manifestación por suspensión injustificada o prestación deficiente', 15, 0, false, true, 'Ley 1755/2015 Art. 14', true),
('DENUNCIA', 'Denuncia', 'Puesta en conocimiento de conductas irregulares', 15, 0, false, false, 'Ley 1755/2015 Art. 14', true),
('SUGERENCIA', 'Sugerencia', 'Propuesta para mejorar el servicio', 15, 0, false, false, 'Ley 1755/2015', true),
('TUTELA', 'Acción de Tutela', 'Mecanismo de protección de derechos fundamentales', 10, 0, true, true, 'Constitución Art. 86', true),
('DESISTIMIENTO', 'Desistimiento Tácito', 'Requerimiento de información adicional', 30, 0, true, true, 'Ley 1755/2015 Art. 17', true),
('SILENCIO', 'Silencio Administrativo', 'Configuración de silencio administrativo positivo', 3, 0, false, true, 'Ley 1755/2015', true);

-- =====================================================
-- ESTADOS DE RADICADO
-- =====================================================
INSERT INTO documentos."Estados_Radicado" ("Cod", "Nombre", "Descripcion", "EsFinal", "Color", "Orden") VALUES
('RADICADO', 'Radicado', 'Documento recibido y radicado', false, '#3498db', 1),
('ASIGNADO', 'Asignado', 'Asignado a funcionario responsable', false, '#9b59b6', 2),
('EN_PROCESO', 'En Proceso', 'En trámite de respuesta', false, '#f39c12', 3),
('PRORROGA', 'En Prórroga', 'Se solicitó ampliación del término', false, '#e67e22', 4),
('TRASLADADO', 'Trasladado', 'Trasladado a otra entidad por competencia', true, '#95a5a6', 5),
('RESPONDIDO', 'Respondido', 'Con respuesta emitida', false, '#27ae60', 6),
('ARCHIVADO', 'Archivado', 'Archivado en expediente', true, '#2c3e50', 7),
('ANULADO', 'Anulado', 'Radicado anulado', true, '#e74c3c', 8),
('DEVUELTO', 'Devuelto', 'Devuelto al remitente', true, '#c0392b', 9);

-- =====================================================
-- MEDIOS DE RECEPCIÓN
-- =====================================================
INSERT INTO documentos."Medios_Recepcion" ("Cod", "Nombre", "RequiereDigitalizacion", "Estado") VALUES
('VENTANILLA', 'Ventanilla Única', true, true),
('CORREO', 'Correo Certificado', true, true),
('EMAIL', 'Correo Electrónico', false, true),
('WEB', 'Portal Web', false, true),
('FAX', 'Fax', true, true),
('PRESENCIAL', 'Entrega Personal', true, true);

-- =====================================================
-- PRIORIDADES DE RADICADO
-- =====================================================
INSERT INTO documentos."Prioridades_Radicado" ("Cod", "Nombre", "DiasReduccion", "Color", "Orden") VALUES
('NORMAL', 'Normal', 0, '#3498db', 1),
('ALTA', 'Alta', 3, '#f39c12', 2),
('URGENTE', 'Urgente', 5, '#e74c3c', 3);

-- =====================================================
-- DISPOSICIONES FINALES TRD (Acuerdo 004/2019 AGN)
-- =====================================================
INSERT INTO documentos."Disposiciones_Finales" ("Cod", "Nombre", "Descripcion") VALUES
('CT', 'Conservación Total', 'Documentos de valor permanente que deben conservarse indefinidamente'),
('E', 'Eliminación', 'Documentos que una vez cumplido su tiempo de retención pueden destruirse'),
('S', 'Selección', 'Proceso de escoger documentación con valor secundario de una serie'),
('M', 'Microfilmación/Digitalización', 'Técnica de reprografía para conservación en otros soportes');

-- =====================================================
-- TIPOS DE DOCUMENTO DE IDENTIDAD (Colombia)
-- =====================================================
INSERT INTO documentos."Tipos_Documento_Identidad" ("Cod", "Nombre", "Pais") VALUES
('CC', 'Cédula de Ciudadanía', 'Colombia'),
('CE', 'Cédula de Extranjería', 'Colombia'),
('NIT', 'Número de Identificación Tributaria', 'Colombia'),
('PA', 'Pasaporte', NULL),
('TI', 'Tarjeta de Identidad', 'Colombia'),
('RC', 'Registro Civil', 'Colombia'),
('PEP', 'Permiso Especial de Permanencia', 'Colombia'),
('PPT', 'Permiso por Protección Temporal', 'Colombia');

-- =====================================================
-- TIPOS DE ENTIDAD
-- =====================================================
INSERT INTO documentos."Tipos_Entidad" ("Cod", "Nombre") VALUES
('PUBLICA', 'Entidad Pública'),
('PRIVADA', 'Entidad Privada'),
('MIXTA', 'Economía Mixta'),
('DESCENTRALIZADA', 'Entidad Descentralizada'),
('TERRITORIAL', 'Entidad Territorial');

-- =====================================================
-- TIPOS DE FIRMA (Ley 527/1999)
-- =====================================================
INSERT INTO documentos."Tipos_Firma" ("Cod", "Nombre", "Descripcion", "NivelSeguridad") VALUES
('OTP', 'Firma Simple OTP', 'Firma con código de un solo uso enviado por correo/SMS', 1),
('X509', 'Firma Digital X.509', 'Firma con certificado digital emitido por entidad certificadora', 3),
('OTP_X509', 'Firma Combinada', 'Firma digital con verificación OTP adicional', 4);

-- =====================================================
-- TIPOS DE NOTIFICACIÓN
-- =====================================================
INSERT INTO documentos."Tipos_Notificacion" ("Cod", "Nombre", "Descripcion", "Icono", "Color", "EnviarCorreo", "RequiereAccion", "Estado") VALUES
('RADICADO_ASIGNADO', 'Radicado Asignado', 'Se le ha asignado un nuevo radicado', 'mail', '#3498db', true, true, true),
('RADICADO_TRASLADADO', 'Radicado Trasladado', 'Un radicado ha sido trasladado', 'swap_horiz', '#9b59b6', true, true, true),
('RADICADO_VENCIMIENTO', 'Próximo a Vencer', 'Un radicado está próximo a vencer', 'warning', '#f39c12', true, true, true),
('RADICADO_VENCIDO', 'Radicado Vencido', 'Un radicado ha vencido su término', 'error', '#e74c3c', true, true, true),
('FIRMA_SOLICITADA', 'Firma Solicitada', 'Se requiere su firma en un documento', 'draw', '#2ecc71', true, true, true),
('FIRMA_COMPLETADA', 'Firma Completada', 'El documento ha sido firmado completamente', 'done_all', '#27ae60', true, false, true),
('FIRMA_RECHAZADA', 'Firma Rechazada', 'Se ha rechazado firmar un documento', 'cancel', '#e74c3c', true, true, true),
('VENCIMIENTO_PASADO', 'Solicitud Vencida', 'Una solicitud de firma ha vencido', 'schedule', '#95a5a6', true, false, true),
('TRANSFERENCIA_RECIBIDA', 'Transferencia Recibida', 'Se ha recibido una transferencia documental', 'move_to_inbox', '#3498db', true, true, true),
('PRESTAMO_SOLICITADO', 'Préstamo Solicitado', 'Se ha solicitado préstamo de expediente', 'folder_shared', '#f39c12', true, true, true),
('PRESTAMO_APROBADO', 'Préstamo Aprobado', 'Su solicitud de préstamo fue aprobada', 'check_circle', '#27ae60', true, false, true),
('PRESTAMO_DEVUELTO', 'Préstamo Devuelto', 'El expediente prestado ha sido devuelto', 'assignment_return', '#3498db', true, false, true),
('SISTEMA', 'Notificación del Sistema', 'Notificación general del sistema', 'info', '#607d8b', false, false, true);

-- =====================================================
-- TIPOS DE ARCHIVOS (categorías)
-- =====================================================
INSERT INTO documentos."Tipos_Archivos" ("Cod", "Nombre", "Estado") VALUES
(1, 'Documento', true),
(2, 'Imagen', true),
(3, 'Video', true),
(4, 'Audio', true),
(5, 'Comprimido', true),
(6, 'Otro', true);

-- =====================================================
-- TIPOS DOCUMENTALES (según normativa AGN)
-- =====================================================
INSERT INTO documentos."Tipos_Documentales" ("Cod", "Nombre", "Descripcion", "Estado") VALUES
(1, 'Oficio', 'Comunicación oficial escrita', true),
(2, 'Memorando', 'Comunicación interna', true),
(3, 'Circular', 'Comunicación de carácter general', true),
(4, 'Resolución', 'Acto administrativo de carácter particular', true),
(5, 'Acta', 'Documento que registra lo acontecido en una reunión', true),
(6, 'Informe', 'Documento que describe hechos o actividades', true),
(7, 'Certificado', 'Documento que acredita un hecho', true),
(8, 'Constancia', 'Documento que hace constar un hecho', true),
(9, 'Contrato', 'Acuerdo de voluntades entre partes', true),
(10, 'Convenio', 'Acuerdo entre entidades', true),
(11, 'Factura', 'Documento comercial de venta', true),
(12, 'Recibo', 'Comprobante de pago', true),
(13, 'Planilla', 'Formato diligenciado', true),
(14, 'Formato', 'Documento con estructura predefinida', true),
(15, 'Plano', 'Representación gráfica técnica', true),
(16, 'Fotografía', 'Imagen fotográfica', true),
(17, 'Video', 'Registro audiovisual', true),
(18, 'Audio', 'Registro sonoro', true);

-- =====================================================
-- BASE DE DATOS INICIAL (para multitenancy)
-- =====================================================
INSERT INTO usuarios."BasesDatos" ("Cod", "Bd", "Pw", "Usu", "Host", "CantEntidades", "MaxEntidades") VALUES
(1, 'nublusoft', '8213', 'postgres', 'localhost', 1, 15);

-- Actualizar secuencia
SELECT setval('usuarios."BasesDatos_Cod_seq"', 1, true);

-- =====================================================
-- ENTIDAD INICIAL (esquema usuarios para autenticación)
-- =====================================================
INSERT INTO usuarios."Entidades" ("Cod", "Nombre", "Telefono", "Nit", "Direccion", "Correo", "Bd", "Url", "Coleccion") VALUES
(1, 'Entidad Demo', '3001234567', '900123456-1', 'Calle 100 # 10-20', 'demo@nublusoft.com', 1, NULL, 'entidad_demo');

-- Actualizar secuencia
SELECT setval('usuarios."seq_Entidades"', 1, true);

-- =====================================================
-- ENTIDAD EN ESQUEMA DOCUMENTOS (datos completos)
-- =====================================================
INSERT INTO documentos."Entidades" ("Cod", "Nombre", "Telefono", "Nit", "Direccion", "Correo", "Ciudad", "Departamento", "TipoEntidad", "Estado") VALUES
(1, 'Entidad Demo', '3001234567', '900123456-1', 'Calle 100 # 10-20', 'demo@nublusoft.com', 'Bogotá D.C.', 'Cundinamarca', 'PUBLICA', true);

-- =====================================================
-- OFICINA INICIAL
-- =====================================================
INSERT INTO documentos."Oficinas" ("Cod", "Nombre", "Codigo", "Entidad", "Estado", "NivelJerarquico") VALUES
(1, 'Dirección General', '100', 1, true, 1);

-- =====================================================
-- USUARIO ADMINISTRADOR INICIAL
-- Contraseña: Admin123* (hash BCrypt)
-- =====================================================
INSERT INTO usuarios."Usuarios" ("Cod", "Nombres", "Apellidos", "Telefono", "Documento", "Usuario", "Contraseña", "Estado", "Entidad") VALUES
(1, 'Administrador', 'Sistema', '3001234567', '1234567890', 'admin', '$2a$11$rBXKQ0c5cVj6D5xQ2n5k7OZ0nEXsMvPvNlMXxqjK0wTZVPWN.CZoK', true, 1);

-- Actualizar secuencia
SELECT setval('usuarios."seq_Usuarios"', 1, true);

-- =====================================================
-- ASIGNAR ROL ADMINISTRADOR AL USUARIO
-- =====================================================
INSERT INTO documentos."Roles_Usuarios" ("Cod", "Oficina", "Rol", "Usuario", "Estado", "FechaInicio") VALUES
(1, 1, 1, 1, true, CURRENT_TIMESTAMP);

-- =====================================================
-- DÍAS FESTIVOS 2025-2026 (Colombia)
-- EsFijo: true = fecha fija (ej: 1 enero), false = fecha móvil (ej: Semana Santa)
-- =====================================================
INSERT INTO documentos."Dias_Festivos" ("Fecha", "Nombre", "EsFijo", "Pais") VALUES
-- 2025
('2025-01-01', 'Año Nuevo', true, 'Colombia'),
('2025-01-06', 'Día de los Reyes Magos', false, 'Colombia'),
('2025-03-24', 'Día de San José', false, 'Colombia'),
('2025-04-17', 'Jueves Santo', false, 'Colombia'),
('2025-04-18', 'Viernes Santo', false, 'Colombia'),
('2025-05-01', 'Día del Trabajo', true, 'Colombia'),
('2025-06-02', 'Ascensión del Señor', false, 'Colombia'),
('2025-06-23', 'Corpus Christi', false, 'Colombia'),
('2025-06-30', 'Sagrado Corazón de Jesús', false, 'Colombia'),
('2025-07-20', 'Día de la Independencia', true, 'Colombia'),
('2025-08-07', 'Batalla de Boyacá', true, 'Colombia'),
('2025-08-18', 'Asunción de la Virgen', false, 'Colombia'),
('2025-10-13', 'Día de la Raza', false, 'Colombia'),
('2025-11-03', 'Todos los Santos', false, 'Colombia'),
('2025-11-17', 'Independencia de Cartagena', false, 'Colombia'),
('2025-12-08', 'Inmaculada Concepción', true, 'Colombia'),
('2025-12-25', 'Navidad', true, 'Colombia'),
-- 2026
('2026-01-01', 'Año Nuevo', true, 'Colombia'),
('2026-01-12', 'Día de los Reyes Magos', false, 'Colombia'),
('2026-03-23', 'Día de San José', false, 'Colombia'),
('2026-04-02', 'Jueves Santo', false, 'Colombia'),
('2026-04-03', 'Viernes Santo', false, 'Colombia'),
('2026-05-01', 'Día del Trabajo', true, 'Colombia'),
('2026-05-18', 'Ascensión del Señor', false, 'Colombia'),
('2026-06-08', 'Corpus Christi', false, 'Colombia'),
('2026-06-15', 'Sagrado Corazón de Jesús', false, 'Colombia'),
('2026-06-29', 'San Pedro y San Pablo', false, 'Colombia'),
('2026-07-20', 'Día de la Independencia', true, 'Colombia'),
('2026-08-07', 'Batalla de Boyacá', true, 'Colombia'),
('2026-08-17', 'Asunción de la Virgen', false, 'Colombia'),
('2026-10-12', 'Día de la Raza', false, 'Colombia'),
('2026-11-02', 'Todos los Santos', false, 'Colombia'),
('2026-11-16', 'Independencia de Cartagena', false, 'Colombia'),
('2026-12-08', 'Inmaculada Concepción', true, 'Colombia'),
('2026-12-25', 'Navidad', true, 'Colombia');

-- =====================================================
-- FIN DEL SCRIPT DE DATOS
-- =====================================================
