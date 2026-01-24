--
-- PostgreSQL database dump
--

-- Dumped from database version 17.5
-- Dumped by pg_dump version 17.5

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET transaction_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- Name: documentos; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA documentos;


--
-- Name: SCHEMA documentos; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA documentos IS 'Gestión documental, TRD, Ventanilla Única - Antes Fily_App';


--
-- Name: usuarios; Type: SCHEMA; Schema: -; Owner: -
--

CREATE SCHEMA usuarios;


--
-- Name: SCHEMA usuarios; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON SCHEMA usuarios IS 'Gestión de usuarios, entidades y planes - Antes Fily_Usuarios';


--
-- Name: pgcrypto; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS pgcrypto WITH SCHEMA public;


--
-- Name: EXTENSION pgcrypto; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION pgcrypto IS 'cryptographic functions';


--
-- Name: unaccent; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS unaccent WITH SCHEMA public;


--
-- Name: EXTENSION unaccent; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION unaccent IS 'text search dictionary that removes accents';


--
-- Name: uuid-ossp; Type: EXTENSION; Schema: -; Owner: -
--

CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;


--
-- Name: EXTENSION "uuid-ossp"; Type: COMMENT; Schema: -; Owner: -
--

COMMENT ON EXTENSION "uuid-ossp" IS 'generate universally unique identifiers (UUIDs)';


--
-- Name: F_ActualizarSolicitudesVencidas(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_ActualizarSolicitudesVencidas"() RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Actualizados INTEGER := 0;
    v_Solicitud RECORD;
BEGIN
    FOR v_Solicitud IN
        SELECT s."Cod", s."Entidad", s."SolicitadoPor", s."Archivo", a."Nombre" AS "ArchivoNombre"
        FROM documentos."Solicitudes_Firma" s
        INNER JOIN documentos."Archivos" a ON s."Archivo" = a."Cod"
        WHERE s."Estado" IN ('PENDIENTE', 'EN_PROCESO') AND s."FechaVencimiento" IS NOT NULL AND s."FechaVencimiento" < CURRENT_TIMESTAMP
    LOOP
        UPDATE documentos."Solicitudes_Firma" SET "Estado" = 'VENCIDA' WHERE "Cod" = v_Solicitud."Cod";
        UPDATE documentos."Archivos" SET "FimarPor" = NULL WHERE "Cod" = v_Solicitud."Archivo";
        INSERT INTO documentos."Notificaciones" ("Entidad", "UsuarioDestino", "TipoNotificacion", "Titulo", "Mensaje", "SolicitudFirmaRef", "Prioridad")
        VALUES (v_Solicitud."Entidad", v_Solicitud."SolicitadoPor", 'VENCIMIENTO_PASADO', 'Solicitud vencida: ' || v_Solicitud."ArchivoNombre", 'La solicitud ha vencido.', v_Solicitud."Cod", 'ALTA');
        v_Actualizados := v_Actualizados + 1;
    END LOOP;
    
    DELETE FROM documentos."Codigos_OTP" WHERE "FechaExpiracion" < CURRENT_TIMESTAMP - INTERVAL '1 day';
    
    RETURN v_Actualizados;
END;
$$;


--
-- Name: F_AgregarDiasHabiles(date, integer); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_AgregarDiasHabiles"(p_fecha_inicio date, p_dias_habiles integer) RETURNS date
    LANGUAGE plpgsql STABLE
    AS $$
DECLARE
    v_fecha_resultado DATE := p_fecha_inicio;
    v_dias_contados INTEGER := 0;
    v_max_iteraciones INTEGER := 500;  -- Límite de seguridad
    v_iteracion INTEGER := 0;
BEGIN
    -- Si días es 0 o negativo, retornar la misma fecha
    IF p_dias_habiles <= 0 THEN
        RETURN p_fecha_inicio;
    END IF;
    
    WHILE v_dias_contados < p_dias_habiles AND v_iteracion < v_max_iteraciones LOOP
        v_fecha_resultado := v_fecha_resultado + INTERVAL '1 day';
        v_iteracion := v_iteracion + 1;
        
        -- Si es día hábil, contar
        IF "F_EsDiaHabil"(v_fecha_resultado) THEN
            v_dias_contados := v_dias_contados + 1;
        END IF;
    END LOOP;
    
    RETURN v_fecha_resultado;
END;
$$;


--
-- Name: FUNCTION "F_AgregarDiasHabiles"(p_fecha_inicio date, p_dias_habiles integer); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_AgregarDiasHabiles"(p_fecha_inicio date, p_dias_habiles integer) IS 'Agrega días hábiles a una fecha, excluyendo fines de semana y festivos';


--
-- Name: F_AgregarExpedienteTransferencia(bigint, bigint, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_AgregarExpedienteTransferencia"(p_transferenciacod bigint, p_carpetacod bigint, p_observaciones character varying DEFAULT NULL::character varying) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_TipoCarpeta BIGINT;
    v_EstadoTransferencia VARCHAR(20);
BEGIN
    -- Verificar transferencia
    SELECT "Estado" INTO v_EstadoTransferencia
    FROM "Transferencias_Documentales"
    WHERE "Cod" = p_TransferenciaCod;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, 'Transferencia no encontrada'::VARCHAR(500);
        RETURN;
    END IF;
    
    IF v_EstadoTransferencia != 'PENDIENTE' THEN
        RETURN QUERY SELECT false, 'Solo se pueden agregar expedientes a transferencias pendientes'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar carpeta (solo expedientes cerrados)
    SELECT "TipoCarpeta" INTO v_TipoCarpeta
    FROM "Carpetas"
    WHERE "Cod" = p_CarpetaCod AND "Estado" = true AND "EstadoCarpeta" = 2; -- 2 = Cerrada
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, 'Expediente no encontrado, no está cerrado, o está inactivo'::VARCHAR(500);
        RETURN;
    END IF;
    
    IF v_TipoCarpeta != 3 THEN -- 3 = Expediente
        RETURN QUERY SELECT false, 'Solo se pueden transferir Expedientes'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar que no esté ya en la transferencia
    IF EXISTS (SELECT 1 FROM "Transferencias_Detalle" WHERE "Transferencia" = p_TransferenciaCod AND "Carpeta" = p_CarpetaCod) THEN
        RETURN QUERY SELECT false, 'El expediente ya está en esta transferencia'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Agregar
    INSERT INTO "Transferencias_Detalle" ("Transferencia", "Carpeta", "Observaciones")
    VALUES (p_TransferenciaCod, p_CarpetaCod, p_Observaciones);
    
    -- Actualizar contadores
    UPDATE "Transferencias_Documentales"
    SET "NumeroExpedientes" = COALESCE("NumeroExpedientes", 0) + 1
    WHERE "Cod" = p_TransferenciaCod;
    
    RETURN QUERY SELECT true, 'Expediente agregado a la transferencia'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_AgregarExpedienteTransferencia"(p_transferenciacod bigint, p_carpetacod bigint, p_observaciones character varying); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_AgregarExpedienteTransferencia"(p_transferenciacod bigint, p_carpetacod bigint, p_observaciones character varying) IS 'Agrega un expediente cerrado a una transferencia documental';


--
-- Name: F_AnularRadicado(bigint, bigint, text, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_AnularRadicado"(p_radicadocod bigint, p_entidad bigint, p_motivoanulacion text, p_usuarioanula bigint) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_EstadoAnterior VARCHAR(20);
BEGIN
    -- Validar motivo
    IF p_MotivoAnulacion IS NULL OR TRIM(p_MotivoAnulacion) = '' THEN
        RETURN QUERY SELECT 
            false,
            'El motivo de la anulación es obligatorio'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Obtener estado actual
    SELECT "Estado" INTO v_EstadoAnterior
    FROM "Radicados"
    WHERE "Cod" = p_RadicadoCod AND "Entidad" = p_Entidad AND "Activo" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            false,
            'Radicado no encontrado'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar que no esté ya anulado
    IF v_EstadoAnterior = 'ANULADO' THEN
        RETURN QUERY SELECT 
            false,
            'El radicado ya está anulado'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Anular radicado
    UPDATE "Radicados"
    SET 
        "Estado" = 'ANULADO',
        "Activo" = false,
        "FechaCambioEstado" = CURRENT_TIMESTAMP,
        "FechaModificacion" = CURRENT_TIMESTAMP,
        "ModificadoPor" = p_UsuarioAnula
    WHERE "Cod" = p_RadicadoCod AND "Entidad" = p_Entidad;
    
    -- Registrar en trazabilidad
    INSERT INTO "Radicados_Trazabilidad" (
        "Radicado", "Entidad", "Accion", "Descripcion",
        "EstadoAnterior", "EstadoNuevo", "Fecha", "Observaciones"
    ) VALUES (
        p_RadicadoCod, p_Entidad, 'ANULADO', 'Radicado anulado',
        v_EstadoAnterior, 'ANULADO', CURRENT_TIMESTAMP, p_MotivoAnulacion
    );
    
    RETURN QUERY SELECT 
        true,
        'Radicado anulado exitosamente'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_AnularRadicado"(p_radicadocod bigint, p_entidad bigint, p_motivoanulacion text, p_usuarioanula bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_AnularRadicado"(p_radicadocod bigint, p_entidad bigint, p_motivoanulacion text, p_usuarioanula bigint) IS 'Anula un radicado (operación irreversible, requiere permisos especiales)';


--
-- Name: F_ArchivarRadicado(bigint, bigint, bigint, bigint, text); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_ArchivarRadicado"(p_radicadocod bigint, p_entidad bigint, p_expedientecod bigint, p_usuarioarchiva bigint, p_observaciones text DEFAULT NULL::text) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_EstadoAnterior VARCHAR(20);
    v_TipoCarpeta BIGINT;
BEGIN
    -- Verificar que el expediente existe y es válido (tipo 3 = Expediente)
    SELECT "TipoCarpeta" INTO v_TipoCarpeta
    FROM "Carpetas"
    WHERE "Cod" = p_ExpedienteCod AND "Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            false,
            'Expediente no encontrado o inactivo'::VARCHAR(500);
        RETURN;
    END IF;
    
    IF v_TipoCarpeta NOT IN (3, 4) THEN -- 3=Expediente, 4=Genérica
        RETURN QUERY SELECT 
            false,
            'Solo se puede archivar en Expedientes o Carpetas Genéricas'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Obtener estado actual del radicado
    SELECT "Estado" INTO v_EstadoAnterior
    FROM "Radicados"
    WHERE "Cod" = p_RadicadoCod AND "Entidad" = p_Entidad AND "Activo" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            false,
            'Radicado no encontrado o inactivo'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar que no esté ya archivado o anulado
    IF v_EstadoAnterior IN ('ARCHIVADO', 'ANULADO') THEN
        RETURN QUERY SELECT 
            false,
            ('El radicado ya está en estado ' || v_EstadoAnterior)::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Actualizar radicado
    UPDATE "Radicados"
    SET 
        "ExpedienteVinculado" = p_ExpedienteCod,
        "Estado" = 'ARCHIVADO',
        "FechaArchivado" = CURRENT_TIMESTAMP,
        "FechaCambioEstado" = CURRENT_TIMESTAMP,
        "FechaModificacion" = CURRENT_TIMESTAMP,
        "ModificadoPor" = p_UsuarioArchiva
    WHERE "Cod" = p_RadicadoCod AND "Entidad" = p_Entidad;
    
    -- Registrar en trazabilidad
    INSERT INTO "Radicados_Trazabilidad" (
        "Radicado", "Entidad", "Accion", "Descripcion",
        "EstadoAnterior", "EstadoNuevo", "Fecha", "Observaciones"
    ) VALUES (
        p_RadicadoCod, p_Entidad, 'ARCHIVADO', 
        'Archivado en expediente ' || p_ExpedienteCod,
        v_EstadoAnterior, 'ARCHIVADO', CURRENT_TIMESTAMP, p_Observaciones
    );
    
    RETURN QUERY SELECT 
        true,
        'Radicado archivado exitosamente'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_ArchivarRadicado"(p_radicadocod bigint, p_entidad bigint, p_expedientecod bigint, p_usuarioarchiva bigint, p_observaciones text); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_ArchivarRadicado"(p_radicadocod bigint, p_entidad bigint, p_expedientecod bigint, p_usuarioarchiva bigint, p_observaciones text) IS 'Vincula un radicado a un expediente y lo marca como archivado';


--
-- Name: F_AsignarRadicado(bigint, bigint, bigint, bigint, bigint, bigint, text); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_AsignarRadicado"(p_radicadocod bigint, p_entidad bigint, p_oficinadestino bigint, p_usuariodestino bigint DEFAULT NULL::bigint, p_usuarioasigna bigint DEFAULT NULL::bigint, p_oficinaorigen bigint DEFAULT NULL::bigint, p_observaciones text DEFAULT NULL::text) RETURNS TABLE("Exito" boolean, "EstadoAnterior" character varying, "EstadoNuevo" character varying, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_EstadoAnterior VARCHAR(20);
    v_OficinaAnterior BIGINT;
    v_UsuarioAnterior BIGINT;
BEGIN
    -- Obtener estado actual
    SELECT "Estado", "OficinaDestino", "UsuarioDestino"
    INTO v_EstadoAnterior, v_OficinaAnterior, v_UsuarioAnterior
    FROM "Radicados"
    WHERE "Cod" = p_RadicadoCod AND "Entidad" = p_Entidad AND "Activo" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            false,
            NULL::VARCHAR(20),
            NULL::VARCHAR(20),
            'Radicado no encontrado o inactivo'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar que no esté en estado final
    IF v_EstadoAnterior IN ('ARCHIVADO', 'ANULADO') THEN
        RETURN QUERY SELECT 
            false,
            v_EstadoAnterior,
            v_EstadoAnterior,
            'No se puede asignar un radicado en estado ' || v_EstadoAnterior::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Actualizar radicado
    UPDATE "Radicados"
    SET 
        "OficinaDestino" = p_OficinaDestino,
        "UsuarioDestino" = p_UsuarioDestino,
        "Estado" = 'ASIGNADO',
        "FechaCambioEstado" = CURRENT_TIMESTAMP,
        "FechaModificacion" = CURRENT_TIMESTAMP,
        "ModificadoPor" = p_UsuarioAsigna
    WHERE "Cod" = p_RadicadoCod AND "Entidad" = p_Entidad;
    
    -- Registrar en trazabilidad
    INSERT INTO "Radicados_Trazabilidad" (
        "Radicado", "Entidad", "Accion", "Descripcion",
        "OficinaOrigen", "UsuarioOrigen", "OficinaDestino", "UsuarioDestino",
        "EstadoAnterior", "EstadoNuevo", "Fecha", "Observaciones"
    ) VALUES (
        p_RadicadoCod, p_Entidad, 
        CASE WHEN v_OficinaAnterior IS NULL THEN 'ASIGNADO' ELSE 'REASIGNADO' END,
        CASE WHEN v_OficinaAnterior IS NULL THEN 'Asignación inicial' ELSE 'Reasignación de radicado' END,
        COALESCE(p_OficinaOrigen, v_OficinaAnterior), v_UsuarioAnterior,
        p_OficinaDestino, p_UsuarioDestino,
        v_EstadoAnterior, 'ASIGNADO', CURRENT_TIMESTAMP, p_Observaciones
    );
    
    RETURN QUERY SELECT 
        true,
        v_EstadoAnterior,
        'ASIGNADO'::VARCHAR(20),
        'Radicado asignado exitosamente'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_AsignarRadicado"(p_radicadocod bigint, p_entidad bigint, p_oficinadestino bigint, p_usuariodestino bigint, p_usuarioasigna bigint, p_oficinaorigen bigint, p_observaciones text); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_AsignarRadicado"(p_radicadocod bigint, p_entidad bigint, p_oficinadestino bigint, p_usuariodestino bigint, p_usuarioasigna bigint, p_oficinaorigen bigint, p_observaciones text) IS 'Asigna o reasigna un radicado a una oficina/usuario con trazabilidad';


--
-- Name: F_AsignarTRDOficina(bigint, bigint, bigint, boolean, boolean); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_AsignarTRDOficina"(p_oficina bigint, p_trd bigint, p_entidad bigint, p_puedeeditar boolean DEFAULT true, p_puedeeliminar boolean DEFAULT false) RETURNS TABLE("Exito" boolean, "AsignacionCod" bigint, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_AsignacionCod BIGINT;
    v_OficinaNombre VARCHAR(200);
    v_TRDNombre VARCHAR(200);
BEGIN
    -- Verificar que la oficina existe
    SELECT "Nombre" INTO v_OficinaNombre
    FROM "Oficinas"
    WHERE "Cod" = p_Oficina AND "Entidad" = p_Entidad AND "Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, NULL::BIGINT, 'Oficina no encontrada'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar que la TRD existe
    SELECT "Nombre" INTO v_TRDNombre
    FROM "Tablas_Retencion_Documental"
    WHERE "Cod" = p_TRD AND "Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, NULL::BIGINT, 'TRD no encontrada'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar si ya existe la asignación
    SELECT "Cod" INTO v_AsignacionCod
    FROM "Oficinas_TRD"
    WHERE "Oficina" = p_Oficina AND "TRD" = p_TRD AND "Entidad" = p_Entidad;
    
    IF FOUND THEN
        -- Actualizar asignación existente
        UPDATE "Oficinas_TRD"
        SET 
            "PuedeEditar" = p_PuedeEditar,
            "PuedeEliminar" = p_PuedeEliminar,
            "Estado" = true,
            "FechaAsignacion" = CURRENT_TIMESTAMP
        WHERE "Cod" = v_AsignacionCod;
        
        RETURN QUERY SELECT true, v_AsignacionCod, 
            ('Asignación actualizada: ' || v_OficinaNombre || ' → ' || v_TRDNombre)::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Crear nueva asignación
    SELECT COALESCE(MAX("Cod"), 0) + 1 INTO v_AsignacionCod FROM "Oficinas_TRD";
    
    INSERT INTO "Oficinas_TRD" (
        "Cod", "Oficina", "TRD", "Entidad",
        "PuedeEditar", "PuedeEliminar", "FechaAsignacion", "Estado"
    ) VALUES (
        v_AsignacionCod, p_Oficina, p_TRD, p_Entidad,
        p_PuedeEditar, p_PuedeEliminar, CURRENT_TIMESTAMP, true
    );
    
    RETURN QUERY SELECT true, v_AsignacionCod,
        ('TRD asignada: ' || v_OficinaNombre || ' → ' || v_TRDNombre)::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_AsignarTRDOficina"(p_oficina bigint, p_trd bigint, p_entidad bigint, p_puedeeditar boolean, p_puedeeliminar boolean); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_AsignarTRDOficina"(p_oficina bigint, p_trd bigint, p_entidad bigint, p_puedeeditar boolean, p_puedeeliminar boolean) IS 'Asigna una TRD a una oficina con permisos específicos';


--
-- Name: F_CalcularVencimientoRadicado(timestamp without time zone, character varying, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CalcularVencimientoRadicado"(p_fecha_radicacion timestamp without time zone, p_tipo_solicitud character varying, p_prioridad character varying DEFAULT 'NORMAL'::character varying) RETURNS timestamp without time zone
    LANGUAGE plpgsql STABLE
    AS $$
DECLARE
    v_dias_respuesta INTEGER;
    v_es_calendario BOOLEAN;
    v_dias_reduccion INTEGER := 0;
    v_fecha_vencimiento TIMESTAMP;
BEGIN
    -- Obtener días de respuesta y tipo de días del tipo de solicitud
    SELECT 
        "DiasHabilesRespuesta",
        "EsCalendario"
    INTO v_dias_respuesta, v_es_calendario
    FROM "Tipos_Solicitud"
    WHERE "Cod" = p_tipo_solicitud AND "Estado" = true;
    
    -- Si no encuentra el tipo, retornar NULL
    IF v_dias_respuesta IS NULL THEN
        RETURN NULL;
    END IF;
    
    -- Obtener reducción por prioridad
    SELECT COALESCE("DiasReduccion", 0)
    INTO v_dias_reduccion
    FROM "Prioridades_Radicado"
    WHERE "Cod" = p_prioridad;
    
    -- Aplicar reducción (no puede quedar en menos de 1 día)
    v_dias_respuesta := GREATEST(v_dias_respuesta - COALESCE(v_dias_reduccion, 0), 1);
    
    -- Calcular fecha de vencimiento
    IF v_es_calendario THEN
        -- Días calendario: simplemente sumar
        v_fecha_vencimiento := p_fecha_radicacion + (v_dias_respuesta || ' days')::INTERVAL;
    ELSE
        -- Días hábiles: usar función
        v_fecha_vencimiento := "F_AgregarDiasHabiles"(p_fecha_radicacion::DATE, v_dias_respuesta)::TIMESTAMP;
    END IF;
    
    RETURN v_fecha_vencimiento;
END;
$$;


--
-- Name: FUNCTION "F_CalcularVencimientoRadicado"(p_fecha_radicacion timestamp without time zone, p_tipo_solicitud character varying, p_prioridad character varying); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_CalcularVencimientoRadicado"(p_fecha_radicacion timestamp without time zone, p_tipo_solicitud character varying, p_prioridad character varying) IS 'Calcula la fecha de vencimiento de un radicado según su tipo de solicitud y prioridad';


--
-- Name: F_CancelarSolicitud(bigint, bigint, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CancelarSolicitud"(p_solicitudcod bigint, p_usuarioid bigint, p_motivocancelacion character varying) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_ArchivoId BIGINT;
    v_Entidad BIGINT;
    v_Estado VARCHAR(20);
    v_SolicitadoPor BIGINT;
BEGIN
    SELECT "Archivo", "Entidad", "Estado", "SolicitadoPor"
    INTO v_ArchivoId, v_Entidad, v_Estado, v_SolicitadoPor
    FROM documentos."Solicitudes_Firma" WHERE "Cod" = p_SolicitudCod;
    
    IF NOT FOUND THEN 
        RETURN QUERY SELECT false, 'Solicitud no encontrada'::VARCHAR(500); 
        RETURN; 
    END IF;
    
    IF v_SolicitadoPor != p_UsuarioId THEN 
        RETURN QUERY SELECT false, 'Solo el solicitante puede cancelar'::VARCHAR(500); 
        RETURN; 
    END IF;
    
    IF v_Estado NOT IN ('PENDIENTE', 'EN_PROCESO') THEN 
        RETURN QUERY SELECT false, ('No cancelable en estado: ' || v_Estado)::VARCHAR(500); 
        RETURN; 
    END IF;
    
    UPDATE documentos."Solicitudes_Firma" 
    SET "Estado" = 'CANCELADA', "FechaCancelada" = CURRENT_TIMESTAMP, "CanceladaPor" = p_UsuarioId, "MotivoCancelacion" = p_MotivoCancelacion 
    WHERE "Cod" = p_SolicitudCod;
    
    UPDATE documentos."Archivos" SET "Firmado" = false, "FimarPor" = NULL WHERE "Cod" = v_ArchivoId;
    
    INSERT INTO documentos."Notificaciones" ("Entidad", "UsuarioDestino", "UsuarioOrigen", "TipoNotificacion", "Titulo", "Mensaje", "SolicitudFirmaRef", "RequiereAccion")
    SELECT v_Entidad, fs."Usuario", p_UsuarioId, 'SISTEMA', 'Solicitud de firma cancelada', 'Cancelada: ' || p_MotivoCancelacion, p_SolicitudCod, false
    FROM documentos."Firmantes_Solicitud" fs WHERE fs."Solicitud" = p_SolicitudCod AND fs."Estado" IN ('PENDIENTE', 'NOTIFICADO');
    
    RETURN QUERY SELECT true, 'Solicitud cancelada'::VARCHAR(500);
END;
$$;


--
-- Name: F_CopiarCarpeta(bigint, bigint, bigint, character varying, boolean); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CopiarCarpeta"(p_carpetaorigen bigint, p_carpetadestino bigint, p_usuario bigint, p_nuevonombre character varying DEFAULT NULL::character varying, p_copiararchivos boolean DEFAULT true) RETURNS TABLE("Exito" boolean, "NuevaCarpetaCod" bigint, "CarpetasCopiadas" integer, "ArchivosCopiados" integer, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
  DECLARE
      v_NuevaCarpetaCod BIGINT;
      v_NombreOrigen VARCHAR(200);
      v_TipoCarpetaOrigen BIGINT;
      v_TipoCarpetaDestino BIGINT;
      v_SerieRaizDestino BIGINT;
      v_CarpetasCopiadas INTEGER := 0;
      v_ArchivosCopiados INTEGER := 0;
      v_HijaCod BIGINT;
      v_NuevoArchivoCod BIGINT;
      v_Validacion RECORD;
      rec RECORD;
  BEGIN
      SELECT "Nombre", "TipoCarpeta"
      INTO v_NombreOrigen, v_TipoCarpetaOrigen
      FROM "Carpetas"
      WHERE "Cod" = p_CarpetaOrigen AND "Estado" = true;

      IF NOT FOUND THEN
          RETURN QUERY SELECT false, NULL::BIGINT, 0, 0, 'Carpeta origen no encontrada'::VARCHAR(500);    
          RETURN;
      END IF;

      IF p_CarpetaDestino IS NOT NULL THEN
          SELECT "TipoCarpeta" INTO v_TipoCarpetaDestino
          FROM "Carpetas"
          WHERE "Cod" = p_CarpetaDestino AND "Estado" = true;

          IF NOT FOUND THEN
              RETURN QUERY SELECT false, NULL::BIGINT, 0, 0, 'Carpeta destino no
  encontrada'::VARCHAR(500);
              RETURN;
          END IF;

          IF p_CarpetaOrigen = p_CarpetaDestino THEN
              RETURN QUERY SELECT false, NULL::BIGINT, 0, 0, 'No se puede copiar una carpeta a sí
  misma'::VARCHAR(500);
              RETURN;
          END IF;

          SELECT * INTO v_Validacion FROM "F_ValidarJerarquiaCarpeta"(v_TipoCarpetaDestino,
  v_TipoCarpetaOrigen);
          IF NOT v_Validacion."Valido" THEN
              RETURN QUERY SELECT false, NULL::BIGINT, 0, 0, v_Validacion."Mensaje";
              RETURN;
          END IF;
      
          v_SerieRaizDestino := "F_ObtenerSerieRaiz"(p_CarpetaDestino);
      END IF;

      v_NuevaCarpetaCod := "F_SiguienteCodCarpeta"();

      INSERT INTO "Carpetas" (
          "Cod", "Nombre", "TipoCarpeta", "CarpetaPadre", "TRD",
          "CodSerie", "CodSubSerie", "SerieRaiz",
          "Descripcion", "NivelVisualizacion", "Delegado",
          "Soporte", "FrecuenciaConsulta", "UbicacionFisica",
          "CodigoExpediente", "PalabrasClave", "Observaciones",
          "CreadoPor", "FechaCreacion", "Estado", "EstadoCarpeta",
          "Copia", "CarpetaOriginal", "Entidad"
      )
      SELECT
          v_NuevaCarpetaCod,
          COALESCE(p_NuevoNombre, "Nombre" || ' (copia)'),
          "TipoCarpeta",
          p_CarpetaDestino,
          "TRD",
          CASE WHEN p_CarpetaDestino IS NULL THEN v_NuevaCarpetaCod ELSE
              (SELECT "CodSerie" FROM "Carpetas" WHERE "Cod" = p_CarpetaDestino) END,
          "CodSubSerie",
          COALESCE(v_SerieRaizDestino, v_NuevaCarpetaCod),
          "Descripcion",
          "NivelVisualizacion",
          "Delegado",
          "Soporte",
          "FrecuenciaConsulta",
          "UbicacionFisica",
          "CodigoExpediente",
          "PalabrasClave",
          "Observaciones",
          p_Usuario,
          CURRENT_TIMESTAMP,
          true,
          1,
          true,
          p_CarpetaOrigen,
          "Entidad"
      FROM "Carpetas"
      WHERE "Cod" = p_CarpetaOrigen;

      v_CarpetasCopiadas := 1;

      IF p_CopiarArchivos THEN
          FOR rec IN
              SELECT * FROM "Archivos" WHERE "Carpeta" = p_CarpetaOrigen AND "Estado" = true
          LOOP
              v_NuevoArchivoCod := COALESCE((SELECT MAX("Cod") FROM "Archivos"), 0) + 1;

              INSERT INTO "Archivos" (
                  "Cod", "Nombre", "Carpeta", "Ruta", "TipoArchivo", "Formato",
                  "NumeroHojas", "Tamaño", "Estado", "Indice",
                  "FechaDocumento", "FechaIncorporacion", "OrigenDocumento",
                  "Hash", "AlgoritmoHash", "TipoDocumental", "Observaciones",
                  "SubidoPor", "Version", "Descripcion", "Copia", "ArchivoOrginal"
              ) VALUES (
                  v_NuevoArchivoCod, rec."Nombre", v_NuevaCarpetaCod, rec."Ruta",
                  rec."TipoArchivo", rec."Formato", rec."NumeroHojas", rec."Tamaño",
                  true, rec."Indice", rec."FechaDocumento", CURRENT_TIMESTAMP,
                  rec."OrigenDocumento", rec."Hash", rec."AlgoritmoHash",
                  rec."TipoDocumental", rec."Observaciones", p_Usuario,
                  1, rec."Descripcion", true, rec."Cod"
              );

              v_ArchivosCopiados := v_ArchivosCopiados + 1;
          END LOOP;
      END IF;

      FOR v_HijaCod IN
          SELECT "Cod" FROM "Carpetas" WHERE "CarpetaPadre" = p_CarpetaOrigen AND "Estado" = true
      LOOP
          DECLARE
              v_ResultadoHija RECORD;
          BEGIN
              SELECT * INTO v_ResultadoHija
              FROM "F_CopiarCarpeta"(v_HijaCod, v_NuevaCarpetaCod, p_Usuario, NULL, p_CopiarArchivos);    

              v_CarpetasCopiadas := v_CarpetasCopiadas + v_ResultadoHija."CarpetasCopiadas";
              v_ArchivosCopiados := v_ArchivosCopiados + v_ResultadoHija."ArchivosCopiados";
          END;
      END LOOP;

      INSERT INTO "Historial_Acciones" (
          "Tabla", "RegistroCod", "Accion", "Usuario", "Fecha", "Observaciones"
      ) VALUES (
          'Carpetas', v_NuevaCarpetaCod, 'COPIAR', p_Usuario, CURRENT_TIMESTAMP,
          'Copiada desde carpeta ' || p_CarpetaOrigen || '. Carpetas: ' || v_CarpetasCopiadas || ',       
  Archivos: ' || v_ArchivosCopiados
      );

      RETURN QUERY SELECT
          true,
          v_NuevaCarpetaCod,
          v_CarpetasCopiadas,
          v_ArchivosCopiados,
          ('Carpeta copiada exitosamente. ' || v_CarpetasCopiadas || ' carpetas, ' || v_ArchivosCopiados  
  || ' archivos')::VARCHAR(500);
  END;
  $$;


--
-- Name: FUNCTION "F_CopiarCarpeta"(p_carpetaorigen bigint, p_carpetadestino bigint, p_usuario bigint, p_nuevonombre character varying, p_copiararchivos boolean); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_CopiarCarpeta"(p_carpetaorigen bigint, p_carpetadestino bigint, p_usuario bigint, p_nuevonombre character varying, p_copiararchivos boolean) IS 'Copia una carpeta recursivamente incluyendo archivos';


--
-- Name: F_CrearArchivo(character varying, bigint, character varying, character varying, character varying, bigint, character varying, smallint, bigint, bigint, character varying, timestamp without time zone, character varying, character varying, text); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CrearArchivo"(p_nombre character varying, p_carpeta bigint, p_ruta character varying, p_hash character varying, "p_tamaño" character varying, p_usuario bigint, p_formato character varying DEFAULT NULL::character varying, p_numerohojas smallint DEFAULT NULL::smallint, p_tipoarchivo bigint DEFAULT NULL::bigint, p_tipodocumental bigint DEFAULT NULL::bigint, p_origendocumento character varying DEFAULT 'NATIVO'::character varying, p_fechadocumento timestamp without time zone DEFAULT NULL::timestamp without time zone, p_descripcion character varying DEFAULT NULL::character varying, p_codigodocumento character varying DEFAULT NULL::character varying, p_observaciones text DEFAULT NULL::text) RETURNS TABLE("ArchivoCod" bigint, "Version" integer, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_ArchivoCod BIGINT;
    v_Indice SMALLINT;
    v_CarpetaExiste BOOLEAN;
    v_TipoCarpeta BIGINT;
BEGIN
    -- Verificar que la carpeta existe y puede contener archivos
    SELECT true, "TipoCarpeta" INTO v_CarpetaExiste, v_TipoCarpeta
    FROM "Carpetas"
    WHERE "Cod" = p_Carpeta AND "Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            NULL::BIGINT,
            NULL::INTEGER,
            'Carpeta no encontrada o inactiva'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Solo expedientes (3) y genéricas (4) pueden contener archivos
    IF v_TipoCarpeta NOT IN (3, 4) THEN
        RETURN QUERY SELECT 
            NULL::BIGINT,
            NULL::INTEGER,
            'Solo se pueden agregar archivos a Expedientes o Carpetas Genéricas'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Obtener siguiente código de archivo
    SELECT COALESCE(MAX("Cod"), 0) + 1 INTO v_ArchivoCod
    FROM "Archivos";
    
    -- Obtener siguiente índice en la carpeta
    SELECT COALESCE(MAX("Indice"), 0) + 1 INTO v_Indice
    FROM "Archivos"
    WHERE "Carpeta" = p_Carpeta AND "Estado" = true;
    
    -- Insertar archivo
    INSERT INTO "Archivos" (
        "Cod", "Nombre", "Carpeta", "Ruta", "Hash", "Tamaño", "Formato",
        "NumeroHojas", "TipoArchivo", "TipoDocumental", "OrigenDocumento",
        "FechaDocumento", "Descripcion", "CodigoDocumento", "Observaciones",
        "Version", "SubidoPor", "FechaIncorporacion", "Estado", "Indice",
        "AlgoritmoHash", "Copia", "Firmado"
    ) VALUES (
        v_ArchivoCod, p_Nombre, p_Carpeta, p_Ruta, p_Hash, p_Tamaño, p_Formato,
        p_NumeroHojas, p_TipoArchivo, p_TipoDocumental, p_OrigenDocumento,
        COALESCE(p_FechaDocumento, CURRENT_TIMESTAMP), p_Descripcion, 
        p_CodigoDocumento, p_Observaciones,
        1, p_Usuario, CURRENT_TIMESTAMP, true, v_Indice,
        'SHA-256', false, false
    );
    
    -- Crear primera versión en historial
    INSERT INTO "Versiones_Archivos" (
        "Archivo", "Version", "Ruta", "Hash", "Tamaño", "FechaVersion",
        "CreadoPor", "Comentario", "EsVersionActual", "Nombre",
        "AlgoritmoHash", "Formato", "NumeroHojas", "MotivoModificacion", "Estado"
    ) VALUES (
        v_ArchivoCod, 1, p_Ruta, p_Hash, p_Tamaño, CURRENT_TIMESTAMP,
        p_Usuario, 'Versión inicial', true, p_Nombre,
        'SHA-256', p_Formato, p_NumeroHojas, 'Creación del documento', true
    );
    
    RETURN QUERY SELECT 
        v_ArchivoCod,
        1,
        'Archivo creado exitosamente'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_CrearArchivo"(p_nombre character varying, p_carpeta bigint, p_ruta character varying, p_hash character varying, "p_tamaño" character varying, p_usuario bigint, p_formato character varying, p_numerohojas smallint, p_tipoarchivo bigint, p_tipodocumental bigint, p_origendocumento character varying, p_fechadocumento timestamp without time zone, p_descripcion character varying, p_codigodocumento character varying, p_observaciones text); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_CrearArchivo"(p_nombre character varying, p_carpeta bigint, p_ruta character varying, p_hash character varying, "p_tamaño" character varying, p_usuario bigint, p_formato character varying, p_numerohojas smallint, p_tipoarchivo bigint, p_tipodocumental bigint, p_origendocumento character varying, p_fechadocumento timestamp without time zone, p_descripcion character varying, p_codigodocumento character varying, p_observaciones text) IS 'Crea un archivo nuevo con su primera versión en el historial';


--
-- Name: F_CrearCarpeta(character varying, character varying, bigint, bigint, bigint, bigint, bigint, bigint, bigint, character varying, character varying, character varying, text, character varying, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CrearCarpeta"(p_nombre character varying, p_descripcion character varying DEFAULT NULL::character varying, p_tipocarpeta bigint DEFAULT 4, p_carpetapadre bigint DEFAULT NULL::bigint, p_trd bigint DEFAULT NULL::bigint, p_oficina bigint DEFAULT NULL::bigint, p_entidad bigint DEFAULT NULL::bigint, p_usuario bigint DEFAULT NULL::bigint, p_nivelvisualizacion bigint DEFAULT 1, p_soporte character varying DEFAULT 'ELECTRONICO'::character varying, p_frecuenciaconsulta character varying DEFAULT 'MEDIA'::character varying, p_ubicacionfisica character varying DEFAULT NULL::character varying, p_observaciones text DEFAULT NULL::text, p_codigoexpediente character varying DEFAULT NULL::character varying, p_palabrasclave character varying DEFAULT NULL::character varying) RETURNS TABLE("Exito" boolean, "CarpetaCod" bigint, "SerieRaiz" bigint, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
  DECLARE
      v_CarpetaCod BIGINT;
      v_SerieRaiz BIGINT;
      v_TipoCarpetaPadre BIGINT;
      v_CodSerie BIGINT;
      v_CodSubSerie BIGINT;
      v_Validacion RECORD;
      v_EntidadFinal BIGINT;
      v_TRD BIGINT;
  BEGIN
      -- Normalizar TRD: si es 0, tratarlo como NULL
      v_TRD := NULLIF(p_TRD, 0);

      -- Validar nombre
      IF p_Nombre IS NULL OR TRIM(p_Nombre) = '' THEN
          RETURN QUERY SELECT false, NULL::BIGINT, NULL::BIGINT, 'El nombre de la carpeta es
  obligatorio'::VARCHAR(500);
          RETURN;
      END IF;

      -- Validar tipo de carpeta
      IF p_TipoCarpeta NOT IN (1, 2, 3, 4) THEN
          RETURN QUERY SELECT false, NULL::BIGINT, NULL::BIGINT, 'Tipo de carpeta no válido (1=Serie,      
  2=Subserie, 3=Expediente, 4=Genérica)'::VARCHAR(500);
          RETURN;
      END IF;

      -- Determinar entidad
      v_EntidadFinal := p_Entidad;

      -- Obtener tipo de carpeta padre si existe
      IF p_CarpetaPadre IS NOT NULL AND p_CarpetaPadre != 0 THEN
          SELECT "TipoCarpeta", "Entidad" INTO v_TipoCarpetaPadre, v_EntidadFinal
          FROM documentos."Carpetas"
          WHERE "Cod" = p_CarpetaPadre AND "Estado" = true;

          IF NOT FOUND THEN
              RETURN QUERY SELECT false, NULL::BIGINT, NULL::BIGINT, 'La carpeta padre no existe o está    
  inactiva'::VARCHAR(500);
              RETURN;
          END IF;

          IF p_Entidad IS NOT NULL THEN
              v_EntidadFinal := p_Entidad;
          END IF;
      END IF;

      -- Validar jerarquía
      SELECT * INTO v_Validacion FROM documentos."F_ValidarJerarquiaCarpeta"(v_TipoCarpetaPadre,
  p_TipoCarpeta);
      IF NOT v_Validacion."Valido" THEN
          RETURN QUERY SELECT false, NULL::BIGINT, NULL::BIGINT, v_Validacion."Mensaje";
          RETURN;
      END IF;

      -- Series y Subseries DEBEN tener TRD
      IF p_TipoCarpeta IN (1, 2) AND v_TRD IS NULL THEN
          RETURN QUERY SELECT false, NULL::BIGINT, NULL::BIGINT, 'Las Series y Subseries deben tener una   
  TRD asociada'::VARCHAR(500);
          RETURN;
      END IF;

      -- Validar que TRD existe si se proporciona (y no es genérica en raíz)
      IF v_TRD IS NOT NULL THEN
          IF NOT EXISTS (SELECT 1 FROM documentos."Tablas_Retencion_Documental" WHERE "Cod" = v_TRD AND    
  "Estado" = true) THEN
              RETURN QUERY SELECT false, NULL::BIGINT, NULL::BIGINT, 'La TRD especificada no
  existe'::VARCHAR(500);
              RETURN;
          END IF;
      END IF;

      -- Obtener siguiente código
      v_CarpetaCod := documentos."F_SiguienteCodCarpeta"();

      -- Calcular Serie Raíz
      IF p_CarpetaPadre IS NOT NULL AND p_CarpetaPadre != 0 THEN
          v_SerieRaiz := documentos."F_ObtenerSerieRaiz"(p_CarpetaPadre);
      ELSIF p_TipoCarpeta = 1 THEN
          v_SerieRaiz := v_CarpetaCod;
      ELSE
          v_SerieRaiz := NULL; -- Genéricas en raíz no tienen serie raíz
      END IF;

      -- Determinar CodSerie y CodSubSerie
      IF p_TipoCarpeta = 1 THEN
          v_CodSerie := v_CarpetaCod;
          v_CodSubSerie := NULL;
      ELSIF p_TipoCarpeta = 2 THEN
          v_CodSerie := v_SerieRaiz;
          v_CodSubSerie := v_CarpetaCod;
      ELSIF p_CarpetaPadre IS NOT NULL AND p_CarpetaPadre != 0 THEN
          SELECT "CodSerie", "CodSubSerie" INTO v_CodSerie, v_CodSubSerie
          FROM documentos."Carpetas"
          WHERE "Cod" = p_CarpetaPadre AND "Estado" = true;
      ELSE
          v_CodSerie := NULL;
          v_CodSubSerie := NULL;
      END IF;

      -- Insertar carpeta
      INSERT INTO documentos."Carpetas" (
          "Cod", "Nombre", "TipoCarpeta", "CarpetaPadre", "TRD",
          "CodSerie", "CodSubSerie", "SerieRaiz",
          "Descripcion", "NivelVisualizacion",
          "Soporte", "FrecuenciaConsulta", "UbicacionFisica",
          "CodigoExpediente", "PalabrasClave", "Observaciones",
          "CreadoPor", "FechaCreacion", "Estado", "EstadoCarpeta",
          "Entidad", "Oficina"
      ) VALUES (
          v_CarpetaCod, p_Nombre, p_TipoCarpeta, NULLIF(p_CarpetaPadre, 0), v_TRD,
          v_CodSerie, v_CodSubSerie, v_SerieRaiz,
          p_Descripcion, p_NivelVisualizacion,
          p_Soporte, p_FrecuenciaConsulta, p_UbicacionFisica,
          p_CodigoExpediente, p_PalabrasClave, p_Observaciones,
          p_Usuario, CURRENT_TIMESTAMP, true, 1,
          v_EntidadFinal, NULLIF(p_Oficina, 0)
      );

      RETURN QUERY SELECT true, v_CarpetaCod, v_SerieRaiz, 'Carpeta creada exitosamente'::VARCHAR(500);    
  END;
  $$;


--
-- Name: F_CrearNotificacion(bigint, bigint, character varying, character varying, text, bigint, character varying, bigint, bigint, bigint, bigint, boolean, timestamp without time zone, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CrearNotificacion"(p_entidad bigint, p_usuariodestino bigint, p_tiponotificacion character varying, p_titulo character varying, p_mensaje text DEFAULT NULL::text, p_usuarioorigen bigint DEFAULT NULL::bigint, p_prioridad character varying DEFAULT 'MEDIA'::character varying, p_radicadoref bigint DEFAULT NULL::bigint, p_archivoref bigint DEFAULT NULL::bigint, p_carpetaref bigint DEFAULT NULL::bigint, p_solicitudfirmaref bigint DEFAULT NULL::bigint, p_requiereaccion boolean DEFAULT NULL::boolean, p_fechavencimiento timestamp without time zone DEFAULT NULL::timestamp without time zone, p_urlaccion character varying DEFAULT NULL::character varying) RETURNS bigint
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_NotificacionCod BIGINT;
    v_RequiereAccion BOOLEAN;
BEGIN
    IF p_RequiereAccion IS NULL THEN
        SELECT "RequiereAccion" INTO v_RequiereAccion
        FROM documentos."Tipos_Notificacion" WHERE "Cod" = p_TipoNotificacion;
    ELSE
        v_RequiereAccion := p_RequiereAccion;
    END IF;
    
    INSERT INTO documentos."Notificaciones" (
        "Entidad", "UsuarioDestino", "UsuarioOrigen", "TipoNotificacion", 
        "Titulo", "Mensaje", "Prioridad", "RadicadoRef", "ArchivoRef", 
        "CarpetaRef", "SolicitudFirmaRef", "RequiereAccion", "FechaVencimientoAccion", "UrlAccion"
    ) VALUES (
        p_Entidad, p_UsuarioDestino, p_UsuarioOrigen, p_TipoNotificacion,
        p_Titulo, p_Mensaje, p_Prioridad, p_RadicadoRef, p_ArchivoRef,
        p_CarpetaRef, p_SolicitudFirmaRef, v_RequiereAccion, p_FechaVencimiento, p_UrlAccion
    ) RETURNING "Cod" INTO v_NotificacionCod;
    
    RETURN v_NotificacionCod;
END;
$$;


--
-- Name: F_CrearSolicitudFirma(bigint, bigint, character varying, bigint, character varying, text, timestamp without time zone, boolean, jsonb); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CrearSolicitudFirma"(p_entidad bigint, p_archivo bigint, p_tipofirma character varying, p_solicitadopor bigint, p_asunto character varying, p_mensaje text DEFAULT NULL::text, p_fechavencimiento timestamp without time zone DEFAULT NULL::timestamp without time zone, p_ordensecuencial boolean DEFAULT false, p_firmantes jsonb DEFAULT '[]'::jsonb) RETURNS TABLE("SolicitudCod" bigint, "CodigoVerificacion" character varying, "Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_SolicitudCod BIGINT;
    v_CodigoVerif VARCHAR(50);
    v_HashDocumento VARCHAR(64);
    v_NombreArchivo VARCHAR(200);
    v_Firmante JSONB;
    v_FirmanteUsuario BIGINT;
    v_FirmanteOrden SMALLINT;
    v_FirmanteRol VARCHAR(50);
    v_FirmanteObligatorio BOOLEAN;
    v_TotalFirmantes INTEGER;
BEGIN
    -- Validar archivo
    SELECT a."Hash", a."Nombre" INTO v_HashDocumento, v_NombreArchivo
    FROM documentos."Archivos" a WHERE a."Cod" = p_Archivo AND a."Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT NULL::BIGINT, NULL::VARCHAR(50), false, 'Archivo no encontrado o inactivo'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Validar solicitante existe
    IF NOT EXISTS (SELECT 1 FROM documentos."Usuarios" WHERE "Cod" = p_SolicitadoPor AND "Entidad" = p_Entidad AND "Estado" = true) THEN
        RETURN QUERY SELECT NULL::BIGINT, NULL::VARCHAR(50), false, 'Usuario solicitante no encontrado'::VARCHAR(500);
        RETURN;
    END IF;
    
    v_TotalFirmantes := jsonb_array_length(p_Firmantes);
    IF v_TotalFirmantes = 0 THEN
        RETURN QUERY SELECT NULL::BIGINT, NULL::VARCHAR(50), false, 'Debe especificar al menos un firmante'::VARCHAR(500);
        RETURN;
    END IF;
    
    IF p_TipoFirma NOT IN ('SIMPLE', 'AVANZADA') THEN
        RETURN QUERY SELECT NULL::BIGINT, NULL::VARCHAR(50), false, 'Tipo de firma inválido'::VARCHAR(500);
        RETURN;
    END IF;
    
    IF EXISTS (SELECT 1 FROM documentos."Solicitudes_Firma" WHERE "Archivo" = p_Archivo AND "Entidad" = p_Entidad AND "Estado" IN ('PENDIENTE', 'EN_PROCESO')) THEN
        RETURN QUERY SELECT NULL::BIGINT, NULL::VARCHAR(50), false, 'El archivo ya tiene una solicitud activa'::VARCHAR(500);
        RETURN;
    END IF;
    
    v_CodigoVerif := documentos."F_GenerarCodigoVerificacion"();
    
    INSERT INTO documentos."Solicitudes_Firma" (
        "Entidad", "Archivo", "TipoFirma", "OrdenSecuencial", "SolicitadoPor", 
        "Asunto", "Mensaje", "FechaVencimiento", "HashOriginal", "CodigoVerificacion", "Estado"
    ) VALUES (
        p_Entidad, p_Archivo, p_TipoFirma, p_OrdenSecuencial, p_SolicitadoPor,
        p_Asunto, p_Mensaje, p_FechaVencimiento, COALESCE(v_HashDocumento, ''), v_CodigoVerif, 'PENDIENTE'
    ) RETURNING "Cod" INTO v_SolicitudCod;
    
    FOR v_Firmante IN SELECT * FROM jsonb_array_elements(p_Firmantes)
    LOOP
        v_FirmanteUsuario := (v_Firmante->>'usuario')::BIGINT;
        v_FirmanteOrden := COALESCE((v_Firmante->>'orden')::SMALLINT, 1);
        v_FirmanteRol := v_Firmante->>'rol';
        v_FirmanteObligatorio := COALESCE((v_Firmante->>'esObligatorio')::BOOLEAN, true);
        
        IF NOT EXISTS (SELECT 1 FROM documentos."Usuarios" WHERE "Cod" = v_FirmanteUsuario AND "Entidad" = p_Entidad AND "Estado" = true) THEN
            RAISE EXCEPTION 'Usuario firmante % no encontrado en la entidad', v_FirmanteUsuario;
        END IF;
        
        INSERT INTO documentos."Firmantes_Solicitud" (
            "Solicitud", "Usuario", "Entidad", "Orden", "RolFirmante", "EsObligatorio", "Estado"
        ) VALUES (
            v_SolicitudCod, v_FirmanteUsuario, p_Entidad, v_FirmanteOrden, v_FirmanteRol, v_FirmanteObligatorio, 'PENDIENTE'
        );
    END LOOP;
    
    UPDATE documentos."Archivos"
    SET "Firmado" = false,
        "FimarPor" = (
            SELECT string_agg(u."Nombres" || ' ' || u."Apellidos", ', ' ORDER BY fs."Orden")
            FROM documentos."Firmantes_Solicitud" fs
            INNER JOIN documentos."Usuarios" u ON fs."Usuario" = u."Cod" AND fs."Entidad" = u."Entidad"
            WHERE fs."Solicitud" = v_SolicitudCod
        )
    WHERE "Cod" = p_Archivo;
    
    RETURN QUERY SELECT v_SolicitudCod, v_CodigoVerif, true, ('Solicitud creada: ' || v_NombreArchivo)::VARCHAR(500);
END;
$$;


--
-- Name: F_CrearTRD(character varying, character varying, character varying, bigint, bigint, integer, integer, character varying, character varying, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CrearTRD"(p_codigo character varying, p_nombre character varying, p_descripcion character varying DEFAULT NULL::character varying, p_trdpadre bigint DEFAULT NULL::bigint, p_entidad bigint DEFAULT NULL::bigint, p_tiempogestion integer DEFAULT NULL::integer, p_tiempocentral integer DEFAULT NULL::integer, p_disposicionfinal character varying DEFAULT NULL::character varying, p_procedimiento character varying DEFAULT NULL::character varying, p_usuario bigint DEFAULT NULL::bigint) RETURNS TABLE("Exito" boolean, "TRDCod" bigint, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
  DECLARE
      v_TRDCod BIGINT;
      v_Tipo VARCHAR(50);
      v_TipoPadre VARCHAR(50);
  BEGIN
      -- Validar código
      IF p_Codigo IS NULL OR TRIM(p_Codigo) = '' THEN
          RETURN QUERY SELECT false, NULL::BIGINT, 'El código es obligatorio'::VARCHAR(500);
          RETURN;
      END IF;

      -- Validar nombre
      IF p_Nombre IS NULL OR TRIM(p_Nombre) = '' THEN
          RETURN QUERY SELECT false, NULL::BIGINT, 'El nombre es obligatorio'::VARCHAR(500);
          RETURN;
      END IF;

      -- Validar entidad
      IF p_Entidad IS NULL THEN
          RETURN QUERY SELECT false, NULL::BIGINT, 'La entidad es obligatoria'::VARCHAR(500);
          RETURN;
      END IF;
    
      -- Determinar tipo basado en si tiene padre
      IF p_TRDPadre IS NULL THEN
          v_Tipo := 'Serie';
      ELSE
          -- Verificar que el padre existe y es una Serie
          SELECT "Tipo" INTO v_TipoPadre
          FROM documentos."Tablas_Retencion_Documental"
          WHERE "Cod" = p_TRDPadre AND "Estado" = true AND "Entidad" = p_Entidad;

          IF NOT FOUND THEN
              RETURN QUERY SELECT false, NULL::BIGINT, 'El TRD padre no existe o está
  inactivo'::VARCHAR(500);
              RETURN;
          END IF;

          IF v_TipoPadre != 'Serie' THEN
              RETURN QUERY SELECT false, NULL::BIGINT, 'Solo se pueden crear Subseries dentro de
  Series'::VARCHAR(500);
              RETURN;
          END IF;

          v_Tipo := 'Subserie';
      END IF;
     
      -- Verificar código único en la entidad
      IF EXISTS (
          SELECT 1 FROM documentos."Tablas_Retencion_Documental"
          WHERE "Codigo" = p_Codigo AND "Entidad" = p_Entidad AND "Estado" = true
      ) THEN
          RETURN QUERY SELECT false, NULL::BIGINT, ('Ya existe una TRD con el código ' ||
  p_Codigo)::VARCHAR(500);
          RETURN;
      END IF;

      -- Obtener siguiente código
      SELECT COALESCE(MAX("Cod"), 0) + 1 INTO v_TRDCod
      FROM documentos."Tablas_Retencion_Documental";

      -- Insertar TRD
      INSERT INTO documentos."Tablas_Retencion_Documental" (
          "Cod", "Codigo", "Nombre", "Descripcion", "Tipo", "TRDPadre", "Entidad",
          "TiempoGestion", "TiempoCentral", "DisposicionFinal", "Procedimiento",
          "Estado", "CreadoPor", "FechaCreacion"
      ) VALUES (
          v_TRDCod, p_Codigo, p_Nombre, p_Descripcion, v_Tipo, p_TRDPadre, p_Entidad,
          p_TiempoGestion, p_TiempoCentral, p_DisposicionFinal, p_Procedimiento,
          true, p_Usuario, NOW()
      );

      RETURN QUERY SELECT true, v_TRDCod, (v_Tipo || ' "' || p_Nombre || '" creada
  exitosamente')::VARCHAR(500);
  END;
  $$;


--
-- Name: FUNCTION "F_CrearTRD"(p_codigo character varying, p_nombre character varying, p_descripcion character varying, p_trdpadre bigint, p_entidad bigint, p_tiempogestion integer, p_tiempocentral integer, p_disposicionfinal character varying, p_procedimiento character varying, p_usuario bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_CrearTRD"(p_codigo character varying, p_nombre character varying, p_descripcion character varying, p_trdpadre bigint, p_entidad bigint, p_tiempogestion integer, p_tiempocentral integer, p_disposicionfinal character varying, p_procedimiento character varying, p_usuario bigint) IS 'Crea una Serie o Subserie en la TRD validando jerarquía';


--
-- Name: F_CrearTransferencia(bigint, bigint, character varying, bigint, text); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CrearTransferencia"(p_entidad bigint, p_oficinaorigen bigint, p_tipotransferencia character varying, p_solicitadopor bigint, p_observaciones text DEFAULT NULL::text) RETURNS TABLE("TransferenciaCod" bigint, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_TransferenciaCod BIGINT;
BEGIN
    -- Validar tipo
    IF p_TipoTransferencia NOT IN ('PRIMARIA', 'SECUNDARIA') THEN
        RETURN QUERY SELECT NULL::BIGINT, 'Tipo debe ser PRIMARIA o SECUNDARIA'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar oficina
    IF NOT EXISTS (SELECT 1 FROM "Oficinas" WHERE "Cod" = p_OficinaOrigen AND "Entidad" = p_Entidad AND "Estado" = true) THEN
        RETURN QUERY SELECT NULL::BIGINT, 'Oficina no encontrada'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Obtener código
    SELECT COALESCE(MAX("Cod"), 0) + 1 INTO v_TransferenciaCod FROM "Transferencias_Documentales";
    
    -- Crear transferencia
    INSERT INTO "Transferencias_Documentales" (
        "Cod", "Entidad", "OficinaOrigen", "TipoTransferencia",
        "FechaSolicitud", "SolicitadoPor", "Estado", "Observaciones"
    ) VALUES (
        v_TransferenciaCod, p_Entidad, p_OficinaOrigen, p_TipoTransferencia,
        CURRENT_TIMESTAMP, p_SolicitadoPor, 'PENDIENTE', p_Observaciones
    );
    
    RETURN QUERY SELECT v_TransferenciaCod, 'Transferencia creada. Agregue expedientes con F_AgregarExpedienteTransferencia'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_CrearTransferencia"(p_entidad bigint, p_oficinaorigen bigint, p_tipotransferencia character varying, p_solicitadopor bigint, p_observaciones text); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_CrearTransferencia"(p_entidad bigint, p_oficinaorigen bigint, p_tipotransferencia character varying, p_solicitadopor bigint, p_observaciones text) IS 'Crea una transferencia documental (primaria o secundaria)';


--
-- Name: F_CrearVersionArchivo(bigint, character varying, character varying, character varying, bigint, character varying, smallint, character varying, character varying, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_CrearVersionArchivo"(p_archivocod bigint, p_nuevaruta character varying, p_nuevohash character varying, "p_nuevotamaño" character varying, p_usuario bigint, p_nuevoformato character varying DEFAULT NULL::character varying, p_nuevonumerohojas smallint DEFAULT NULL::smallint, p_nuevonombre character varying DEFAULT NULL::character varying, p_comentario character varying DEFAULT NULL::character varying, p_motivomodificacion character varying DEFAULT 'Actualización de documento'::character varying) RETURNS TABLE("Exito" boolean, "VersionAnterior" integer, "VersionNueva" integer, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_VersionActual INTEGER;
    v_NuevaVersion INTEGER;
    v_ArchivoExiste BOOLEAN;
    v_NombreActual VARCHAR(200);
    v_FormatoActual VARCHAR(50);
    v_HojasActual SMALLINT;
    v_RutaActual VARCHAR(2000);
    v_HashActual VARCHAR(64);
    v_TamañoActual VARCHAR(50);
BEGIN
    -- Verificar que el archivo existe y está activo
    SELECT 
        true, "Version", "Nombre", "Formato", "NumeroHojas", "Ruta", "Hash", "Tamaño"
    INTO 
        v_ArchivoExiste, v_VersionActual, v_NombreActual, v_FormatoActual, 
        v_HojasActual, v_RutaActual, v_HashActual, v_TamañoActual
    FROM "Archivos"
    WHERE "Cod" = p_ArchivoCod AND "Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            false,
            NULL::INTEGER,
            NULL::INTEGER,
            'Archivo no encontrado o inactivo'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Calcular nueva versión
    v_NuevaVersion := COALESCE(v_VersionActual, 0) + 1;
    
    -- Marcar versión actual como NO actual en historial (si existe)
    UPDATE "Versiones_Archivos"
    SET "EsVersionActual" = false
    WHERE "Archivo" = p_ArchivoCod AND "EsVersionActual" = true;
    
    -- Guardar versión actual en historial antes de actualizar
    INSERT INTO "Versiones_Archivos" (
        "Archivo", "Version", "Ruta", "Hash", "Tamaño", "FechaVersion",
        "CreadoPor", "Comentario", "EsVersionActual", "Nombre", 
        "AlgoritmoHash", "Formato", "NumeroHojas", "MotivoModificacion", "Estado"
    ) VALUES (
        p_ArchivoCod, v_VersionActual, v_RutaActual, v_HashActual, v_TamañoActual,
        CURRENT_TIMESTAMP, p_Usuario, 'Versión anterior archivada', false, v_NombreActual,
        'SHA-256', v_FormatoActual, v_HojasActual, 'Reemplazado por nueva versión', true
    );
    
    -- Insertar nueva versión en historial como actual
    INSERT INTO "Versiones_Archivos" (
        "Archivo", "Version", "Ruta", "Hash", "Tamaño", "FechaVersion",
        "CreadoPor", "Comentario", "EsVersionActual", "Nombre", 
        "AlgoritmoHash", "Formato", "NumeroHojas", "MotivoModificacion", "Estado"
    ) VALUES (
        p_ArchivoCod, v_NuevaVersion, p_NuevaRuta, p_NuevoHash, p_NuevoTamaño,
        CURRENT_TIMESTAMP, p_Usuario, p_Comentario, true, 
        COALESCE(p_NuevoNombre, v_NombreActual),
        'SHA-256', COALESCE(p_NuevoFormato, v_FormatoActual), 
        COALESCE(p_NuevoNumeroHojas, v_HojasActual), p_MotivoModificacion, true
    );
    
    -- Actualizar tabla principal de Archivos con la nueva versión
    UPDATE "Archivos"
    SET 
        "Ruta" = p_NuevaRuta,
        "Hash" = p_NuevoHash,
        "Tamaño" = p_NuevoTamaño,
        "Version" = v_NuevaVersion,
        "Nombre" = COALESCE(p_NuevoNombre, "Nombre"),
        "Formato" = COALESCE(p_NuevoFormato, "Formato"),
        "NumeroHojas" = COALESCE(p_NuevoNumeroHojas, "NumeroHojas"),
        "FechaModificacion" = CURRENT_TIMESTAMP
    WHERE "Cod" = p_ArchivoCod AND "Estado" = true;
    
    RETURN QUERY SELECT 
        true,
        v_VersionActual,
        v_NuevaVersion,
        ('Versión ' || v_NuevaVersion || ' creada exitosamente')::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_CrearVersionArchivo"(p_archivocod bigint, p_nuevaruta character varying, p_nuevohash character varying, "p_nuevotamaño" character varying, p_usuario bigint, p_nuevoformato character varying, p_nuevonumerohojas smallint, p_nuevonombre character varying, p_comentario character varying, p_motivomodificacion character varying); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_CrearVersionArchivo"(p_archivocod bigint, p_nuevaruta character varying, p_nuevohash character varying, "p_nuevotamaño" character varying, p_usuario bigint, p_nuevoformato character varying, p_nuevonumerohojas smallint, p_nuevonombre character varying, p_comentario character varying, p_motivomodificacion character varying) IS 'Crea una nueva versión de archivo manteniendo historial completo';


--
-- Name: F_DiasHabilesEntre(date, date); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_DiasHabilesEntre"(p_fecha_inicio date, p_fecha_fin date) RETURNS integer
    LANGUAGE plpgsql STABLE
    AS $$
DECLARE
    v_dias_habiles INTEGER := 0;
    v_fecha_actual DATE := p_fecha_inicio;
    v_max_iteraciones INTEGER := 1000;  -- Límite de seguridad
    v_iteracion INTEGER := 0;
BEGIN
    -- Si fecha fin es menor o igual a inicio, retornar 0
    IF p_fecha_fin <= p_fecha_inicio THEN
        RETURN 0;
    END IF;
    
    WHILE v_fecha_actual < p_fecha_fin AND v_iteracion < v_max_iteraciones LOOP
        v_fecha_actual := v_fecha_actual + INTERVAL '1 day';
        v_iteracion := v_iteracion + 1;
        
        IF "F_EsDiaHabil"(v_fecha_actual) THEN
            v_dias_habiles := v_dias_habiles + 1;
        END IF;
    END LOOP;
    
    RETURN v_dias_habiles;
END;
$$;


--
-- Name: FUNCTION "F_DiasHabilesEntre"(p_fecha_inicio date, p_fecha_fin date); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_DiasHabilesEntre"(p_fecha_inicio date, p_fecha_fin date) IS 'Calcula los días hábiles entre dos fechas';


--
-- Name: F_EjecutarTransferencia(bigint, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_EjecutarTransferencia"(p_transferenciacod bigint, p_aprobadopor bigint) RETURNS TABLE("Exito" boolean, "ExpedientesTransferidos" integer, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_EstadoActual VARCHAR(20);
    v_TipoTransferencia VARCHAR(20);
    v_NumeroExpedientes INTEGER;
    v_Expediente BIGINT;
BEGIN
    -- Verificar transferencia
    SELECT "Estado", "TipoTransferencia", "NumeroExpedientes"
    INTO v_EstadoActual, v_TipoTransferencia, v_NumeroExpedientes
    FROM "Transferencias_Documentales"
    WHERE "Cod" = p_TransferenciaCod;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, 0, 'Transferencia no encontrada'::VARCHAR(500);
        RETURN;
    END IF;
    
    IF v_EstadoActual != 'PENDIENTE' THEN
        RETURN QUERY SELECT false, 0, ('La transferencia está en estado ' || v_EstadoActual)::VARCHAR(500);
        RETURN;
    END IF;
    
    IF COALESCE(v_NumeroExpedientes, 0) = 0 THEN
        RETURN QUERY SELECT false, 0, 'La transferencia no tiene expedientes'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Cambiar estado de expedientes según tipo de transferencia
    FOR v_Expediente IN 
        SELECT td."Carpeta" FROM "Transferencias_Detalle" td WHERE td."Transferencia" = p_TransferenciaCod
    LOOP
        UPDATE "Carpetas"
        SET 
            "EstadoCarpeta" = CASE 
                WHEN v_TipoTransferencia = 'PRIMARIA' THEN 3  -- En archivo central
                WHEN v_TipoTransferencia = 'SECUNDARIA' THEN 4  -- En archivo histórico
                ELSE "EstadoCarpeta"
            END,
            "FechaModificacion" = CURRENT_TIMESTAMP,
            "ModificadoPor" = p_AprobadoPor
        WHERE "Cod" = v_Expediente;
    END LOOP;
    
    -- Actualizar transferencia
    UPDATE "Transferencias_Documentales"
    SET 
        "Estado" = 'EJECUTADA',
        "FechaAprobacion" = CURRENT_TIMESTAMP,
        "FechaEjecucion" = CURRENT_TIMESTAMP,
        "AprobadoPor" = p_AprobadoPor
    WHERE "Cod" = p_TransferenciaCod;
    
    RETURN QUERY SELECT true, v_NumeroExpedientes, 
        ('Transferencia ejecutada. ' || v_NumeroExpedientes || ' expedientes transferidos')::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_EjecutarTransferencia"(p_transferenciacod bigint, p_aprobadopor bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_EjecutarTransferencia"(p_transferenciacod bigint, p_aprobadopor bigint) IS 'Ejecuta una transferencia documental moviendo los expedientes';


--
-- Name: F_EliminarArchivo(bigint, bigint, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_EliminarArchivo"(p_archivocod bigint, p_usuario bigint, p_motivoeliminacion character varying DEFAULT NULL::character varying) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Nombre VARCHAR(500);
    v_Ruta VARCHAR(2000);
    v_Carpeta BIGINT;
    v_CarpetaNombre VARCHAR(200);
    v_Formato VARCHAR(50);
    v_Hash VARCHAR(64);
    v_Tamaño VARCHAR(50);
BEGIN
    -- Obtener datos del archivo
    SELECT a."Nombre", a."Ruta", a."Carpeta", a."Formato", a."Hash", a."Tamaño", c."Nombre"
    INTO v_Nombre, v_Ruta, v_Carpeta, v_Formato, v_Hash, v_Tamaño, v_CarpetaNombre
    FROM "Archivos" a
    LEFT JOIN "Carpetas" c ON a."Carpeta" = c."Cod"
    WHERE a."Cod" = p_ArchivoCod AND a."Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, 'Archivo no encontrado o ya eliminado'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Registrar en auditoría ANTES de eliminar
    INSERT INTO "Archivos_Eliminados" (
        "Cod", "Nombre", "Ruta", "FechaEliminacion", "Carpeta",
        "Formato", "EliminadoPor", "MotivoEliminacion", "Hash", "Tamaño", "CarpetaCod"
    ) VALUES (
        p_ArchivoCod, v_Nombre, v_Ruta, CURRENT_TIMESTAMP, v_CarpetaNombre,
        v_Formato, p_Usuario, p_MotivoEliminacion, v_Hash, v_Tamaño, v_Carpeta
    );
    
    -- Soft delete del archivo
    UPDATE "Archivos"
    SET "Estado" = false, "FechaModificacion" = CURRENT_TIMESTAMP
    WHERE "Cod" = p_ArchivoCod;
    
    -- Soft delete de las versiones
    UPDATE "Versiones_Archivos"
    SET "Estado" = false
    WHERE "Archivo" = p_ArchivoCod;
    
    -- Registrar en historial de acciones
    INSERT INTO "Historial_Acciones" (
        "Tabla", "RegistroCod", "Accion", "Usuario", "Fecha", "Observaciones"
    ) VALUES (
        'Archivos', p_ArchivoCod, 'ELIMINAR', p_Usuario, CURRENT_TIMESTAMP,
        'Archivo eliminado: ' || v_Nombre || COALESCE('. Motivo: ' || p_MotivoEliminacion, '')
    );
    
    RETURN QUERY SELECT true, ('Archivo "' || v_Nombre || '" eliminado correctamente')::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_EliminarArchivo"(p_archivocod bigint, p_usuario bigint, p_motivoeliminacion character varying); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_EliminarArchivo"(p_archivocod bigint, p_usuario bigint, p_motivoeliminacion character varying) IS 'Elimina un archivo (soft delete) con registro de auditoría obligatorio';


--
-- Name: F_EliminarCarpeta(bigint, bigint, character varying, boolean); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_EliminarCarpeta"(p_carpetacod bigint, p_usuario bigint, p_motivoeliminacion character varying DEFAULT NULL::character varying, p_eliminarcontenido boolean DEFAULT true) RETURNS TABLE("Exito" boolean, "CarpetasEliminadas" integer, "ArchivosEliminados" integer, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Nombre VARCHAR(200);
    v_TipoCarpeta BIGINT;
    v_CarpetaPadre BIGINT;
    v_SerieRaiz BIGINT;
    v_NumeroFolios INTEGER;
    v_CarpetasEliminadas INTEGER := 0;
    v_ArchivosEliminados INTEGER := 0;
    v_HijaCod BIGINT;
    v_ArchivoCod BIGINT;
    v_TieneHijos BOOLEAN;
BEGIN
    -- Obtener datos de la carpeta
    SELECT "Nombre", "TipoCarpeta", "CarpetaPadre", "SerieRaiz", "NumeroFolios"
    INTO v_Nombre, v_TipoCarpeta, v_CarpetaPadre, v_SerieRaiz, v_NumeroFolios
    FROM "Carpetas"
    WHERE "Cod" = p_CarpetaCod AND "Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, 0, 0, 'Carpeta no encontrada o ya eliminada'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar si tiene contenido
    SELECT EXISTS (
        SELECT 1 FROM "Carpetas" WHERE "CarpetaPadre" = p_CarpetaCod AND "Estado" = true
        UNION ALL
        SELECT 1 FROM "Archivos" WHERE "Carpeta" = p_CarpetaCod AND "Estado" = true
    ) INTO v_TieneHijos;
    
    IF v_TieneHijos AND NOT p_EliminarContenido THEN
        RETURN QUERY SELECT false, 0, 0, 'La carpeta tiene contenido. Use p_EliminarContenido=true para eliminar todo'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Eliminar archivos directos de esta carpeta
    FOR v_ArchivoCod IN 
        SELECT "Cod" FROM "Archivos" WHERE "Carpeta" = p_CarpetaCod AND "Estado" = true
    LOOP
        PERFORM "F_EliminarArchivo"(v_ArchivoCod, p_Usuario, 'Eliminado con carpeta: ' || v_Nombre);
        v_ArchivosEliminados := v_ArchivosEliminados + 1;
    END LOOP;
    
    -- Eliminar carpetas hijas recursivamente
    FOR v_HijaCod IN 
        SELECT "Cod" FROM "Carpetas" WHERE "CarpetaPadre" = p_CarpetaCod AND "Estado" = true
    LOOP
        DECLARE
            v_ResultadoHija RECORD;
        BEGIN
            SELECT * INTO v_ResultadoHija 
            FROM "F_EliminarCarpeta"(v_HijaCod, p_Usuario, 'Eliminado con carpeta padre: ' || v_Nombre, true);
            
            v_CarpetasEliminadas := v_CarpetasEliminadas + v_ResultadoHija."CarpetasEliminadas";
            v_ArchivosEliminados := v_ArchivosEliminados + v_ResultadoHija."ArchivosEliminados";
        END;
    END LOOP;
    
    -- Contar archivos que tenía esta carpeta para auditoría
    SELECT COUNT(*) INTO v_NumeroFolios
    FROM "Archivos_Eliminados"
    WHERE "CarpetaCod" = p_CarpetaCod;
    
    -- Registrar en auditoría
    INSERT INTO "Carpetas_Eliminadas" (
        "Cod", "Nombre", "TipoCarpeta", "CarpetaPadre", "SerieRaiz",
        "FechaCreacion", "FechaEliminacion", "EliminadoPor", "MotivoEliminacion",
        "NumeroFolios", "NumeroArchivos"
    )
    SELECT 
        "Cod", "Nombre", "TipoCarpeta", "CarpetaPadre", "SerieRaiz",
        "FechaCreacion", CURRENT_TIMESTAMP, p_Usuario, p_MotivoEliminacion,
        "NumeroFolios", v_NumeroFolios
    FROM "Carpetas"
    WHERE "Cod" = p_CarpetaCod;
    
    -- Soft delete de la carpeta
    UPDATE "Carpetas"
    SET "Estado" = false, "FechaModificacion" = CURRENT_TIMESTAMP, "ModificadoPor" = p_Usuario
    WHERE "Cod" = p_CarpetaCod;
    
    v_CarpetasEliminadas := v_CarpetasEliminadas + 1;
    
    -- Registrar en historial
    INSERT INTO "Historial_Acciones" (
        "Tabla", "RegistroCod", "Accion", "Usuario", "Fecha", "Observaciones"
    ) VALUES (
        'Carpetas', p_CarpetaCod, 'ELIMINAR', p_Usuario, CURRENT_TIMESTAMP,
        'Carpeta eliminada: ' || v_Nombre || '. Archivos: ' || v_ArchivosEliminados || 
        '. Subcarpetas: ' || (v_CarpetasEliminadas - 1)
    );
    
    RETURN QUERY SELECT 
        true, 
        v_CarpetasEliminadas, 
        v_ArchivosEliminados,
        ('Carpeta "' || v_Nombre || '" eliminada. Total: ' || v_CarpetasEliminadas || 
         ' carpetas, ' || v_ArchivosEliminados || ' archivos')::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_EliminarCarpeta"(p_carpetacod bigint, p_usuario bigint, p_motivoeliminacion character varying, p_eliminarcontenido boolean); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_EliminarCarpeta"(p_carpetacod bigint, p_usuario bigint, p_motivoeliminacion character varying, p_eliminarcontenido boolean) IS 'Elimina una carpeta recursivamente (soft delete) con auditoría completa';


--
-- Name: F_EsDiaHabil(date); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_EsDiaHabil"(p_fecha date) RETURNS boolean
    LANGUAGE plpgsql STABLE
    AS $$
DECLARE
    v_es_habil BOOLEAN := true;
    v_dia_semana INTEGER;
BEGIN
    -- Obtener día de la semana (0=Domingo, 6=Sábado en PostgreSQL con EXTRACT)
    -- ISODOW: 1=Lunes, 7=Domingo
    v_dia_semana := EXTRACT(ISODOW FROM p_fecha);
    
    -- Verificar fin de semana (6=Sábado, 7=Domingo)
    IF v_dia_semana IN (6, 7) THEN
        v_es_habil := false;
    END IF;
    
    -- Verificar si es festivo
    IF v_es_habil THEN
        IF EXISTS (SELECT 1 FROM "Dias_Festivos" WHERE "Fecha" = p_fecha) THEN
            v_es_habil := false;
        END IF;
    END IF;
    
    RETURN v_es_habil;
END;
$$;


--
-- Name: FUNCTION "F_EsDiaHabil"(p_fecha date); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_EsDiaHabil"(p_fecha date) IS 'Verifica si una fecha es día hábil (no es fin de semana ni festivo)';


--
-- Name: F_GenerarCodigoVerificacion(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_GenerarCodigoVerificacion"() RETURNS character varying
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Codigo VARCHAR(50);
    v_Existe BOOLEAN;
BEGIN
    LOOP
        v_Codigo := 'NUBLU-' || EXTRACT(YEAR FROM CURRENT_DATE)::TEXT || '-' ||
                    UPPER(SUBSTRING(MD5(RANDOM()::TEXT || CLOCK_TIMESTAMP()::TEXT) FROM 1 FOR 6));
        SELECT EXISTS(SELECT 1 FROM documentos."Solicitudes_Firma" WHERE "CodigoVerificacion" = v_Codigo) INTO v_Existe;
        EXIT WHEN NOT v_Existe;
    END LOOP;
    RETURN v_Codigo;
END;
$$;


--
-- Name: F_GenerarIndiceElectronico(bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_GenerarIndiceElectronico"(p_expediente_id bigint) RETURNS void
    LANGUAGE plpgsql
    AS $$
  DECLARE
      v_indice JSON;
      v_tipo_carpeta BIGINT;
  BEGIN
      SELECT "TipoCarpeta" INTO v_tipo_carpeta
      FROM documentos."Carpetas"
      WHERE "Cod" = p_expediente_id AND "Estado" = true;

      IF v_tipo_carpeta IS NULL OR v_tipo_carpeta != 3 THEN
          RETURN;
      END IF;

      WITH RECURSIVE
      subcarpetas AS (
          SELECT "Cod", "Nombre", "CarpetaPadre", ''::TEXT AS ruta_relativa
          FROM documentos."Carpetas"
          WHERE "Cod" = p_expediente_id AND "Estado" = true

          UNION ALL

          SELECT c."Cod", c."Nombre", c."CarpetaPadre",
                 CASE
                     WHEN s.ruta_relativa = '' THEN c."Nombre"
                     ELSE s.ruta_relativa || '/' || c."Nombre"
                 END
          FROM documentos."Carpetas" c
          INNER JOIN subcarpetas s ON c."CarpetaPadre" = s."Cod"
          WHERE c."Estado" = true AND c."TipoCarpeta" = 4
      ),
      archivos_ordenados AS (
          SELECT
              a."Cod",
              a."Nombre",
              a."TipoDocumental",
              td."Nombre" AS "NombreTipoDocumental",
              a."FechaDocumento",
              a."FechaSubida",
              a."PaginaInicio",
              a."PaginaFin",
              a."Formato",
              a."Tamaño",
              a."Hash",
              a."ContentType",
              a."Descripcion",
              a."CodigoDocumento",
              COALESCE(s.ruta_relativa, '') AS "RutaRelativa",
              ROW_NUMBER() OVER (ORDER BY a."FechaSubida", a."Cod") AS orden
          FROM documentos."Archivos" a
          INNER JOIN subcarpetas s ON a."Carpeta" = s."Cod"
          LEFT JOIN documentos."Tipos_Documentales" td ON a."TipoDocumental" = td."Cod"
          WHERE a."Estado" = true
            AND COALESCE(a."EstadoUpload", 'CONFIRMADO') = 'CONFIRMADO'
      )
      SELECT json_build_object(
          'expediente', (
              SELECT json_build_object(
                  'cod', c."Cod",
                  'nombre', c."Nombre",
                  'codigoExpediente', c."CodigoExpediente",
                  'fechaCreacion', c."FechaCreacion",
                  'fechaCierre', c."FechaCierre",
                  'estadoCarpeta', ec."Nombre",
                  'serie', (
                      SELECT t."Nombre"
                      FROM documentos."Tablas_Retencion_Documental" t
                      WHERE t."Cod" = c."TRD"
                  ),
                  'codigoSerie', (
                      SELECT t."Codigo"
                      FROM documentos."Tablas_Retencion_Documental" t
                      WHERE t."Cod" = c."TRD"
                  )
              )
              FROM documentos."Carpetas" c
              LEFT JOIN documentos."Estados_Carpetas" ec ON c."EstadoCarpeta" = ec."Cod"
              WHERE c."Cod" = p_expediente_id
          ),
          'documentos', COALESCE((
              SELECT json_agg(
                  json_build_object(
                      'orden', orden,
                      'cod', "Cod",
                      'nombre', "Nombre",
                      'tipoDocumental', "NombreTipoDocumental",
                      'fechaDocumento', "FechaDocumento",
                      'fechaIncorporacion', "FechaSubida",
                      'paginaInicio', "PaginaInicio",
                      'paginaFin', "PaginaFin",
                      'formato', "Formato",
                      'tamano', "Tamaño",
                      'hash', "Hash",
                      'rutaRelativa', "RutaRelativa",
                      'descripcion', "Descripcion",
                      'codigoDocumento', "CodigoDocumento"
                  ) ORDER BY orden
              )
              FROM archivos_ordenados
          ), '[]'::json),
          'totalDocumentos', (SELECT COUNT(*) FROM archivos_ordenados),
          'generadoEn', NOW(),
          'version', '1.0',
          'norma', 'Acuerdo 002/2014 AGN'
      ) INTO v_indice;

      UPDATE documentos."Carpetas"
      SET "IndiceElectronico" = v_indice::TEXT,
          "FechaModificacion" = NOW()
      WHERE "Cod" = p_expediente_id;

  END;
  $$;


--
-- Name: F_GenerarNumeroRadicado(bigint, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_GenerarNumeroRadicado"(p_entidad bigint, p_tipo_comunicacion character varying) RETURNS character varying
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_anio INTEGER;
    v_consecutivo INTEGER;
    v_prefijo VARCHAR(5);
    v_numero_radicado VARCHAR(30);
BEGIN
    v_anio := EXTRACT(YEAR FROM CURRENT_DATE);
    
    -- Obtener prefijo del tipo de comunicación
    SELECT "Prefijo" INTO v_prefijo
    FROM "Tipos_Comunicacion"
    WHERE "Cod" = p_tipo_comunicacion;
    
    IF v_prefijo IS NULL THEN
        v_prefijo := p_tipo_comunicacion;
    END IF;
    
    -- Obtener y actualizar consecutivo (con bloqueo para evitar duplicados)
    INSERT INTO "Consecutivos_Radicado" ("Entidad", "TipoComunicacion", "Anio", "UltimoConsecutivo", "FechaActualizacion")
    VALUES (p_entidad, p_tipo_comunicacion, v_anio, 1, CURRENT_TIMESTAMP)
    ON CONFLICT ("Entidad", "TipoComunicacion", "Anio")
    DO UPDATE SET 
        "UltimoConsecutivo" = "Consecutivos_Radicado"."UltimoConsecutivo" + 1,
        "FechaActualizacion" = CURRENT_TIMESTAMP
    RETURNING "UltimoConsecutivo" INTO v_consecutivo;
    
    -- Formato: E-2025-000001
    v_numero_radicado := v_prefijo || '-' || v_anio::TEXT || '-' || LPAD(v_consecutivo::TEXT, 6, '0');
    
    RETURN v_numero_radicado;
END;
$$;


--
-- Name: FUNCTION "F_GenerarNumeroRadicado"(p_entidad bigint, p_tipo_comunicacion character varying); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_GenerarNumeroRadicado"(p_entidad bigint, p_tipo_comunicacion character varying) IS 'Genera el número de radicado con formato {Prefijo}-{Año}-{Consecutivo}';


--
-- Name: F_MarcarNotificacionLeida(bigint, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_MarcarNotificacionLeida"(p_notificacioncod bigint, p_usuarioid bigint) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_RowCount INTEGER;
BEGIN
    UPDATE documentos."Notificaciones"
    SET "FechaLeido" = CURRENT_TIMESTAMP, "Estado" = 'LEIDA'
    WHERE "Cod" = p_NotificacionCod AND "UsuarioDestino" = p_UsuarioId AND "FechaLeido" IS NULL;
    
    GET DIAGNOSTICS v_RowCount = ROW_COUNT;
    
    RETURN v_RowCount > 0;
END;
$$;


--
-- Name: F_MarcarTodasNotificacionesLeidas(bigint, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_MarcarTodasNotificacionesLeidas"(p_usuarioid bigint, p_entidadid bigint) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Cantidad INTEGER;
BEGIN
    UPDATE documentos."Notificaciones"
    SET "FechaLeido" = CURRENT_TIMESTAMP, "Estado" = 'LEIDA'
    WHERE "UsuarioDestino" = p_UsuarioId AND "Entidad" = p_EntidadId AND "FechaLeido" IS NULL;
    
    GET DIAGNOSTICS v_Cantidad = ROW_COUNT;
    
    RETURN v_Cantidad;
END;
$$;


--
-- Name: F_MoverCarpeta(bigint, bigint, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_MoverCarpeta"(p_carpetacod bigint, p_nuevodestino bigint, p_usuario bigint) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Nombre VARCHAR(200);
    v_TipoCarpeta BIGINT;
    v_CarpetaPadreAnterior BIGINT;
    v_TipoCarpetaDestino BIGINT;
    v_NuevaSerieRaiz BIGINT;
    v_Validacion RECORD;
    v_EsDescendiente BOOLEAN;
BEGIN
    -- Obtener datos de la carpeta a mover
    SELECT "Nombre", "TipoCarpeta", "CarpetaPadre"
    INTO v_Nombre, v_TipoCarpeta, v_CarpetaPadreAnterior
    FROM "Carpetas"
    WHERE "Cod" = p_CarpetaCod AND "Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, 'Carpeta no encontrada'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- No mover al mismo lugar
    IF v_CarpetaPadreAnterior IS NOT DISTINCT FROM p_NuevoDestino THEN
        RETURN QUERY SELECT false, 'La carpeta ya está en ese destino'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar destino
    IF p_NuevoDestino IS NOT NULL THEN
        SELECT "TipoCarpeta" INTO v_TipoCarpetaDestino
        FROM "Carpetas"
        WHERE "Cod" = p_NuevoDestino AND "Estado" = true;
        
        IF NOT FOUND THEN
            RETURN QUERY SELECT false, 'Carpeta destino no encontrada'::VARCHAR(500);
            RETURN;
        END IF;
        
        -- No mover a sí misma
        IF p_CarpetaCod = p_NuevoDestino THEN
            RETURN QUERY SELECT false, 'No se puede mover una carpeta a sí misma'::VARCHAR(500);
            RETURN;
        END IF;
        
        -- Verificar que el destino no sea descendiente de la carpeta a mover
        WITH RECURSIVE Descendientes AS (
            SELECT "Cod" FROM "Carpetas" WHERE "Cod" = p_CarpetaCod AND "Estado" = true
            UNION ALL
            SELECT c."Cod" FROM "Carpetas" c
            INNER JOIN Descendientes d ON c."CarpetaPadre" = d."Cod"
            WHERE c."Estado" = true
        )
        SELECT EXISTS (SELECT 1 FROM Descendientes WHERE "Cod" = p_NuevoDestino) INTO v_EsDescendiente;
        
        IF v_EsDescendiente THEN
            RETURN QUERY SELECT false, 'No se puede mover una carpeta a uno de sus descendientes'::VARCHAR(500);
            RETURN;
        END IF;
        
        -- Validar jerarquía
        SELECT * INTO v_Validacion FROM "F_ValidarJerarquiaCarpeta"(v_TipoCarpetaDestino, v_TipoCarpeta);
        IF NOT v_Validacion."Valido" THEN
            RETURN QUERY SELECT false, v_Validacion."Mensaje";
            RETURN;
        END IF;
        
        v_NuevaSerieRaiz := "F_ObtenerSerieRaiz"(p_NuevoDestino);
    ELSE
        -- Mover a raíz: solo Series pueden estar en raíz
        IF v_TipoCarpeta != 1 THEN
            RETURN QUERY SELECT false, 'Solo las Series pueden estar en la raíz'::VARCHAR(500);
            RETURN;
        END IF;
        v_NuevaSerieRaiz := p_CarpetaCod;
    END IF;
    
    -- Actualizar la carpeta
    UPDATE "Carpetas"
    SET 
        "CarpetaPadre" = p_NuevoDestino,
        "SerieRaiz" = v_NuevaSerieRaiz,
        "FechaModificacion" = CURRENT_TIMESTAMP,
        "ModificadoPor" = p_Usuario
    WHERE "Cod" = p_CarpetaCod;
    
    -- Actualizar SerieRaiz de todos los descendientes
    WITH RECURSIVE Descendientes AS (
        SELECT "Cod" FROM "Carpetas" WHERE "CarpetaPadre" = p_CarpetaCod AND "Estado" = true
        UNION ALL
        SELECT c."Cod" FROM "Carpetas" c
        INNER JOIN Descendientes d ON c."CarpetaPadre" = d."Cod"
        WHERE c."Estado" = true
    )
    UPDATE "Carpetas"
    SET "SerieRaiz" = v_NuevaSerieRaiz
    WHERE "Cod" IN (SELECT "Cod" FROM Descendientes);
    
    -- Registrar en historial
    INSERT INTO "Historial_Acciones" (
        "Tabla", "RegistroCod", "Accion", "Usuario", "Fecha", 
        "DetalleAnterior", "DetalleNuevo", "Observaciones"
    ) VALUES (
        'Carpetas', p_CarpetaCod, 'MOVER', p_Usuario, CURRENT_TIMESTAMP,
        'CarpetaPadre: ' || COALESCE(v_CarpetaPadreAnterior::TEXT, 'NULL'),
        'CarpetaPadre: ' || COALESCE(p_NuevoDestino::TEXT, 'NULL'),
        'Carpeta movida: ' || v_Nombre
    );
    
    RETURN QUERY SELECT true, ('Carpeta "' || v_Nombre || '" movida exitosamente')::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_MoverCarpeta"(p_carpetacod bigint, p_nuevodestino bigint, p_usuario bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_MoverCarpeta"(p_carpetacod bigint, p_nuevodestino bigint, p_usuario bigint) IS 'Mueve una carpeta a otro destino validando jerarquía';


--
-- Name: F_NotificarCambioCarpeta(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_NotificarCambioCarpeta"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
  DECLARE
      v_carpeta_id BIGINT;
      v_expediente_id BIGINT;
      v_tipo_carpeta BIGINT;
  BEGIN
      IF TG_OP = 'DELETE' THEN
          v_carpeta_id := OLD."Cod";
          v_tipo_carpeta := OLD."TipoCarpeta";
      ELSE
          v_carpeta_id := NEW."Cod";
          v_tipo_carpeta := NEW."TipoCarpeta";
      END IF;

      IF v_tipo_carpeta = 4 THEN
          v_expediente_id := documentos."F_ObtenerExpedienteContenedor"(v_carpeta_id);

          IF v_expediente_id IS NOT NULL THEN
              PERFORM pg_notify(
                  'indice_electronico_cambios',
                  json_build_object(
                      'carpeta_id', v_expediente_id,
                      'operacion', TG_OP,
                      'tabla', 'Carpetas',
                      'timestamp', NOW()
                  )::text
              );
          END IF;
      END IF;

      RETURN COALESCE(NEW, OLD);
  END;
  $$;


--
-- Name: F_NotificarCambioIndice(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_NotificarCambioIndice"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
  DECLARE
      v_carpeta_id BIGINT;
      v_expediente_id BIGINT;
  BEGIN
      IF TG_OP = 'DELETE' THEN
          v_carpeta_id := OLD."Carpeta";
      ELSE
          v_carpeta_id := NEW."Carpeta";
      END IF;
      
      v_expediente_id := documentos."F_ObtenerExpedienteContenedor"(v_carpeta_id);

      IF v_expediente_id IS NOT NULL THEN
          PERFORM pg_notify(
              'indice_electronico_cambios',
              json_build_object(
                  'carpeta_id', v_expediente_id,
                  'operacion', TG_OP,
                  'tabla', 'Archivos',
                  'timestamp', NOW()
              )::text
          );
      END IF;

      RETURN COALESCE(NEW, OLD);
  END;
  $$;


--
-- Name: F_NotificarFirmante(bigint, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_NotificarFirmante"(p_firmantecod bigint, p_emaildestino character varying DEFAULT NULL::character varying) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_UsuarioDestino BIGINT;
    v_Entidad BIGINT;
    v_SolicitudCod BIGINT;
    v_ArchivoNombre VARCHAR(200);
    v_Asunto VARCHAR(200);
    v_SolicitanteNombre VARCHAR(400);
BEGIN
    SELECT fs."Usuario", fs."Entidad", fs."Solicitud", a."Nombre", s."Asunto", u."Nombres" || ' ' || u."Apellidos"
    INTO v_UsuarioDestino, v_Entidad, v_SolicitudCod, v_ArchivoNombre, v_Asunto, v_SolicitanteNombre
    FROM documentos."Firmantes_Solicitud" fs
    INNER JOIN documentos."Solicitudes_Firma" s ON fs."Solicitud" = s."Cod"
    INNER JOIN documentos."Archivos" a ON s."Archivo" = a."Cod"
    INNER JOIN documentos."Usuarios" u ON s."SolicitadoPor" = u."Cod" AND s."Entidad" = u."Entidad"
    WHERE fs."Cod" = p_FirmanteCod;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, 'Firmante no encontrado'::VARCHAR(500);
        RETURN;
    END IF;
    
    UPDATE documentos."Firmantes_Solicitud"
    SET "Estado" = 'NOTIFICADO', "FechaNotificacion" = CURRENT_TIMESTAMP, "EmailNotificacion" = p_EmailDestino
    WHERE "Cod" = p_FirmanteCod;
    
    INSERT INTO documentos."Notificaciones" (
        "Entidad", "UsuarioDestino", "UsuarioOrigen", "TipoNotificacion", 
        "Titulo", "Mensaje", "Prioridad", "SolicitudFirmaRef", "RequiereAccion", "UrlAccion"
    ) 
    SELECT v_Entidad, v_UsuarioDestino, s."SolicitadoPor", 'SOLICITUD_FIRMA',
           'Documento pendiente de firma: ' || v_ArchivoNombre,
           'Asunto: ' || v_Asunto || '. Solicitado por: ' || v_SolicitanteNombre,
           'ALTA', v_SolicitudCod, true, '/firma/solicitud/' || v_SolicitudCod::TEXT
    FROM documentos."Solicitudes_Firma" s WHERE s."Cod" = v_SolicitudCod;
    
    RETURN QUERY SELECT true, 'Firmante notificado'::VARCHAR(500);
END;
$$;


--
-- Name: F_NotifyNavIndex(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_NotifyNavIndex"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$                                                                                                 DECLARE
      v_payload JSON;                                                                                     
      v_tipo_cambio VARCHAR(20);
      v_es_estructura BOOLEAN;
      v_padre_tipo BIGINT;
  BEGIN
      IF TG_OP = 'INSERT' THEN
          v_tipo_cambio := 'creado';
      ELSIF TG_OP = 'UPDATE' THEN
          v_tipo_cambio := 'modificado';
      ELSIF TG_OP = 'DELETE' THEN
          v_tipo_cambio := 'eliminado';
      END IF;

      IF TG_OP = 'DELETE' THEN
          v_es_estructura := OLD."TipoCarpeta" IN (1, 2)
                            OR (OLD."TipoCarpeta" = 4 AND OLD."CarpetaPadre" IS NULL);

          IF NOT v_es_estructura AND OLD."CarpetaPadre" IS NOT NULL THEN
              SELECT "TipoCarpeta" INTO v_padre_tipo
              FROM documentos."Carpetas"
              WHERE "Cod" = OLD."CarpetaPadre" AND "Estado" = true;
              v_es_estructura := v_padre_tipo IN (1, 2);
          END IF;

          v_payload := json_build_object(
              'tipoCambio', v_tipo_cambio,
              'carpetaId', OLD."Cod",
              'tipoCarpeta', OLD."TipoCarpeta",
              'carpetaPadre', OLD."CarpetaPadre",
              'serieRaiz', OLD."SerieRaiz",
              'entidadId', OLD."Entidad",
              'esEstructura', v_es_estructura,
              'timestamp', EXTRACT(EPOCH FROM NOW())::BIGINT
          );
      ELSE
          v_es_estructura := NEW."TipoCarpeta" IN (1, 2)
                            OR (NEW."TipoCarpeta" = 4 AND NEW."CarpetaPadre" IS NULL);

          IF NOT v_es_estructura AND NEW."CarpetaPadre" IS NOT NULL THEN
              SELECT "TipoCarpeta" INTO v_padre_tipo
              FROM documentos."Carpetas"
              WHERE "Cod" = NEW."CarpetaPadre" AND "Estado" = true;
              v_es_estructura := v_padre_tipo IN (1, 2);
          END IF;

          v_payload := json_build_object(
              'tipoCambio', v_tipo_cambio,
              'carpetaId', NEW."Cod",
              'tipoCarpeta', NEW."TipoCarpeta",
              'carpetaPadre', NEW."CarpetaPadre",
              'serieRaiz', NEW."SerieRaiz",
              'nombre', NEW."Nombre",
              'entidadId', NEW."Entidad",
              'esEstructura', v_es_estructura,
              'timestamp', EXTRACT(EPOCH FROM NOW())::BIGINT
          );
      END IF;

      PERFORM pg_notify('navindex_cambios', v_payload::TEXT);

      IF TG_OP = 'DELETE' THEN
          RETURN OLD;
      ELSE
          RETURN NEW;
      END IF;
  END;
  $$;


--
-- Name: FUNCTION "F_NotifyNavIndex"(); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_NotifyNavIndex"() IS 'Emite NOTIFY cuando cambian carpetas que afectan la estructura documental (Series, Subseries, y sus hijos directos)';


--
-- Name: F_NotifyNavIndex_Archivos(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_NotifyNavIndex_Archivos"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$                                                                                       
  DECLARE
      v_payload JSON;
      v_tipo_cambio VARCHAR(20);
      v_carpeta_cod BIGINT;
      v_serie_raiz BIGINT;
      v_entidad BIGINT;
  BEGIN
      IF TG_OP = 'INSERT' THEN
          v_tipo_cambio := 'archivo_creado';
          v_carpeta_cod := NEW."Carpeta";
      ELSIF TG_OP = 'UPDATE' THEN
          v_tipo_cambio := 'archivo_modificado';
          v_carpeta_cod := NEW."Carpeta";
      ELSIF TG_OP = 'DELETE' THEN
          v_tipo_cambio := 'archivo_eliminado';
          v_carpeta_cod := OLD."Carpeta";
      END IF;

      SELECT "SerieRaiz", "Entidad"
      INTO v_serie_raiz, v_entidad
      FROM documentos."Carpetas"
      WHERE "Cod" = v_carpeta_cod;

      IF v_serie_raiz IS NULL THEN
          v_serie_raiz := v_carpeta_cod;
      END IF;

      IF v_entidad IS NOT NULL THEN
          v_payload := json_build_object(
              'TipoCambio', v_tipo_cambio,
              'CarpetaId', COALESCE(v_serie_raiz, v_carpeta_cod),
              'TipoCarpeta', 1,
              'CarpetaPadre', NULL,
              'EntidadId', v_entidad,
              'ArchivoId', CASE WHEN TG_OP = 'DELETE' THEN OLD."Cod" ELSE NEW."Cod" END       
          );

          PERFORM pg_notify('navindex_cambios', v_payload::TEXT);
      END IF;

      IF TG_OP = 'DELETE' THEN
          RETURN OLD;
      ELSE
          RETURN NEW;
      END IF;
  END;
  $$;


--
-- Name: F_ObtenerAuditoriaExpediente(bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_ObtenerAuditoriaExpediente"(p_expediente_id bigint) RETURNS TABLE("Fecha" timestamp without time zone, "Accion" character varying, "Tabla" character varying, "RegistroCod" bigint, "Detalle" text, "Usuario" bigint, "NombreUsuario" text, "IP" character varying)
    LANGUAGE plpgsql
    AS $$
  BEGIN
      RETURN QUERY
      WITH RECURSIVE
      subcarpetas AS (
          SELECT c."Cod"
          FROM documentos."Carpetas" c
          WHERE c."Cod" = p_expediente_id
        
          UNION ALL

          SELECT c."Cod"
          FROM documentos."Carpetas" c
          INNER JOIN subcarpetas s ON c."CarpetaPadre" = s."Cod"
      ),
      archivos_expediente AS (
          SELECT a."Cod"
          FROM documentos."Archivos" a
          WHERE a."Carpeta" IN (SELECT "Cod" FROM subcarpetas)
      )
      SELECT
          h."Fecha",
          h."Accion",
          h."Tabla",
          h."RegistroCod",
          COALESCE(h."Observaciones",
              CASE
                  WHEN h."Tabla" = 'Carpetas' THEN (SELECT c."Nombre" FROM documentos."Carpetas" c WHERE  
  c."Cod" = h."RegistroCod")
                  WHEN h."Tabla" = 'Archivos' THEN (SELECT a."Nombre" FROM documentos."Archivos" a WHERE  
  a."Cod" = h."RegistroCod")
                  ELSE NULL
              END
          )::TEXT AS "Detalle",
          h."Usuario",
          COALESCE(u."Nombres" || ' ' || u."Apellidos", 'Sistema')::TEXT AS "NombreUsuario",
          h."IP"
      FROM documentos."Historial_Acciones" h
      LEFT JOIN documentos."Usuarios" u ON h."Usuario" = u."Cod"
      WHERE
          (h."Tabla" = 'Carpetas' AND h."RegistroCod" IN (SELECT "Cod" FROM subcarpetas))
          OR
          (h."Tabla" = 'Archivos' AND h."RegistroCod" IN (SELECT "Cod" FROM archivos_expediente))
      ORDER BY h."Fecha" DESC;
  END;
  $$;


--
-- Name: F_ObtenerConsecutivoRadicado(bigint, character varying, integer); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_ObtenerConsecutivoRadicado"(p_entidad bigint, p_tipocomunicacion character varying, p_anio integer DEFAULT NULL::integer) RETURNS integer
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Anio INTEGER;
    v_Consecutivo INTEGER;
BEGIN
    -- Usar año actual si no se especifica
    v_Anio := COALESCE(p_Anio, EXTRACT(YEAR FROM CURRENT_DATE)::INTEGER);
    
    -- INSERT o UPDATE atómico con UPSERT
    INSERT INTO "Consecutivos_Radicado" ("Entidad", "TipoComunicacion", "Anio", "UltimoConsecutivo", "FechaActualizacion")
    VALUES (p_Entidad, p_TipoComunicacion, v_Anio, 1, CURRENT_TIMESTAMP)
    ON CONFLICT ("Entidad", "TipoComunicacion", "Anio")
    DO UPDATE SET 
        "UltimoConsecutivo" = "Consecutivos_Radicado"."UltimoConsecutivo" + 1,
        "FechaActualizacion" = CURRENT_TIMESTAMP
    RETURNING "UltimoConsecutivo" INTO v_Consecutivo;
    
    RETURN v_Consecutivo;
END;
$$;


--
-- Name: FUNCTION "F_ObtenerConsecutivoRadicado"(p_entidad bigint, p_tipocomunicacion character varying, p_anio integer); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_ObtenerConsecutivoRadicado"(p_entidad bigint, p_tipocomunicacion character varying, p_anio integer) IS 'Obtiene el siguiente consecutivo de radicado de forma atómica (thread-safe)';


--
-- Name: F_ObtenerExpedienteContenedor(bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_ObtenerExpedienteContenedor"(p_carpeta_id bigint) RETURNS bigint
    LANGUAGE plpgsql
    AS $$
  DECLARE
      v_expediente_id BIGINT;
  BEGIN
      WITH RECURSIVE jerarquia AS (
          SELECT "Cod", "CarpetaPadre", "TipoCarpeta"
          FROM documentos."Carpetas"
          WHERE "Cod" = p_carpeta_id AND "Estado" = true

          UNION ALL

          SELECT c."Cod", c."CarpetaPadre", c."TipoCarpeta"
          FROM documentos."Carpetas" c
          INNER JOIN jerarquia j ON c."Cod" = j."CarpetaPadre"
          WHERE c."Estado" = true
      )
      SELECT "Cod" INTO v_expediente_id
      FROM jerarquia
      WHERE "TipoCarpeta" = 3
      LIMIT 1;

      RETURN v_expediente_id;
  END;
  $$;


--
-- Name: F_ObtenerHistorialFirma(bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_ObtenerHistorialFirma"(p_archivoid bigint) RETURNS TABLE("SolicitudCod" bigint, "TipoFirma" character varying, "Estado" character varying, "FechaSolicitud" timestamp without time zone, "FechaCompletada" timestamp without time zone, "CodigoVerificacion" character varying, "Firmantes" jsonb)
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN QUERY
    SELECT s."Cod", s."TipoFirma", s."Estado", s."FechaSolicitud", s."FechaCompletada", s."CodigoVerificacion",
        (SELECT jsonb_agg(jsonb_build_object('nombre', u."Nombres" || ' ' || u."Apellidos", 'rol', fs."RolFirmante", 'estado', fs."Estado", 'fechaFirma', fs."FechaFirma", 'tipoFirma', fs."TipoFirmaUsada") ORDER BY fs."Orden")
         FROM documentos."Firmantes_Solicitud" fs INNER JOIN documentos."Usuarios" u ON fs."Usuario" = u."Cod" AND fs."Entidad" = u."Entidad" WHERE fs."Solicitud" = s."Cod")
    FROM documentos."Solicitudes_Firma" s WHERE s."Archivo" = p_ArchivoId ORDER BY s."FechaSolicitud" DESC;
END;
$$;


--
-- Name: F_ObtenerSerieRaiz(bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_ObtenerSerieRaiz"(p_carpetacod bigint) RETURNS bigint
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_SerieRaiz BIGINT;
    v_CarpetaActual BIGINT;
    v_CarpetaPadre BIGINT;
    v_TipoCarpeta BIGINT;
    v_Iteraciones INTEGER := 0;
BEGIN
    v_CarpetaActual := p_CarpetaCod;
    
    LOOP
        SELECT "CarpetaPadre", "TipoCarpeta", "SerieRaiz"
        INTO v_CarpetaPadre, v_TipoCarpeta, v_SerieRaiz
        FROM "Carpetas"
        WHERE "Cod" = v_CarpetaActual AND "Estado" = true;
        
        IF NOT FOUND THEN
            RETURN NULL;
        END IF;
        
        -- Si ya tiene SerieRaiz, retornarla
        IF v_SerieRaiz IS NOT NULL THEN
            RETURN v_SerieRaiz;
        END IF;
        
        -- Si es Serie (tipo 1) y no tiene padre, es la raíz
        IF v_TipoCarpeta = 1 AND v_CarpetaPadre IS NULL THEN
            RETURN v_CarpetaActual;
        END IF;
        
        -- Subir al padre
        IF v_CarpetaPadre IS NULL THEN
            RETURN v_CarpetaActual; -- Es la raíz
        END IF;
        
        v_CarpetaActual := v_CarpetaPadre;
        v_Iteraciones := v_Iteraciones + 1;
        
        -- Protección contra loops infinitos
        IF v_Iteraciones > 100 THEN
            RAISE EXCEPTION 'Loop detectado en jerarquía de carpetas';
        END IF;
    END LOOP;
END;
$$;


--
-- Name: F_ObtenerTRDsOficina(bigint, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_ObtenerTRDsOficina"(p_oficina_cod bigint, p_entidad bigint) RETURNS TABLE("TRDCod" bigint, "TRDNombre" character varying, "TRDCodigo" bigint, "TRDPadre" bigint, "Tipo" character varying, "TiempoGestion" bigint, "TiempoCentral" bigint, "DisposicionFinal" character varying, "PuedeEditar" boolean, "PuedeEliminar" boolean)
    LANGUAGE plpgsql STABLE
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        t."Cod" AS "TRDCod",
        t."Nombre" AS "TRDNombre",
        t."Codigo" AS "TRDCodigo",
        t."TRDPadre",
        t."Tipo",
        t."TiempoGestion",
        t."TiempoCentral",
        t."DisposicionFinal",
        ot."PuedeEditar",
        ot."PuedeEliminar"
    FROM "Oficinas_TRD" ot
    INNER JOIN "Tablas_Retencion_Documental" t 
        ON ot."TRD" = t."Cod"
        AND t."Estado" = true
    WHERE ot."Oficina" = p_oficina_cod
    AND ot."Entidad" = p_entidad
    AND ot."Estado" = true;
END;
$$;


--
-- Name: FUNCTION "F_ObtenerTRDsOficina"(p_oficina_cod bigint, p_entidad bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_ObtenerTRDsOficina"(p_oficina_cod bigint, p_entidad bigint) IS 'Obtiene las Series/Subseries (TRD) asignadas a una oficina con sus permisos';


--
-- Name: F_OficinaAccesoCarpeta(bigint, bigint, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_OficinaAccesoCarpeta"(p_oficina_cod bigint, p_entidad bigint, p_carpeta_cod bigint) RETURNS TABLE("TieneAcceso" integer, "PuedeEditar" boolean, "PuedeEliminar" boolean, "CarpetaTRDCod" bigint, "TRDCod" bigint, "TRDNombre" character varying, "TRDCodigo" bigint)
    LANGUAGE plpgsql STABLE
    AS $$
BEGIN
    RETURN QUERY
    WITH RECURSIVE "CarpetaJerarquia" AS (
        -- Carpeta inicial
        SELECT 
            c."Cod",
            c."CarpetaPadre",
            c."TRD",
            c."SerieRaiz",
            c."TipoCarpeta",
            1 AS "Nivel"
        FROM "Carpetas" c
        WHERE c."Cod" = p_carpeta_cod AND c."Estado" = true
        
        UNION ALL
        
        -- Subir en la jerarquía
        SELECT 
            p."Cod",
            p."CarpetaPadre",
            p."TRD",
            p."SerieRaiz",
            p."TipoCarpeta",
            h."Nivel" + 1
        FROM "Carpetas" p
        INNER JOIN "CarpetaJerarquia" h ON p."Cod" = h."CarpetaPadre"
        WHERE p."Estado" = true AND h."Nivel" < 50
    )
    SELECT 
        1 AS "TieneAcceso",
        ot."PuedeEditar",
        ot."PuedeEliminar",
        cj."Cod" AS "CarpetaTRDCod",
        cj."TRD" AS "TRDCod",
        t."Nombre" AS "TRDNombre",
        t."Codigo" AS "TRDCodigo"
    FROM "CarpetaJerarquia" cj
    INNER JOIN "Tablas_Retencion_Documental" t
        ON cj."TRD" = t."Cod"
        AND t."Estado" = true
    INNER JOIN "Oficinas_TRD" ot 
        ON cj."TRD" = ot."TRD" 
        AND ot."Oficina" = p_oficina_cod 
        AND ot."Entidad" = p_entidad
        AND ot."Estado" = true
    WHERE cj."TRD" IS NOT NULL
    ORDER BY cj."Nivel" ASC
    LIMIT 1;
END;
$$;


--
-- Name: FUNCTION "F_OficinaAccesoCarpeta"(p_oficina_cod bigint, p_entidad bigint, p_carpeta_cod bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_OficinaAccesoCarpeta"(p_oficina_cod bigint, p_entidad bigint, p_carpeta_cod bigint) IS 'Verifica si una oficina tiene acceso a una carpeta y retorna los permisos';


--
-- Name: F_PrevenirModificacionEvidencias(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_PrevenirModificacionEvidencias"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    RAISE EXCEPTION 'Tabla Evidencias_Firma es APPEND-ONLY. No se permiten UPDATE ni DELETE.';
    RETURN NULL;
END;
$$;


--
-- Name: F_RadicadosPorVencer(bigint, integer); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_RadicadosPorVencer"(p_entidad bigint, p_dias_alerta integer DEFAULT 5) RETURNS TABLE("Cod" bigint, "NumeroRadicado" character varying, "Asunto" character varying, "FechaVencimiento" timestamp without time zone, "DiasParaVencer" integer, "Estado" character varying, "OficinaDestino" bigint, "UsuarioDestino" bigint, "Prioridad" character varying)
    LANGUAGE plpgsql STABLE
    AS $$
BEGIN
    RETURN QUERY
    SELECT 
        r."Cod",
        r."NumeroRadicado",
        r."Asunto",
        r."FechaVencimiento",
        EXTRACT(DAY FROM (r."FechaVencimiento" - CURRENT_TIMESTAMP))::INTEGER AS "DiasParaVencer",
        r."Estado",
        r."OficinaDestino",
        r."UsuarioDestino",
        r."Prioridad"
    FROM "Radicados" r
    WHERE r."Entidad" = p_entidad
    AND r."Activo" = true
    AND r."FechaVencimiento" IS NOT NULL
    AND r."Estado" NOT IN ('ARCHIVADO', 'ANULADO', 'RESPONDIDO')
    AND r."FechaVencimiento" <= CURRENT_TIMESTAMP + (p_dias_alerta || ' days')::INTERVAL
    ORDER BY r."FechaVencimiento" ASC;
END;
$$;


--
-- Name: FUNCTION "F_RadicadosPorVencer"(p_entidad bigint, p_dias_alerta integer); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_RadicadosPorVencer"(p_entidad bigint, p_dias_alerta integer) IS 'Obtiene los radicados próximos a vencer en los próximos N días';


--
-- Name: F_RadicarEntrada(bigint, character varying, bigint, bigint, character varying, character varying, character varying, character varying, bigint, bigint, character varying, character varying, boolean, character varying, bigint, text, integer, character varying, character varying, character varying, date); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_RadicarEntrada"(p_entidad bigint, p_asunto character varying, p_usuarioradica bigint, p_tercero bigint DEFAULT NULL::bigint, p_nombreremitente character varying DEFAULT NULL::character varying, p_correoremitente character varying DEFAULT NULL::character varying, p_telefonoremitente character varying DEFAULT NULL::character varying, p_direccionremitente character varying DEFAULT NULL::character varying, p_oficinadestino bigint DEFAULT NULL::bigint, p_usuariodestino bigint DEFAULT NULL::bigint, p_tiposolicitud character varying DEFAULT NULL::character varying, p_prioridad character varying DEFAULT 'NORMAL'::character varying, p_confidencial boolean DEFAULT false, p_mediorecepcion character varying DEFAULT 'PRESENCIAL'::character varying, p_oficinaradica bigint DEFAULT NULL::bigint, p_descripcion text DEFAULT NULL::text, p_folios integer DEFAULT NULL::integer, p_anexos character varying DEFAULT NULL::character varying, p_palabrasclave character varying DEFAULT NULL::character varying, p_numeroreferencia character varying DEFAULT NULL::character varying, p_fechadocumento date DEFAULT NULL::date) RETURNS TABLE("RadicadoCod" bigint, "NumeroRadicado" character varying, "FechaVencimiento" timestamp without time zone, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Consecutivo INTEGER;
    v_NumeroRadicado VARCHAR(30);
    v_RadicadoCod BIGINT;
    v_FechaVencimiento TIMESTAMP;
    v_Anio INTEGER;
    v_DiasRespuesta INTEGER;
    v_EsCalendario BOOLEAN;
BEGIN
    -- Obtener año actual
    v_Anio := EXTRACT(YEAR FROM CURRENT_DATE)::INTEGER;
    
    -- Obtener consecutivo atómico
    v_Consecutivo := "F_ObtenerConsecutivoRadicado"(p_Entidad, 'E', v_Anio);
    
    -- Generar número de radicado: E-2025-000001
    v_NumeroRadicado := 'E-' || v_Anio::VARCHAR || '-' || LPAD(v_Consecutivo::VARCHAR, 6, '0');
    
    -- Obtener nuevo código de radicado
    SELECT COALESCE(MAX("Cod"), 0) + 1 INTO v_RadicadoCod
    FROM "Radicados"
    WHERE "Entidad" = p_Entidad;
    
    -- Calcular fecha de vencimiento si hay tipo de solicitud
    IF p_TipoSolicitud IS NOT NULL THEN
        SELECT "DiasHabilesRespuesta", "EsCalendario"
        INTO v_DiasRespuesta, v_EsCalendario
        FROM "Tipos_Solicitud"
        WHERE "Cod" = p_TipoSolicitud AND "Estado" = true;
        
        IF v_DiasRespuesta IS NOT NULL THEN
            IF v_EsCalendario THEN
                -- Días calendario
                v_FechaVencimiento := CURRENT_TIMESTAMP + (v_DiasRespuesta || ' days')::INTERVAL;
            ELSE
                -- Días hábiles
                v_FechaVencimiento := "F_AgregarDiasHabiles"(CURRENT_DATE, v_DiasRespuesta)::TIMESTAMP;
            END IF;
        END IF;
    END IF;
    
    -- Insertar radicado
    INSERT INTO "Radicados" (
        "Cod", "NumeroRadicado", "AnioRadicado", "ConsecutivoAnio",
        "TipoComunicacion", "TipoSolicitud", "Prioridad", "Confidencial",
        "FechaRadicacion", "UsuarioRadica", "OficinaRadica", "MedioRecepcion",
        "Tercero", "NombreRemitente", "CorreoRemitente", "TelefonoRemitente", "DireccionRemitente",
        "OficinaDestino", "UsuarioDestino",
        "Asunto", "Descripcion", "Folios", "Anexos", "PalabrasClave",
        "FechaVencimiento", "Estado", "NumeroReferencia", "FechaDocumento",
        "Entidad", "FechaCreacion", "Activo"
    ) VALUES (
        v_RadicadoCod, v_NumeroRadicado, v_Anio, v_Consecutivo,
        'E', p_TipoSolicitud, p_Prioridad, p_Confidencial,
        CURRENT_TIMESTAMP, p_UsuarioRadica, p_OficinaRadica, p_MedioRecepcion,
        p_Tercero, p_NombreRemitente, p_CorreoRemitente, p_TelefonoRemitente, p_DireccionRemitente,
        p_OficinaDestino, p_UsuarioDestino,
        p_Asunto, p_Descripcion, p_Folios, p_Anexos, p_PalabrasClave,
        v_FechaVencimiento, 'RADICADO', p_NumeroReferencia, p_FechaDocumento,
        p_Entidad, CURRENT_TIMESTAMP, true
    );
    
    -- Registrar en trazabilidad
    INSERT INTO "Radicados_Trazabilidad" (
        "Radicado", "Entidad", "Accion", "Descripcion",
        "OficinaDestino", "UsuarioDestino", "EstadoNuevo", "Fecha"
    ) VALUES (
        v_RadicadoCod, p_Entidad, 'RADICADO', 'Radicación de entrada',
        p_OficinaDestino, p_UsuarioDestino, 'RADICADO', CURRENT_TIMESTAMP
    );
    
    -- Si hay destino, registrar asignación automática
    IF p_OficinaDestino IS NOT NULL OR p_UsuarioDestino IS NOT NULL THEN
        -- Actualizar estado a ASIGNADO
        UPDATE "Radicados"
        SET "Estado" = 'ASIGNADO', "FechaCambioEstado" = CURRENT_TIMESTAMP
        WHERE "Cod" = v_RadicadoCod AND "Entidad" = p_Entidad;
        
        -- Registrar asignación en trazabilidad
        INSERT INTO "Radicados_Trazabilidad" (
            "Radicado", "Entidad", "Accion", "Descripcion",
            "OficinaDestino", "UsuarioDestino", "EstadoAnterior", "EstadoNuevo", "Fecha"
        ) VALUES (
            v_RadicadoCod, p_Entidad, 'ASIGNADO', 'Asignación automática al radicar',
            p_OficinaDestino, p_UsuarioDestino, 'RADICADO', 'ASIGNADO', CURRENT_TIMESTAMP
        );
    END IF;
    
    -- Retornar resultado
    RETURN QUERY SELECT 
        v_RadicadoCod,
        v_NumeroRadicado,
        v_FechaVencimiento,
        'Radicado creado exitosamente'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_RadicarEntrada"(p_entidad bigint, p_asunto character varying, p_usuarioradica bigint, p_tercero bigint, p_nombreremitente character varying, p_correoremitente character varying, p_telefonoremitente character varying, p_direccionremitente character varying, p_oficinadestino bigint, p_usuariodestino bigint, p_tiposolicitud character varying, p_prioridad character varying, p_confidencial boolean, p_mediorecepcion character varying, p_oficinaradica bigint, p_descripcion text, p_folios integer, p_anexos character varying, p_palabrasclave character varying, p_numeroreferencia character varying, p_fechadocumento date); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_RadicarEntrada"(p_entidad bigint, p_asunto character varying, p_usuarioradica bigint, p_tercero bigint, p_nombreremitente character varying, p_correoremitente character varying, p_telefonoremitente character varying, p_direccionremitente character varying, p_oficinadestino bigint, p_usuariodestino bigint, p_tiposolicitud character varying, p_prioridad character varying, p_confidencial boolean, p_mediorecepcion character varying, p_oficinaradica bigint, p_descripcion text, p_folios integer, p_anexos character varying, p_palabrasclave character varying, p_numeroreferencia character varying, p_fechadocumento date) IS 'Radica una comunicación de entrada con cálculo automático de vencimiento según Ley 1755/2015';


--
-- Name: F_RadicarInterna(bigint, character varying, bigint, bigint, bigint, bigint, bigint, character varying, boolean, bigint, text, integer, character varying, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_RadicarInterna"(p_entidad bigint, p_asunto character varying, p_usuarioradica bigint, p_oficinaremitente bigint, p_oficinadestino bigint, p_usuarioremitente bigint DEFAULT NULL::bigint, p_usuariodestino bigint DEFAULT NULL::bigint, p_prioridad character varying DEFAULT 'NORMAL'::character varying, p_confidencial boolean DEFAULT false, p_oficinaradica bigint DEFAULT NULL::bigint, p_descripcion text DEFAULT NULL::text, p_folios integer DEFAULT NULL::integer, p_anexos character varying DEFAULT NULL::character varying, p_palabrasclave character varying DEFAULT NULL::character varying) RETURNS TABLE("RadicadoCod" bigint, "NumeroRadicado" character varying, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Consecutivo INTEGER;
    v_NumeroRadicado VARCHAR(30);
    v_RadicadoCod BIGINT;
    v_Anio INTEGER;
BEGIN
    -- Obtener año actual
    v_Anio := EXTRACT(YEAR FROM CURRENT_DATE)::INTEGER;
    
    -- Obtener consecutivo atómico
    v_Consecutivo := "F_ObtenerConsecutivoRadicado"(p_Entidad, 'I', v_Anio);
    
    -- Generar número de radicado: I-2025-000001
    v_NumeroRadicado := 'I-' || v_Anio::VARCHAR || '-' || LPAD(v_Consecutivo::VARCHAR, 6, '0');
    
    -- Obtener nuevo código de radicado
    SELECT COALESCE(MAX("Cod"), 0) + 1 INTO v_RadicadoCod
    FROM "Radicados"
    WHERE "Entidad" = p_Entidad;
    
    -- Insertar radicado interno
    INSERT INTO "Radicados" (
        "Cod", "NumeroRadicado", "AnioRadicado", "ConsecutivoAnio",
        "TipoComunicacion", "Prioridad", "Confidencial",
        "FechaRadicacion", "UsuarioRadica", "OficinaRadica",
        "OficinaRemitente", "UsuarioRemitente",
        "OficinaDestino", "UsuarioDestino",
        "Asunto", "Descripcion", "Folios", "Anexos", "PalabrasClave",
        "Estado", "Entidad", "FechaCreacion", "Activo"
    ) VALUES (
        v_RadicadoCod, v_NumeroRadicado, v_Anio, v_Consecutivo,
        'I', p_Prioridad, p_Confidencial,
        CURRENT_TIMESTAMP, p_UsuarioRadica, COALESCE(p_OficinaRadica, p_OficinaRemitente),
        p_OficinaRemitente, COALESCE(p_UsuarioRemitente, p_UsuarioRadica),
        p_OficinaDestino, p_UsuarioDestino,
        p_Asunto, p_Descripcion, p_Folios, p_Anexos, p_PalabrasClave,
        'ASIGNADO', p_Entidad, CURRENT_TIMESTAMP, true
    );
    
    -- Registrar en trazabilidad
    INSERT INTO "Radicados_Trazabilidad" (
        "Radicado", "Entidad", "Accion", "Descripcion",
        "OficinaOrigen", "UsuarioOrigen", "OficinaDestino", "UsuarioDestino",
        "EstadoNuevo", "Fecha"
    ) VALUES (
        v_RadicadoCod, p_Entidad, 'RADICADO', 'Radicación interna',
        p_OficinaRemitente, p_UsuarioRemitente, p_OficinaDestino, p_UsuarioDestino,
        'ASIGNADO', CURRENT_TIMESTAMP
    );
    
    -- Retornar resultado
    RETURN QUERY SELECT 
        v_RadicadoCod,
        v_NumeroRadicado,
        'Radicado interno creado exitosamente'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_RadicarInterna"(p_entidad bigint, p_asunto character varying, p_usuarioradica bigint, p_oficinaremitente bigint, p_oficinadestino bigint, p_usuarioremitente bigint, p_usuariodestino bigint, p_prioridad character varying, p_confidencial boolean, p_oficinaradica bigint, p_descripcion text, p_folios integer, p_anexos character varying, p_palabrasclave character varying); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_RadicarInterna"(p_entidad bigint, p_asunto character varying, p_usuarioradica bigint, p_oficinaremitente bigint, p_oficinadestino bigint, p_usuarioremitente bigint, p_usuariodestino bigint, p_prioridad character varying, p_confidencial boolean, p_oficinaradica bigint, p_descripcion text, p_folios integer, p_anexos character varying, p_palabrasclave character varying) IS 'Radica una comunicación interna entre dependencias';


--
-- Name: F_RadicarSalida(bigint, character varying, bigint, bigint, character varying, bigint, bigint, bigint, character varying, character varying, boolean, character varying, bigint, text, integer, character varying, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_RadicarSalida"(p_entidad bigint, p_asunto character varying, p_usuarioradica bigint, p_tercerodestino bigint DEFAULT NULL::bigint, p_nombredestinatario character varying DEFAULT NULL::character varying, p_oficinaremitente bigint DEFAULT NULL::bigint, p_usuarioremitente bigint DEFAULT NULL::bigint, p_radicadopadre bigint DEFAULT NULL::bigint, p_tiporelacion character varying DEFAULT 'RESPUESTA'::character varying, p_prioridad character varying DEFAULT 'NORMAL'::character varying, p_confidencial boolean DEFAULT false, p_mediorecepcion character varying DEFAULT 'PRESENCIAL'::character varying, p_oficinaradica bigint DEFAULT NULL::bigint, p_descripcion text DEFAULT NULL::text, p_folios integer DEFAULT NULL::integer, p_anexos character varying DEFAULT NULL::character varying, p_palabrasclave character varying DEFAULT NULL::character varying) RETURNS TABLE("RadicadoCod" bigint, "NumeroRadicado" character varying, "RadicadoPadreActualizado" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Consecutivo INTEGER;
    v_NumeroRadicado VARCHAR(30);
    v_RadicadoCod BIGINT;
    v_Anio INTEGER;
    v_RadicadoPadreActualizado BOOLEAN := false;
BEGIN
    -- Obtener año actual
    v_Anio := EXTRACT(YEAR FROM CURRENT_DATE)::INTEGER;
    
    -- Obtener consecutivo atómico
    v_Consecutivo := "F_ObtenerConsecutivoRadicado"(p_Entidad, 'S', v_Anio);
    
    -- Generar número de radicado: S-2025-000001
    v_NumeroRadicado := 'S-' || v_Anio::VARCHAR || '-' || LPAD(v_Consecutivo::VARCHAR, 6, '0');
    
    -- Obtener nuevo código de radicado
    SELECT COALESCE(MAX("Cod"), 0) + 1 INTO v_RadicadoCod
    FROM "Radicados"
    WHERE "Entidad" = p_Entidad;
    
    -- Insertar radicado de salida
    INSERT INTO "Radicados" (
        "Cod", "NumeroRadicado", "AnioRadicado", "ConsecutivoAnio",
        "TipoComunicacion", "Prioridad", "Confidencial",
        "FechaRadicacion", "UsuarioRadica", "OficinaRadica", "MedioRecepcion",
        "TerceroDestino", "NombreDestinatario",
        "OficinaRemitente", "UsuarioRemitente",
        "RadicadoPadre", "TipoRelacion",
        "Asunto", "Descripcion", "Folios", "Anexos", "PalabrasClave",
        "Estado", "Entidad", "FechaCreacion", "Activo"
    ) VALUES (
        v_RadicadoCod, v_NumeroRadicado, v_Anio, v_Consecutivo,
        'S', p_Prioridad, p_Confidencial,
        CURRENT_TIMESTAMP, p_UsuarioRadica, p_OficinaRadica, p_MedioRecepcion,
        p_TerceroDestino, p_NombreDestinatario,
        p_OficinaRemitente, p_UsuarioRemitente,
        p_RadicadoPadre, p_TipoRelacion,
        p_Asunto, p_Descripcion, p_Folios, p_Anexos, p_PalabrasClave,
        'RADICADO', p_Entidad, CURRENT_TIMESTAMP, true
    );
    
    -- Registrar en trazabilidad
    INSERT INTO "Radicados_Trazabilidad" (
        "Radicado", "Entidad", "Accion", "Descripcion",
        "OficinaOrigen", "UsuarioOrigen", "EstadoNuevo", "Fecha"
    ) VALUES (
        v_RadicadoCod, p_Entidad, 'RADICADO', 'Radicación de salida',
        p_OficinaRemitente, p_UsuarioRemitente, 'RADICADO', CURRENT_TIMESTAMP
    );
    
    -- Si es respuesta a un radicado padre, actualizar el padre
    IF p_RadicadoPadre IS NOT NULL AND p_TipoRelacion = 'RESPUESTA' THEN
        UPDATE "Radicados"
        SET 
            "Estado" = 'RESPONDIDO',
            "FechaCambioEstado" = CURRENT_TIMESTAMP,
            "FechaRespuesta" = CURRENT_TIMESTAMP,
            "RadicadoRespuesta" = v_RadicadoCod
        WHERE "Cod" = p_RadicadoPadre 
          AND "Entidad" = p_Entidad
          AND "Estado" NOT IN ('ARCHIVADO', 'ANULADO');
        
        IF FOUND THEN
            v_RadicadoPadreActualizado := true;
            
            -- Registrar en trazabilidad del padre
            INSERT INTO "Radicados_Trazabilidad" (
                "Radicado", "Entidad", "Accion", "Descripcion",
                "EstadoAnterior", "EstadoNuevo", "Fecha", "Observaciones"
            ) VALUES (
                p_RadicadoPadre, p_Entidad, 'RESPONDIDO', 
                'Respuesta generada con radicado ' || v_NumeroRadicado,
                'EN_TRAMITE', 'RESPONDIDO', CURRENT_TIMESTAMP,
                'Radicado de respuesta: ' || v_NumeroRadicado
            );
        END IF;
    END IF;
    
    -- Retornar resultado
    RETURN QUERY SELECT 
        v_RadicadoCod,
        v_NumeroRadicado,
        v_RadicadoPadreActualizado,
        'Radicado de salida creado exitosamente'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_RadicarSalida"(p_entidad bigint, p_asunto character varying, p_usuarioradica bigint, p_tercerodestino bigint, p_nombredestinatario character varying, p_oficinaremitente bigint, p_usuarioremitente bigint, p_radicadopadre bigint, p_tiporelacion character varying, p_prioridad character varying, p_confidencial boolean, p_mediorecepcion character varying, p_oficinaradica bigint, p_descripcion text, p_folios integer, p_anexos character varying, p_palabrasclave character varying); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_RadicarSalida"(p_entidad bigint, p_asunto character varying, p_usuarioradica bigint, p_tercerodestino bigint, p_nombredestinatario character varying, p_oficinaremitente bigint, p_usuarioremitente bigint, p_radicadopadre bigint, p_tiporelacion character varying, p_prioridad character varying, p_confidencial boolean, p_mediorecepcion character varying, p_oficinaradica bigint, p_descripcion text, p_folios integer, p_anexos character varying, p_palabrasclave character varying) IS 'Radica una comunicación de salida y actualiza el radicado padre si es respuesta';


--
-- Name: F_RechazarFirma(bigint, character varying, character varying, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_RechazarFirma"(p_firmantecod bigint, p_motivorechazo character varying, p_ip character varying, p_useragent character varying) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_SolicitudCod BIGINT;
    v_Entidad BIGINT;
    v_ArchivoId BIGINT;
    v_UsuarioFirmante BIGINT;
    v_NombreFirmante VARCHAR(400);
BEGIN
    SELECT fs."Solicitud", fs."Entidad", fs."Usuario", u."Nombres" || ' ' || u."Apellidos", s."Archivo"
    INTO v_SolicitudCod, v_Entidad, v_UsuarioFirmante, v_NombreFirmante, v_ArchivoId
    FROM documentos."Firmantes_Solicitud" fs
    INNER JOIN documentos."Solicitudes_Firma" s ON fs."Solicitud" = s."Cod"
    INNER JOIN documentos."Usuarios" u ON fs."Usuario" = u."Cod" AND fs."Entidad" = u."Entidad"
    WHERE fs."Cod" = p_FirmanteCod;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, 'Firmante no encontrado'::VARCHAR(500);
        RETURN;
    END IF;
    
    UPDATE documentos."Firmantes_Solicitud"
    SET "Estado" = 'RECHAZADO', "FechaRechazo" = CURRENT_TIMESTAMP, "MotivoRechazo" = p_MotivoRechazo, "IP" = p_IP, "UserAgent" = p_UserAgent
    WHERE "Cod" = p_FirmanteCod;
    
    INSERT INTO documentos."Evidencias_Firma" ("Firmante", "TipoEvidencia", "Descripcion", "IP", "UserAgent")
    VALUES (p_FirmanteCod, 'RECHAZO', 'Rechazado: ' || p_MotivoRechazo, p_IP, p_UserAgent);
    
    UPDATE documentos."Solicitudes_Firma" SET "Estado" = 'RECHAZADA' WHERE "Cod" = v_SolicitudCod;
    UPDATE documentos."Archivos" SET "Firmado" = false, "FimarPor" = NULL WHERE "Cod" = v_ArchivoId;
    
    INSERT INTO documentos."Notificaciones" ("Entidad", "UsuarioDestino", "UsuarioOrigen", "TipoNotificacion", "Titulo", "Mensaje", "SolicitudFirmaRef", "RequiereAccion", "Prioridad")
    SELECT s."Entidad", s."SolicitadoPor", v_UsuarioFirmante, 'FIRMA_RECHAZADA', 'Firma rechazada: ' || a."Nombre", v_NombreFirmante || ' rechazó: ' || p_MotivoRechazo, v_SolicitudCod, false, 'ALTA'
    FROM documentos."Solicitudes_Firma" s INNER JOIN documentos."Archivos" a ON s."Archivo" = a."Cod" WHERE s."Cod" = v_SolicitudCod;
    
    RETURN QUERY SELECT true, 'Firma rechazada'::VARCHAR(500);
END;
$$;


--
-- Name: F_RegistrarFirma(bigint, character varying, character varying, character varying, character varying, bigint, jsonb); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_RegistrarFirma"(p_firmantecod bigint, p_tipofirmausada character varying, p_hashalfirmar character varying, p_ip character varying, p_useragent character varying, p_certificadousado bigint DEFAULT NULL::bigint, p_datosadicionales jsonb DEFAULT NULL::jsonb) RETURNS TABLE("SolicitudCompletada" boolean, "SiguienteFirmante" bigint, "Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_SolicitudCod BIGINT;
    v_Entidad BIGINT;
    v_OrdenActual SMALLINT;
    v_OrdenSecuencial BOOLEAN;
    v_TotalFirmantes INTEGER;
    v_TotalFirmados INTEGER;
    v_RequiereTodos BOOLEAN;
    v_MinimoFirmantes SMALLINT;
    v_SiguienteFirmante BIGINT;
    v_ArchivoId BIGINT;
    v_SolicitudCompleta BOOLEAN := false;
BEGIN
    SELECT fs."Solicitud", fs."Entidad", fs."Orden", s."OrdenSecuencial", s."RequiereTodos", s."MinimoFirmantes", s."Archivo"
    INTO v_SolicitudCod, v_Entidad, v_OrdenActual, v_OrdenSecuencial, v_RequiereTodos, v_MinimoFirmantes, v_ArchivoId
    FROM documentos."Firmantes_Solicitud" fs
    INNER JOIN documentos."Solicitudes_Firma" s ON fs."Solicitud" = s."Cod"
    WHERE fs."Cod" = p_FirmanteCod;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT false, NULL::BIGINT, false, 'Firmante no encontrado'::VARCHAR(500);
        RETURN;
    END IF;
    
    UPDATE documentos."Firmantes_Solicitud"
    SET "Estado" = 'FIRMADO', "FechaFirma" = CURRENT_TIMESTAMP,
        "TipoFirmaUsada" = p_TipoFirmaUsada, "HashAlFirmar" = p_HashAlFirmar,
        "CertificadoUsado" = p_CertificadoUsado, "IP" = p_IP, "UserAgent" = p_UserAgent
    WHERE "Cod" = p_FirmanteCod;
    
    INSERT INTO documentos."Evidencias_Firma" ("Firmante", "TipoEvidencia", "Descripcion", "HashDocumento", "IP", "UserAgent", "DatosAdicionales")
    VALUES (p_FirmanteCod, 'FIRMA_APLICADA', 'Firma: ' || p_TipoFirmaUsada, p_HashAlFirmar, p_IP, p_UserAgent, p_DatosAdicionales);
    
    SELECT COUNT(*), COUNT(*) FILTER (WHERE "Estado" = 'FIRMADO')
    INTO v_TotalFirmantes, v_TotalFirmados
    FROM documentos."Firmantes_Solicitud" WHERE "Solicitud" = v_SolicitudCod AND "EsObligatorio" = true;
    
    IF v_RequiereTodos THEN
        v_SolicitudCompleta := (v_TotalFirmados >= v_TotalFirmantes);
    ELSE
        v_SolicitudCompleta := (v_TotalFirmados >= v_MinimoFirmantes);
    END IF;
    
    IF NOT v_SolicitudCompleta AND v_OrdenSecuencial THEN
        SELECT fs."Usuario" INTO v_SiguienteFirmante
        FROM documentos."Firmantes_Solicitud" fs
        WHERE fs."Solicitud" = v_SolicitudCod AND fs."Estado" = 'PENDIENTE' AND fs."Orden" > v_OrdenActual
        ORDER BY fs."Orden" LIMIT 1;
        
        IF v_SiguienteFirmante IS NOT NULL THEN
            PERFORM documentos."F_NotificarFirmante"(
                (SELECT "Cod" FROM documentos."Firmantes_Solicitud" WHERE "Solicitud" = v_SolicitudCod AND "Usuario" = v_SiguienteFirmante)
            );
        END IF;
    END IF;
    
    IF v_SolicitudCompleta THEN
        UPDATE documentos."Solicitudes_Firma" SET "Estado" = 'COMPLETADA', "FechaCompletada" = CURRENT_TIMESTAMP WHERE "Cod" = v_SolicitudCod;
        
        UPDATE documentos."Archivos"
        SET "Firmado" = true,
            "FirmadoPor" = (
                SELECT string_agg(u."Nombres" || ' ' || u."Apellidos" || ' (' || TO_CHAR(fs."FechaFirma", 'DD/MM/YYYY HH24:MI') || ')', ', ' ORDER BY fs."FechaFirma")
                FROM documentos."Firmantes_Solicitud" fs
                INNER JOIN documentos."Usuarios" u ON fs."Usuario" = u."Cod" AND fs."Entidad" = u."Entidad"
                WHERE fs."Solicitud" = v_SolicitudCod AND fs."Estado" = 'FIRMADO'
            ),
            "FechaFirma" = CURRENT_TIMESTAMP,
            "TipoFirma" = (SELECT "TipoFirma" FROM documentos."Solicitudes_Firma" WHERE "Cod" = v_SolicitudCod),
            "FimarPor" = NULL
        WHERE "Cod" = v_ArchivoId;
        
        INSERT INTO documentos."Notificaciones" ("Entidad", "UsuarioDestino", "TipoNotificacion", "Titulo", "Mensaje", "SolicitudFirmaRef", "RequiereAccion")
        SELECT s."Entidad", s."SolicitadoPor", 'FIRMA_COMPLETADA', 'Documento firmado: ' || a."Nombre", 'Todos han firmado.', v_SolicitudCod, false
        FROM documentos."Solicitudes_Firma" s INNER JOIN documentos."Archivos" a ON s."Archivo" = a."Cod" WHERE s."Cod" = v_SolicitudCod;
    ELSE
        UPDATE documentos."Solicitudes_Firma" SET "Estado" = 'EN_PROCESO' WHERE "Cod" = v_SolicitudCod AND "Estado" = 'PENDIENTE';
    END IF;
    
    RETURN QUERY SELECT v_SolicitudCompleta, v_SiguienteFirmante, true, 
        CASE WHEN v_SolicitudCompleta THEN 'Solicitud completada' ELSE 'Firma registrada' END::VARCHAR(500);
END;
$$;


--
-- Name: F_RestaurarVersionArchivo(bigint, integer, bigint, character varying); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_RestaurarVersionArchivo"(p_archivocod bigint, p_versionarestaurar integer, p_usuario bigint, p_comentario character varying DEFAULT NULL::character varying) RETURNS TABLE("Exito" boolean, "VersionRestaurada" integer, "VersionNueva" integer, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_VersionActual INTEGER;
    v_RutaRestaurar VARCHAR(2000);
    v_HashRestaurar VARCHAR(64);
    v_TamañoRestaurar VARCHAR(50);
    v_NombreRestaurar VARCHAR(200);
    v_FormatoRestaurar VARCHAR(50);
    v_HojasRestaurar SMALLINT;
    v_NuevaVersion INTEGER;
    v_Resultado RECORD;
BEGIN
    -- Verificar que el archivo existe
    SELECT "Version" INTO v_VersionActual
    FROM "Archivos"
    WHERE "Cod" = p_ArchivoCod AND "Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            false,
            NULL::INTEGER,
            NULL::INTEGER,
            'Archivo no encontrado o inactivo'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar que la versión a restaurar existe
    SELECT "Ruta", "Hash", "Tamaño", "Nombre", "Formato", "NumeroHojas"
    INTO v_RutaRestaurar, v_HashRestaurar, v_TamañoRestaurar, 
         v_NombreRestaurar, v_FormatoRestaurar, v_HojasRestaurar
    FROM "Versiones_Archivos"
    WHERE "Archivo" = p_ArchivoCod AND "Version" = p_VersionARestaurar AND "Estado" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            false,
            NULL::INTEGER,
            NULL::INTEGER,
            'Versión especificada no encontrada'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- No restaurar si ya es la versión actual
    IF p_VersionARestaurar = v_VersionActual THEN
        RETURN QUERY SELECT 
            false,
            p_VersionARestaurar,
            v_VersionActual,
            'La versión especificada ya es la versión actual'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Crear nueva versión con los datos de la versión anterior
    SELECT * INTO v_Resultado FROM "F_CrearVersionArchivo"(
        p_ArchivoCod,
        v_RutaRestaurar,
        v_HashRestaurar,
        v_TamañoRestaurar,
        p_Usuario,
        v_FormatoRestaurar,
        v_HojasRestaurar,
        v_NombreRestaurar,
        COALESCE(p_Comentario, 'Restauración desde versión ' || p_VersionARestaurar),
        'Restauración desde versión ' || p_VersionARestaurar
    );
    
    RETURN QUERY SELECT 
        true,
        p_VersionARestaurar,
        v_Resultado."VersionNueva",
        ('Versión ' || p_VersionARestaurar || ' restaurada como versión ' || v_Resultado."VersionNueva")::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_RestaurarVersionArchivo"(p_archivocod bigint, p_versionarestaurar integer, p_usuario bigint, p_comentario character varying); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_RestaurarVersionArchivo"(p_archivocod bigint, p_versionarestaurar integer, p_usuario bigint, p_comentario character varying) IS 'Restaura una versión anterior creando una nueva versión (no elimina historial)';


--
-- Name: F_RevocarTRDOficina(bigint, bigint, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_RevocarTRDOficina"(p_oficina bigint, p_trd bigint, p_entidad bigint) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
BEGIN
    -- Verificar que existe la asignación
    IF NOT EXISTS (
        SELECT 1 FROM "Oficinas_TRD"
        WHERE "Oficina" = p_Oficina AND "TRD" = p_TRD AND "Entidad" = p_Entidad AND "Estado" = true
    ) THEN
        RETURN QUERY SELECT false, 'No existe esa asignación activa'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Desactivar asignación
    UPDATE "Oficinas_TRD"
    SET "Estado" = false
    WHERE "Oficina" = p_Oficina AND "TRD" = p_TRD AND "Entidad" = p_Entidad;
    
    RETURN QUERY SELECT true, 'Asignación revocada exitosamente'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_RevocarTRDOficina"(p_oficina bigint, p_trd bigint, p_entidad bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_RevocarTRDOficina"(p_oficina bigint, p_trd bigint, p_entidad bigint) IS 'Revoca el acceso de una oficina a una TRD';


--
-- Name: F_SiguienteCod(character varying, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_SiguienteCod"(p_tabla character varying, p_entidad bigint DEFAULT NULL::bigint) RETURNS bigint
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_siguiente BIGINT;
    v_sql TEXT;
BEGIN
    -- Construir consulta dinámica
    IF p_entidad IS NOT NULL THEN
        v_sql := format(
            'SELECT COALESCE(MAX("Cod") + 1, 1) FROM documentos.%I WHERE "Entidad" = %s',
            p_tabla, p_entidad
        );
    ELSE
        v_sql := format(
            'SELECT COALESCE(MAX("Cod") + 1, 1) FROM documentos.%I',
            p_tabla
        );
    END IF;
    
    EXECUTE v_sql INTO v_siguiente;
    
    RETURN COALESCE(v_siguiente, 1);
END;
$$;


--
-- Name: FUNCTION "F_SiguienteCod"(p_tabla character varying, p_entidad bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_SiguienteCod"(p_tabla character varying, p_entidad bigint) IS 'Obtiene el siguiente código disponible para una tabla (compatibilidad con MAX(Cod)+1 de SQL Server)';


--
-- Name: F_SiguienteCodCarpeta(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_SiguienteCodCarpeta"() RETURNS bigint
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Siguiente BIGINT;
BEGIN
    SELECT COALESCE(MAX("Cod"), 0) + 1 INTO v_Siguiente FROM "Carpetas";
    RETURN v_Siguiente;
END;
$$;


--
-- Name: F_SolicitarProrroga(bigint, bigint, integer, character varying, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_SolicitarProrroga"(p_radicadocod bigint, p_entidad bigint, p_diasprorroga integer, p_motivoprorroga character varying, p_usuariosolicita bigint) RETURNS TABLE("Exito" boolean, "NuevaFechaVencimiento" timestamp without time zone, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_FechaVencimientoActual TIMESTAMP;
    v_NuevaFechaVencimiento TIMESTAMP;
    v_TipoSolicitud VARCHAR(20);
    v_DiasMaxProrroga INTEGER;
    v_Estado VARCHAR(20);
BEGIN
    -- Validar motivo
    IF p_MotivoProrroga IS NULL OR TRIM(p_MotivoProrroga) = '' THEN
        RETURN QUERY SELECT 
            false,
            NULL::TIMESTAMP,
            'El motivo de la prórroga es obligatorio'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Obtener datos del radicado
    SELECT r."FechaVencimiento", r."TipoSolicitud", r."Estado", ts."DiasHabilesProrroga"
    INTO v_FechaVencimientoActual, v_TipoSolicitud, v_Estado, v_DiasMaxProrroga
    FROM "Radicados" r
    LEFT JOIN "Tipos_Solicitud" ts ON r."TipoSolicitud" = ts."Cod"
    WHERE r."Cod" = p_RadicadoCod AND r."Entidad" = p_Entidad AND r."Activo" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            false,
            NULL::TIMESTAMP,
            'Radicado no encontrado'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar estado
    IF v_Estado IN ('ARCHIVADO', 'ANULADO', 'RESPONDIDO') THEN
        RETURN QUERY SELECT 
            false,
            NULL::TIMESTAMP,
            ('No se puede prorrogar un radicado en estado ' || v_Estado)::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar que tenga fecha de vencimiento
    IF v_FechaVencimientoActual IS NULL THEN
        RETURN QUERY SELECT 
            false,
            NULL::TIMESTAMP,
            'El radicado no tiene fecha de vencimiento'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar días de prórroga permitidos
    IF v_DiasMaxProrroga IS NOT NULL AND p_DiasProrroga > v_DiasMaxProrroga THEN
        RETURN QUERY SELECT 
            false,
            NULL::TIMESTAMP,
            ('Máximo de días de prórroga permitidos: ' || v_DiasMaxProrroga)::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Calcular nueva fecha de vencimiento (días hábiles)
    v_NuevaFechaVencimiento := "F_AgregarDiasHabiles"(v_FechaVencimientoActual::DATE, p_DiasProrroga)::TIMESTAMP;
    
    -- Actualizar radicado
    UPDATE "Radicados"
    SET 
        "FechaVencimiento" = v_NuevaFechaVencimiento,
        "FechaProrroga" = CURRENT_TIMESTAMP,
        "MotivoProrroga" = p_MotivoProrroga,
        "FechaModificacion" = CURRENT_TIMESTAMP,
        "ModificadoPor" = p_UsuarioSolicita
    WHERE "Cod" = p_RadicadoCod AND "Entidad" = p_Entidad;
    
    -- Registrar en trazabilidad
    INSERT INTO "Radicados_Trazabilidad" (
        "Radicado", "Entidad", "Accion", "Descripcion",
        "Fecha", "Observaciones"
    ) VALUES (
        p_RadicadoCod, p_Entidad, 'PRORROGA', 
        'Prórroga de ' || p_DiasProrroga || ' días hábiles',
        CURRENT_TIMESTAMP, p_MotivoProrroga
    );
    
    RETURN QUERY SELECT 
        true,
        v_NuevaFechaVencimiento,
        ('Prórroga aplicada. Nueva fecha de vencimiento: ' || v_NuevaFechaVencimiento::DATE)::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_SolicitarProrroga"(p_radicadocod bigint, p_entidad bigint, p_diasprorroga integer, p_motivoprorroga character varying, p_usuariosolicita bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_SolicitarProrroga"(p_radicadocod bigint, p_entidad bigint, p_diasprorroga integer, p_motivoprorroga character varying, p_usuariosolicita bigint) IS 'Solicita prórroga para un radicado según Ley 1755 Art. 14';


--
-- Name: F_TrasladarRadicado(bigint, bigint, bigint, bigint, text, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_TrasladarRadicado"(p_radicadocod bigint, p_entidad bigint, p_oficinadestino bigint, p_usuariotraslada bigint, p_motivotraslado text, p_usuariodestino bigint DEFAULT NULL::bigint) RETURNS TABLE("Exito" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_EstadoAnterior VARCHAR(20);
    v_OficinaAnterior BIGINT;
    v_UsuarioAnterior BIGINT;
BEGIN
    -- Validar motivo
    IF p_MotivoTraslado IS NULL OR TRIM(p_MotivoTraslado) = '' THEN
        RETURN QUERY SELECT 
            false,
            'El motivo del traslado es obligatorio'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Obtener estado actual
    SELECT "Estado", "OficinaDestino", "UsuarioDestino"
    INTO v_EstadoAnterior, v_OficinaAnterior, v_UsuarioAnterior
    FROM "Radicados"
    WHERE "Cod" = p_RadicadoCod AND "Entidad" = p_Entidad AND "Activo" = true;
    
    IF NOT FOUND THEN
        RETURN QUERY SELECT 
            false,
            'Radicado no encontrado o inactivo'::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Verificar que no esté en estado final
    IF v_EstadoAnterior IN ('ARCHIVADO', 'ANULADO', 'RESPONDIDO') THEN
        RETURN QUERY SELECT 
            false,
            ('No se puede trasladar un radicado en estado ' || v_EstadoAnterior)::VARCHAR(500);
        RETURN;
    END IF;
    
    -- Actualizar radicado
    UPDATE "Radicados"
    SET 
        "OficinaDestino" = p_OficinaDestino,
        "UsuarioDestino" = p_UsuarioDestino,
        "Estado" = 'TRASLADADO',
        "FechaCambioEstado" = CURRENT_TIMESTAMP,
        "FechaModificacion" = CURRENT_TIMESTAMP,
        "ModificadoPor" = p_UsuarioTraslada
    WHERE "Cod" = p_RadicadoCod AND "Entidad" = p_Entidad;
    
    -- Registrar en trazabilidad
    INSERT INTO "Radicados_Trazabilidad" (
        "Radicado", "Entidad", "Accion", "Descripcion",
        "OficinaOrigen", "UsuarioOrigen", "OficinaDestino", "UsuarioDestino",
        "EstadoAnterior", "EstadoNuevo", "Fecha", "Observaciones"
    ) VALUES (
        p_RadicadoCod, p_Entidad, 'TRASLADADO', 'Traslado por competencia',
        v_OficinaAnterior, v_UsuarioAnterior, p_OficinaDestino, p_UsuarioDestino,
        v_EstadoAnterior, 'TRASLADADO', CURRENT_TIMESTAMP, p_MotivoTraslado
    );
    
    RETURN QUERY SELECT 
        true,
        'Radicado trasladado exitosamente'::VARCHAR(500);
END;
$$;


--
-- Name: FUNCTION "F_TrasladarRadicado"(p_radicadocod bigint, p_entidad bigint, p_oficinadestino bigint, p_usuariotraslada bigint, p_motivotraslado text, p_usuariodestino bigint); Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON FUNCTION documentos."F_TrasladarRadicado"(p_radicadocod bigint, p_entidad bigint, p_oficinadestino bigint, p_usuariotraslada bigint, p_motivotraslado text, p_usuariodestino bigint) IS 'Traslada un radicado a otra oficina por competencia (Ley 1755 Art. 21)';


--
-- Name: F_TriggerNuevaNotificacion(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_TriggerNuevaNotificacion"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE v_Payload JSONB;
BEGIN
    v_Payload := jsonb_build_object('tipo', 'NUEVA_NOTIFICACION', 'notificacionId', NEW."Cod", 'usuarioDestino', NEW."UsuarioDestino", 'entidad', NEW."Entidad", 'tipoNotificacion', NEW."TipoNotificacion", 'titulo', NEW."Titulo", 'prioridad', NEW."Prioridad", 'requiereAccion', NEW."RequiereAccion", 'timestamp', EXTRACT(EPOCH FROM CURRENT_TIMESTAMP)::BIGINT);
    PERFORM pg_notify('notificaciones_cambios', v_Payload::TEXT);
    RETURN NEW;
END;
$$;


--
-- Name: F_TriggerNuevaSolicitudFirma(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_TriggerNuevaSolicitudFirma"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_Payload JSONB;
    v_FirmantePendiente RECORD;
BEGIN
    IF NEW."OrdenSecuencial" THEN
        SELECT * INTO v_FirmantePendiente FROM documentos."Firmantes_Solicitud" WHERE "Solicitud" = NEW."Cod" ORDER BY "Orden" LIMIT 1;
        IF FOUND THEN PERFORM documentos."F_NotificarFirmante"(v_FirmantePendiente."Cod"); END IF;
    ELSE
        FOR v_FirmantePendiente IN SELECT * FROM documentos."Firmantes_Solicitud" WHERE "Solicitud" = NEW."Cod"
        LOOP PERFORM documentos."F_NotificarFirmante"(v_FirmantePendiente."Cod"); END LOOP;
    END IF;
    
    v_Payload := jsonb_build_object('tipo', 'SOLICITUD_FIRMA_CREADA', 'solicitudId', NEW."Cod", 'archivo', NEW."Archivo", 'entidad', NEW."Entidad", 'timestamp', EXTRACT(EPOCH FROM CURRENT_TIMESTAMP)::BIGINT);
    PERFORM pg_notify('firma_cambios', v_Payload::TEXT);
    PERFORM pg_notify('notificaciones_cambios', v_Payload::TEXT);
    RETURN NEW;
END;
$$;


--
-- Name: F_ValidarJerarquiaCarpeta(bigint, bigint); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."F_ValidarJerarquiaCarpeta"(p_tipocarpetapadre bigint, p_tipocarpetahijo bigint) RETURNS TABLE("Valido" boolean, "Mensaje" character varying)
    LANGUAGE plpgsql
    AS $$
  BEGIN
      IF p_TipoCarpetaPadre IS NULL THEN
          IF p_TipoCarpetaHijo IN (1, 4) THEN
              RETURN QUERY SELECT true, 'Válido: Serie o Genérica en raíz'::VARCHAR(500);
          ELSE
              RETURN QUERY SELECT false, 'Solo se pueden crear Series o Genéricas en la
  raíz'::VARCHAR(500);
          END IF;
          RETURN;
      END IF;

      CASE p_TipoCarpetaPadre
          WHEN 1 THEN
              IF p_TipoCarpetaHijo IN (2, 3, 4) THEN
                  RETURN QUERY SELECT true, 'Válido'::VARCHAR(500);
              ELSE
                  RETURN QUERY SELECT false, 'Una Serie solo puede contener Subseries, Expedientes o       
  Genéricas'::VARCHAR(500);
              END IF;
          WHEN 2 THEN
              IF p_TipoCarpetaHijo IN (2, 3, 4) THEN
                  RETURN QUERY SELECT true, 'Válido'::VARCHAR(500);
              ELSE
                  RETURN QUERY SELECT false, 'Una Subserie solo puede contener Subseries, Expedientes o    
  Genéricas'::VARCHAR(500);
              END IF;
          WHEN 3 THEN
              IF p_TipoCarpetaHijo = 4 THEN
                  RETURN QUERY SELECT true, 'Válido'::VARCHAR(500);
              ELSE
                  RETURN QUERY SELECT false, 'Un Expediente solo puede contener Carpetas
  Genéricas'::VARCHAR(500);
              END IF;
          WHEN 4 THEN
              IF p_TipoCarpetaHijo = 4 THEN
                  RETURN QUERY SELECT true, 'Válido'::VARCHAR(500);
              ELSE
                  RETURN QUERY SELECT false, 'Una Carpeta Genérica solo puede contener otras
  Genéricas'::VARCHAR(500);
              END IF;
          ELSE
              RETURN QUERY SELECT false, 'Tipo de carpeta padre no válido'::VARCHAR(500);
      END CASE;
  END;
  $$;


--
-- Name: TRG_Radicados_HoraRadicacion(); Type: FUNCTION; Schema: documentos; Owner: -
--

CREATE FUNCTION documentos."TRG_Radicados_HoraRadicacion"() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
BEGIN
    NEW."HoraRadicacion" := NEW."FechaRadicacion"::TIME;
    RETURN NEW;
END;
$$;


--
-- Name: F_SiguienteCodEntidad(); Type: FUNCTION; Schema: usuarios; Owner: -
--

CREATE FUNCTION usuarios."F_SiguienteCodEntidad"() RETURNS bigint
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN COALESCE((SELECT MAX("Cod") + 1 FROM usuarios."Entidades"), 1);
END;
$$;


--
-- Name: F_SiguienteCodUsuario(bigint); Type: FUNCTION; Schema: usuarios; Owner: -
--

CREATE FUNCTION usuarios."F_SiguienteCodUsuario"(p_entidad bigint) RETURNS bigint
    LANGUAGE plpgsql
    AS $$
BEGIN
    RETURN COALESCE((SELECT MAX("Cod") + 1 FROM usuarios."Usuarios" WHERE "Entidad" = p_entidad), 1);
END;
$$;


SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: Archivos; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Archivos" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(200),
    "Carpeta" bigint,
    "Copia" boolean DEFAULT false,
    "ArchivoOrginal" bigint,
    "Firmado" boolean DEFAULT false,
    "FimarPor" text,
    "FirmadoPor" text,
    "Ruta" character varying(2000),
    "TipoArchivo" bigint,
    "Formato" character varying(50),
    "NumeroHojas" smallint,
    "Duracion" character varying(50),
    "Tamaño" character varying(50),
    "Estado" boolean DEFAULT true NOT NULL,
    "Indice" smallint DEFAULT 0 NOT NULL,
    "FechaDocumento" timestamp without time zone,
    "FechaIncorporacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "OrigenDocumento" character varying(20),
    "Hash" character varying(64),
    "AlgoritmoHash" character varying(20) DEFAULT 'SHA256'::character varying,
    "TipoDocumental" bigint,
    "Observaciones" text,
    "SubidoPor" bigint,
    "FechaFirma" timestamp without time zone,
    "TipoFirma" character varying(20),
    "CertificadoFirma" text,
    "PaginaInicio" integer,
    "PaginaFin" integer,
    "Version" integer DEFAULT 1,
    "Descripcion" character varying(500),
    "FechaVerificacionIntegridad" timestamp without time zone,
    "ResultadoVerificacion" boolean,
    "Metadatos" text,
    "CodigoDocumento" character varying(50),
    "FechaModificacion" timestamp without time zone,
    "EstadoUpload" character varying(20) DEFAULT 'COMPLETADO'::character varying,
    "ContentType" character varying(100)
);


--
-- Name: TABLE "Archivos"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Archivos" IS 'Documentos almacenados en el sistema de gestión documental';


--
-- Name: COLUMN "Archivos"."Ruta"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Archivos"."Ruta" IS 'URL de GCS: gs://bucket/entidad/serie/expediente/archivo';


--
-- Name: COLUMN "Archivos"."Indice"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Archivos"."Indice" IS 'Orden del documento dentro del expediente';


--
-- Name: COLUMN "Archivos"."Hash"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Archivos"."Hash" IS 'Hash SHA-256 para verificación de integridad';


--
-- Name: COLUMN "Archivos"."Version"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Archivos"."Version" IS 'Número de versión actual (la tabla Versiones_Archivos tiene el historial)';


--
-- Name: COLUMN "Archivos"."EstadoUpload"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Archivos"."EstadoUpload" IS 'Estado del upload: PENDIENTE, COMPLETADO, ERROR';


--
-- Name: COLUMN "Archivos"."ContentType"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Archivos"."ContentType" IS 'Tipo MIME del archivo (ej: application/pdf, image/png)';


--
-- Name: Archivos_Eliminados; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Archivos_Eliminados" (
    "Cod" bigint,
    "Nombre" character varying(500),
    "Ruta" character varying(2000),
    "FechaEliminacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "Carpeta" character varying(200),
    "Formato" character varying(20),
    "EliminadoPor" bigint,
    "MotivoEliminacion" character varying(500),
    "Hash" character varying(64),
    "Tamaño" character varying(50),
    "CarpetaCod" bigint
);


--
-- Name: TABLE "Archivos_Eliminados"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Archivos_Eliminados" IS 'Auditoría de archivos eliminados del sistema';


--
-- Name: Carpetas; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Carpetas" (
    "Cod" bigint NOT NULL,
    "CodSerie" bigint,
    "CodSubSerie" bigint,
    "Estado" boolean DEFAULT true NOT NULL,
    "EstadoCarpeta" bigint,
    "Nombre" character varying(200),
    "Descripcion" character varying(500),
    "Copia" boolean DEFAULT false,
    "CarpetaOriginal" bigint,
    "CarpetaPadre" bigint,
    "FechaCreacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "IndiceElectronico" text,
    "Delegado" bigint,
    "TipoCarpeta" bigint,
    "NivelVisualizacion" bigint,
    "SerieRaiz" bigint,
    "TRD" bigint,
    "FechaCierre" timestamp without time zone,
    "NumeroFolios" integer DEFAULT 0,
    "Soporte" character varying(20),
    "FrecuenciaConsulta" character varying(10),
    "UbicacionFisica" character varying(200),
    "Observaciones" text,
    "CreadoPor" bigint,
    "ModificadoPor" bigint,
    "FechaModificacion" timestamp without time zone,
    "Tomo" integer DEFAULT 1,
    "TotalTomos" integer DEFAULT 1,
    "CodigoExpediente" character varying(50),
    "PalabrasClave" character varying(500),
    "FechaDocumentoInicial" timestamp without time zone,
    "FechaDocumentoFinal" timestamp without time zone,
    "ExpedienteRelacionado" bigint,
    "Entidad" bigint,
    "Oficina" bigint
);


--
-- Name: TABLE "Carpetas"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Carpetas" IS 'Instancias de carpetas del árbol documental (Series, Subseries, Expedientes, Genéricas)';


--
-- Name: COLUMN "Carpetas"."IndiceElectronico"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Carpetas"."IndiceElectronico" IS 'JSON con índice electrónico según Acuerdo 002/2014 AGN';


--
-- Name: COLUMN "Carpetas"."TipoCarpeta"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Carpetas"."TipoCarpeta" IS '1=Serie, 2=Subserie, 3=Expediente, 4=Genérica';


--
-- Name: COLUMN "Carpetas"."SerieRaiz"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Carpetas"."SerieRaiz" IS 'Código de la Serie raíz para optimizar consultas';


--
-- Name: COLUMN "Carpetas"."TRD"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Carpetas"."TRD" IS 'Solo para TipoCarpeta 1 o 2. NULL para Expedientes y Genéricas';


--
-- Name: COLUMN "Carpetas"."Entidad"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Carpetas"."Entidad" IS 'Entidad a la que pertenece la carpeta (multi-tenant)';


--
-- Name: COLUMN "Carpetas"."Oficina"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Carpetas"."Oficina" IS 'Oficina/Dependencia asociada a la carpeta';


--
-- Name: Carpetas_Eliminadas; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Carpetas_Eliminadas" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(200),
    "TipoCarpeta" bigint,
    "CarpetaPadre" bigint,
    "SerieRaiz" bigint,
    "FechaCreacion" timestamp without time zone,
    "FechaEliminacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "EliminadoPor" bigint,
    "MotivoEliminacion" character varying(500),
    "NumeroFolios" integer,
    "NumeroArchivos" integer
);


--
-- Name: TABLE "Carpetas_Eliminadas"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Carpetas_Eliminadas" IS 'Auditoría de carpetas eliminadas del sistema';


--
-- Name: Certificados_Usuarios; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Certificados_Usuarios" (
    "Cod" bigint NOT NULL,
    "Usuario" bigint NOT NULL,
    "Entidad" bigint NOT NULL,
    "NumeroSerie" character varying(100) NOT NULL,
    "SubjectName" character varying(500) NOT NULL,
    "Huella" character varying(64) NOT NULL,
    "CertificadoPublico" text NOT NULL,
    "ClavePrivadaEncriptada" text NOT NULL,
    "SaltEncriptacion" character varying(64) NOT NULL,
    "AlgoritmoEncriptacion" character varying(20) DEFAULT 'AES256'::character varying,
    "AlgoritmoFirma" character varying(20) DEFAULT 'RSA'::character varying,
    "TamanoClave" integer DEFAULT 2048,
    "FechaEmision" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "FechaVencimiento" timestamp without time zone NOT NULL,
    "Estado" character varying(20) DEFAULT 'ACTIVO'::character varying,
    "FechaRevocacion" timestamp without time zone,
    "MotivoRevocacion" character varying(500),
    "RevocadoPor" bigint,
    "FechaCreacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "UltimoUso" timestamp without time zone,
    "VecesUsado" integer DEFAULT 0,
    CONSTRAINT "CK_Certificado_Estado" CHECK ((("Estado")::text = ANY ((ARRAY['ACTIVO'::character varying, 'REVOCADO'::character varying, 'VENCIDO'::character varying, 'SUSPENDIDO'::character varying])::text[])))
);


--
-- Name: Certificados_Usuarios_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Certificados_Usuarios_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Certificados_Usuarios_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Certificados_Usuarios_Cod_seq" OWNED BY documentos."Certificados_Usuarios"."Cod";


--
-- Name: Codigos_OTP; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Codigos_OTP" (
    "Cod" bigint NOT NULL,
    "Firmante" bigint NOT NULL,
    "CodigoHash" character varying(64) NOT NULL,
    "Salt" character varying(32) NOT NULL,
    "MedioEnvio" character varying(20) NOT NULL,
    "DestinoEnvio" character varying(200) NOT NULL,
    "DestinoCompleto" character varying(200),
    "FechaEnvio" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "FechaExpiracion" timestamp without time zone NOT NULL,
    "Intentos" smallint DEFAULT 0,
    "MaxIntentos" smallint DEFAULT 3,
    "Usado" boolean DEFAULT false,
    "FechaUso" timestamp without time zone,
    "IPUso" character varying(50),
    CONSTRAINT "CK_OTP_MedioEnvio" CHECK ((("MedioEnvio")::text = ANY ((ARRAY['EMAIL'::character varying, 'SMS'::character varying])::text[])))
);


--
-- Name: Codigos_OTP_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Codigos_OTP_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Codigos_OTP_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Codigos_OTP_Cod_seq" OWNED BY documentos."Codigos_OTP"."Cod";


--
-- Name: Configuracion_Firma; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Configuracion_Firma" (
    "Cod" bigint NOT NULL,
    "Entidad" bigint NOT NULL,
    "FirmaSimpleHabilitada" boolean DEFAULT true,
    "OtpPorEmail" boolean DEFAULT true,
    "OtpPorSms" boolean DEFAULT false,
    "OtpLongitud" smallint DEFAULT 6,
    "OtpVigenciaMinutos" smallint DEFAULT 10,
    "OtpMaxIntentos" smallint DEFAULT 3,
    "FirmaAvanzadaHabilitada" boolean DEFAULT true,
    "GenerarCertificadoAuto" boolean DEFAULT true,
    "VigenciaCertificadoDias" integer DEFAULT 365,
    "RequiereContrasenaFuerte" boolean DEFAULT true,
    "AgregarPaginaConstancia" boolean DEFAULT true,
    "AgregarSelloVisual" boolean DEFAULT true,
    "PosicionSelloX" integer DEFAULT 400,
    "PosicionSelloY" integer DEFAULT 50,
    "TamanoSelloAncho" integer DEFAULT 200,
    "TamanoSelloAlto" integer DEFAULT 70,
    "TextoSello" character varying(200) DEFAULT 'Firmado electrónicamente'::character varying,
    "IncluirFechaEnSello" boolean DEFAULT true,
    "IncluirNombreEnSello" boolean DEFAULT true,
    "EnviarEmailSolicitud" boolean DEFAULT true,
    "EnviarEmailRecordatorio" boolean DEFAULT true,
    "EnviarEmailFirmaCompletada" boolean DEFAULT true,
    "DiasRecordatorioVencimiento" smallint DEFAULT 2,
    "PlantillaEmailSolicitud" text,
    "PlantillaEmailOtp" text,
    "PlantillaEmailCompletada" text,
    "PlantillaEmailRecordatorio" text,
    "MaxFirmantesPorSolicitud" smallint DEFAULT 10,
    "DiasMaxVigenciaSolicitud" smallint DEFAULT 30
);


--
-- Name: Configuracion_Firma_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Configuracion_Firma_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Configuracion_Firma_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Configuracion_Firma_Cod_seq" OWNED BY documentos."Configuracion_Firma"."Cod";


--
-- Name: Consecutivos_Radicado; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Consecutivos_Radicado" (
    "Entidad" bigint NOT NULL,
    "TipoComunicacion" character varying(10) NOT NULL,
    "Anio" integer NOT NULL,
    "UltimoConsecutivo" integer DEFAULT 0 NOT NULL,
    "FechaActualizacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL
);


--
-- Name: TABLE "Consecutivos_Radicado"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Consecutivos_Radicado" IS 'Control de consecutivos de radicación por año y tipo';


--
-- Name: COLUMN "Consecutivos_Radicado"."TipoComunicacion"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Consecutivos_Radicado"."TipoComunicacion" IS 'E=Entrada, S=Salida, I=Interna';


--
-- Name: Dias_Festivos; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Dias_Festivos" (
    "Fecha" date NOT NULL,
    "Nombre" character varying(100),
    "EsFijo" boolean DEFAULT false NOT NULL,
    "Pais" character varying(50) DEFAULT 'Colombia'::character varying
);


--
-- Name: TABLE "Dias_Festivos"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Dias_Festivos" IS 'Calendario de días festivos para cálculo de términos legales';


--
-- Name: COLUMN "Dias_Festivos"."EsFijo"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Dias_Festivos"."EsFijo" IS 'true=Fecha fija cada año, false=Fecha móvil';


--
-- Name: Disposiciones_Finales; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Disposiciones_Finales" (
    "Cod" character varying(10) NOT NULL,
    "Nombre" character varying(100),
    "Descripcion" character varying(500)
);


--
-- Name: TABLE "Disposiciones_Finales"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Disposiciones_Finales" IS 'Disposición final según TRD (Conservación, Eliminación, etc.)';


--
-- Name: Entidades; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Entidades" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(200),
    "Telefono" character varying(20),
    "Nit" character varying(20) NOT NULL,
    "Direccion" character varying(200),
    "Correo" character varying(200),
    "Logo" bytea,
    "Ciudad" character varying(200),
    "Departamento" character varying(100),
    "Pais" character varying(100) DEFAULT 'Colombia'::character varying,
    "SitioWeb" character varying(200),
    "TipoEntidad" character varying(20),
    "Sector" bigint,
    "RepresentanteLegal" character varying(200),
    "DocumentoRepresentante" character varying(20),
    "FechaCreacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "CodigoDane" character varying(20),
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Entidades"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Entidades" IS 'Entidades gubernamentales con información completa para gestión documental';


--
-- Name: COLUMN "Entidades"."TipoEntidad"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Entidades"."TipoEntidad" IS 'Referencia a Tipos_Entidad (PUBLICA, PRIVADA, MIXTA)';


--
-- Name: COLUMN "Entidades"."CodigoDane"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Entidades"."CodigoDane" IS 'Código DANE del municipio/departamento';


--
-- Name: ErrorLog; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."ErrorLog" (
    "Id" bigint NOT NULL,
    "ProcedureName" character varying(200) NOT NULL,
    "Parameters" text,
    "ErrorMessage" text NOT NULL,
    "ErrorDate" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "ErrorNumber" integer,
    "ErrorLine" integer,
    "ErrorSeverity" integer
);


--
-- Name: TABLE "ErrorLog"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."ErrorLog" IS 'Log de errores en procedimientos almacenados';


--
-- Name: ErrorLog_Id_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."ErrorLog_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: ErrorLog_Id_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."ErrorLog_Id_seq" OWNED BY documentos."ErrorLog"."Id";


--
-- Name: Estados_Carpetas; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Estados_Carpetas" (
    "Cod" bigint,
    "Nombre" character varying(200)
);


--
-- Name: TABLE "Estados_Carpetas"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Estados_Carpetas" IS 'Estados posibles de una carpeta/expediente';


--
-- Name: Estados_Radicado; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Estados_Radicado" (
    "Cod" character varying(20) NOT NULL,
    "Nombre" character varying(100),
    "Descripcion" character varying(200),
    "EsFinal" boolean DEFAULT false NOT NULL,
    "Color" character varying(20),
    "Orden" integer
);


--
-- Name: TABLE "Estados_Radicado"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Estados_Radicado" IS 'Estados posibles de un radicado en el flujo de trabajo';


--
-- Name: Evidencias_Firma; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Evidencias_Firma" (
    "Cod" bigint NOT NULL,
    "Firmante" bigint NOT NULL,
    "TipoEvidencia" character varying(30) NOT NULL,
    "FechaEvidencia" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "Descripcion" character varying(500),
    "HashDocumento" character varying(64),
    "IP" character varying(50),
    "UserAgent" character varying(500),
    "Geolocalizacion" character varying(100),
    "DatosAdicionales" jsonb
);


--
-- Name: Evidencias_Firma_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Evidencias_Firma_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Evidencias_Firma_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Evidencias_Firma_Cod_seq" OWNED BY documentos."Evidencias_Firma"."Cod";


--
-- Name: Firmantes_Solicitud; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Firmantes_Solicitud" (
    "Cod" bigint NOT NULL,
    "Solicitud" bigint NOT NULL,
    "Usuario" bigint NOT NULL,
    "Entidad" bigint NOT NULL,
    "Orden" smallint DEFAULT 1,
    "RolFirmante" character varying(50),
    "EsObligatorio" boolean DEFAULT true,
    "Estado" character varying(20) DEFAULT 'PENDIENTE'::character varying,
    "FechaNotificacion" timestamp without time zone,
    "NotificadoPorEmail" boolean DEFAULT false,
    "EmailNotificacion" character varying(200),
    "FechaFirma" timestamp without time zone,
    "TipoFirmaUsada" character varying(30),
    "HashAlFirmar" character varying(64),
    "CertificadoUsado" bigint,
    "FechaRechazo" timestamp without time zone,
    "MotivoRechazo" character varying(500),
    "IP" character varying(50),
    "UserAgent" character varying(500),
    "Geolocalizacion" character varying(100),
    CONSTRAINT "CK_Firmante_Estado" CHECK ((("Estado")::text = ANY ((ARRAY['PENDIENTE'::character varying, 'NOTIFICADO'::character varying, 'FIRMADO'::character varying, 'RECHAZADO'::character varying])::text[])))
);


--
-- Name: Firmantes_Solicitud_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Firmantes_Solicitud_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Firmantes_Solicitud_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Firmantes_Solicitud_Cod_seq" OWNED BY documentos."Firmantes_Solicitud"."Cod";


--
-- Name: Firmas_Archivos; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Firmas_Archivos" (
    "Cod" bigint NOT NULL,
    "Archivo" bigint NOT NULL,
    "Usuario" bigint NOT NULL,
    "TipoFirma" character varying(20),
    "FechaFirma" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CertificadoSerial" character varying(100),
    "CertificadoEmisor" character varying(200),
    "CertificadoVence" timestamp without time zone,
    "HashFirmado" character varying(64),
    "DatosFirma" text,
    "Estado" boolean DEFAULT true NOT NULL,
    "Observaciones" character varying(500)
);


--
-- Name: TABLE "Firmas_Archivos"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Firmas_Archivos" IS 'Registro de firmas electrónicas por documento';


--
-- Name: Firmas_Archivos_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Firmas_Archivos_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Firmas_Archivos_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Firmas_Archivos_Cod_seq" OWNED BY documentos."Firmas_Archivos"."Cod";


--
-- Name: Frecuencias_Consulta; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Frecuencias_Consulta" (
    "Cod" character varying(10) NOT NULL,
    "Nombre" character varying(50),
    "DiasMaxRespuesta" integer
);


--
-- Name: TABLE "Frecuencias_Consulta"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Frecuencias_Consulta" IS 'Frecuencia de consulta para priorización de acceso';


--
-- Name: Historial_Acciones; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Historial_Acciones" (
    "Cod" bigint NOT NULL,
    "Tabla" character varying(50) NOT NULL,
    "RegistroCod" bigint NOT NULL,
    "Accion" character varying(20) NOT NULL,
    "Usuario" bigint,
    "Fecha" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "IP" character varying(50),
    "DetalleAnterior" text,
    "DetalleNuevo" text,
    "Observaciones" character varying(500)
);


--
-- Name: TABLE "Historial_Acciones"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Historial_Acciones" IS 'Auditoría general de acciones en el sistema';


--
-- Name: Historial_Acciones_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Historial_Acciones_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Historial_Acciones_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Historial_Acciones_Cod_seq" OWNED BY documentos."Historial_Acciones"."Cod";


--
-- Name: Medios_Recepcion; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Medios_Recepcion" (
    "Cod" character varying(20) NOT NULL,
    "Nombre" character varying(100),
    "RequiereDigitalizacion" boolean DEFAULT false NOT NULL,
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Medios_Recepcion"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Medios_Recepcion" IS 'Medios por los cuales se reciben las comunicaciones';


--
-- Name: Niveles_Visualizacion_Carpetas; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Niveles_Visualizacion_Carpetas" (
    "Cod" bigint,
    "Nombre" character varying(200)
);


--
-- Name: TABLE "Niveles_Visualizacion_Carpetas"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Niveles_Visualizacion_Carpetas" IS 'Niveles de acceso para visualización de carpetas';


--
-- Name: Notificaciones; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Notificaciones" (
    "Cod" bigint NOT NULL,
    "Entidad" bigint NOT NULL,
    "UsuarioDestino" bigint NOT NULL,
    "UsuarioOrigen" bigint,
    "TipoNotificacion" character varying(30) NOT NULL,
    "Titulo" character varying(200) NOT NULL,
    "Mensaje" text,
    "Prioridad" character varying(10) DEFAULT 'MEDIA'::character varying,
    "FechaCreacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "FechaLeido" timestamp without time zone,
    "FechaAccion" timestamp without time zone,
    "Estado" character varying(20) DEFAULT 'PENDIENTE'::character varying,
    "RadicadoRef" bigint,
    "ArchivoRef" bigint,
    "CarpetaRef" bigint,
    "SolicitudFirmaRef" bigint,
    "RequiereAccion" boolean DEFAULT false,
    "FechaVencimientoAccion" timestamp without time zone,
    "UrlAccion" character varying(500),
    "DatosAdicionales" jsonb,
    "EnviadoPorCorreo" boolean DEFAULT false,
    "FechaEnvioCorreo" timestamp without time zone,
    CONSTRAINT "CK_Notificacion_Estado" CHECK ((("Estado")::text = ANY ((ARRAY['PENDIENTE'::character varying, 'LEIDA'::character varying, 'ARCHIVADA'::character varying, 'ELIMINADA'::character varying])::text[]))),
    CONSTRAINT "CK_Notificacion_Prioridad" CHECK ((("Prioridad")::text = ANY ((ARRAY['BAJA'::character varying, 'MEDIA'::character varying, 'ALTA'::character varying, 'URGENTE'::character varying])::text[])))
);


--
-- Name: Notificaciones_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Notificaciones_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Notificaciones_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Notificaciones_Cod_seq" OWNED BY documentos."Notificaciones"."Cod";


--
-- Name: Oficinas; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Oficinas" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(200),
    "Estado" boolean DEFAULT true,
    "Entidad" bigint NOT NULL,
    "CodigoSerie" bigint,
    "Icono" bytea,
    "Codigo" character varying(20),
    "OficinaPadre" bigint,
    "Responsable" bigint,
    "Telefono" character varying(20),
    "Correo" character varying(200),
    "Ubicacion" character varying(200),
    "NivelJerarquico" integer,
    "FechaCreacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "Sigla" character varying(20)
);


--
-- Name: TABLE "Oficinas"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Oficinas" IS 'Dependencias/Oficinas de la estructura organizacional';


--
-- Name: COLUMN "Oficinas"."CodigoSerie"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Oficinas"."CodigoSerie" IS 'LEGACY: Campo obsoleto, usar Oficinas_TRD';


--
-- Name: COLUMN "Oficinas"."OficinaPadre"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Oficinas"."OficinaPadre" IS 'Referencia a oficina padre para jerarquía';


--
-- Name: Oficinas_TRD; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Oficinas_TRD" (
    "Cod" bigint NOT NULL,
    "Oficina" bigint NOT NULL,
    "TRD" bigint NOT NULL,
    "Entidad" bigint NOT NULL,
    "PuedeEditar" boolean DEFAULT false NOT NULL,
    "PuedeEliminar" boolean DEFAULT false NOT NULL,
    "FechaAsignacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Oficinas_TRD"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Oficinas_TRD" IS 'Asignación de Series/Subseries (TRD) a Oficinas con permisos';


--
-- Name: COLUMN "Oficinas_TRD"."PuedeEditar"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Oficinas_TRD"."PuedeEditar" IS 'Permiso para editar documentos en esta Serie';


--
-- Name: COLUMN "Oficinas_TRD"."PuedeEliminar"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Oficinas_TRD"."PuedeEliminar" IS 'Permiso para eliminar documentos en esta Serie';


--
-- Name: Origenes_Documentos; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Origenes_Documentos" (
    "Cod" character varying(20) NOT NULL,
    "Nombre" character varying(100),
    "Descripcion" character varying(500)
);


--
-- Name: TABLE "Origenes_Documentos"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Origenes_Documentos" IS 'Origen del documento digital';


--
-- Name: Prestamos_Expedientes; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Prestamos_Expedientes" (
    "Cod" bigint NOT NULL,
    "Carpeta" bigint NOT NULL,
    "SolicitadoPor" bigint NOT NULL,
    "AutorizadoPor" bigint,
    "FechaSolicitud" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "FechaAutorizacion" timestamp without time zone,
    "FechaPrestamo" timestamp without time zone,
    "FechaDevolucionEsperada" timestamp without time zone,
    "FechaDevolucionReal" timestamp without time zone,
    "Estado" character varying(20) DEFAULT 'SOLICITADO'::character varying NOT NULL,
    "Motivo" character varying(500),
    "Observaciones" text,
    "ObservacionesDevolucion" character varying(500)
);


--
-- Name: TABLE "Prestamos_Expedientes"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Prestamos_Expedientes" IS 'Control de préstamos de expedientes físicos';


--
-- Name: Prestamos_Expedientes_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Prestamos_Expedientes_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Prestamos_Expedientes_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Prestamos_Expedientes_Cod_seq" OWNED BY documentos."Prestamos_Expedientes"."Cod";


--
-- Name: Prioridades_Radicado; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Prioridades_Radicado" (
    "Cod" character varying(20) NOT NULL,
    "Nombre" character varying(100),
    "DiasReduccion" integer,
    "Color" character varying(50),
    "Orden" integer
);


--
-- Name: TABLE "Prioridades_Radicado"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Prioridades_Radicado" IS 'Niveles de prioridad para radicados';


--
-- Name: COLUMN "Prioridades_Radicado"."DiasReduccion"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Prioridades_Radicado"."DiasReduccion" IS 'Días que se reducen del término normal';


--
-- Name: Radicados; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Radicados" (
    "Cod" bigint NOT NULL,
    "NumeroRadicado" character varying(30) NOT NULL,
    "AnioRadicado" integer NOT NULL,
    "ConsecutivoAnio" integer NOT NULL,
    "TipoComunicacion" character varying(10) NOT NULL,
    "TipoSolicitud" character varying(20),
    "Prioridad" character varying(10) DEFAULT 'NORMAL'::character varying NOT NULL,
    "Confidencial" boolean DEFAULT false NOT NULL,
    "FechaRadicacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "HoraRadicacion" time without time zone,
    "UsuarioRadica" bigint NOT NULL,
    "OficinaRadica" bigint,
    "MedioRecepcion" character varying(20),
    "Tercero" bigint,
    "OficinaRemitente" bigint,
    "UsuarioRemitente" bigint,
    "NombreRemitente" character varying(300),
    "CorreoRemitente" character varying(200),
    "TelefonoRemitente" character varying(50),
    "DireccionRemitente" character varying(300),
    "OficinaDestino" bigint,
    "UsuarioDestino" bigint,
    "TerceroDestino" bigint,
    "NombreDestinatario" character varying(300),
    "Asunto" character varying(500) NOT NULL,
    "Descripcion" text,
    "Folios" integer,
    "Anexos" character varying(500),
    "PalabrasClave" character varying(500),
    "FechaVencimiento" timestamp without time zone,
    "FechaProrroga" timestamp without time zone,
    "MotivoProrroga" character varying(500),
    "Estado" character varying(20) DEFAULT 'RADICADO'::character varying NOT NULL,
    "FechaCambioEstado" timestamp without time zone,
    "FechaRespuesta" timestamp without time zone,
    "RadicadoRespuesta" bigint,
    "ExpedienteVinculado" bigint,
    "FechaArchivado" timestamp without time zone,
    "RadicadoPadre" bigint,
    "TipoRelacion" character varying(20),
    "NumeroReferencia" character varying(100),
    "FechaDocumento" date,
    "NotificadoPorCorreo" boolean DEFAULT false NOT NULL,
    "FechaNotificacionCorreo" timestamp without time zone,
    "NotificadoPorSMS" boolean DEFAULT false NOT NULL,
    "FechaNotificacionSMS" timestamp without time zone,
    "Entidad" bigint NOT NULL,
    "FechaCreacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "FechaModificacion" timestamp without time zone,
    "ModificadoPor" bigint,
    "Activo" boolean DEFAULT true NOT NULL,
    "DatosAdicionales" text
);


--
-- Name: TABLE "Radicados"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Radicados" IS 'Comunicaciones oficiales según Acuerdo 060 AGN - Ventanilla Única';


--
-- Name: COLUMN "Radicados"."NumeroRadicado"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Radicados"."NumeroRadicado" IS 'Formato: {TipoCom}-{Año}-{Consecutivo} Ej: E-2025-000001';


--
-- Name: COLUMN "Radicados"."FechaVencimiento"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Radicados"."FechaVencimiento" IS 'Calculada según Tipo de Solicitud y días hábiles';


--
-- Name: COLUMN "Radicados"."ExpedienteVinculado"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Radicados"."ExpedienteVinculado" IS 'Carpeta donde se archiva el radicado';


--
-- Name: Radicados_Anexos; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Radicados_Anexos" (
    "Cod" bigint NOT NULL,
    "Radicado" bigint NOT NULL,
    "Entidad" bigint NOT NULL,
    "Nombre" character varying(200) NOT NULL,
    "Descripcion" character varying(500),
    "Ruta" character varying(2000),
    "Hash" character varying(64),
    "Tamaño" character varying(50),
    "Formato" character varying(50),
    "NumeroFolios" integer,
    "TipoAnexo" character varying(20),
    "Orden" integer DEFAULT 1 NOT NULL,
    "FechaCarga" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "SubidoPor" bigint,
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Radicados_Anexos"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Radicados_Anexos" IS 'Archivos adjuntos a las comunicaciones radicadas';


--
-- Name: Radicados_Anexos_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Radicados_Anexos_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Radicados_Anexos_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Radicados_Anexos_Cod_seq" OWNED BY documentos."Radicados_Anexos"."Cod";


--
-- Name: Radicados_Trazabilidad; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Radicados_Trazabilidad" (
    "Cod" bigint NOT NULL,
    "Radicado" bigint NOT NULL,
    "Entidad" bigint NOT NULL,
    "Accion" character varying(50) NOT NULL,
    "Descripcion" character varying(500),
    "OficinaOrigen" bigint,
    "UsuarioOrigen" bigint,
    "OficinaDestino" bigint,
    "UsuarioDestino" bigint,
    "EstadoAnterior" character varying(20),
    "EstadoNuevo" character varying(20),
    "Fecha" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "IP" character varying(50),
    "Observaciones" text
);


--
-- Name: TABLE "Radicados_Trazabilidad"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Radicados_Trazabilidad" IS 'Trazabilidad completa de movimientos de radicados';


--
-- Name: COLUMN "Radicados_Trazabilidad"."Accion"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Radicados_Trazabilidad"."Accion" IS 'RADICADO, ASIGNADO, TRASLADADO, DEVUELTO, RESPONDIDO, ARCHIVADO, etc.';


--
-- Name: Radicados_Trazabilidad_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Radicados_Trazabilidad_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Radicados_Trazabilidad_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Radicados_Trazabilidad_Cod_seq" OWNED BY documentos."Radicados_Trazabilidad"."Cod";


--
-- Name: Roles; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Roles" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(50)
);


--
-- Name: TABLE "Roles"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Roles" IS 'Catálogo de roles del sistema';


--
-- Name: Roles_Usuarios; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Roles_Usuarios" (
    "Cod" bigint NOT NULL,
    "Oficina" bigint NOT NULL,
    "Rol" bigint,
    "Usuario" bigint,
    "Estado" boolean DEFAULT true NOT NULL,
    "FechaInicio" timestamp without time zone,
    "FechaFin" timestamp without time zone
);


--
-- Name: TABLE "Roles_Usuarios"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Roles_Usuarios" IS 'Asignación de roles a usuarios en cada oficina';


--
-- Name: Sectores; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Sectores" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(100),
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Sectores"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Sectores" IS 'Sectores económicos/administrativos de las entidades';


--
-- Name: Solicitudes_Firma; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Solicitudes_Firma" (
    "Cod" bigint NOT NULL,
    "Entidad" bigint NOT NULL,
    "Archivo" bigint NOT NULL,
    "TipoFirma" character varying(20) NOT NULL,
    "OrdenSecuencial" boolean DEFAULT false,
    "RequiereTodos" boolean DEFAULT true,
    "MinimoFirmantes" smallint DEFAULT 1,
    "Estado" character varying(20) DEFAULT 'PENDIENTE'::character varying,
    "SolicitadoPor" bigint NOT NULL,
    "FechaSolicitud" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "FechaVencimiento" timestamp without time zone,
    "FechaCompletada" timestamp without time zone,
    "FechaCancelada" timestamp without time zone,
    "CanceladaPor" bigint,
    "MotivoCancelacion" character varying(500),
    "Asunto" character varying(200) NOT NULL,
    "Mensaje" text,
    "HashOriginal" character varying(64) NOT NULL,
    "AlgoritmoHash" character varying(20) DEFAULT 'SHA256'::character varying,
    "HashFinal" character varying(64),
    "CodigoVerificacion" character varying(50),
    "DatosAdicionales" jsonb,
    CONSTRAINT "CK_Solicitud_Estado" CHECK ((("Estado")::text = ANY ((ARRAY['PENDIENTE'::character varying, 'EN_PROCESO'::character varying, 'COMPLETADA'::character varying, 'CANCELADA'::character varying, 'RECHAZADA'::character varying, 'VENCIDA'::character varying])::text[]))),
    CONSTRAINT "CK_Solicitud_TipoFirma" CHECK ((("TipoFirma")::text = ANY ((ARRAY['SIMPLE'::character varying, 'AVANZADA'::character varying])::text[])))
);


--
-- Name: Solicitudes_Firma_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Solicitudes_Firma_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Solicitudes_Firma_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Solicitudes_Firma_Cod_seq" OWNED BY documentos."Solicitudes_Firma"."Cod";


--
-- Name: Soportes_Documento; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Soportes_Documento" (
    "Cod" character varying(20) NOT NULL,
    "Nombre" character varying(100),
    "Descripcion" character varying(500)
);


--
-- Name: TABLE "Soportes_Documento"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Soportes_Documento" IS 'Soporte físico del documento';


--
-- Name: Tablas_Retencion_Documental; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tablas_Retencion_Documental" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(200),
    "Tipo" character varying(50),
    "MesesVigencia" bigint,
    "Codigo" character varying(20),
    "TRDPadre" bigint,
    "Entidad" bigint,
    "DisposicionFinal" character varying(50),
    "TiempoGestion" bigint,
    "TiempoCentral" bigint,
    "Procedimiento" character varying(500),
    "Estado" boolean DEFAULT true NOT NULL,
    "Descripcion" character varying(500),
    "CreadoPor" bigint,
    "FechaCreacion" timestamp without time zone DEFAULT now(),
    "ModificadoPor" bigint,
    "FechaModificacion" timestamp without time zone
);


--
-- Name: TABLE "Tablas_Retencion_Documental"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Tablas_Retencion_Documental" IS 'Tablas de Retención Documental según Acuerdo 042 AGN';


--
-- Name: COLUMN "Tablas_Retencion_Documental"."TRDPadre"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Tablas_Retencion_Documental"."TRDPadre" IS 'NULL para Series, Cod de Serie padre para Subseries';


--
-- Name: COLUMN "Tablas_Retencion_Documental"."DisposicionFinal"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Tablas_Retencion_Documental"."DisposicionFinal" IS 'CT=Conservar, E=Eliminar, S=Seleccionar, M=Microfilmar, D=Digitalizar';


--
-- Name: Terceros; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Terceros" (
    "Cod" bigint NOT NULL,
    "TipoPersona" character varying(20) NOT NULL,
    "TipoDocumento" character varying(10),
    "NumeroDocumento" character varying(20),
    "RazonSocial" character varying(300),
    "Nombres" character varying(100),
    "Apellidos" character varying(100),
    "NombreCompleto" character varying(400) GENERATED ALWAYS AS (
CASE
    WHEN (("TipoPersona")::text = 'NATURAL'::text) THEN (TRIM(BOTH FROM (((COALESCE("Nombres", ''::character varying))::text || ' '::text) || (COALESCE("Apellidos", ''::character varying))::text)))::character varying
    ELSE "RazonSocial"
END) STORED,
    "Direccion" character varying(300),
    "Ciudad" character varying(100),
    "Departamento" character varying(100),
    "Pais" character varying(100) DEFAULT 'Colombia'::character varying,
    "CodigoPostal" character varying(20),
    "Telefono" character varying(50),
    "Celular" character varying(50),
    "Correo" character varying(200),
    "CorreoAlternativo" character varying(200),
    "RepresentanteLegal" character varying(200),
    "NotificarPorCorreo" boolean DEFAULT true NOT NULL,
    "NotificarPorSMS" boolean DEFAULT false NOT NULL,
    "Observaciones" text,
    "Entidad" bigint NOT NULL,
    "FechaCreacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreadoPor" bigint,
    "FechaModificacion" timestamp without time zone,
    "ModificadoPor" bigint,
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Terceros"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Terceros" IS 'Ciudadanos, empresas y entidades externas para correspondencia';


--
-- Name: COLUMN "Terceros"."TipoPersona"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Terceros"."TipoPersona" IS 'NATURAL o JURIDICA';


--
-- Name: COLUMN "Terceros"."NombreCompleto"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Terceros"."NombreCompleto" IS 'Campo calculado: Nombre completo o Razón Social';


--
-- Name: Tipos_Archivos; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tipos_Archivos" (
    "Cod" bigint,
    "Nombre" character varying(200),
    "Estado" boolean DEFAULT true
);


--
-- Name: TABLE "Tipos_Archivos"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Tipos_Archivos" IS 'Catálogo de tipos de archivo (Documento, Imagen, Video, etc.)';


--
-- Name: Tipos_Carpetas; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tipos_Carpetas" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(200),
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Tipos_Carpetas"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Tipos_Carpetas" IS 'Catálogo de tipos de carpeta: 1=Serie, 2=Subserie, 3=Expediente, 4=Genérica';


--
-- Name: Tipos_Comunicacion; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tipos_Comunicacion" (
    "Cod" character varying(10) NOT NULL,
    "Nombre" character varying(50),
    "Descripcion" character varying(200),
    "RequiereRespuesta" boolean DEFAULT true NOT NULL,
    "Prefijo" character varying(5)
);


--
-- Name: TABLE "Tipos_Comunicacion"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Tipos_Comunicacion" IS 'Tipos de comunicación oficial según Acuerdo 060 AGN';


--
-- Name: Tipos_Documentales; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tipos_Documentales" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(100),
    "Descripcion" character varying(500),
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Tipos_Documentales"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Tipos_Documentales" IS 'Catálogo de tipos documentales según normativa AGN';


--
-- Name: Tipos_Documento_Identidad; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tipos_Documento_Identidad" (
    "Cod" character varying(10) NOT NULL,
    "Nombre" character varying(100),
    "Pais" character varying(50)
);


--
-- Name: TABLE "Tipos_Documento_Identidad"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Tipos_Documento_Identidad" IS 'Tipos de documento de identificación';


--
-- Name: Tipos_Entidad; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tipos_Entidad" (
    "Cod" character varying(20) NOT NULL,
    "Nombre" character varying(100)
);


--
-- Name: TABLE "Tipos_Entidad"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Tipos_Entidad" IS 'Clasificación de entidades (Pública, Privada, Mixta)';


--
-- Name: Tipos_Firma; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tipos_Firma" (
    "Cod" character varying(20) NOT NULL,
    "Nombre" character varying(100),
    "Descripcion" character varying(500),
    "NivelSeguridad" integer
);


--
-- Name: TABLE "Tipos_Firma"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Tipos_Firma" IS 'Tipos de firma electrónica según normativa colombiana';


--
-- Name: Tipos_Notificacion; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tipos_Notificacion" (
    "Cod" character varying(30) NOT NULL,
    "Nombre" character varying(100) NOT NULL,
    "Descripcion" character varying(500),
    "Icono" character varying(50),
    "Color" character varying(20),
    "EnviarCorreo" boolean DEFAULT true,
    "RequiereAccion" boolean DEFAULT false,
    "Plantilla" text,
    "Estado" boolean DEFAULT true
);


--
-- Name: Tipos_Solicitud; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Tipos_Solicitud" (
    "Cod" character varying(20) NOT NULL,
    "Nombre" character varying(100),
    "Descripcion" character varying(500),
    "DiasHabilesRespuesta" integer NOT NULL,
    "DiasHabilesProrroga" integer,
    "EsCalendario" boolean DEFAULT false NOT NULL,
    "RequiereRespuestaEscrita" boolean DEFAULT true NOT NULL,
    "Normativa" character varying(200),
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Tipos_Solicitud"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Tipos_Solicitud" IS 'Tipos de solicitud con términos legales según normativa colombiana';


--
-- Name: COLUMN "Tipos_Solicitud"."EsCalendario"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Tipos_Solicitud"."EsCalendario" IS 'true=días calendario, false=días hábiles';


--
-- Name: Transferencias_Detalle; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Transferencias_Detalle" (
    "Cod" bigint NOT NULL,
    "Transferencia" bigint NOT NULL,
    "Carpeta" bigint NOT NULL,
    "Observaciones" character varying(500)
);


--
-- Name: Transferencias_Detalle_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Transferencias_Detalle_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Transferencias_Detalle_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Transferencias_Detalle_Cod_seq" OWNED BY documentos."Transferencias_Detalle"."Cod";


--
-- Name: Transferencias_Documentales; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Transferencias_Documentales" (
    "Cod" bigint NOT NULL,
    "Entidad" bigint NOT NULL,
    "OficinaOrigen" bigint NOT NULL,
    "TipoTransferencia" character varying(20) NOT NULL,
    "FechaSolicitud" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "FechaAprobacion" timestamp without time zone,
    "FechaEjecucion" timestamp without time zone,
    "SolicitadoPor" bigint,
    "AprobadoPor" bigint,
    "Estado" character varying(20) DEFAULT 'SOLICITADA'::character varying NOT NULL,
    "NumeroExpedientes" integer,
    "NumeroFolios" integer,
    "Observaciones" text
);


--
-- Name: TABLE "Transferencias_Documentales"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Transferencias_Documentales" IS 'Transferencias de archivo de gestión a central/histórico';


--
-- Name: Transferencias_Documentales_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Transferencias_Documentales_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Transferencias_Documentales_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Transferencias_Documentales_Cod_seq" OWNED BY documentos."Transferencias_Documentales"."Cod";


--
-- Name: Usuarios; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Usuarios" (
    "Cod" bigint NOT NULL,
    "Nombres" character varying(200),
    "Apellidos" character varying(200),
    "Telefono" character varying(20),
    "Documento" character varying(20) NOT NULL,
    "Usuario" character varying(200) NOT NULL,
    "Contraseña" character varying(200),
    "Estado" boolean DEFAULT true,
    "Entidad" bigint NOT NULL,
    "Correo" character varying(200),
    "TipoDocumento" character varying(10),
    "Cargo" character varying(200),
    "Foto" bytea,
    "FechaCreacion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP,
    "FechaUltimoAcceso" timestamp without time zone,
    "FirmaDigitalRuta" character varying(500),
    "FirmaDigitalVence" timestamp without time zone,
    "IntentosFallidos" integer DEFAULT 0,
    "FechaBloqueo" timestamp without time zone,
    "DebeCambiarContrasena" boolean DEFAULT false,
    "FechaCambioContrasena" timestamp without time zone,
    "NotificacionesCorreo" boolean DEFAULT true,
    "OficinaPredeterminada" bigint
);


--
-- Name: TABLE "Usuarios"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Usuarios" IS 'Usuarios del sistema con información completa para gestión documental';


--
-- Name: COLUMN "Usuarios"."FirmaDigitalRuta"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Usuarios"."FirmaDigitalRuta" IS 'Ruta al certificado de firma digital';


--
-- Name: COLUMN "Usuarios"."DebeCambiarContrasena"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Usuarios"."DebeCambiarContrasena" IS 'Si debe cambiar contraseña en próximo login';


--
-- Name: Versiones_Archivos; Type: TABLE; Schema: documentos; Owner: -
--

CREATE TABLE documentos."Versiones_Archivos" (
    "Cod" bigint NOT NULL,
    "Archivo" bigint NOT NULL,
    "Version" integer NOT NULL,
    "Ruta" character varying(2000),
    "Hash" character varying(64),
    "Tamaño" character varying(50),
    "FechaVersion" timestamp without time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "CreadoPor" bigint,
    "Comentario" character varying(500),
    "EsVersionActual" boolean DEFAULT false NOT NULL,
    "Nombre" character varying(200),
    "AlgoritmoHash" character varying(20) DEFAULT 'SHA256'::character varying,
    "Formato" character varying(50),
    "NumeroHojas" smallint,
    "MotivoModificacion" character varying(200),
    "Estado" boolean DEFAULT true NOT NULL
);


--
-- Name: TABLE "Versiones_Archivos"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TABLE documentos."Versiones_Archivos" IS 'Historial completo de versiones de cada archivo';


--
-- Name: COLUMN "Versiones_Archivos"."EsVersionActual"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON COLUMN documentos."Versiones_Archivos"."EsVersionActual" IS 'Solo una versión por archivo debe ser true';


--
-- Name: V_Archivos_ConVersiones; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Archivos_ConVersiones" AS
 SELECT a."Cod",
    a."Nombre",
    a."Carpeta",
    a."Ruta",
    a."Hash",
    a."Tamaño",
    a."Formato",
    a."Version" AS "VersionActual",
    a."FechaIncorporacion",
    a."FechaModificacion",
    a."SubidoPor",
    (((us."Nombres")::text || ' '::text) || (us."Apellidos")::text) AS "SubidoPorNombre",
    a."Estado",
    ( SELECT count(*) AS count
           FROM documentos."Versiones_Archivos" v
          WHERE ((v."Archivo" = a."Cod") AND (v."Estado" = true))) AS "TotalVersiones",
    ( SELECT min(v."FechaVersion") AS min
           FROM documentos."Versiones_Archivos" v
          WHERE ((v."Archivo" = a."Cod") AND (v."Estado" = true))) AS "FechaPrimeraVersion",
    ( SELECT max(v."FechaVersion") AS max
           FROM documentos."Versiones_Archivos" v
          WHERE ((v."Archivo" = a."Cod") AND (v."Estado" = true))) AS "FechaUltimaVersion"
   FROM (documentos."Archivos" a
     LEFT JOIN documentos."Usuarios" us ON ((a."SubidoPor" = us."Cod")));


--
-- Name: VIEW "V_Archivos_ConVersiones"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON VIEW documentos."V_Archivos_ConVersiones" IS 'Archivos con información resumida de versiones';


--
-- Name: V_Archivos_IndiceElectronico; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Archivos_IndiceElectronico" AS
 SELECT a."Cod",
    a."Nombre",
    a."Descripcion",
    a."CodigoDocumento",
    a."Carpeta",
    a."Indice" AS "OrdenEnExpediente",
    a."PaginaInicio",
    a."PaginaFin",
    a."NumeroHojas",
    a."FechaDocumento",
    a."FechaIncorporacion",
    a."TipoDocumental",
    td."Nombre" AS "TipoDocumentalNombre",
    a."TipoArchivo",
    ta."Nombre" AS "TipoArchivoNombre",
    a."Formato",
    a."Tamaño",
    a."OrigenDocumento",
    od."Nombre" AS "OrigenDocumentoNombre",
    a."Hash",
    a."AlgoritmoHash",
    a."Version",
    a."Firmado",
    a."TipoFirma",
    tf."Nombre" AS "TipoFirmaNombre",
    a."FechaFirma",
    a."FirmadoPor",
    a."SubidoPor",
    (((us."Nombres")::text || ' '::text) || (us."Apellidos")::text) AS "SubidoPorNombre",
    a."Ruta",
    a."Observaciones",
    a."FechaVerificacionIntegridad",
    a."ResultadoVerificacion",
    a."Estado"
   FROM (((((documentos."Archivos" a
     LEFT JOIN documentos."Tipos_Documentales" td ON ((a."TipoDocumental" = td."Cod")))
     LEFT JOIN documentos."Tipos_Archivos" ta ON ((a."TipoArchivo" = ta."Cod")))
     LEFT JOIN documentos."Origenes_Documentos" od ON (((a."OrigenDocumento")::text = (od."Cod")::text)))
     LEFT JOIN documentos."Tipos_Firma" tf ON (((a."TipoFirma")::text = (tf."Cod")::text)))
     LEFT JOIN documentos."Usuarios" us ON ((a."SubidoPor" = us."Cod")))
  WHERE (a."Estado" = true);


--
-- Name: VIEW "V_Archivos_IndiceElectronico"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON VIEW documentos."V_Archivos_IndiceElectronico" IS 'Archivos con todos los campos requeridos para el índice electrónico según Acuerdo 002/2014 AGN';


--
-- Name: V_Dashboard_Firmas; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Dashboard_Firmas" AS
 SELECT "Entidad",
    count(*) AS "Total",
    count(*) FILTER (WHERE (("Estado")::text = 'PENDIENTE'::text)) AS "Pendientes",
    count(*) FILTER (WHERE (("Estado")::text = 'EN_PROCESO'::text)) AS "EnProceso",
    count(*) FILTER (WHERE (("Estado")::text = 'COMPLETADA'::text)) AS "Completadas",
    count(*) FILTER (WHERE (("Estado")::text = 'RECHAZADA'::text)) AS "Rechazadas",
    count(*) FILTER (WHERE (("Estado")::text = 'CANCELADA'::text)) AS "Canceladas",
    count(*) FILTER (WHERE (("Estado")::text = 'VENCIDA'::text)) AS "Vencidas",
    count(*) FILTER (WHERE ((("FechaVencimiento" >= CURRENT_TIMESTAMP) AND ("FechaVencimiento" <= (CURRENT_TIMESTAMP + '3 days'::interval))) AND (("Estado")::text = ANY ((ARRAY['PENDIENTE'::character varying, 'EN_PROCESO'::character varying])::text[])))) AS "ProximasVencer",
    count(*) FILTER (WHERE (("TipoFirma")::text = 'SIMPLE'::text)) AS "FirmasSimples",
    count(*) FILTER (WHERE (("TipoFirma")::text = 'AVANZADA'::text)) AS "FirmasAvanzadas"
   FROM documentos."Solicitudes_Firma" s
  GROUP BY "Entidad";


--
-- Name: V_Estadisticas_Radicados_Oficina; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Estadisticas_Radicados_Oficina" AS
 SELECT r."Entidad",
    r."OficinaDestino",
    o."Nombre" AS "OficinaNombre",
    r."TipoComunicacion",
    count(*) AS "TotalRadicados",
    sum(
        CASE
            WHEN ((r."Estado")::text = 'RADICADO'::text) THEN 1
            ELSE 0
        END) AS "PendientesAsignar",
    sum(
        CASE
            WHEN ((r."Estado")::text = 'ASIGNADO'::text) THEN 1
            ELSE 0
        END) AS "Asignados",
    sum(
        CASE
            WHEN ((r."Estado")::text = 'EN_TRAMITE'::text) THEN 1
            ELSE 0
        END) AS "EnTramite",
    sum(
        CASE
            WHEN ((r."Estado")::text = 'RESPONDIDO'::text) THEN 1
            ELSE 0
        END) AS "Respondidos",
    sum(
        CASE
            WHEN ((r."Estado")::text = 'ARCHIVADO'::text) THEN 1
            ELSE 0
        END) AS "Archivados",
    sum(
        CASE
            WHEN ((r."FechaVencimiento" < CURRENT_TIMESTAMP) AND ((r."Estado")::text <> ALL ((ARRAY['ARCHIVADO'::character varying, 'ANULADO'::character varying, 'RESPONDIDO'::character varying])::text[]))) THEN 1
            ELSE 0
        END) AS "Vencidos",
    sum(
        CASE
            WHEN (((r."FechaVencimiento" >= CURRENT_TIMESTAMP) AND (r."FechaVencimiento" <= (CURRENT_TIMESTAMP + '3 days'::interval))) AND ((r."Estado")::text <> ALL ((ARRAY['ARCHIVADO'::character varying, 'ANULADO'::character varying, 'RESPONDIDO'::character varying])::text[]))) THEN 1
            ELSE 0
        END) AS "PorVencer"
   FROM (documentos."Radicados" r
     LEFT JOIN documentos."Oficinas" o ON (((r."OficinaDestino" = o."Cod") AND (r."Entidad" = o."Entidad"))))
  WHERE ((r."Activo" = true) AND ((r."AnioRadicado")::numeric = EXTRACT(year FROM CURRENT_DATE)))
  GROUP BY r."Entidad", r."OficinaDestino", o."Nombre", r."TipoComunicacion";


--
-- Name: VIEW "V_Estadisticas_Radicados_Oficina"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON VIEW documentos."V_Estadisticas_Radicados_Oficina" IS 'Estadísticas de radicados por oficina y tipo de comunicación';


--
-- Name: V_Expedientes_Completo; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Expedientes_Completo" AS
 SELECT c."Cod",
    c."Nombre",
    c."Descripcion",
    c."CodigoExpediente",
    c."TipoCarpeta",
    tc."Nombre" AS "TipoCarpetaNombre",
    c."EstadoCarpeta",
    ec."Nombre" AS "EstadoCarpetaNombre",
    c."CodSerie",
    c."CodSubSerie",
    c."SerieRaiz",
    c."CarpetaPadre",
    c."FechaCreacion",
    c."FechaCierre",
    c."FechaDocumentoInicial",
    c."FechaDocumentoFinal",
    c."NumeroFolios",
    c."Tomo",
    c."TotalTomos",
    c."Soporte",
    sd."Nombre" AS "SoporteNombre",
    c."FrecuenciaConsulta",
    fc."Nombre" AS "FrecuenciaConsultaNombre",
    c."UbicacionFisica",
    c."NivelVisualizacion",
    nv."Nombre" AS "NivelVisualizacionNombre",
    c."Delegado",
    (((ud."Nombres")::text || ' '::text) || (ud."Apellidos")::text) AS "DelegadoNombre",
    c."CreadoPor",
    (((uc."Nombres")::text || ' '::text) || (uc."Apellidos")::text) AS "CreadoPorNombre",
    c."FechaModificacion",
    c."ModificadoPor",
    c."PalabrasClave",
    c."Observaciones",
    c."Estado",
    ( SELECT count(*) AS count
           FROM documentos."Archivos" a
          WHERE ((a."Carpeta" = c."Cod") AND (a."Estado" = true))) AS "TotalArchivos"
   FROM (((((((documentos."Carpetas" c
     LEFT JOIN documentos."Tipos_Carpetas" tc ON (((c."TipoCarpeta" = tc."Cod") AND (tc."Estado" = true))))
     LEFT JOIN documentos."Estados_Carpetas" ec ON ((c."EstadoCarpeta" = ec."Cod")))
     LEFT JOIN documentos."Soportes_Documento" sd ON (((c."Soporte")::text = (sd."Cod")::text)))
     LEFT JOIN documentos."Frecuencias_Consulta" fc ON (((c."FrecuenciaConsulta")::text = (fc."Cod")::text)))
     LEFT JOIN documentos."Niveles_Visualizacion_Carpetas" nv ON ((c."NivelVisualizacion" = nv."Cod")))
     LEFT JOIN documentos."Usuarios" ud ON ((c."Delegado" = ud."Cod")))
     LEFT JOIN documentos."Usuarios" uc ON ((c."CreadoPor" = uc."Cod")))
  WHERE ((c."TipoCarpeta" = ANY (ARRAY[(3)::bigint, (4)::bigint])) AND (c."Estado" = true));


--
-- Name: VIEW "V_Expedientes_Completo"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON VIEW documentos."V_Expedientes_Completo" IS 'Expedientes y Carpetas Genéricas con información completa para consultas';


--
-- Name: V_Firmas_Pendientes; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Firmas_Pendientes" AS
 SELECT f."Cod" AS "FirmanteCod",
    f."Usuario",
    f."Entidad",
    s."Cod" AS "SolicitudCod",
    s."Archivo" AS "ArchivoCod",
    a."Nombre" AS "ArchivoNombre",
    a."Formato" AS "ArchivoFormato",
    c."Nombre" AS "CarpetaNombre",
    s."TipoFirma",
    s."Asunto",
    s."Mensaje",
    s."FechaSolicitud",
    s."FechaVencimiento",
        CASE
            WHEN (s."FechaVencimiento" IS NULL) THEN NULL::integer
            WHEN (s."FechaVencimiento" < CURRENT_TIMESTAMP) THEN 0
            ELSE (EXTRACT(day FROM ((s."FechaVencimiento")::timestamp with time zone - CURRENT_TIMESTAMP)))::integer
        END AS "DiasParaVencer",
        CASE
            WHEN (s."FechaVencimiento" IS NULL) THEN 'SIN_LIMITE'::text
            WHEN (s."FechaVencimiento" < CURRENT_TIMESTAMP) THEN 'VENCIDO'::text
            WHEN (((s."FechaVencimiento")::timestamp with time zone - CURRENT_TIMESTAMP) <= '1 day'::interval) THEN 'URGENTE'::text
            WHEN (((s."FechaVencimiento")::timestamp with time zone - CURRENT_TIMESTAMP) <= '3 days'::interval) THEN 'PROXIMO'::text
            ELSE 'NORMAL'::text
        END AS "AlertaVencimiento",
    f."Orden",
    f."RolFirmante",
    f."Estado" AS "EstadoFirmante",
    s."SolicitadoPor",
    (((sol."Nombres")::text || ' '::text) || (sol."Apellidos")::text) AS "SolicitadoPorNombre",
    s."CodigoVerificacion"
   FROM ((((documentos."Firmantes_Solicitud" f
     JOIN documentos."Solicitudes_Firma" s ON ((f."Solicitud" = s."Cod")))
     JOIN documentos."Archivos" a ON ((s."Archivo" = a."Cod")))
     LEFT JOIN documentos."Carpetas" c ON ((a."Carpeta" = c."Cod")))
     LEFT JOIN documentos."Usuarios" sol ON (((s."SolicitadoPor" = sol."Cod") AND (s."Entidad" = sol."Entidad"))))
  WHERE (((f."Estado")::text = ANY ((ARRAY['PENDIENTE'::character varying, 'NOTIFICADO'::character varying])::text[])) AND ((s."Estado")::text = ANY ((ARRAY['PENDIENTE'::character varying, 'EN_PROCESO'::character varying])::text[])));


--
-- Name: V_Notificaciones_Usuario; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Notificaciones_Usuario" AS
 SELECT n."Cod",
    n."Entidad",
    n."UsuarioDestino",
    n."UsuarioOrigen",
    (((uo."Nombres")::text || ' '::text) || (uo."Apellidos")::text) AS "UsuarioOrigenNombre",
    n."TipoNotificacion",
    tn."Nombre" AS "TipoNotificacionNombre",
    tn."Icono",
    tn."Color",
    n."Titulo",
    n."Mensaje",
    n."Prioridad",
    n."FechaCreacion",
    n."FechaLeido",
    (n."FechaLeido" IS NULL) AS "NoLeido",
    n."Estado",
    n."RadicadoRef",
    n."ArchivoRef",
    n."CarpetaRef",
    n."SolicitudFirmaRef",
    n."RequiereAccion",
    n."FechaVencimientoAccion",
        CASE
            WHEN (n."FechaVencimientoAccion" IS NULL) THEN NULL::text
            WHEN (n."FechaVencimientoAccion" < CURRENT_TIMESTAMP) THEN 'VENCIDO'::text
            WHEN (((n."FechaVencimientoAccion")::timestamp with time zone - CURRENT_TIMESTAMP) <= '1 day'::interval) THEN 'URGENTE'::text
            WHEN (((n."FechaVencimientoAccion")::timestamp with time zone - CURRENT_TIMESTAMP) <= '3 days'::interval) THEN 'PROXIMO'::text
            ELSE 'NORMAL'::text
        END AS "AlertaVencimiento",
    n."UrlAccion"
   FROM ((documentos."Notificaciones" n
     JOIN documentos."Tipos_Notificacion" tn ON (((n."TipoNotificacion")::text = (tn."Cod")::text)))
     LEFT JOIN documentos."Usuarios" uo ON (((n."UsuarioOrigen" = uo."Cod") AND (n."Entidad" = uo."Entidad"))))
  WHERE ((n."Estado")::text <> ALL ((ARRAY['ARCHIVADA'::character varying, 'ELIMINADA'::character varying])::text[]));


--
-- Name: V_Oficinas_Compat; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Oficinas_Compat" AS
 SELECT "Cod",
    "Nombre",
    "Estado",
    "Entidad",
    "Icono",
    ( SELECT c."Cod"
           FROM (documentos."Oficinas_TRD" ot
             JOIN documentos."Carpetas" c ON (((c."TRD" = ot."TRD") AND (c."TipoCarpeta" = 1) AND (c."Estado" = true))))
          WHERE ((ot."Oficina" = o."Cod") AND (ot."Entidad" = o."Entidad") AND (ot."Estado" = true))
         LIMIT 1) AS "CodigoSerie"
   FROM documentos."Oficinas" o;


--
-- Name: VIEW "V_Oficinas_Compat"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON VIEW documentos."V_Oficinas_Compat" IS 'Vista de compatibilidad para código legacy que usa CodigoSerie directo';


--
-- Name: V_Oficinas_Jerarquia; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Oficinas_Jerarquia" AS
 WITH RECURSIVE "Oficinas_Arbol" AS (
         SELECT o."Cod",
            o."Nombre",
            o."Codigo",
            o."Sigla",
            o."OficinaPadre",
            o."Entidad",
            o."Responsable",
            o."NivelJerarquico",
            o."Estado",
            1 AS "Nivel",
            (o."Codigo")::character varying(200) AS "RutaCodigo",
            (o."Nombre")::character varying(500) AS "RutaNombre"
           FROM documentos."Oficinas" o
          WHERE ((o."OficinaPadre" IS NULL) AND (o."Estado" = true))
        UNION ALL
         SELECT o."Cod",
            o."Nombre",
            o."Codigo",
            o."Sigla",
            o."OficinaPadre",
            o."Entidad",
            o."Responsable",
            o."NivelJerarquico",
            o."Estado",
            (a."Nivel" + 1),
            ((((a."RutaCodigo")::text || '.'::text) || (o."Codigo")::text))::character varying(200) AS "varchar",
            ((((a."RutaNombre")::text || ' > '::text) || (o."Nombre")::text))::character varying(500) AS "varchar"
           FROM (documentos."Oficinas" o
             JOIN "Oficinas_Arbol" a ON (((o."OficinaPadre" = a."Cod") AND (o."Entidad" = a."Entidad"))))
          WHERE (o."Estado" = true)
        )
 SELECT "Cod",
    "Nombre",
    "Codigo",
    "Sigla",
    "OficinaPadre",
    "Entidad",
    "Responsable",
    "NivelJerarquico",
    "Nivel" AS "NivelCalculado",
    "RutaCodigo",
    "RutaNombre",
    "Estado"
   FROM "Oficinas_Arbol";


--
-- Name: VIEW "V_Oficinas_Jerarquia"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON VIEW documentos."V_Oficinas_Jerarquia" IS 'Árbol jerárquico de oficinas con rutas calculadas';


--
-- Name: V_TRD_Jerarquia; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_TRD_Jerarquia" AS
 WITH RECURSIVE "TRD_Arbol" AS (
         SELECT t."Cod",
            t."Nombre",
            t."Codigo",
            t."Tipo",
            t."TRDPadre",
            t."Entidad",
            t."MesesVigencia",
            t."TiempoGestion",
            t."TiempoCentral",
            t."DisposicionFinal",
            t."Procedimiento",
            t."Estado",
            1 AS "Nivel",
            (t."Codigo")::character varying(100) AS "RutaCodigo",
            (t."Nombre")::character varying(500) AS "RutaNombre"
           FROM documentos."Tablas_Retencion_Documental" t
          WHERE ((t."TRDPadre" IS NULL) AND (t."Estado" = true))
        UNION ALL
         SELECT t."Cod",
            t."Nombre",
            t."Codigo",
            t."Tipo",
            t."TRDPadre",
            t."Entidad",
            t."MesesVigencia",
            t."TiempoGestion",
            t."TiempoCentral",
            t."DisposicionFinal",
            t."Procedimiento",
            t."Estado",
            (a."Nivel" + 1),
            ((((a."RutaCodigo")::text || '.'::text) || (t."Codigo")::text))::character varying(100) AS "varchar",
            ((((a."RutaNombre")::text || ' > '::text) || (t."Nombre")::text))::character varying(500) AS "varchar"
           FROM (documentos."Tablas_Retencion_Documental" t
             JOIN "TRD_Arbol" a ON ((t."TRDPadre" = a."Cod")))
          WHERE (t."Estado" = true)
        )
 SELECT "Cod",
    "Nombre",
    "Codigo",
    "Tipo",
    "TRDPadre",
    "Entidad",
    "MesesVigencia",
    "TiempoGestion",
    "TiempoCentral",
    "DisposicionFinal",
    "Procedimiento",
    "Nivel",
    "RutaCodigo",
    "RutaNombre",
        CASE
            WHEN ("Nivel" = 1) THEN 'Serie'::text
            ELSE 'Subserie'::text
        END AS "TipoNivel"
   FROM "TRD_Arbol";


--
-- Name: VIEW "V_TRD_Jerarquia"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON VIEW documentos."V_TRD_Jerarquia" IS 'Árbol completo de TRD con rutas y niveles
   calculados';


--
-- Name: V_Oficinas_Series; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Oficinas_Series" AS
 SELECT o."Cod" AS "OficinaCod",
    o."Nombre" AS "OficinaNombre",
    o."Entidad",
    ot."TRD" AS "TRDCod",
    t."Nombre" AS "TRDNombre",
    t."Codigo" AS "TRDCodigo",
    t."TRDPadre",
    tj."RutaCodigo",
    tj."RutaNombre",
    tj."TipoNivel",
    c."Cod" AS "CarpetaCod",
    c."Nombre" AS "CarpetaNombre",
    c."TipoCarpeta",
    ot."PuedeEditar",
    ot."PuedeEliminar"
   FROM ((((documentos."Oficinas" o
     JOIN documentos."Oficinas_TRD" ot ON (((o."Cod" = ot."Oficina") AND (o."Entidad" = ot."Entidad") AND (ot."Estado" = true))))
     JOIN documentos."Tablas_Retencion_Documental" t ON (((ot."TRD" = t."Cod") AND (t."Estado" = true))))
     JOIN documentos."V_TRD_Jerarquia" tj ON ((t."Cod" = tj."Cod")))
     LEFT JOIN documentos."Carpetas" c ON (((c."TRD" = t."Cod") AND (c."TipoCarpeta" = ANY (ARRAY[(1)::bigint, (2)::bigint])) AND (c."Estado" = true))))
  WHERE (o."Estado" = true);


--
-- Name: VIEW "V_Oficinas_Series"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON VIEW documentos."V_Oficinas_Series" IS 'Series y Subseries accesibles por cada 
  oficina con permisos';


--
-- Name: V_Radicados_Completo; Type: VIEW; Schema: documentos; Owner: -
--

CREATE VIEW documentos."V_Radicados_Completo" AS
 SELECT r."Cod",
    r."NumeroRadicado",
    r."AnioRadicado",
    r."TipoComunicacion",
    tc."Nombre" AS "TipoComunicacionNombre",
    r."TipoSolicitud",
    ts."Nombre" AS "TipoSolicitudNombre",
    ts."DiasHabilesRespuesta",
    r."Prioridad",
    pr."Nombre" AS "PrioridadNombre",
    pr."Color" AS "PrioridadColor",
    r."Confidencial",
    r."FechaRadicacion",
    r."UsuarioRadica",
    (((ur."Nombres")::text || ' '::text) || (ur."Apellidos")::text) AS "UsuarioRadicaNombre",
    r."OficinaRadica",
    ofr."Nombre" AS "OficinaRadicaNombre",
    r."MedioRecepcion",
    mr."Nombre" AS "MedioRecepcionNombre",
    r."Tercero",
    t."NombreCompleto" AS "TerceroNombre",
    r."NombreRemitente",
    r."CorreoRemitente",
    r."OficinaDestino",
    ofd."Nombre" AS "OficinaDestinoNombre",
    r."UsuarioDestino",
    (((ud."Nombres")::text || ' '::text) || (ud."Apellidos")::text) AS "UsuarioDestinoNombre",
    r."Asunto",
    r."Descripcion",
    r."Folios",
    r."Anexos",
    r."FechaVencimiento",
        CASE
            WHEN (r."FechaVencimiento" IS NULL) THEN NULL::integer
            WHEN (CURRENT_TIMESTAMP > r."FechaVencimiento") THEN 0
            ELSE (EXTRACT(day FROM ((r."FechaVencimiento")::timestamp with time zone - CURRENT_TIMESTAMP)))::integer
        END AS "DiasParaVencer",
        CASE
            WHEN (r."FechaVencimiento" IS NULL) THEN 'SIN_TERMINO'::text
            WHEN (r."FechaVencimiento" < CURRENT_TIMESTAMP) THEN 'VENCIDO'::text
            WHEN (((r."FechaVencimiento")::timestamp with time zone - CURRENT_TIMESTAMP) <= '2 days'::interval) THEN 'CRITICO'::text
            WHEN (((r."FechaVencimiento")::timestamp with time zone - CURRENT_TIMESTAMP) <= '5 days'::interval) THEN 'PROXIMO'::text
            ELSE 'NORMAL'::text
        END AS "AlertaVencimiento",
    r."Estado",
    er."Nombre" AS "EstadoNombre",
    er."Color" AS "EstadoColor",
    er."EsFinal" AS "EstadoEsFinal",
    r."FechaCambioEstado",
    r."FechaRespuesta",
    r."RadicadoRespuesta",
    rr."NumeroRadicado" AS "NumeroRadicadoRespuesta",
    r."ExpedienteVinculado",
    c."Nombre" AS "ExpedienteNombre",
    r."RadicadoPadre",
    rp."NumeroRadicado" AS "NumeroRadicadoPadre",
    r."TipoRelacion",
    r."NumeroReferencia",
    r."FechaDocumento",
    r."Entidad",
    r."Activo",
    ( SELECT count(*) AS count
           FROM documentos."Radicados_Anexos" ra
          WHERE ((ra."Radicado" = r."Cod") AND (ra."Entidad" = r."Entidad") AND (ra."Estado" = true))) AS "TotalAnexos",
    ( SELECT count(*) AS count
           FROM documentos."Radicados_Trazabilidad" rt
          WHERE ((rt."Radicado" = r."Cod") AND (rt."Entidad" = r."Entidad"))) AS "TotalMovimientos"
   FROM (((((((((((((documentos."Radicados" r
     LEFT JOIN documentos."Tipos_Comunicacion" tc ON (((r."TipoComunicacion")::text = (tc."Cod")::text)))
     LEFT JOIN documentos."Tipos_Solicitud" ts ON (((r."TipoSolicitud")::text = (ts."Cod")::text)))
     LEFT JOIN documentos."Prioridades_Radicado" pr ON (((r."Prioridad")::text = (pr."Cod")::text)))
     LEFT JOIN documentos."Estados_Radicado" er ON (((r."Estado")::text = (er."Cod")::text)))
     LEFT JOIN documentos."Medios_Recepcion" mr ON (((r."MedioRecepcion")::text = (mr."Cod")::text)))
     LEFT JOIN documentos."Usuarios" ur ON ((r."UsuarioRadica" = ur."Cod")))
     LEFT JOIN documentos."Oficinas" ofr ON (((r."OficinaRadica" = ofr."Cod") AND (r."Entidad" = ofr."Entidad"))))
     LEFT JOIN documentos."Terceros" t ON (((r."Tercero" = t."Cod") AND (r."Entidad" = t."Entidad"))))
     LEFT JOIN documentos."Oficinas" ofd ON (((r."OficinaDestino" = ofd."Cod") AND (r."Entidad" = ofd."Entidad"))))
     LEFT JOIN documentos."Usuarios" ud ON ((r."UsuarioDestino" = ud."Cod")))
     LEFT JOIN documentos."Radicados" rr ON (((r."RadicadoRespuesta" = rr."Cod") AND (r."Entidad" = rr."Entidad"))))
     LEFT JOIN documentos."Radicados" rp ON (((r."RadicadoPadre" = rp."Cod") AND (r."Entidad" = rp."Entidad"))))
     LEFT JOIN documentos."Carpetas" c ON ((r."ExpedienteVinculado" = c."Cod")));


--
-- Name: VIEW "V_Radicados_Completo"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON VIEW documentos."V_Radicados_Completo" IS 'Radicados con toda la información relacionada para consultas y reportes';


--
-- Name: Versiones_Archivos_Cod_seq; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."Versiones_Archivos_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Versiones_Archivos_Cod_seq; Type: SEQUENCE OWNED BY; Schema: documentos; Owner: -
--

ALTER SEQUENCE documentos."Versiones_Archivos_Cod_seq" OWNED BY documentos."Versiones_Archivos"."Cod";


--
-- Name: seq_Radicados; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."seq_Radicados"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: seq_Terceros; Type: SEQUENCE; Schema: documentos; Owner: -
--

CREATE SEQUENCE documentos."seq_Terceros"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: BasesDatos; Type: TABLE; Schema: usuarios; Owner: -
--

CREATE TABLE usuarios."BasesDatos" (
    "Cod" bigint NOT NULL,
    "Bd" character varying(200),
    "Pw" character varying(200),
    "Usu" character varying(200),
    "Host" character varying(200),
    "CantEntidades" bigint DEFAULT 0,
    "MaxEntidades" bigint DEFAULT 15
);


--
-- Name: TABLE "BasesDatos"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON TABLE usuarios."BasesDatos" IS 'Configuración de servidores de base de datos para multitenancy';


--
-- Name: COLUMN "BasesDatos"."Cod"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."BasesDatos"."Cod" IS 'Identificador único de la configuración de BD';


--
-- Name: COLUMN "BasesDatos"."Bd"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."BasesDatos"."Bd" IS 'Nombre de la base de datos';


--
-- Name: COLUMN "BasesDatos"."Pw"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."BasesDatos"."Pw" IS 'Contraseña de conexión (encriptada)';


--
-- Name: COLUMN "BasesDatos"."Usu"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."BasesDatos"."Usu" IS 'Usuario de conexión';


--
-- Name: COLUMN "BasesDatos"."Host"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."BasesDatos"."Host" IS 'Servidor de base de datos';


--
-- Name: COLUMN "BasesDatos"."CantEntidades"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."BasesDatos"."CantEntidades" IS 'Cantidad actual de entidades en esta BD';


--
-- Name: COLUMN "BasesDatos"."MaxEntidades"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."BasesDatos"."MaxEntidades" IS 'Máximo de entidades permitidas';


--
-- Name: BasesDatos_Cod_seq; Type: SEQUENCE; Schema: usuarios; Owner: -
--

CREATE SEQUENCE usuarios."BasesDatos_Cod_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: BasesDatos_Cod_seq; Type: SEQUENCE OWNED BY; Schema: usuarios; Owner: -
--

ALTER SEQUENCE usuarios."BasesDatos_Cod_seq" OWNED BY usuarios."BasesDatos"."Cod";


--
-- Name: Entidades; Type: TABLE; Schema: usuarios; Owner: -
--

CREATE TABLE usuarios."Entidades" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(200),
    "Telefono" character varying(20),
    "Nit" character varying(20),
    "Direccion" character varying(200),
    "Correo" character varying(200),
    "FechaLimite" date,
    "Bd" bigint,
    "Url" character varying(500),
    "Coleccion" character varying(100)
);


--
-- Name: TABLE "Entidades"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON TABLE usuarios."Entidades" IS 'Entidades registradas para autenticación y control de planes';


--
-- Name: COLUMN "Entidades"."Cod"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Entidades"."Cod" IS 'Código único de la entidad';


--
-- Name: COLUMN "Entidades"."FechaLimite"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Entidades"."FechaLimite" IS 'Fecha de vencimiento del plan activo';


--
-- Name: COLUMN "Entidades"."Bd"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Entidades"."Bd" IS 'Referencia a la configuración de BD asignada';


--
-- Name: COLUMN "Entidades"."Coleccion"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Entidades"."Coleccion" IS 'Identificador de colección (legacy Firebase)';


--
-- Name: Planes; Type: TABLE; Schema: usuarios; Owner: -
--

CREATE TABLE usuarios."Planes" (
    "Cod" bigint NOT NULL,
    "Nombre" character varying(200),
    "Valor" numeric(19,4),
    "NumeroUsuarios" bigint
);


--
-- Name: TABLE "Planes"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON TABLE usuarios."Planes" IS 'Catálogo de planes de suscripción disponibles';


--
-- Name: COLUMN "Planes"."Valor"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Planes"."Valor" IS 'Precio del plan en pesos colombianos';


--
-- Name: COLUMN "Planes"."NumeroUsuarios"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Planes"."NumeroUsuarios" IS 'Cantidad máxima de usuarios permitidos';


--
-- Name: Planes_Entidades; Type: TABLE; Schema: usuarios; Owner: -
--

CREATE TABLE usuarios."Planes_Entidades" (
    "Cod" bigint NOT NULL,
    "Entidad" bigint,
    "Plan" bigint,
    "Cantidad" numeric(19,4),
    "Valor" numeric(19,4),
    "Estado" boolean DEFAULT true,
    "FechaInicio" timestamp without time zone,
    "FechaFin" timestamp without time zone
);


--
-- Name: TABLE "Planes_Entidades"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON TABLE usuarios."Planes_Entidades" IS 'Historial de planes asignados a cada entidad';


--
-- Name: COLUMN "Planes_Entidades"."Estado"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Planes_Entidades"."Estado" IS 'true=Activo, false=Inactivo/Vencido';


--
-- Name: Usuarios; Type: TABLE; Schema: usuarios; Owner: -
--

CREATE TABLE usuarios."Usuarios" (
    "Cod" bigint NOT NULL,
    "Nombres" character varying(200),
    "Apellidos" character varying(200),
    "Telefono" character varying(20),
    "Documento" character varying(20),
    "Usuario" character varying(200) NOT NULL,
    "Contraseña" character varying(200),
    "Estado" boolean DEFAULT true,
    "Entidad" bigint NOT NULL,
    "SesionActiva" boolean DEFAULT false,
    "Token" character varying(2000)
);


--
-- Name: TABLE "Usuarios"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON TABLE usuarios."Usuarios" IS 'Usuarios para autenticación del sistema';


--
-- Name: COLUMN "Usuarios"."Usuario"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Usuarios"."Usuario" IS 'Nombre de usuario para login (único por entidad)';


--
-- Name: COLUMN "Usuarios"."Contraseña"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Usuarios"."Contraseña" IS 'Contraseña hasheada';


--
-- Name: COLUMN "Usuarios"."Estado"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Usuarios"."Estado" IS 'true=Activo, false=Inactivo';


--
-- Name: COLUMN "Usuarios"."SesionActiva"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Usuarios"."SesionActiva" IS 'Indica si tiene sesión activa actualmente';


--
-- Name: COLUMN "Usuarios"."Token"; Type: COMMENT; Schema: usuarios; Owner: -
--

COMMENT ON COLUMN usuarios."Usuarios"."Token" IS 'JWT token de sesión actual';


--
-- Name: seq_Entidades; Type: SEQUENCE; Schema: usuarios; Owner: -
--

CREATE SEQUENCE usuarios."seq_Entidades"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: seq_Planes; Type: SEQUENCE; Schema: usuarios; Owner: -
--

CREATE SEQUENCE usuarios."seq_Planes"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: seq_Planes_Entidades; Type: SEQUENCE; Schema: usuarios; Owner: -
--

CREATE SEQUENCE usuarios."seq_Planes_Entidades"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: seq_Usuarios; Type: SEQUENCE; Schema: usuarios; Owner: -
--

CREATE SEQUENCE usuarios."seq_Usuarios"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


--
-- Name: Certificados_Usuarios Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Certificados_Usuarios" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Certificados_Usuarios_Cod_seq"'::regclass);


--
-- Name: Codigos_OTP Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Codigos_OTP" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Codigos_OTP_Cod_seq"'::regclass);


--
-- Name: Configuracion_Firma Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Configuracion_Firma" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Configuracion_Firma_Cod_seq"'::regclass);


--
-- Name: ErrorLog Id; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."ErrorLog" ALTER COLUMN "Id" SET DEFAULT nextval('documentos."ErrorLog_Id_seq"'::regclass);


--
-- Name: Evidencias_Firma Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Evidencias_Firma" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Evidencias_Firma_Cod_seq"'::regclass);


--
-- Name: Firmantes_Solicitud Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Firmantes_Solicitud" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Firmantes_Solicitud_Cod_seq"'::regclass);


--
-- Name: Firmas_Archivos Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Firmas_Archivos" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Firmas_Archivos_Cod_seq"'::regclass);


--
-- Name: Historial_Acciones Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Historial_Acciones" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Historial_Acciones_Cod_seq"'::regclass);


--
-- Name: Notificaciones Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Notificaciones" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Notificaciones_Cod_seq"'::regclass);


--
-- Name: Prestamos_Expedientes Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Prestamos_Expedientes" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Prestamos_Expedientes_Cod_seq"'::regclass);


--
-- Name: Radicados_Anexos Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Radicados_Anexos" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Radicados_Anexos_Cod_seq"'::regclass);


--
-- Name: Radicados_Trazabilidad Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Radicados_Trazabilidad" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Radicados_Trazabilidad_Cod_seq"'::regclass);


--
-- Name: Solicitudes_Firma Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Solicitudes_Firma" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Solicitudes_Firma_Cod_seq"'::regclass);


--
-- Name: Transferencias_Detalle Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Transferencias_Detalle" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Transferencias_Detalle_Cod_seq"'::regclass);


--
-- Name: Transferencias_Documentales Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Transferencias_Documentales" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Transferencias_Documentales_Cod_seq"'::regclass);


--
-- Name: Versiones_Archivos Cod; Type: DEFAULT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Versiones_Archivos" ALTER COLUMN "Cod" SET DEFAULT nextval('documentos."Versiones_Archivos_Cod_seq"'::regclass);


--
-- Name: BasesDatos Cod; Type: DEFAULT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."BasesDatos" ALTER COLUMN "Cod" SET DEFAULT nextval('usuarios."BasesDatos_Cod_seq"'::regclass);


--
-- Data for Name: Archivos; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Archivos" ("Cod", "Nombre", "Carpeta", "Copia", "ArchivoOrginal", "Firmado", "FimarPor", "FirmadoPor", "Ruta", "TipoArchivo", "Formato", "NumeroHojas", "Duracion", "Tamaño", "Estado", "Indice", "FechaDocumento", "FechaIncorporacion", "OrigenDocumento", "Hash", "AlgoritmoHash", "TipoDocumental", "Observaciones", "SubidoPor", "FechaFirma", "TipoFirma", "CertificadoFirma", "PaginaInicio", "PaginaFin", "Version", "Descripcion", "FechaVerificacionIntegridad", "ResultadoVerificacion", "Metadatos", "CodigoDocumento", "FechaModificacion", "EstadoUpload", "ContentType") FROM stdin;
103	20251126150603354_page-0001.jpg	1	f	\N	f	\N	\N	entidad_1/carpeta_1/20260123184951_56d584ac_20251126150603354_page-0001.jpg	\N	\N	\N	\N	2553977	t	0	\N	2026-01-23 13:49:51.27474	\N	6ef3c49d0b0bd693bfe2c9083373484fa18560c0df2f99d70012979aa105841d	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:49:53.853215	COMPLETADO	image/jpeg
25	softsolutions20223-nublusoftapi-8a5edab282632443.txt	1	f	\N	f	\N	\N	entidad_1/carpeta_1/20260122225557_5ede35b2_softsolutions20223-nublusoftapi-8a5edab282632443.txt	\N	\N	\N	\N	1243529	t	0	\N	2026-01-22 17:55:57.372309	\N	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-22 17:55:59.380237	COMPLETADO	text/plain
26	NubluSoft_API_Completa_postman_collection.json	1	f	\N	f	\N	\N	entidad_1/carpeta_1/20260122230753_d334d864_NubluSoft_API_Completa_postman_collection.json	\N	\N	\N	\N	87419	t	0	\N	2026-01-22 18:07:54.077905	\N	df739a9011a5e680ccb18866e153b993c98b5976bee0ec9953b3a1dcbb79ca05	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-22 18:07:55.921636	COMPLETADO	application/json
27	MAPEO_APIS_INTERFACES.md	1	f	\N	f	\N	\N	entidad_1/carpeta_1/20260122233903_500e6768_MAPEO_APIS_INTERFACES.md	\N	\N	\N	\N	20797	t	0	\N	2026-01-22 18:39:03.429087	\N	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-22 18:39:05.364321	COMPLETADO	application/octet-stream
40	softsolutions20223-nublusoftapi-8a5edab282632443.txt	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181709_75a22077_softsolutions20223-nublusoftapi-8a5edab282632443.txt	\N	\N	\N	\N	1243529	f	0	\N	2026-01-23 13:17:10.263055	\N	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	text/plain
41	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181712_3fed34a2_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	\N	\N	\N	\N	22194	f	0	\N	2026-01-23 13:17:12.829834	\N	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	application/octet-stream
42	MAPEO_APIS_INTERFACES.md	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181714_648706d2_MAPEO_APIS_INTERFACES.md	\N	\N	\N	\N	20797	f	0	\N	2026-01-23 13:17:14.210575	\N	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	application/octet-stream
43	parte 2.docx	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181715_d23e3565_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-23 13:17:15.581339	\N	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	application/vnd.openxmlformats-officedocument.wordprocessingml.document
44	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181716_caa373a2_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	\N	\N	\N	\N	118729	f	0	\N	2026-01-23 13:17:16.910837	\N	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	text/plain
45	guia_firma_notificaciones_parte6.txt	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181718_84e3ec85_guia_firma_notificaciones_parte6.txt	\N	\N	\N	\N	28392	f	0	\N	2026-01-23 13:17:18.549195	\N	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	text/plain
46	guia_firma_notificaciones_parte3.txt	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181719_8c57f5cf_guia_firma_notificaciones_parte3.txt	\N	\N	\N	\N	25830	f	0	\N	2026-01-23 13:17:20.048168	\N	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	text/plain
47	guia_firma_notificaciones_parte5.txt	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181721_e98ee730_guia_firma_notificaciones_parte5.txt	\N	\N	\N	\N	17575	f	0	\N	2026-01-23 13:17:21.416493	\N	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	text/plain
48	guia_firma_notificaciones_parte4.txt	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181722_2d6e2669_guia_firma_notificaciones_parte4.txt	\N	\N	\N	\N	16681	f	0	\N	2026-01-23 13:17:22.801661	\N	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	text/plain
49	guia_firma_notificaciones_parte2.txt	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181724_ef57f446_guia_firma_notificaciones_parte2.txt	\N	\N	\N	\N	15890	f	0	\N	2026-01-23 13:17:24.099954	\N	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	text/plain
50	guia_firma_notificaciones_parte1.txt	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181725_2bd243fa_guia_firma_notificaciones_parte1.txt	\N	\N	\N	\N	14361	f	0	\N	2026-01-23 13:17:25.399703	\N	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	text/plain
51	analog-period-389922-7b9f19681b38.json	5	f	\N	f	\N	\N	entidad_1/carpeta_5/20260123181726_43352a57_analog-period-389922-7b9f19681b38.json	\N	\N	\N	\N	2388	f	0	\N	2026-01-23 13:17:26.821587	\N	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:58.888824	COMPLETADO	application/json
1	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260121065651_a789380c_parte 2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 01:56:52.198718	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
2	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122005347_d80b5e5d_parte 2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 19:53:48.019525	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
3	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122034318_9bb9b9d7_parte 2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 22:43:19.634973	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
4	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122034613_5034310e_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 22:46:13.85802	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
5	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122035505_950710d2_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 22:55:06.407104	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
6	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122040200_6d2ee147_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 23:02:01.676886	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
7	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122041333_22c2ffaf_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 23:13:34.136394	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
8	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122041636_f943b644_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 23:16:37.122549	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
9	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122042354_2ef5fb89_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 23:23:54.967098	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
10	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122043037_848bf899_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 23:30:37.967128	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
11	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122043840_2aaf1ab9_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 23:38:40.93034	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
12	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122043923_89c7e623_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-21 23:39:23.117186	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
13	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122050134_be72b98a_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-22 00:01:35.385562	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
14	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122050843_436e7e52_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-22 00:08:44.168455	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
15	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122051406_4904947f_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-22 00:14:07.418949	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
16	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122123744_df41e031_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-22 07:37:44.990794	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
17	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122124329_eb50632e_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-22 07:43:30.002982	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
18	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122131053_9082bca4_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-22 08:10:54.111463	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
19	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122131612_b65e5c4a_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-22 08:16:13.241469	\N	\N	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	PENDIENTE	application/vnd.openxmlformats-officedocument.wordprocessingml.document
20	parte 2.docx	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122134658_3d61e214_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-22 08:46:59.167065	\N	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/vnd.openxmlformats-officedocument.wordprocessingml.document
21	softsolutions20223-nublusoftapi-8a5edab282632443.txt	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122134808_a42888a9_softsolutions20223-nublusoftapi-8a5edab282632443.txt	\N	\N	\N	\N	1243529	f	0	\N	2026-01-22 08:48:08.365027	\N	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
22	Grabación de pantalla 2025-11-01 155406.mp4	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122134902_3aa74a1b_Grabación_de_pantalla_2025-11-01_155406.mp4	\N	\N	\N	\N	114191082	f	0	\N	2026-01-22 08:49:02.451382	\N	f93c843c59d534e44c6a61921fc215ea37eaabb8cb4c9d995e762a30560f44b0	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	video/mp4
23	guia_firma_notificaciones_parte6.txt	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122184452_9ebaec6e_guia_firma_notificaciones_parte6.txt	\N	\N	\N	\N	28392	f	0	\N	2026-01-22 13:44:52.448426	\N	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
24	SQLServer2022-x64-ESN-Dev.iso	2	f	\N	f	\N	\N	entidad_1/carpeta_2/20260122223437_6c5589d5_SQLServer2022-x64-ESN-Dev.iso	\N	\N	\N	\N	1363390464	f	0	\N	2026-01-22 17:34:37.768932	\N	90aae7672be0485274a1a0fd77d9102c38b80dddf60f47f50f01f3ed2211861f	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/octet-stream
28	softsolutions20223-nublusoftapi-8a5edab282632443.txt	3	f	\N	f	\N	\N	entidad_1/carpeta_1/20260122225557_5ede35b2_softsolutions20223-nublusoftapi-8a5edab282632443_copy_639047800763206655.txt	\N	\N	\N	\N	1243529	f	0	\N	2026-01-23 10:47:57.556076	\N	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
52	softsolutions20223-nublusoftapi-8a5edab282632443.txt	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182441_f8ebcef0_softsolutions20223-nublusoftapi-8a5edab282632443.txt	\N	\N	\N	\N	1243529	f	0	\N	2026-01-23 13:24:41.264247	\N	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
53	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182443_a00287cb_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	\N	\N	\N	\N	22194	f	0	\N	2026-01-23 13:24:44.025568	\N	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/octet-stream
54	MAPEO_APIS_INTERFACES.md	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182445_560a985c_MAPEO_APIS_INTERFACES.md	\N	\N	\N	\N	20797	f	0	\N	2026-01-23 13:24:45.434075	\N	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/octet-stream
55	parte 2.docx	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182446_560e8ba9_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-23 13:24:46.891697	\N	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/vnd.openxmlformats-officedocument.wordprocessingml.document
56	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182448_80e66570_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	\N	\N	\N	\N	118729	f	0	\N	2026-01-23 13:24:48.327984	\N	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
57	guia_firma_notificaciones_parte6.txt	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182449_efdf28b8_guia_firma_notificaciones_parte6.txt	\N	\N	\N	\N	28392	f	0	\N	2026-01-23 13:24:49.99492	\N	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
58	guia_firma_notificaciones_parte3.txt	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182451_bb7c5f6e_guia_firma_notificaciones_parte3.txt	\N	\N	\N	\N	25830	f	0	\N	2026-01-23 13:24:51.446168	\N	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
59	guia_firma_notificaciones_parte5.txt	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182452_fbe5dd0b_guia_firma_notificaciones_parte5.txt	\N	\N	\N	\N	17575	f	0	\N	2026-01-23 13:24:52.625499	\N	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
60	guia_firma_notificaciones_parte4.txt	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182453_0e73ba23_guia_firma_notificaciones_parte4.txt	\N	\N	\N	\N	16681	f	0	\N	2026-01-23 13:24:53.805635	\N	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
61	guia_firma_notificaciones_parte2.txt	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182454_67c57f4d_guia_firma_notificaciones_parte2.txt	\N	\N	\N	\N	15890	f	0	\N	2026-01-23 13:24:54.907917	\N	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
62	guia_firma_notificaciones_parte1.txt	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182455_8df4c057_guia_firma_notificaciones_parte1.txt	\N	\N	\N	\N	14361	f	0	\N	2026-01-23 13:24:56.038352	\N	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
63	analog-period-389922-7b9f19681b38.json	4	f	\N	f	\N	\N	entidad_1/carpeta_4/20260123182457_662749b7_analog-period-389922-7b9f19681b38.json	\N	\N	\N	\N	2388	f	0	\N	2026-01-23 13:24:57.158435	\N	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/json
64	softsolutions20223-nublusoftapi-8a5edab282632443.txt	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183555_3862b75c_softsolutions20223-nublusoftapi-8a5edab282632443.txt	\N	\N	\N	\N	1243529	f	0	\N	2026-01-23 13:35:56.12126	\N	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
65	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183558_f6a15804_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	\N	\N	\N	\N	22194	f	0	\N	2026-01-23 13:35:58.38058	\N	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/octet-stream
66	MAPEO_APIS_INTERFACES.md	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183559_23604b53_MAPEO_APIS_INTERFACES.md	\N	\N	\N	\N	20797	f	0	\N	2026-01-23 13:35:59.501367	\N	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/octet-stream
67	parte 2.docx	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183600_dac770b4_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-23 13:36:00.661969	\N	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/vnd.openxmlformats-officedocument.wordprocessingml.document
68	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183601_8ac79637_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	\N	\N	\N	\N	118729	f	0	\N	2026-01-23 13:36:01.732782	\N	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
69	guia_firma_notificaciones_parte6.txt	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183603_ccacff3c_guia_firma_notificaciones_parte6.txt	\N	\N	\N	\N	28392	f	0	\N	2026-01-23 13:36:03.031927	\N	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
70	guia_firma_notificaciones_parte3.txt	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183604_87c69052_guia_firma_notificaciones_parte3.txt	\N	\N	\N	\N	25830	f	0	\N	2026-01-23 13:36:04.129627	\N	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
71	guia_firma_notificaciones_parte5.txt	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183605_0f52052b_guia_firma_notificaciones_parte5.txt	\N	\N	\N	\N	17575	f	0	\N	2026-01-23 13:36:05.215336	\N	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
72	guia_firma_notificaciones_parte4.txt	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183606_b55f1b82_guia_firma_notificaciones_parte4.txt	\N	\N	\N	\N	16681	f	0	\N	2026-01-23 13:36:06.258751	\N	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
73	guia_firma_notificaciones_parte2.txt	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183607_adcd9830_guia_firma_notificaciones_parte2.txt	\N	\N	\N	\N	15890	f	0	\N	2026-01-23 13:36:07.33173	\N	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
74	guia_firma_notificaciones_parte1.txt	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183608_f107e16a_guia_firma_notificaciones_parte1.txt	\N	\N	\N	\N	14361	f	0	\N	2026-01-23 13:36:08.41089	\N	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
75	analog-period-389922-7b9f19681b38.json	6	f	\N	f	\N	\N	entidad_1/carpeta_6/20260123183609_ef51a73e_analog-period-389922-7b9f19681b38.json	\N	\N	\N	\N	2388	f	0	\N	2026-01-23 13:36:09.448146	\N	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/json
88	Solicitud de Vinculacion - ID  CC  29543587 - No Solicitud  1376549 -.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184505_11f0e4c5_Solicitud_de_Vinculacion_-_ID__CC__29543587_-_No_S.pdf	\N	\N	\N	\N	763170	f	0	\N	2026-01-23 13:45:06.077935	\N	79465a35f97d1a2634a164823f86fe723ea2dca12fb61441d390e9e8bf6cc48a	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
91	2118716946363 (2) (1).pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184512_f973bf7c_2118716946363_(2)_(1).pdf	\N	\N	\N	\N	573254	f	0	\N	2026-01-23 13:45:12.492892	\N	47a2d60b92ece8db98da0c07850c3befb935799cf32c22756946a37aff62187d	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
94	2118716946363.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184518_37b29920_2118716946363.pdf	\N	\N	\N	\N	569037	f	0	\N	2026-01-23 13:45:18.361584	\N	3012452b3c662cc38766b3c6b4d25215f54fe21991d66b2908e4af50f58e9032	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
76	softsolutions20223-nublusoftapi-8a5edab282632443.txt	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184042_77f71991_softsolutions20223-nublusoftapi-8a5edab282632443.txt	\N	\N	\N	\N	1243529	f	0	\N	2026-01-23 13:40:43.076907	\N	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
77	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184045_3d7bb3aa_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	\N	\N	\N	\N	22194	f	0	\N	2026-01-23 13:40:45.748504	\N	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/octet-stream
89	Autorizacion de Desembolso - ID  CC  29543587 - No Solicitud  1376549 -.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184508_22b492ac_Autorizacion_de_Desembolso_-_ID__CC__29543587_-_No.pdf	\N	\N	\N	\N	719342	f	0	\N	2026-01-23 13:45:08.656177	\N	4239aa69fa16d590330cbc49c4af2c7632e5952fc93a29d2eaf2f2fafdfb0a65	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
78	MAPEO_APIS_INTERFACES.md	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184046_3aa0765b_MAPEO_APIS_INTERFACES.md	\N	\N	\N	\N	20797	f	0	\N	2026-01-23 13:40:46.91525	\N	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/octet-stream
79	parte 2.docx	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184048_4624ff90_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-23 13:40:48.310856	\N	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/vnd.openxmlformats-officedocument.wordprocessingml.document
90	2118644388827.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184510_c832dfaa_2118644388827.pdf	\N	\N	\N	\N	573295	f	0	\N	2026-01-23 13:45:10.748947	\N	b0d267132359fd08ec1d50b413243d88aede35b84da1454846df0bea1d6e52b8	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
80	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184049_ea09f3b6_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	\N	\N	\N	\N	118729	f	0	\N	2026-01-23 13:40:49.706222	\N	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
81	guia_firma_notificaciones_parte6.txt	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184051_9468de7c_guia_firma_notificaciones_parte6.txt	\N	\N	\N	\N	28392	f	0	\N	2026-01-23 13:40:51.292453	\N	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
82	guia_firma_notificaciones_parte3.txt	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184053_e75a3e8e_guia_firma_notificaciones_parte3.txt	\N	\N	\N	\N	25830	f	0	\N	2026-01-23 13:40:53.309021	\N	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
92	2118716946363 (2).pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184514_d1f51a40_2118716946363_(2).pdf	\N	\N	\N	\N	573254	f	0	\N	2026-01-23 13:45:14.480866	\N	47a2d60b92ece8db98da0c07850c3befb935799cf32c22756946a37aff62187d	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
83	guia_firma_notificaciones_parte5.txt	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184054_9a58ef68_guia_firma_notificaciones_parte5.txt	\N	\N	\N	\N	17575	f	0	\N	2026-01-23 13:40:54.743469	\N	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
93	2118719064580.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184516_547aee1a_2118719064580.pdf	\N	\N	\N	\N	573127	f	0	\N	2026-01-23 13:45:16.416278	\N	942da43d7c4fee278b4c75d058644963b319636577afdc983cf92baf5968d296	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
84	guia_firma_notificaciones_parte4.txt	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184056_e995117c_guia_firma_notificaciones_parte4.txt	\N	\N	\N	\N	16681	f	0	\N	2026-01-23 13:40:56.307086	\N	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
85	guia_firma_notificaciones_parte2.txt	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184057_ad9dbe91_guia_firma_notificaciones_parte2.txt	\N	\N	\N	\N	15890	f	0	\N	2026-01-23 13:40:57.799298	\N	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
86	guia_firma_notificaciones_parte1.txt	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184059_bd4c1573_guia_firma_notificaciones_parte1.txt	\N	\N	\N	\N	14361	f	0	\N	2026-01-23 13:40:59.210778	\N	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
95	2118716946363 (1).pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184520_faff2ff4_2118716946363_(1).pdf	\N	\N	\N	\N	568799	f	0	\N	2026-01-23 13:45:20.33323	\N	2bfb2d4d50a8173442fe508f2813f8a97aa93a9966f981e885efacca10db4be9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
87	analog-period-389922-7b9f19681b38.json	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184100_cfed72b9_analog-period-389922-7b9f19681b38.json	\N	\N	\N	\N	2388	f	0	\N	2026-01-23 13:41:00.594025	\N	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/json
96	Solicitud de Productos - ID  CC  29543587 - No Solicitud  1376549 -.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184522_558d915b_Solicitud_de_Productos_-_ID__CC__29543587_-_No_Sol.pdf	\N	\N	\N	\N	542436	f	0	\N	2026-01-23 13:45:22.329397	\N	aafd037080e3441057261bc6560c446b8ba0ac48c7f4b2ee68258596f072b344	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
97	WhatsApp Image 2025-11-27 at 14.08.16.jpeg	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184524_22571c86_WhatsApp_Image_2025-11-27_at_14.08.16.jpeg	\N	\N	\N	\N	492103	f	0	\N	2026-01-23 13:45:24.364395	\N	ff75905a8667a632b179d43c821306c7a587235828fc439f78c9a730bba62c02	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	image/jpeg
98	4-1231756227932394029_Autorizacion_de_Desembolso.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184526_e61e77c6_4-1231756227932394029_Autorizacion_de_Desembolso.pdf	\N	\N	\N	\N	450184	f	0	\N	2026-01-23 13:45:26.232035	\N	54905f0544d3119c65aa89ff4e70b0e49c2cb12395704a317220ffae4316a4bd	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
99	T-2025142584-5840300.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184528_aca60db1_T-2025142584-5840300.pdf	\N	\N	\N	\N	373822	f	0	\N	2026-01-23 13:45:28.158106	\N	0214cca290728f4248bf92b3a7eebe89e673b25763667821b61821a62c489632	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
100	4-1231756227932394029E0909_Respuesta_1_RESP_FINAL_SFC.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184530_257e1bbd_4-1231756227932394029E0909_Respuesta_1_RESP_FINAL_.pdf	\N	\N	\N	\N	214032	f	0	\N	2026-01-23 13:45:30.077556	\N	02addf1c3f9f64c0d8de1ec5e11cab19244fbd118ce31ee586c6cdcdeae685bc	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
101	4-1231756227932394029_Respuesta_RESP_FINAL_SFC.pdf	8	f	\N	f	\N	\N	entidad_1/carpeta_8/20260123184531_4c75df1a_4-1231756227932394029_Respuesta_RESP_FINAL_SFC.pdf	\N	\N	\N	\N	214028	f	0	\N	2026-01-23 13:45:31.918643	\N	5fb087fd77878f41f485d30efede1f9e9e00d4542e92bd7568e7cf90cd134123	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/pdf
29	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180838_8b6e510e_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	\N	\N	\N	\N	22194	f	0	\N	2026-01-23 13:08:39.263607	\N	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/octet-stream
30	MAPEO_APIS_INTERFACES.md	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180841_066c531a_MAPEO_APIS_INTERFACES.md	\N	\N	\N	\N	20797	f	0	\N	2026-01-23 13:08:41.603804	\N	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/octet-stream
31	parte 2.docx	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180842_4d7b47a2_parte_2.docx	\N	\N	\N	\N	17907	f	0	\N	2026-01-23 13:08:42.746082	\N	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/vnd.openxmlformats-officedocument.wordprocessingml.document
32	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180844_94f4c169_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	\N	\N	\N	\N	118729	f	0	\N	2026-01-23 13:08:44.187682	\N	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
33	guia_firma_notificaciones_parte6.txt	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180845_2253f130_guia_firma_notificaciones_parte6.txt	\N	\N	\N	\N	28392	f	0	\N	2026-01-23 13:08:45.812031	\N	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
34	guia_firma_notificaciones_parte3.txt	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180847_d9705707_guia_firma_notificaciones_parte3.txt	\N	\N	\N	\N	25830	f	0	\N	2026-01-23 13:08:47.258373	\N	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
35	guia_firma_notificaciones_parte5.txt	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180848_4f0d4657_guia_firma_notificaciones_parte5.txt	\N	\N	\N	\N	17575	f	0	\N	2026-01-23 13:08:48.729956	\N	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
36	guia_firma_notificaciones_parte4.txt	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180849_f1fac361_guia_firma_notificaciones_parte4.txt	\N	\N	\N	\N	16681	f	0	\N	2026-01-23 13:08:49.894538	\N	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
37	guia_firma_notificaciones_parte2.txt	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180851_b17115e2_guia_firma_notificaciones_parte2.txt	\N	\N	\N	\N	15890	f	0	\N	2026-01-23 13:08:51.248799	\N	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
38	guia_firma_notificaciones_parte1.txt	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180852_edbde665_guia_firma_notificaciones_parte1.txt	\N	\N	\N	\N	14361	f	0	\N	2026-01-23 13:08:52.629322	\N	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	text/plain
39	analog-period-389922-7b9f19681b38.json	9	f	\N	f	\N	\N	entidad_1/carpeta_9/20260123180853_3dd818e6_analog-period-389922-7b9f19681b38.json	\N	\N	\N	\N	2388	f	0	\N	2026-01-23 13:08:54.012369	\N	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:47:52.689689	COMPLETADO	application/json
102	2118644388827 (1).pdf	1	f	\N	f	\N	\N	entidad_1/carpeta_1/20260123184810_6a34318b_2118644388827_(1).pdf	\N	\N	\N	\N	573295	t	0	\N	2026-01-23 13:48:10.827022	\N	b0d267132359fd08ec1d50b413243d88aede35b84da1454846df0bea1d6e52b8	SHA256	\N	\N	1	\N	\N	\N	\N	\N	1	\N	\N	\N	\N	\N	2026-01-23 13:48:13.088134	COMPLETADO	application/pdf
\.


--
-- Data for Name: Archivos_Eliminados; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Archivos_Eliminados" ("Cod", "Nombre", "Ruta", "FechaEliminacion", "Carpeta", "Formato", "EliminadoPor", "MotivoEliminacion", "Hash", "Tamaño", "CarpetaCod") FROM stdin;
1	parte 2.docx	entidad_1/carpeta_2/20260121065651_a789380c_parte 2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
2	parte 2.docx	entidad_1/carpeta_2/20260122005347_d80b5e5d_parte 2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
3	parte 2.docx	entidad_1/carpeta_2/20260122034318_9bb9b9d7_parte 2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
4	parte 2.docx	entidad_1/carpeta_2/20260122034613_5034310e_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
5	parte 2.docx	entidad_1/carpeta_2/20260122035505_950710d2_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
6	parte 2.docx	entidad_1/carpeta_2/20260122040200_6d2ee147_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
7	parte 2.docx	entidad_1/carpeta_2/20260122041333_22c2ffaf_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
8	parte 2.docx	entidad_1/carpeta_2/20260122041636_f943b644_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
9	parte 2.docx	entidad_1/carpeta_2/20260122042354_2ef5fb89_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
10	parte 2.docx	entidad_1/carpeta_2/20260122043037_848bf899_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
11	parte 2.docx	entidad_1/carpeta_2/20260122043840_2aaf1ab9_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
12	parte 2.docx	entidad_1/carpeta_2/20260122043923_89c7e623_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
13	parte 2.docx	entidad_1/carpeta_2/20260122050134_be72b98a_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
14	parte 2.docx	entidad_1/carpeta_2/20260122050843_436e7e52_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
15	parte 2.docx	entidad_1/carpeta_2/20260122051406_4904947f_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
16	parte 2.docx	entidad_1/carpeta_2/20260122123744_df41e031_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
17	parte 2.docx	entidad_1/carpeta_2/20260122124329_eb50632e_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
18	parte 2.docx	entidad_1/carpeta_2/20260122131053_9082bca4_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
19	parte 2.docx	entidad_1/carpeta_2/20260122131612_b65e5c4a_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	\N	17907	2
20	parte 2.docx	entidad_1/carpeta_2/20260122134658_3d61e214_parte_2.docx	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	17907	2
21	softsolutions20223-nublusoftapi-8a5edab282632443.txt	entidad_1/carpeta_2/20260122134808_a42888a9_softsolutions20223-nublusoftapi-8a5edab282632443.txt	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	1243529	2
22	Grabación de pantalla 2025-11-01 155406.mp4	entidad_1/carpeta_2/20260122134902_3aa74a1b_Grabación_de_pantalla_2025-11-01_155406.mp4	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	f93c843c59d534e44c6a61921fc215ea37eaabb8cb4c9d995e762a30560f44b0	114191082	2
23	guia_firma_notificaciones_parte6.txt	entidad_1/carpeta_2/20260122184452_9ebaec6e_guia_firma_notificaciones_parte6.txt	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	28392	2
24	SQLServer2022-x64-ESN-Dev.iso	entidad_1/carpeta_2/20260122223437_6c5589d5_SQLServer2022-x64-ESN-Dev.iso	2026-01-23 13:47:52.689689	otro	\N	1	Eliminado con carpeta: otro	90aae7672be0485274a1a0fd77d9102c38b80dddf60f47f50f01f3ed2211861f	1363390464	2
28	softsolutions20223-nublusoftapi-8a5edab282632443.txt	entidad_1/carpeta_1/20260122225557_5ede35b2_softsolutions20223-nublusoftapi-8a5edab282632443_copy_639047800763206655.txt	2026-01-23 13:47:52.689689	nuevaa	\N	1	Eliminado con carpeta: nuevaa	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	1243529	3
52	softsolutions20223-nublusoftapi-8a5edab282632443.txt	entidad_1/carpeta_4/20260123182441_f8ebcef0_softsolutions20223-nublusoftapi-8a5edab282632443.txt	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	1243529	4
53	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	entidad_1/carpeta_4/20260123182443_a00287cb_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	22194	4
54	MAPEO_APIS_INTERFACES.md	entidad_1/carpeta_4/20260123182445_560a985c_MAPEO_APIS_INTERFACES.md	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	20797	4
55	parte 2.docx	entidad_1/carpeta_4/20260123182446_560e8ba9_parte_2.docx	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	17907	4
56	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	entidad_1/carpeta_4/20260123182448_80e66570_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	118729	4
57	guia_firma_notificaciones_parte6.txt	entidad_1/carpeta_4/20260123182449_efdf28b8_guia_firma_notificaciones_parte6.txt	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	28392	4
58	guia_firma_notificaciones_parte3.txt	entidad_1/carpeta_4/20260123182451_bb7c5f6e_guia_firma_notificaciones_parte3.txt	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	25830	4
59	guia_firma_notificaciones_parte5.txt	entidad_1/carpeta_4/20260123182452_fbe5dd0b_guia_firma_notificaciones_parte5.txt	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	17575	4
60	guia_firma_notificaciones_parte4.txt	entidad_1/carpeta_4/20260123182453_0e73ba23_guia_firma_notificaciones_parte4.txt	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	16681	4
61	guia_firma_notificaciones_parte2.txt	entidad_1/carpeta_4/20260123182454_67c57f4d_guia_firma_notificaciones_parte2.txt	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	15890	4
62	guia_firma_notificaciones_parte1.txt	entidad_1/carpeta_4/20260123182455_8df4c057_guia_firma_notificaciones_parte1.txt	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	14361	4
63	analog-period-389922-7b9f19681b38.json	entidad_1/carpeta_4/20260123182457_662749b7_analog-period-389922-7b9f19681b38.json	2026-01-23 13:47:52.689689	otraaaa	\N	1	Eliminado con carpeta: otraaaa	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	2388	4
64	softsolutions20223-nublusoftapi-8a5edab282632443.txt	entidad_1/carpeta_6/20260123183555_3862b75c_softsolutions20223-nublusoftapi-8a5edab282632443.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	1243529	6
65	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	entidad_1/carpeta_6/20260123183558_f6a15804_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	22194	6
66	MAPEO_APIS_INTERFACES.md	entidad_1/carpeta_6/20260123183559_23604b53_MAPEO_APIS_INTERFACES.md	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	20797	6
67	parte 2.docx	entidad_1/carpeta_6/20260123183600_dac770b4_parte_2.docx	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	17907	6
68	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	entidad_1/carpeta_6/20260123183601_8ac79637_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	118729	6
69	guia_firma_notificaciones_parte6.txt	entidad_1/carpeta_6/20260123183603_ccacff3c_guia_firma_notificaciones_parte6.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	28392	6
70	guia_firma_notificaciones_parte3.txt	entidad_1/carpeta_6/20260123183604_87c69052_guia_firma_notificaciones_parte3.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	25830	6
71	guia_firma_notificaciones_parte5.txt	entidad_1/carpeta_6/20260123183605_0f52052b_guia_firma_notificaciones_parte5.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	17575	6
72	guia_firma_notificaciones_parte4.txt	entidad_1/carpeta_6/20260123183606_b55f1b82_guia_firma_notificaciones_parte4.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	16681	6
73	guia_firma_notificaciones_parte2.txt	entidad_1/carpeta_6/20260123183607_adcd9830_guia_firma_notificaciones_parte2.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	15890	6
74	guia_firma_notificaciones_parte1.txt	entidad_1/carpeta_6/20260123183608_f107e16a_guia_firma_notificaciones_parte1.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	14361	6
75	analog-period-389922-7b9f19681b38.json	entidad_1/carpeta_6/20260123183609_ef51a73e_analog-period-389922-7b9f19681b38.json	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	2388	6
88	Solicitud de Vinculacion - ID  CC  29543587 - No Solicitud  1376549 -.pdf	entidad_1/carpeta_8/20260123184505_11f0e4c5_Solicitud_de_Vinculacion_-_ID__CC__29543587_-_No_S.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	79465a35f97d1a2634a164823f86fe723ea2dca12fb61441d390e9e8bf6cc48a	763170	8
91	2118716946363 (2) (1).pdf	entidad_1/carpeta_8/20260123184512_f973bf7c_2118716946363_(2)_(1).pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	47a2d60b92ece8db98da0c07850c3befb935799cf32c22756946a37aff62187d	573254	8
94	2118716946363.pdf	entidad_1/carpeta_8/20260123184518_37b29920_2118716946363.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	3012452b3c662cc38766b3c6b4d25215f54fe21991d66b2908e4af50f58e9032	569037	8
76	softsolutions20223-nublusoftapi-8a5edab282632443.txt	entidad_1/carpeta_8/20260123184042_77f71991_softsolutions20223-nublusoftapi-8a5edab282632443.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	1243529	8
77	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	entidad_1/carpeta_8/20260123184045_3d7bb3aa_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	22194	8
89	Autorizacion de Desembolso - ID  CC  29543587 - No Solicitud  1376549 -.pdf	entidad_1/carpeta_8/20260123184508_22b492ac_Autorizacion_de_Desembolso_-_ID__CC__29543587_-_No.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	4239aa69fa16d590330cbc49c4af2c7632e5952fc93a29d2eaf2f2fafdfb0a65	719342	8
78	MAPEO_APIS_INTERFACES.md	entidad_1/carpeta_8/20260123184046_3aa0765b_MAPEO_APIS_INTERFACES.md	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	20797	8
79	parte 2.docx	entidad_1/carpeta_8/20260123184048_4624ff90_parte_2.docx	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	17907	8
90	2118644388827.pdf	entidad_1/carpeta_8/20260123184510_c832dfaa_2118644388827.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	b0d267132359fd08ec1d50b413243d88aede35b84da1454846df0bea1d6e52b8	573295	8
80	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	entidad_1/carpeta_8/20260123184049_ea09f3b6_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	118729	8
81	guia_firma_notificaciones_parte6.txt	entidad_1/carpeta_8/20260123184051_9468de7c_guia_firma_notificaciones_parte6.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	28392	8
82	guia_firma_notificaciones_parte3.txt	entidad_1/carpeta_8/20260123184053_e75a3e8e_guia_firma_notificaciones_parte3.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	25830	8
92	2118716946363 (2).pdf	entidad_1/carpeta_8/20260123184514_d1f51a40_2118716946363_(2).pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	47a2d60b92ece8db98da0c07850c3befb935799cf32c22756946a37aff62187d	573254	8
83	guia_firma_notificaciones_parte5.txt	entidad_1/carpeta_8/20260123184054_9a58ef68_guia_firma_notificaciones_parte5.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	17575	8
93	2118719064580.pdf	entidad_1/carpeta_8/20260123184516_547aee1a_2118719064580.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	942da43d7c4fee278b4c75d058644963b319636577afdc983cf92baf5968d296	573127	8
84	guia_firma_notificaciones_parte4.txt	entidad_1/carpeta_8/20260123184056_e995117c_guia_firma_notificaciones_parte4.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	16681	8
85	guia_firma_notificaciones_parte2.txt	entidad_1/carpeta_8/20260123184057_ad9dbe91_guia_firma_notificaciones_parte2.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	15890	8
86	guia_firma_notificaciones_parte1.txt	entidad_1/carpeta_8/20260123184059_bd4c1573_guia_firma_notificaciones_parte1.txt	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	14361	8
95	2118716946363 (1).pdf	entidad_1/carpeta_8/20260123184520_faff2ff4_2118716946363_(1).pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	2bfb2d4d50a8173442fe508f2813f8a97aa93a9966f981e885efacca10db4be9	568799	8
87	analog-period-389922-7b9f19681b38.json	entidad_1/carpeta_8/20260123184100_cfed72b9_analog-period-389922-7b9f19681b38.json	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	2388	8
96	Solicitud de Productos - ID  CC  29543587 - No Solicitud  1376549 -.pdf	entidad_1/carpeta_8/20260123184522_558d915b_Solicitud_de_Productos_-_ID__CC__29543587_-_No_Sol.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	aafd037080e3441057261bc6560c446b8ba0ac48c7f4b2ee68258596f072b344	542436	8
97	WhatsApp Image 2025-11-27 at 14.08.16.jpeg	entidad_1/carpeta_8/20260123184524_22571c86_WhatsApp_Image_2025-11-27_at_14.08.16.jpeg	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	ff75905a8667a632b179d43c821306c7a587235828fc439f78c9a730bba62c02	492103	8
98	4-1231756227932394029_Autorizacion_de_Desembolso.pdf	entidad_1/carpeta_8/20260123184526_e61e77c6_4-1231756227932394029_Autorizacion_de_Desembolso.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	54905f0544d3119c65aa89ff4e70b0e49c2cb12395704a317220ffae4316a4bd	450184	8
99	T-2025142584-5840300.pdf	entidad_1/carpeta_8/20260123184528_aca60db1_T-2025142584-5840300.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	0214cca290728f4248bf92b3a7eebe89e673b25763667821b61821a62c489632	373822	8
100	4-1231756227932394029E0909_Respuesta_1_RESP_FINAL_SFC.pdf	entidad_1/carpeta_8/20260123184530_257e1bbd_4-1231756227932394029E0909_Respuesta_1_RESP_FINAL_.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	02addf1c3f9f64c0d8de1ec5e11cab19244fbd118ce31ee586c6cdcdeae685bc	214032	8
101	4-1231756227932394029_Respuesta_RESP_FINAL_SFC.pdf	entidad_1/carpeta_8/20260123184531_4c75df1a_4-1231756227932394029_Respuesta_RESP_FINAL_SFC.pdf	2026-01-23 13:47:52.689689	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	5fb087fd77878f41f485d30efede1f9e9e00d4542e92bd7568e7cf90cd134123	214028	8
29	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	entidad_1/carpeta_9/20260123180838_8b6e510e_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	22194	9
30	MAPEO_APIS_INTERFACES.md	entidad_1/carpeta_9/20260123180841_066c531a_MAPEO_APIS_INTERFACES.md	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	20797	9
31	parte 2.docx	entidad_1/carpeta_9/20260123180842_4d7b47a2_parte_2.docx	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	17907	9
32	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	entidad_1/carpeta_9/20260123180844_94f4c169_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	118729	9
33	guia_firma_notificaciones_parte6.txt	entidad_1/carpeta_9/20260123180845_2253f130_guia_firma_notificaciones_parte6.txt	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	28392	9
34	guia_firma_notificaciones_parte3.txt	entidad_1/carpeta_9/20260123180847_d9705707_guia_firma_notificaciones_parte3.txt	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	25830	9
35	guia_firma_notificaciones_parte5.txt	entidad_1/carpeta_9/20260123180848_4f0d4657_guia_firma_notificaciones_parte5.txt	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	17575	9
36	guia_firma_notificaciones_parte4.txt	entidad_1/carpeta_9/20260123180849_f1fac361_guia_firma_notificaciones_parte4.txt	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	16681	9
37	guia_firma_notificaciones_parte2.txt	entidad_1/carpeta_9/20260123180851_b17115e2_guia_firma_notificaciones_parte2.txt	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	15890	9
38	guia_firma_notificaciones_parte1.txt	entidad_1/carpeta_9/20260123180852_edbde665_guia_firma_notificaciones_parte1.txt	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	14361	9
39	analog-period-389922-7b9f19681b38.json	entidad_1/carpeta_9/20260123180853_3dd818e6_analog-period-389922-7b9f19681b38.json	2026-01-23 13:47:52.689689	otraaaa (copia) (copia)	\N	1	Eliminado con carpeta: otraaaa (copia) (copia)	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	2388	9
40	softsolutions20223-nublusoftapi-8a5edab282632443.txt	entidad_1/carpeta_5/20260123181709_75a22077_softsolutions20223-nublusoftapi-8a5edab282632443.txt	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	7f112c36e6d29611633e6afdd95efcb5b73eb9e33f04611a8c91e73eef5b87d9	1243529	5
41	PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	entidad_1/carpeta_5/20260123181712_3fed34a2_PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	792217066917ffd5c2ee5664d3699531dd5de72feb6f1aebb91d46b7fc242e9d	22194	5
42	MAPEO_APIS_INTERFACES.md	entidad_1/carpeta_5/20260123181714_648706d2_MAPEO_APIS_INTERFACES.md	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	f4b562845f6984fa9927e56daf87446fc5a26029ec134b9ee831de7f4eea7e9b	20797	5
43	parte 2.docx	entidad_1/carpeta_5/20260123181715_d23e3565_parte_2.docx	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	652774a35280e20223d766c41fd5785bc59841bfe1641134a1ea416345ee46dc	17907	5
44	GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	entidad_1/carpeta_5/20260123181716_caa373a2_GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	11f333d2131f43a7e506639bbad538b2cda843e1478404071365600530837e5c	118729	5
45	guia_firma_notificaciones_parte6.txt	entidad_1/carpeta_5/20260123181718_84e3ec85_guia_firma_notificaciones_parte6.txt	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	42480dc99b3f9273c23cddbd3d90c50b3d4343232dfa60568d68f191812bf490	28392	5
46	guia_firma_notificaciones_parte3.txt	entidad_1/carpeta_5/20260123181719_8c57f5cf_guia_firma_notificaciones_parte3.txt	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	b8639cb6b6e900942558f4be7f47c1bc8a028b06e373b7985a15f10891cc03d9	25830	5
47	guia_firma_notificaciones_parte5.txt	entidad_1/carpeta_5/20260123181721_e98ee730_guia_firma_notificaciones_parte5.txt	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	7cf5c1612cfcf5c9060913c184903145042528d8a323fd0430947d777428b5f3	17575	5
48	guia_firma_notificaciones_parte4.txt	entidad_1/carpeta_5/20260123181722_2d6e2669_guia_firma_notificaciones_parte4.txt	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	e1e3fd8e21d7367a9653126789b058ec8a54db57d84d360a8af6d9071a2cb157	16681	5
49	guia_firma_notificaciones_parte2.txt	entidad_1/carpeta_5/20260123181724_ef57f446_guia_firma_notificaciones_parte2.txt	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	24f1fc2ef984e706aa3cfb30eb3710adce96aa5b462dbd62a2686e85a869187a	15890	5
50	guia_firma_notificaciones_parte1.txt	entidad_1/carpeta_5/20260123181725_2bd243fa_guia_firma_notificaciones_parte1.txt	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	669b5d35bee08227f2d32e599ae193258c2ec8a28b34c3a8aeac41dbc571f3a4	14361	5
51	analog-period-389922-7b9f19681b38.json	entidad_1/carpeta_5/20260123181726_43352a57_analog-period-389922-7b9f19681b38.json	2026-01-23 13:47:58.888824	otraaaa (copia)	\N	1	Eliminado con carpeta: otraaaa (copia)	016026bd2df66da955246f3b08c1f250b40f943d375977e76c6802ef05ff77cf	2388	5
\.


--
-- Data for Name: Carpetas; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Carpetas" ("Cod", "CodSerie", "CodSubSerie", "Estado", "EstadoCarpeta", "Nombre", "Descripcion", "Copia", "CarpetaOriginal", "CarpetaPadre", "FechaCreacion", "IndiceElectronico", "Delegado", "TipoCarpeta", "NivelVisualizacion", "SerieRaiz", "TRD", "FechaCierre", "NumeroFolios", "Soporte", "FrecuenciaConsulta", "UbicacionFisica", "Observaciones", "CreadoPor", "ModificadoPor", "FechaModificacion", "Tomo", "TotalTomos", "CodigoExpediente", "PalabrasClave", "FechaDocumentoInicial", "FechaDocumentoFinal", "ExpedienteRelacionado", "Entidad", "Oficina") FROM stdin;
1	\N	\N	t	1	Nueva	nueva	f	\N	\N	2026-01-18 20:16:54.251631	\N	\N	4	\N	\N	\N	\N	0	\N	\N	\N	\N	1	\N	\N	1	1	\N	\N	\N	\N	\N	1	\N
4	\N	\N	f	1	otraaaa	ASF	f	\N	3	2026-01-22 21:14:17.561226	\N	\N	4	1	1	\N	\N	0	\N	\N	\N	\N	1	1	2026-01-23 13:47:52.689689	1	1	\N	\N	\N	\N	\N	1	\N
6	\N	\N	f	1	otraaaa (copia)	ASF	t	4	3	2026-01-23 11:56:37.950639	\N	\N	4	1	1	\N	\N	0	\N	\N	\N	\N	1	1	2026-01-23 13:47:52.689689	1	1	\N	\N	\N	\N	\N	1	\N
8	\N	\N	f	1	otraaaa (copia)	ASF	t	4	3	2026-01-23 12:23:16.929613	\N	\N	4	1	1	\N	\N	0	\N	\N	\N	\N	1	1	2026-01-23 13:47:52.689689	1	1	\N	\N	\N	\N	\N	1	\N
3	\N	\N	f	1	nuevaa	nuevaa	f	\N	2	2026-01-22 20:51:10.521045	\N	\N	4	1	1	\N	\N	0	\N	\N	\N	\N	1	1	2026-01-23 13:47:52.689689	1	1	\N	\N	\N	\N	\N	1	\N
9	\N	\N	f	1	otraaaa (copia) (copia)	ASF	t	8	7	2026-01-23 12:30:14.112618	\N	\N	4	1	1	\N	\N	0	\N	\N	\N	\N	1	1	2026-01-23 13:47:52.689689	1	1	\N	\N	\N	\N	\N	1	\N
7	\N	\N	f	1	otraaaa (copia) (copia)	ASF	t	6	2	2026-01-23 11:56:59.922477	\N	\N	4	1	1	\N	\N	0	\N	\N	\N	\N	1	1	2026-01-23 13:47:52.689689	1	1	\N	\N	\N	\N	\N	1	\N
2	\N	\N	f	1	otro	adf	f	\N	1	2026-01-20 00:49:45.948441	\N	\N	4	1	1	\N	\N	0	\N	\N	\N	\N	1	1	2026-01-23 13:47:52.689689	1	1	\N	\N	\N	\N	\N	1	\N
5	\N	\N	f	1	otraaaa (copia)	ASF	t	4	1	2026-01-23 11:26:11.194318	\N	\N	4	1	1	\N	\N	0	\N	\N	\N	\N	1	1	2026-01-23 13:47:58.888824	1	1	\N	\N	\N	\N	\N	1	\N
\.


--
-- Data for Name: Carpetas_Eliminadas; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Carpetas_Eliminadas" ("Cod", "Nombre", "TipoCarpeta", "CarpetaPadre", "SerieRaiz", "FechaCreacion", "FechaEliminacion", "EliminadoPor", "MotivoEliminacion", "NumeroFolios", "NumeroArchivos") FROM stdin;
4	otraaaa	4	3	1	2026-01-22 21:14:17.561226	2026-01-23 13:47:52.689689	1	Eliminado con carpeta padre: nuevaa	0	12
6	otraaaa (copia)	4	3	1	2026-01-23 11:56:37.950639	2026-01-23 13:47:52.689689	1	Eliminado con carpeta padre: nuevaa	0	12
8	otraaaa (copia)	4	3	1	2026-01-23 12:23:16.929613	2026-01-23 13:47:52.689689	1	Eliminado con carpeta padre: nuevaa	0	26
3	nuevaa	4	2	1	2026-01-22 20:51:10.521045	2026-01-23 13:47:52.689689	1	Eliminado con carpeta padre: otro	0	1
9	otraaaa (copia) (copia)	4	7	1	2026-01-23 12:30:14.112618	2026-01-23 13:47:52.689689	1	Eliminado con carpeta padre: otraaaa (copia) (copia)	0	11
7	otraaaa (copia) (copia)	4	2	1	2026-01-23 11:56:59.922477	2026-01-23 13:47:52.689689	1	Eliminado con carpeta padre: otro	0	0
2	otro	4	1	1	2026-01-20 00:49:45.948441	2026-01-23 13:47:52.689689	1	\N	0	24
5	otraaaa (copia)	4	1	1	2026-01-23 11:26:11.194318	2026-01-23 13:47:58.888824	1	\N	0	12
\.


--
-- Data for Name: Certificados_Usuarios; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Certificados_Usuarios" ("Cod", "Usuario", "Entidad", "NumeroSerie", "SubjectName", "Huella", "CertificadoPublico", "ClavePrivadaEncriptada", "SaltEncriptacion", "AlgoritmoEncriptacion", "AlgoritmoFirma", "TamanoClave", "FechaEmision", "FechaVencimiento", "Estado", "FechaRevocacion", "MotivoRevocacion", "RevocadoPor", "FechaCreacion", "UltimoUso", "VecesUsado") FROM stdin;
\.


--
-- Data for Name: Codigos_OTP; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Codigos_OTP" ("Cod", "Firmante", "CodigoHash", "Salt", "MedioEnvio", "DestinoEnvio", "DestinoCompleto", "FechaEnvio", "FechaExpiracion", "Intentos", "MaxIntentos", "Usado", "FechaUso", "IPUso") FROM stdin;
\.


--
-- Data for Name: Configuracion_Firma; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Configuracion_Firma" ("Cod", "Entidad", "FirmaSimpleHabilitada", "OtpPorEmail", "OtpPorSms", "OtpLongitud", "OtpVigenciaMinutos", "OtpMaxIntentos", "FirmaAvanzadaHabilitada", "GenerarCertificadoAuto", "VigenciaCertificadoDias", "RequiereContrasenaFuerte", "AgregarPaginaConstancia", "AgregarSelloVisual", "PosicionSelloX", "PosicionSelloY", "TamanoSelloAncho", "TamanoSelloAlto", "TextoSello", "IncluirFechaEnSello", "IncluirNombreEnSello", "EnviarEmailSolicitud", "EnviarEmailRecordatorio", "EnviarEmailFirmaCompletada", "DiasRecordatorioVencimiento", "PlantillaEmailSolicitud", "PlantillaEmailOtp", "PlantillaEmailCompletada", "PlantillaEmailRecordatorio", "MaxFirmantesPorSolicitud", "DiasMaxVigenciaSolicitud") FROM stdin;
1	1	t	t	f	6	10	3	t	t	365	t	t	t	400	50	200	70	Firmado electrónicamente	t	t	t	t	t	2	\N	\N	\N	\N	10	30
2	2	t	t	f	6	10	3	t	t	365	t	t	t	400	50	200	70	Firmado electrónicamente	t	t	t	t	t	2	\N	\N	\N	\N	10	30
3	3	t	t	f	6	10	3	t	t	365	t	t	t	400	50	200	70	Firmado electrónicamente	t	t	t	t	t	2	\N	\N	\N	\N	10	30
\.


--
-- Data for Name: Consecutivos_Radicado; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Consecutivos_Radicado" ("Entidad", "TipoComunicacion", "Anio", "UltimoConsecutivo", "FechaActualizacion") FROM stdin;
\.


--
-- Data for Name: Dias_Festivos; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Dias_Festivos" ("Fecha", "Nombre", "EsFijo", "Pais") FROM stdin;
2024-01-01	Año Nuevo	t	Colombia
2024-01-08	Día de los Reyes Magos	f	Colombia
2024-03-25	Día de San José	f	Colombia
2024-03-28	Jueves Santo	f	Colombia
2024-03-29	Viernes Santo	f	Colombia
2024-05-01	Día del Trabajo	t	Colombia
2024-05-13	Día de la Ascensión	f	Colombia
2024-06-03	Corpus Christi	f	Colombia
2024-06-10	Sagrado Corazón	f	Colombia
2024-07-01	San Pedro y San Pablo	f	Colombia
2024-07-20	Día de la Independencia	t	Colombia
2024-08-07	Batalla de Boyacá	t	Colombia
2024-08-19	La Asunción de la Virgen	f	Colombia
2024-10-14	Día de la Raza	f	Colombia
2024-11-04	Todos los Santos	f	Colombia
2024-11-11	Independencia de Cartagena	f	Colombia
2024-12-08	Día de la Inmaculada Concepción	t	Colombia
2024-12-25	Día de Navidad	t	Colombia
2025-01-01	Año Nuevo	t	Colombia
2025-01-06	Día de los Reyes Magos	f	Colombia
2025-03-24	Día de San José	f	Colombia
2025-04-17	Jueves Santo	f	Colombia
2025-04-18	Viernes Santo	f	Colombia
2025-05-01	Día del Trabajo	t	Colombia
2025-06-02	Día de la Ascensión	f	Colombia
2025-06-23	Corpus Christi	f	Colombia
2025-06-30	Sagrado Corazón	f	Colombia
2025-07-20	Día de la Independencia	t	Colombia
2025-08-07	Batalla de Boyacá	t	Colombia
2025-08-18	La Asunción de la Virgen	f	Colombia
2025-10-13	Día de la Raza	f	Colombia
2025-11-03	Todos los Santos	f	Colombia
2025-11-17	Independencia de Cartagena	f	Colombia
2025-12-08	Día de la Inmaculada Concepción	t	Colombia
2025-12-25	Día de Navidad	t	Colombia
2026-01-01	Año Nuevo	t	Colombia
2026-01-12	Día de los Reyes Magos	f	Colombia
2026-03-23	Día de San José	f	Colombia
2026-04-02	Jueves Santo	f	Colombia
2026-04-03	Viernes Santo	f	Colombia
2026-05-01	Día del Trabajo	t	Colombia
2026-05-18	Día de la Ascensión	f	Colombia
2026-06-08	Corpus Christi	f	Colombia
2026-06-15	Sagrado Corazón	f	Colombia
2026-06-29	San Pedro y San Pablo	f	Colombia
2026-07-20	Día de la Independencia	t	Colombia
2026-08-07	Batalla de Boyacá	t	Colombia
2026-08-17	La Asunción de la Virgen	f	Colombia
2026-10-12	Día de la Raza	f	Colombia
2026-11-02	Todos los Santos	f	Colombia
2026-11-16	Independencia de Cartagena	f	Colombia
2026-12-08	Día de la Inmaculada Concepción	t	Colombia
2026-12-25	Día de Navidad	t	Colombia
\.


--
-- Data for Name: Disposiciones_Finales; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Disposiciones_Finales" ("Cod", "Nombre", "Descripcion") FROM stdin;
CT	Conservación Total	El documento se conserva permanentemente
E	Eliminación	El documento se elimina después del tiempo de retención
S	Selección	Se selecciona una muestra representativa
M	Microfilmación	Se microfilma y se puede eliminar el original
D	Digitalización	Se digitaliza y se puede eliminar el original
\.


--
-- Data for Name: Entidades; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Entidades" ("Cod", "Nombre", "Telefono", "Nit", "Direccion", "Correo", "Logo", "Ciudad", "Departamento", "Pais", "SitioWeb", "TipoEntidad", "Sector", "RepresentanteLegal", "DocumentoRepresentante", "FechaCreacion", "CodigoDane", "Estado") FROM stdin;
\.


--
-- Data for Name: ErrorLog; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."ErrorLog" ("Id", "ProcedureName", "Parameters", "ErrorMessage", "ErrorDate", "ErrorNumber", "ErrorLine", "ErrorSeverity") FROM stdin;
\.


--
-- Data for Name: Estados_Carpetas; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Estados_Carpetas" ("Cod", "Nombre") FROM stdin;
1	Abierto
2	Cerrado
3	En Trámite
4	Archivado
5	Transferido
\.


--
-- Data for Name: Estados_Radicado; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Estados_Radicado" ("Cod", "Nombre", "Descripcion", "EsFinal", "Color", "Orden") FROM stdin;
RADICADO	Radicado	Documento recién radicado	f	#3498db	1
ASIGNADO	Asignado	Asignado a funcionario	f	#9b59b6	2
EN_TRAMITE	En Trámite	En proceso de gestión	f	#f39c12	3
TRASLADADO	Trasladado	Remitido a otra dependencia	f	#1abc9c	4
DEVUELTO	Devuelto	Devuelto al remitente	f	#e67e22	5
PRORROGA	En Prórroga	Se solicitó prórroga	f	#d35400	6
RESPONDIDO	Respondido	Tiene respuesta generada	t	#27ae60	7
ARCHIVADO	Archivado	Vinculado a expediente	t	#2c3e50	8
ANULADO	Anulado	Radicado anulado	t	#c0392b	9
\.


--
-- Data for Name: Evidencias_Firma; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Evidencias_Firma" ("Cod", "Firmante", "TipoEvidencia", "FechaEvidencia", "Descripcion", "HashDocumento", "IP", "UserAgent", "Geolocalizacion", "DatosAdicionales") FROM stdin;
\.


--
-- Data for Name: Firmantes_Solicitud; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Firmantes_Solicitud" ("Cod", "Solicitud", "Usuario", "Entidad", "Orden", "RolFirmante", "EsObligatorio", "Estado", "FechaNotificacion", "NotificadoPorEmail", "EmailNotificacion", "FechaFirma", "TipoFirmaUsada", "HashAlFirmar", "CertificadoUsado", "FechaRechazo", "MotivoRechazo", "IP", "UserAgent", "Geolocalizacion") FROM stdin;
\.


--
-- Data for Name: Firmas_Archivos; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Firmas_Archivos" ("Cod", "Archivo", "Usuario", "TipoFirma", "FechaFirma", "CertificadoSerial", "CertificadoEmisor", "CertificadoVence", "HashFirmado", "DatosFirma", "Estado", "Observaciones") FROM stdin;
\.


--
-- Data for Name: Frecuencias_Consulta; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Frecuencias_Consulta" ("Cod", "Nombre", "DiasMaxRespuesta") FROM stdin;
ALTA	Alta	1
MEDIA	Media	3
BAJA	Baja	7
\.


--
-- Data for Name: Historial_Acciones; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Historial_Acciones" ("Cod", "Tabla", "RegistroCod", "Accion", "Usuario", "Fecha", "IP", "DetalleAnterior", "DetalleNuevo", "Observaciones") FROM stdin;
1	Carpetas	4	MOVER	1	2026-01-23 11:24:31.146407	\N	CarpetaPadre: 2	CarpetaPadre: 3	Carpeta movida: otraaaa
2	Carpetas	5	COPIAR	1	2026-01-23 11:26:11.194318	\N	\N	\N	Copiada desde carpeta 4. Carpetas: 1, Archivos: 0
3	Carpetas	6	COPIAR	1	2026-01-23 11:56:37.950639	\N	\N	\N	Copiada desde carpeta 4. Carpetas: 1, Archivos: 0
4	Carpetas	7	COPIAR	1	2026-01-23 11:56:59.922477	\N	\N	\N	Copiada desde carpeta 6. Carpetas: 1, Archivos: 0
5	Carpetas	8	COPIAR	1	2026-01-23 12:23:16.929613	\N	\N	\N	Copiada desde carpeta 4. Carpetas: 1, Archivos: 0
6	Carpetas	7	MOVER	1	2026-01-23 12:29:57.68959	\N	CarpetaPadre: 3	CarpetaPadre: 2	Carpeta movida: otraaaa (copia) (copia)
7	Carpetas	9	COPIAR	1	2026-01-23 12:30:14.112618	\N	\N	\N	Copiada desde carpeta 8. Carpetas: 1, Archivos: 0
8	Archivos	1	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
9	Archivos	2	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
10	Archivos	3	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
11	Archivos	4	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
12	Archivos	5	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
13	Archivos	6	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
14	Archivos	7	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
15	Archivos	8	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
16	Archivos	9	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
17	Archivos	10	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
18	Archivos	11	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
19	Archivos	12	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
20	Archivos	13	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
21	Archivos	14	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
22	Archivos	15	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
23	Archivos	16	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
24	Archivos	17	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
25	Archivos	18	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
26	Archivos	19	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
27	Archivos	20	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otro
28	Archivos	21	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: softsolutions20223-nublusoftapi-8a5edab282632443.txt. Motivo: Eliminado con carpeta: otro
29	Archivos	22	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: Grabación de pantalla 2025-11-01 155406.mp4. Motivo: Eliminado con carpeta: otro
30	Archivos	23	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte6.txt. Motivo: Eliminado con carpeta: otro
31	Archivos	24	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: SQLServer2022-x64-ESN-Dev.iso. Motivo: Eliminado con carpeta: otro
32	Archivos	28	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: softsolutions20223-nublusoftapi-8a5edab282632443.txt. Motivo: Eliminado con carpeta: nuevaa
33	Archivos	52	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: softsolutions20223-nublusoftapi-8a5edab282632443.txt. Motivo: Eliminado con carpeta: otraaaa
34	Archivos	53	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md. Motivo: Eliminado con carpeta: otraaaa
35	Archivos	54	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: MAPEO_APIS_INTERFACES.md. Motivo: Eliminado con carpeta: otraaaa
36	Archivos	55	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otraaaa
37	Archivos	56	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt. Motivo: Eliminado con carpeta: otraaaa
38	Archivos	57	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte6.txt. Motivo: Eliminado con carpeta: otraaaa
39	Archivos	58	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte3.txt. Motivo: Eliminado con carpeta: otraaaa
40	Archivos	59	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte5.txt. Motivo: Eliminado con carpeta: otraaaa
41	Archivos	60	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte4.txt. Motivo: Eliminado con carpeta: otraaaa
42	Archivos	61	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte2.txt. Motivo: Eliminado con carpeta: otraaaa
43	Archivos	62	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte1.txt. Motivo: Eliminado con carpeta: otraaaa
44	Archivos	63	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: analog-period-389922-7b9f19681b38.json. Motivo: Eliminado con carpeta: otraaaa
45	Carpetas	4	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Carpeta eliminada: otraaaa. Archivos: 12. Subcarpetas: 0
46	Archivos	64	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: softsolutions20223-nublusoftapi-8a5edab282632443.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
47	Archivos	65	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md. Motivo: Eliminado con carpeta: otraaaa (copia)
48	Archivos	66	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: MAPEO_APIS_INTERFACES.md. Motivo: Eliminado con carpeta: otraaaa (copia)
49	Archivos	67	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otraaaa (copia)
50	Archivos	68	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
51	Archivos	69	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte6.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
52	Archivos	70	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte3.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
53	Archivos	71	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte5.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
54	Archivos	72	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte4.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
55	Archivos	73	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte2.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
56	Archivos	74	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte1.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
57	Archivos	75	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: analog-period-389922-7b9f19681b38.json. Motivo: Eliminado con carpeta: otraaaa (copia)
58	Carpetas	6	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Carpeta eliminada: otraaaa (copia). Archivos: 12. Subcarpetas: 0
59	Archivos	88	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: Solicitud de Vinculacion - ID  CC  29543587 - No Solicitud  1376549 -.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
60	Archivos	91	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: 2118716946363 (2) (1).pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
61	Archivos	94	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: 2118716946363.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
62	Archivos	76	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: softsolutions20223-nublusoftapi-8a5edab282632443.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
63	Archivos	77	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md. Motivo: Eliminado con carpeta: otraaaa (copia)
64	Archivos	89	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: Autorizacion de Desembolso - ID  CC  29543587 - No Solicitud  1376549 -.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
65	Archivos	78	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: MAPEO_APIS_INTERFACES.md. Motivo: Eliminado con carpeta: otraaaa (copia)
66	Archivos	79	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otraaaa (copia)
67	Archivos	90	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: 2118644388827.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
68	Archivos	80	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
69	Archivos	81	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte6.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
70	Archivos	82	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte3.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
71	Archivos	92	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: 2118716946363 (2).pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
72	Archivos	83	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte5.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
73	Archivos	93	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: 2118719064580.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
74	Archivos	84	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte4.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
75	Archivos	85	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte2.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
76	Archivos	86	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte1.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
77	Archivos	95	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: 2118716946363 (1).pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
78	Archivos	87	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: analog-period-389922-7b9f19681b38.json. Motivo: Eliminado con carpeta: otraaaa (copia)
79	Archivos	96	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: Solicitud de Productos - ID  CC  29543587 - No Solicitud  1376549 -.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
80	Archivos	97	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: WhatsApp Image 2025-11-27 at 14.08.16.jpeg. Motivo: Eliminado con carpeta: otraaaa (copia)
81	Archivos	98	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: 4-1231756227932394029_Autorizacion_de_Desembolso.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
82	Archivos	99	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: T-2025142584-5840300.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
83	Archivos	100	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: 4-1231756227932394029E0909_Respuesta_1_RESP_FINAL_SFC.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
84	Archivos	101	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: 4-1231756227932394029_Respuesta_RESP_FINAL_SFC.pdf. Motivo: Eliminado con carpeta: otraaaa (copia)
85	Carpetas	8	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Carpeta eliminada: otraaaa (copia). Archivos: 26. Subcarpetas: 0
86	Carpetas	3	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Carpeta eliminada: nuevaa. Archivos: 51. Subcarpetas: 3
87	Archivos	29	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
88	Archivos	30	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: MAPEO_APIS_INTERFACES.md. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
89	Archivos	31	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
90	Archivos	32	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
91	Archivos	33	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte6.txt. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
92	Archivos	34	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte3.txt. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
93	Archivos	35	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte5.txt. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
94	Archivos	36	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte4.txt. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
95	Archivos	37	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte2.txt. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
96	Archivos	38	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte1.txt. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
97	Archivos	39	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Archivo eliminado: analog-period-389922-7b9f19681b38.json. Motivo: Eliminado con carpeta: otraaaa (copia) (copia)
98	Carpetas	9	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Carpeta eliminada: otraaaa (copia) (copia). Archivos: 11. Subcarpetas: 0
99	Carpetas	7	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Carpeta eliminada: otraaaa (copia) (copia). Archivos: 11. Subcarpetas: 1
100	Carpetas	2	ELIMINAR	1	2026-01-23 13:47:52.689689	\N	\N	\N	Carpeta eliminada: otro. Archivos: 86. Subcarpetas: 6
101	Archivos	40	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: softsolutions20223-nublusoftapi-8a5edab282632443.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
102	Archivos	41	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: PLAN_MIGRACION_FRONTEND_NUBLUSOFT.md. Motivo: Eliminado con carpeta: otraaaa (copia)
103	Archivos	42	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: MAPEO_APIS_INTERFACES.md. Motivo: Eliminado con carpeta: otraaaa (copia)
104	Archivos	43	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: parte 2.docx. Motivo: Eliminado con carpeta: otraaaa (copia)
105	Archivos	44	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: GUIA_COMPLETA_FIRMA_ELECTRONICA_NOTIFICACIONES.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
106	Archivos	45	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte6.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
107	Archivos	46	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte3.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
108	Archivos	47	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte5.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
109	Archivos	48	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte4.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
110	Archivos	49	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte2.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
111	Archivos	50	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: guia_firma_notificaciones_parte1.txt. Motivo: Eliminado con carpeta: otraaaa (copia)
112	Archivos	51	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Archivo eliminado: analog-period-389922-7b9f19681b38.json. Motivo: Eliminado con carpeta: otraaaa (copia)
113	Carpetas	5	ELIMINAR	1	2026-01-23 13:47:58.888824	\N	\N	\N	Carpeta eliminada: otraaaa (copia). Archivos: 12. Subcarpetas: 0
\.


--
-- Data for Name: Medios_Recepcion; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Medios_Recepcion" ("Cod", "Nombre", "RequiereDigitalizacion", "Estado") FROM stdin;
PRESENCIAL	Presencial	t	t
CORREO_FISICO	Correo Físico	t	t
CORREO_ELECTRONICO	Correo Electrónico	f	t
WEB	Portal Web	f	t
FAX	Fax	t	t
BUZON	Buzón de Sugerencias	t	t
TELEFONO	Telefónico	f	t
\.


--
-- Data for Name: Niveles_Visualizacion_Carpetas; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Niveles_Visualizacion_Carpetas" ("Cod", "Nombre") FROM stdin;
1	Público
2	Interno
3	Confidencial
4	Reservado
5	Altamente Reservado
\.


--
-- Data for Name: Notificaciones; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Notificaciones" ("Cod", "Entidad", "UsuarioDestino", "UsuarioOrigen", "TipoNotificacion", "Titulo", "Mensaje", "Prioridad", "FechaCreacion", "FechaLeido", "FechaAccion", "Estado", "RadicadoRef", "ArchivoRef", "CarpetaRef", "SolicitudFirmaRef", "RequiereAccion", "FechaVencimientoAccion", "UrlAccion", "DatosAdicionales", "EnviadoPorCorreo", "FechaEnvioCorreo") FROM stdin;
\.


--
-- Data for Name: Oficinas; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Oficinas" ("Cod", "Nombre", "Estado", "Entidad", "CodigoSerie", "Icono", "Codigo", "OficinaPadre", "Responsable", "Telefono", "Correo", "Ubicacion", "NivelJerarquico", "FechaCreacion", "Sigla") FROM stdin;
1	Despacho del Alcalde	t	1	\N	\N	100	\N	1	\N	\N	\N	1	2025-12-31 10:35:24.124692	DA
5	Ventanilla Única de Correspondencia	t	1	\N	\N	201	2	3	\N	\N	\N	2	2025-12-31 10:35:24.124692	VUC
6	Archivo Central	t	1	\N	\N	202	2	\N	\N	\N	\N	2	2025-12-31 10:35:24.124692	AC
7	Gestión Documental	t	1	\N	\N	203	2	\N	\N	\N	\N	2	2025-12-31 10:35:24.124692	GD
1	Despacho del Gobernador	t	2	\N	\N	100	\N	5	\N	\N	\N	1	2025-12-31 10:35:24.124692	DG
2	Secretaría General	t	2	\N	\N	200	\N	6	\N	\N	\N	1	2025-12-31 10:35:24.124692	SG
3	Secretaría de Hacienda	t	2	\N	\N	300	\N	\N	\N	\N	\N	1	2025-12-31 10:35:24.124692	SH
2	Secretaría General	t	1	\N	\N	200	\N	2	\N	\N	\N	\N	2025-12-31 10:35:24.124692	SG
4	Secretaría de Planeación	f	1	\N	\N	400	\N	\N	\N	\N	\N	1	2025-12-31 10:35:24.124692	SP
3	Secretaría de Hacienda	f	1	\N	\N	300	\N	\N	\N	\N	\N	1	2025-12-31 10:35:24.124692	SH
8	Nueva	t	1	\N	\N	106	\N	3	\N	\N	\N	\N	2026-01-24 10:15:49.999209	NE
\.


--
-- Data for Name: Oficinas_TRD; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Oficinas_TRD" ("Cod", "Oficina", "TRD", "Entidad", "PuedeEditar", "PuedeEliminar", "FechaAsignacion", "Estado") FROM stdin;
\.


--
-- Data for Name: Origenes_Documentos; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Origenes_Documentos" ("Cod", "Nombre", "Descripcion") FROM stdin;
NATIVO	Nativo Digital	Documento creado digitalmente
ESCANEADO	Escaneado	Documento físico digitalizado
CONVERTIDO	Convertido	Documento convertido de otro formato
MIGRADO	Migrado	Documento migrado de otro sistema
\.


--
-- Data for Name: Prestamos_Expedientes; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Prestamos_Expedientes" ("Cod", "Carpeta", "SolicitadoPor", "AutorizadoPor", "FechaSolicitud", "FechaAutorizacion", "FechaPrestamo", "FechaDevolucionEsperada", "FechaDevolucionReal", "Estado", "Motivo", "Observaciones", "ObservacionesDevolucion") FROM stdin;
\.


--
-- Data for Name: Prioridades_Radicado; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Prioridades_Radicado" ("Cod", "Nombre", "DiasReduccion", "Color", "Orden") FROM stdin;
NORMAL	Normal	0	#3498db	1
URGENTE	Urgente	3	#f39c12	2
MUY_URGENTE	Muy Urgente	5	#e74c3c	3
\.


--
-- Data for Name: Radicados; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Radicados" ("Cod", "NumeroRadicado", "AnioRadicado", "ConsecutivoAnio", "TipoComunicacion", "TipoSolicitud", "Prioridad", "Confidencial", "FechaRadicacion", "HoraRadicacion", "UsuarioRadica", "OficinaRadica", "MedioRecepcion", "Tercero", "OficinaRemitente", "UsuarioRemitente", "NombreRemitente", "CorreoRemitente", "TelefonoRemitente", "DireccionRemitente", "OficinaDestino", "UsuarioDestino", "TerceroDestino", "NombreDestinatario", "Asunto", "Descripcion", "Folios", "Anexos", "PalabrasClave", "FechaVencimiento", "FechaProrroga", "MotivoProrroga", "Estado", "FechaCambioEstado", "FechaRespuesta", "RadicadoRespuesta", "ExpedienteVinculado", "FechaArchivado", "RadicadoPadre", "TipoRelacion", "NumeroReferencia", "FechaDocumento", "NotificadoPorCorreo", "FechaNotificacionCorreo", "NotificadoPorSMS", "FechaNotificacionSMS", "Entidad", "FechaCreacion", "FechaModificacion", "ModificadoPor", "Activo", "DatosAdicionales") FROM stdin;
\.


--
-- Data for Name: Radicados_Anexos; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Radicados_Anexos" ("Cod", "Radicado", "Entidad", "Nombre", "Descripcion", "Ruta", "Hash", "Tamaño", "Formato", "NumeroFolios", "TipoAnexo", "Orden", "FechaCarga", "SubidoPor", "Estado") FROM stdin;
\.


--
-- Data for Name: Radicados_Trazabilidad; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Radicados_Trazabilidad" ("Cod", "Radicado", "Entidad", "Accion", "Descripcion", "OficinaOrigen", "UsuarioOrigen", "OficinaDestino", "UsuarioDestino", "EstadoAnterior", "EstadoNuevo", "Fecha", "IP", "Observaciones") FROM stdin;
\.


--
-- Data for Name: Roles; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Roles" ("Cod", "Nombre") FROM stdin;
1	Administrador
2	Gestor Documental
3	Radicador
4	Consulta
5	Jefe de Oficina
6	Jefe de Oficina
\.


--
-- Data for Name: Roles_Usuarios; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Roles_Usuarios" ("Cod", "Oficina", "Rol", "Usuario", "Estado", "FechaInicio", "FechaFin") FROM stdin;
1	1	1	1	t	2025-12-31 10:35:24.124692	\N
2	2	1	1	t	2025-12-31 10:35:24.124692	\N
3	3	1	1	t	2025-12-31 10:35:24.124692	\N
4	2	2	2	t	2025-12-31 10:35:24.124692	\N
5	7	2	2	t	2025-12-31 10:35:24.124692	\N
6	5	4	3	t	2025-12-31 10:35:24.124692	\N
7	6	5	3	t	2025-12-31 10:35:24.124692	\N
8	1	1	5	t	2025-12-31 10:35:24.124692	\N
9	2	1	5	t	2025-12-31 10:35:24.124692	\N
10	2	2	6	t	2025-12-31 10:35:24.124692	\N
\.


--
-- Data for Name: Sectores; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Sectores" ("Cod", "Nombre", "Estado") FROM stdin;
1	Educación	t
2	Salud	t
3	Gobierno	t
4	Justicia	t
5	Defensa	t
6	Hacienda	t
7	Ambiente	t
8	Transporte	t
9	Cultura	t
10	Trabajo	t
\.


--
-- Data for Name: Solicitudes_Firma; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Solicitudes_Firma" ("Cod", "Entidad", "Archivo", "TipoFirma", "OrdenSecuencial", "RequiereTodos", "MinimoFirmantes", "Estado", "SolicitadoPor", "FechaSolicitud", "FechaVencimiento", "FechaCompletada", "FechaCancelada", "CanceladaPor", "MotivoCancelacion", "Asunto", "Mensaje", "HashOriginal", "AlgoritmoHash", "HashFinal", "CodigoVerificacion", "DatosAdicionales") FROM stdin;
\.


--
-- Data for Name: Soportes_Documento; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Soportes_Documento" ("Cod", "Nombre", "Descripcion") FROM stdin;
FISICO	Físico	Documento en papel
ELECTRONICO	Electrónico	Documento digital
HIBRIDO	Híbrido	Documento con componentes físicos y digitales
\.


--
-- Data for Name: Tablas_Retencion_Documental; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tablas_Retencion_Documental" ("Cod", "Nombre", "Tipo", "MesesVigencia", "Codigo", "TRDPadre", "Entidad", "DisposicionFinal", "TiempoGestion", "TiempoCentral", "Procedimiento", "Estado", "Descripcion", "CreadoPor", "FechaCreacion", "ModificadoPor", "FechaModificacion") FROM stdin;
2	nueva serie br	Serie	\N	100	\N	1	CT	5	5	safasf	f	aslljksda	1	2026-01-24 08:38:46.20926	1	2026-01-24 09:59:58.601602
1	Nueva seriee	Serie	\N	200	\N	1	E	5	5	lfafd	t	Contratos	1	2026-01-24 08:28:52.035959	1	2026-01-24 10:03:21.781526
\.


--
-- Data for Name: Terceros; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Terceros" ("Cod", "TipoPersona", "TipoDocumento", "NumeroDocumento", "RazonSocial", "Nombres", "Apellidos", "Direccion", "Ciudad", "Departamento", "Pais", "CodigoPostal", "Telefono", "Celular", "Correo", "CorreoAlternativo", "RepresentanteLegal", "NotificarPorCorreo", "NotificarPorSMS", "Observaciones", "Entidad", "FechaCreacion", "CreadoPor", "FechaModificacion", "ModificadoPor", "Estado") FROM stdin;
\.


--
-- Data for Name: Tipos_Archivos; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tipos_Archivos" ("Cod", "Nombre", "Estado") FROM stdin;
1	Documento	t
2	Imagen	t
3	Video	t
4	Audio	t
5	Hoja de Cálculo	t
6	Presentación	t
7	Comprimido	t
8	Otro	t
\.


--
-- Data for Name: Tipos_Carpetas; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tipos_Carpetas" ("Cod", "Nombre", "Estado") FROM stdin;
1	Serie Documental	t
2	Subserie Documental	t
3	Expediente	t
4	Carpeta Genérica	t
\.


--
-- Data for Name: Tipos_Comunicacion; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tipos_Comunicacion" ("Cod", "Nombre", "Descripcion", "RequiereRespuesta", "Prefijo") FROM stdin;
E	Entrada	Comunicación recibida de terceros	t	E
S	Salida	Comunicación enviada a terceros	f	S
I	Interna	Comunicación entre dependencias	f	I
\.


--
-- Data for Name: Tipos_Documentales; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tipos_Documentales" ("Cod", "Nombre", "Descripcion", "Estado") FROM stdin;
1	Acta	Documento que registra lo tratado en una reunión	t
2	Acuerdo	Decisión tomada de común acuerdo	t
3	Carta	Comunicación escrita dirigida a una persona	t
4	Certificado	Documento que certifica un hecho	t
5	Circular	Comunicación interna de carácter general	t
6	Concepto	Opinión o dictamen sobre un asunto	t
7	Constancia	Documento que hace constar un hecho	t
8	Contrato	Acuerdo legal entre partes	t
9	Convenio	Acuerdo entre entidades	t
10	Decreto	Disposición emanada del ejecutivo	t
11	Informe	Documento que presenta resultados de una gestión	t
12	Memorando	Comunicación interna breve	t
13	Oficio	Comunicación oficial externa	t
14	Resolución	Acto administrativo de carácter particular	t
15	Acta de Reunión	Registro de reunión de trabajo	t
16	Factura	Documento comercial	t
17	Orden de Pago	Autorización de pago	t
18	Poder	Autorización para actuar en nombre de otro	t
19	Petición	Solicitud formal ante autoridad	t
20	Tutela	Acción de tutela	t
\.


--
-- Data for Name: Tipos_Documento_Identidad; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tipos_Documento_Identidad" ("Cod", "Nombre", "Pais") FROM stdin;
CC	Cédula de Ciudadanía	Colombia
CE	Cédula de Extranjería	Colombia
NIT	Número de Identificación Tributaria	Colombia
TI	Tarjeta de Identidad	Colombia
PA	Pasaporte	Internacional
RC	Registro Civil	Colombia
PEP	Permiso Especial de Permanencia	Colombia
PPT	Permiso por Protección Temporal	Colombia
\.


--
-- Data for Name: Tipos_Entidad; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tipos_Entidad" ("Cod", "Nombre") FROM stdin;
PUBLICA	Entidad Pública
PRIVADA	Entidad Privada
MIXTA	Entidad Mixta
ONG	Organización No Gubernamental
COOPERATIVA	Cooperativa
\.


--
-- Data for Name: Tipos_Firma; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tipos_Firma" ("Cod", "Nombre", "Descripcion", "NivelSeguridad") FROM stdin;
SIMPLE	Firma Simple	Firma electrónica básica	1
AVANZADA	Firma Avanzada	Firma con certificado digital	2
CUALIFICADA	Firma Cualificada	Firma con certificado de entidad acreditada	3
\.


--
-- Data for Name: Tipos_Notificacion; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tipos_Notificacion" ("Cod", "Nombre", "Descripcion", "Icono", "Color", "EnviarCorreo", "RequiereAccion", "Plantilla", "Estado") FROM stdin;
ASIGNACION_EXPEDIENTE	Expediente Asignado	Se le ha asignado un expediente	folder	#2196F3	t	t	\N	t
ASIGNACION_ARCHIVO	Archivo Asignado	Se le ha asignado un archivo	description	#4CAF50	t	t	\N	t
RADICADO_RECIBIDO	Radicado Recibido	Ha recibido un nuevo radicado	mail	#FF9800	t	t	\N	t
RADICADO_TRASLADADO	Radicado Trasladado	Se le ha trasladado un radicado	forward	#9C27B0	t	t	\N	t
SOLICITUD_FIRMA	Solicitud de Firma	Documento pendiente por firmar	draw	#F44336	t	t	\N	t
FIRMA_COMPLETADA	Firma Completada	Su documento ha sido firmado	check_circle	#4CAF50	t	f	\N	t
FIRMA_RECHAZADA	Firma Rechazada	Su solicitud fue rechazada	cancel	#F44336	t	f	\N	t
VENCIMIENTO_PROXIMO	Vencimiento Próximo	Plazo próximo a vencer	schedule	#FF9800	t	t	\N	t
VENCIMIENTO_HOY	Vence Hoy	Plazo que vence hoy	alarm	#FF5722	t	t	\N	t
VENCIMIENTO_PASADO	Plazo Vencido	Plazo vencido	alarm_off	#F44336	t	t	\N	t
TRANSFERENCIA_RECIBIDA	Transferencia Recibida	Ha recibido una transferencia	swap_horiz	#00BCD4	t	t	\N	t
MENCION	Mención	Le han mencionado	alternate_email	#607D8B	t	f	\N	t
SISTEMA	Mensaje del Sistema	Notificación del sistema	info	#9E9E9E	f	f	\N	t
OTP_ENVIADO	Código OTP Enviado	Se ha enviado código OTP	vpn_key	#673AB7	f	f	\N	t
CERTIFICADO_CREADO	Certificado Creado	Se ha generado su certificado	verified	#009688	t	f	\N	t
CERTIFICADO_VENCIMIENTO	Certificado por Vencer	Certificado próximo a vencer	warning	#FF5722	t	t	\N	t
\.


--
-- Data for Name: Tipos_Solicitud; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Tipos_Solicitud" ("Cod", "Nombre", "Descripcion", "DiasHabilesRespuesta", "DiasHabilesProrroga", "EsCalendario", "RequiereRespuestaEscrita", "Normativa", "Estado") FROM stdin;
PETICION_GENERAL	Derecho de Petición General	Petición de interés general o particular	15	15	f	t	Ley 1755 de 2015, Art. 14	t
PETICION_INFO	Petición de Información	Solicitud de acceso a información pública	10	10	f	t	Ley 1755 de 2015, Art. 14	t
PETICION_CONSULTA	Petición de Consulta	Consulta sobre temas de competencia	30	30	f	t	Ley 1755 de 2015, Art. 14	t
TUTELA	Acción de Tutela	Mecanismo de protección de derechos fundamentales	10	0	t	t	Decreto 2591 de 1991	t
QUEJA	Queja	Manifestación de inconformidad	15	15	f	t	Ley 1755 de 2015	t
RECLAMO	Reclamo	Exigencia de derecho	15	15	f	t	Ley 1755 de 2015	t
SUGERENCIA	Sugerencia	Propuesta de mejora	15	0	f	f	Ley 1755 de 2015	t
RECURSO_REPOSICION	Recurso de Reposición	Recurso ante la misma autoridad	10	0	f	t	CPACA Art. 76	t
RECURSO_APELACION	Recurso de Apelación	Recurso ante el superior jerárquico	10	0	f	t	CPACA Art. 76	t
TRASLADO	Traslado por Competencia	Remisión a entidad competente	5	0	f	t	Ley 1755 de 2015, Art. 21	t
SILENCIO_POS	Silencio Administrativo Positivo	Solicitud con silencio positivo	15	0	f	t	CPACA	t
DENUNCIA	Denuncia	Comunicación de hechos irregulares	15	15	f	f	Ley 1755 de 2015	t
\.


--
-- Data for Name: Transferencias_Detalle; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Transferencias_Detalle" ("Cod", "Transferencia", "Carpeta", "Observaciones") FROM stdin;
\.


--
-- Data for Name: Transferencias_Documentales; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Transferencias_Documentales" ("Cod", "Entidad", "OficinaOrigen", "TipoTransferencia", "FechaSolicitud", "FechaAprobacion", "FechaEjecucion", "SolicitadoPor", "AprobadoPor", "Estado", "NumeroExpedientes", "NumeroFolios", "Observaciones") FROM stdin;
\.


--
-- Data for Name: Usuarios; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Usuarios" ("Cod", "Nombres", "Apellidos", "Telefono", "Documento", "Usuario", "Contraseña", "Estado", "Entidad", "Correo", "TipoDocumento", "Cargo", "Foto", "FechaCreacion", "FechaUltimoAcceso", "FirmaDigitalRuta", "FirmaDigitalVence", "IntentosFallidos", "FechaBloqueo", "DebeCambiarContrasena", "FechaCambioContrasena", "NotificacionesCorreo", "OficinaPredeterminada") FROM stdin;
7	Usuario	Vencido	3009999999	00000001	uvencido	123456	t	3	\N	\N	\N	\N	2026-01-24 10:53:06.131605	\N	\N	\N	0	\N	f	\N	t	\N
3	María Elena	López Rodríguez	3201234567	11223344	mlopez	123456	t	1	\N	\N	\N	\N	2026-01-24 10:53:06.131605	\N	\N	\N	0	\N	f	\N	t	\N
4	Pedro	Inactivo	3001111111	99999999	pinactivo	123456	f	1	\N	\N	\N	\N	2026-01-24 10:53:06.131605	\N	\N	\N	0	\N	f	\N	t	\N
6	Carlos Andrés	Martínez Ruiz	3178901234	44332211	cmartinez	123456	t	2	\N	\N	\N	\N	2026-01-24 10:53:06.131605	\N	\N	\N	0	\N	f	\N	t	\N
1	Administrador	Sistema	3001234567	12345678	admin	123456	t	1	\N	\N	\N	\N	2026-01-24 10:53:06.131605	\N	\N	\N	0	\N	f	\N	t	\N
5	Ana María	Gómez Torres	3156789012	55667788	agomez	123456	t	2	\N	\N	\N	\N	2026-01-24 10:53:06.131605	\N	\N	\N	0	\N	f	\N	t	\N
2	Juan Carlos	Pérez García	3109876543	87654321	jperez	123456	t	1	\N	\N	\N	\N	2026-01-24 10:53:06.131605	\N	\N	\N	0	\N	f	\N	t	\N
\.


--
-- Data for Name: Versiones_Archivos; Type: TABLE DATA; Schema: documentos; Owner: -
--

COPY documentos."Versiones_Archivos" ("Cod", "Archivo", "Version", "Ruta", "Hash", "Tamaño", "FechaVersion", "CreadoPor", "Comentario", "EsVersionActual", "Nombre", "AlgoritmoHash", "Formato", "NumeroHojas", "MotivoModificacion", "Estado") FROM stdin;
\.


--
-- Data for Name: BasesDatos; Type: TABLE DATA; Schema: usuarios; Owner: -
--

COPY usuarios."BasesDatos" ("Cod", "Bd", "Pw", "Usu", "Host", "CantEntidades", "MaxEntidades") FROM stdin;
1	NubluSoft	postgres123	postgres	localhost	1	15
\.


--
-- Data for Name: Entidades; Type: TABLE DATA; Schema: usuarios; Owner: -
--

COPY usuarios."Entidades" ("Cod", "Nombre", "Telefono", "Nit", "Direccion", "Correo", "FechaLimite", "Bd", "Url", "Coleccion") FROM stdin;
1	Alcaldía Municipal de Neiva	8713000	891.180.009-1	Calle 7 No. 6-34	contacto@alcaldianeiva.gov.co	2026-12-31	1	https://nublusoft.com/neiva	alcaldia_neiva
2	Gobernación del Huila	8671300	800.100.326-5	Calle 8 No. 4-50	contacto@huila.gov.co	2026-12-31	1	https://nublusoft.com/huila	gobernacion_huila
3	Empresa Plan Vencido SAS	3001234567	900.123.456-7	Calle Test 123	test@vencido.com	2024-01-01	1	\N	\N
\.


--
-- Data for Name: Planes; Type: TABLE DATA; Schema: usuarios; Owner: -
--

COPY usuarios."Planes" ("Cod", "Nombre", "Valor", "NumeroUsuarios") FROM stdin;
1	Prueba Gratuita	0.0000	3
2	Básico	150000.0000	5
3	Profesional	350000.0000	15
4	Empresarial	750000.0000	50
5	Corporativo	1500000.0000	200
6	Demo	0.0000	2
\.


--
-- Data for Name: Planes_Entidades; Type: TABLE DATA; Schema: usuarios; Owner: -
--

COPY usuarios."Planes_Entidades" ("Cod", "Entidad", "Plan", "Cantidad", "Valor", "Estado", "FechaInicio", "FechaFin") FROM stdin;
1	1	4	12.0000	9000000.0000	t	2025-01-01 00:00:00	2026-12-31 00:00:00
2	2	3	12.0000	4200000.0000	t	2025-01-01 00:00:00	2026-12-31 00:00:00
3	3	1	1.0000	0.0000	f	2023-01-01 00:00:00	2024-01-01 00:00:00
\.


--
-- Data for Name: Usuarios; Type: TABLE DATA; Schema: usuarios; Owner: -
--

COPY usuarios."Usuarios" ("Cod", "Nombres", "Apellidos", "Telefono", "Documento", "Usuario", "Contraseña", "Estado", "Entidad", "SesionActiva", "Token") FROM stdin;
2	Juan Carlos	Pérez García	3109876543	87654321	jperez	123456	t	1	f	\N
3	María Elena	López Rodríguez	3201234567	11223344	mlopez	123456	t	1	f	\N
4	Pedro	Inactivo	3001111111	99999999	pinactivo	123456	f	1	f	\N
6	Carlos Andrés	Martínez Ruiz	3178901234	44332211	cmartinez	123456	t	2	f	\N
7	Usuario	Vencido	3009999999	00000001	uvencido	123456	t	3	f	\N
5	Ana María	Gómez Torres	3156789012	55667788	agomez	123456	t	2	t	eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1IiwidW5pcXVlX25hbWUiOiJhZ29tZXoiLCJDb2QiOiI1IiwiVXN1YXJpbyI6ImFnb21leiIsIkVudGlkYWQiOiIyIiwiTm9tYnJlQ29tcGxldG8iOiJBbmEgTWFyw61hIEfDs21leiBUb3JyZXMiLCJOb21icmVFbnRpZGFkIjoiR29iZXJuYWNpw7NuIGRlbCBIdWlsYSIsIlNlc3Npb25JZCI6ImJmNzA1ZDQ3LTE3MzctNDZhYy1iMzA3LWZmZjEwMmNkYjY1NCIsImp0aSI6IjBjOTE1ZWNmLTg2ZTEtNDA5YS1hMWViLWYxNjZhNmZhZGIwNyIsImlhdCI6MTc2NzE5NTc0NCwicm9sZSI6WyJBZG1pbmlzdHJhZG9yIiwiQWRtaW5pc3RyYWRvciIsIkFkbWluaXN0cmFkb3IiLCJBZG1pbmlzdHJhZG9yIl0sIlJvbE9maWNpbmEiOlsiMToyIiwiMToxIiwiMToyIiwiMToxIl0sIm5iZiI6MTc2NzE5NTc0NCwiZXhwIjoxNzY3MjI0NTQ0LCJpc3MiOiJOdWJsdVNvZnRfRGV2IiwiYXVkIjoiTnVibHVTb2Z0Q2xpZW50c19EZXYifQ.MYfSE4a32n3hgOoN4VySk8C6l1eoXCMhXF2ZUc24QX4
1	Administrador	Sistema	3001234567	12345678	admin	123456	t	1	t	eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiIxIiwidW5pcXVlX25hbWUiOiJhZG1pbiIsIkNvZCI6IjEiLCJVc3VhcmlvIjoiYWRtaW4iLCJFbnRpZGFkIjoiMSIsIk5vbWJyZUNvbXBsZXRvIjoiQWRtaW5pc3RyYWRvciBTaXN0ZW1hIiwiTm9tYnJlRW50aWRhZCI6IkFsY2FsZMOtYSBNdW5pY2lwYWwgZGUgTmVpdmEiLCJTZXNzaW9uSWQiOiI3MzI4NzgwZi00NjZiLTQyOGEtYjRkOC1iYWYzMjkyMWQyZmEiLCJqdGkiOiJkOTFlY2VhYy0yNmE4LTRhYWYtYTY2ZC0yOWY0M2FhZmFjYmUiLCJpYXQiOjE3NjkyNjA1MjEsInJvbGUiOlsiQWRtaW5pc3RyYWRvciIsIkFkbWluaXN0cmFkb3IiLCJBZG1pbmlzdHJhZG9yIiwiQWRtaW5pc3RyYWRvciIsIkFkbWluaXN0cmFkb3IiLCJBZG1pbmlzdHJhZG9yIl0sIlJvbE9maWNpbmEiOlsiMTozIiwiMToyIiwiMToxIiwiMTozIiwiMToyIiwiMToxIl0sIm5iZiI6MTc2OTI2MDUyMSwiZXhwIjoxNzY5Mjg5MzIxLCJpc3MiOiJOdWJsdVNvZnQiLCJhdWQiOiJOdWJsdVNvZnRDbGllbnRzIn0.PwmHsPbAuL2Jnd-S31fnIKhrK7v-Tjzbjgikd-SEfcY
\.


--
-- Name: Certificados_Usuarios_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Certificados_Usuarios_Cod_seq"', 1, false);


--
-- Name: Codigos_OTP_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Codigos_OTP_Cod_seq"', 1, false);


--
-- Name: Configuracion_Firma_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Configuracion_Firma_Cod_seq"', 3, true);


--
-- Name: ErrorLog_Id_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."ErrorLog_Id_seq"', 1, false);


--
-- Name: Evidencias_Firma_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Evidencias_Firma_Cod_seq"', 1, false);


--
-- Name: Firmantes_Solicitud_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Firmantes_Solicitud_Cod_seq"', 1, false);


--
-- Name: Firmas_Archivos_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Firmas_Archivos_Cod_seq"', 1, false);


--
-- Name: Historial_Acciones_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Historial_Acciones_Cod_seq"', 113, true);


--
-- Name: Notificaciones_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Notificaciones_Cod_seq"', 1, false);


--
-- Name: Prestamos_Expedientes_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Prestamos_Expedientes_Cod_seq"', 1, false);


--
-- Name: Radicados_Anexos_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Radicados_Anexos_Cod_seq"', 1, false);


--
-- Name: Radicados_Trazabilidad_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Radicados_Trazabilidad_Cod_seq"', 1, false);


--
-- Name: Solicitudes_Firma_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Solicitudes_Firma_Cod_seq"', 1, false);


--
-- Name: Transferencias_Detalle_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Transferencias_Detalle_Cod_seq"', 1, false);


--
-- Name: Transferencias_Documentales_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Transferencias_Documentales_Cod_seq"', 1, false);


--
-- Name: Versiones_Archivos_Cod_seq; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."Versiones_Archivos_Cod_seq"', 1, false);


--
-- Name: seq_Radicados; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."seq_Radicados"', 1, false);


--
-- Name: seq_Terceros; Type: SEQUENCE SET; Schema: documentos; Owner: -
--

SELECT pg_catalog.setval('documentos."seq_Terceros"', 1, false);


--
-- Name: BasesDatos_Cod_seq; Type: SEQUENCE SET; Schema: usuarios; Owner: -
--

SELECT pg_catalog.setval('usuarios."BasesDatos_Cod_seq"', 2, false);


--
-- Name: seq_Entidades; Type: SEQUENCE SET; Schema: usuarios; Owner: -
--

SELECT pg_catalog.setval('usuarios."seq_Entidades"', 4, false);


--
-- Name: seq_Planes; Type: SEQUENCE SET; Schema: usuarios; Owner: -
--

SELECT pg_catalog.setval('usuarios."seq_Planes"', 1, false);


--
-- Name: seq_Planes_Entidades; Type: SEQUENCE SET; Schema: usuarios; Owner: -
--

SELECT pg_catalog.setval('usuarios."seq_Planes_Entidades"', 4, false);


--
-- Name: seq_Usuarios; Type: SEQUENCE SET; Schema: usuarios; Owner: -
--

SELECT pg_catalog.setval('usuarios."seq_Usuarios"', 8, false);


--
-- Name: Certificados_Usuarios Certificados_Usuarios_NumeroSerie_key; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Certificados_Usuarios"
    ADD CONSTRAINT "Certificados_Usuarios_NumeroSerie_key" UNIQUE ("NumeroSerie");


--
-- Name: Certificados_Usuarios Certificados_Usuarios_pkey; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Certificados_Usuarios"
    ADD CONSTRAINT "Certificados_Usuarios_pkey" PRIMARY KEY ("Cod");


--
-- Name: Codigos_OTP Codigos_OTP_pkey; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Codigos_OTP"
    ADD CONSTRAINT "Codigos_OTP_pkey" PRIMARY KEY ("Cod");


--
-- Name: Configuracion_Firma Configuracion_Firma_Entidad_key; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Configuracion_Firma"
    ADD CONSTRAINT "Configuracion_Firma_Entidad_key" UNIQUE ("Entidad");


--
-- Name: Configuracion_Firma Configuracion_Firma_pkey; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Configuracion_Firma"
    ADD CONSTRAINT "Configuracion_Firma_pkey" PRIMARY KEY ("Cod");


--
-- Name: Evidencias_Firma Evidencias_Firma_pkey; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Evidencias_Firma"
    ADD CONSTRAINT "Evidencias_Firma_pkey" PRIMARY KEY ("Cod");


--
-- Name: Firmantes_Solicitud Firmantes_Solicitud_Solicitud_Usuario_key; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Firmantes_Solicitud"
    ADD CONSTRAINT "Firmantes_Solicitud_Solicitud_Usuario_key" UNIQUE ("Solicitud", "Usuario");


--
-- Name: Firmantes_Solicitud Firmantes_Solicitud_pkey; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Firmantes_Solicitud"
    ADD CONSTRAINT "Firmantes_Solicitud_pkey" PRIMARY KEY ("Cod");


--
-- Name: Notificaciones Notificaciones_pkey; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Notificaciones"
    ADD CONSTRAINT "Notificaciones_pkey" PRIMARY KEY ("Cod");


--
-- Name: Archivos PK_Archivos; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Archivos"
    ADD CONSTRAINT "PK_Archivos" PRIMARY KEY ("Cod", "Estado", "Indice");


--
-- Name: Carpetas PK_Carpetas; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Carpetas"
    ADD CONSTRAINT "PK_Carpetas" PRIMARY KEY ("Cod", "Estado");


--
-- Name: Consecutivos_Radicado PK_Consecutivos_Radicado; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Consecutivos_Radicado"
    ADD CONSTRAINT "PK_Consecutivos_Radicado" PRIMARY KEY ("Entidad", "TipoComunicacion", "Anio");


--
-- Name: Dias_Festivos PK_Dias_Festivos; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Dias_Festivos"
    ADD CONSTRAINT "PK_Dias_Festivos" PRIMARY KEY ("Fecha");


--
-- Name: Disposiciones_Finales PK_Disposiciones_Finales; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Disposiciones_Finales"
    ADD CONSTRAINT "PK_Disposiciones_Finales" PRIMARY KEY ("Cod");


--
-- Name: Entidades PK_Entidades_Doc; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Entidades"
    ADD CONSTRAINT "PK_Entidades_Doc" PRIMARY KEY ("Cod", "Nit");


--
-- Name: ErrorLog PK_ErrorLog; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."ErrorLog"
    ADD CONSTRAINT "PK_ErrorLog" PRIMARY KEY ("Id");


--
-- Name: Estados_Radicado PK_Estados_Radicado; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Estados_Radicado"
    ADD CONSTRAINT "PK_Estados_Radicado" PRIMARY KEY ("Cod");


--
-- Name: Firmas_Archivos PK_Firmas_Archivos; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Firmas_Archivos"
    ADD CONSTRAINT "PK_Firmas_Archivos" PRIMARY KEY ("Cod");


--
-- Name: Frecuencias_Consulta PK_Frecuencias_Consulta; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Frecuencias_Consulta"
    ADD CONSTRAINT "PK_Frecuencias_Consulta" PRIMARY KEY ("Cod");


--
-- Name: Historial_Acciones PK_Historial_Acciones; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Historial_Acciones"
    ADD CONSTRAINT "PK_Historial_Acciones" PRIMARY KEY ("Cod");


--
-- Name: Medios_Recepcion PK_Medios_Recepcion; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Medios_Recepcion"
    ADD CONSTRAINT "PK_Medios_Recepcion" PRIMARY KEY ("Cod");


--
-- Name: Oficinas PK_Oficinas; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Oficinas"
    ADD CONSTRAINT "PK_Oficinas" PRIMARY KEY ("Entidad", "Cod");


--
-- Name: Oficinas_TRD PK_Oficinas_TRD; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Oficinas_TRD"
    ADD CONSTRAINT "PK_Oficinas_TRD" PRIMARY KEY ("Entidad", "Cod");


--
-- Name: Origenes_Documentos PK_Origenes_Documentos; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Origenes_Documentos"
    ADD CONSTRAINT "PK_Origenes_Documentos" PRIMARY KEY ("Cod");


--
-- Name: Prestamos_Expedientes PK_Prestamos_Expedientes; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Prestamos_Expedientes"
    ADD CONSTRAINT "PK_Prestamos_Expedientes" PRIMARY KEY ("Cod");


--
-- Name: Prioridades_Radicado PK_Prioridades_Radicado; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Prioridades_Radicado"
    ADD CONSTRAINT "PK_Prioridades_Radicado" PRIMARY KEY ("Cod");


--
-- Name: Radicados PK_Radicados; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Radicados"
    ADD CONSTRAINT "PK_Radicados" PRIMARY KEY ("Cod", "Entidad");


--
-- Name: Radicados_Anexos PK_Radicados_Anexos; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Radicados_Anexos"
    ADD CONSTRAINT "PK_Radicados_Anexos" PRIMARY KEY ("Cod");


--
-- Name: Radicados_Trazabilidad PK_Radicados_Trazabilidad; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Radicados_Trazabilidad"
    ADD CONSTRAINT "PK_Radicados_Trazabilidad" PRIMARY KEY ("Cod");


--
-- Name: Roles PK_Roles; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Roles"
    ADD CONSTRAINT "PK_Roles" PRIMARY KEY ("Cod");


--
-- Name: Roles_Usuarios PK_Roles_Usuarios; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Roles_Usuarios"
    ADD CONSTRAINT "PK_Roles_Usuarios" PRIMARY KEY ("Cod", "Oficina", "Estado");


--
-- Name: Sectores PK_Sectores; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Sectores"
    ADD CONSTRAINT "PK_Sectores" PRIMARY KEY ("Cod");


--
-- Name: Soportes_Documento PK_Soportes_Documento; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Soportes_Documento"
    ADD CONSTRAINT "PK_Soportes_Documento" PRIMARY KEY ("Cod");


--
-- Name: Tablas_Retencion_Documental PK_Tablas_Retencion_Documental; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Tablas_Retencion_Documental"
    ADD CONSTRAINT "PK_Tablas_Retencion_Documental" PRIMARY KEY ("Cod");


--
-- Name: Terceros PK_Terceros; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Terceros"
    ADD CONSTRAINT "PK_Terceros" PRIMARY KEY ("Cod", "Entidad");


--
-- Name: Tipos_Carpetas PK_Tipos_Carpetas; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Tipos_Carpetas"
    ADD CONSTRAINT "PK_Tipos_Carpetas" PRIMARY KEY ("Cod", "Estado");


--
-- Name: Tipos_Comunicacion PK_Tipos_Comunicacion; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Tipos_Comunicacion"
    ADD CONSTRAINT "PK_Tipos_Comunicacion" PRIMARY KEY ("Cod");


--
-- Name: Tipos_Documentales PK_Tipos_Documentales; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Tipos_Documentales"
    ADD CONSTRAINT "PK_Tipos_Documentales" PRIMARY KEY ("Cod");


--
-- Name: Tipos_Documento_Identidad PK_Tipos_Documento_Identidad; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Tipos_Documento_Identidad"
    ADD CONSTRAINT "PK_Tipos_Documento_Identidad" PRIMARY KEY ("Cod");


--
-- Name: Tipos_Entidad PK_Tipos_Entidad; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Tipos_Entidad"
    ADD CONSTRAINT "PK_Tipos_Entidad" PRIMARY KEY ("Cod");


--
-- Name: Tipos_Firma PK_Tipos_Firma; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Tipos_Firma"
    ADD CONSTRAINT "PK_Tipos_Firma" PRIMARY KEY ("Cod");


--
-- Name: Tipos_Solicitud PK_Tipos_Solicitud; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Tipos_Solicitud"
    ADD CONSTRAINT "PK_Tipos_Solicitud" PRIMARY KEY ("Cod");


--
-- Name: Transferencias_Detalle PK_Transferencias_Detalle; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Transferencias_Detalle"
    ADD CONSTRAINT "PK_Transferencias_Detalle" PRIMARY KEY ("Cod");


--
-- Name: Transferencias_Documentales PK_Transferencias_Documentales; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Transferencias_Documentales"
    ADD CONSTRAINT "PK_Transferencias_Documentales" PRIMARY KEY ("Cod");


--
-- Name: Usuarios PK_Usuarios_Doc; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Usuarios"
    ADD CONSTRAINT "PK_Usuarios_Doc" PRIMARY KEY ("Entidad", "Cod", "Usuario");


--
-- Name: Versiones_Archivos PK_Versiones_Archivos; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Versiones_Archivos"
    ADD CONSTRAINT "PK_Versiones_Archivos" PRIMARY KEY ("Cod");


--
-- Name: Solicitudes_Firma Solicitudes_Firma_CodigoVerificacion_key; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Solicitudes_Firma"
    ADD CONSTRAINT "Solicitudes_Firma_CodigoVerificacion_key" UNIQUE ("CodigoVerificacion");


--
-- Name: Solicitudes_Firma Solicitudes_Firma_pkey; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Solicitudes_Firma"
    ADD CONSTRAINT "Solicitudes_Firma_pkey" PRIMARY KEY ("Cod");


--
-- Name: Tipos_Notificacion Tipos_Notificacion_pkey; Type: CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Tipos_Notificacion"
    ADD CONSTRAINT "Tipos_Notificacion_pkey" PRIMARY KEY ("Cod");


--
-- Name: BasesDatos BasesDatos_pkey; Type: CONSTRAINT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."BasesDatos"
    ADD CONSTRAINT "BasesDatos_pkey" PRIMARY KEY ("Cod");


--
-- Name: Entidades PK_Entidades_Usuarios; Type: CONSTRAINT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."Entidades"
    ADD CONSTRAINT "PK_Entidades_Usuarios" PRIMARY KEY ("Cod");


--
-- Name: Planes PK_Planes; Type: CONSTRAINT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."Planes"
    ADD CONSTRAINT "PK_Planes" PRIMARY KEY ("Cod");


--
-- Name: Planes_Entidades PK_Planes_Entidades; Type: CONSTRAINT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."Planes_Entidades"
    ADD CONSTRAINT "PK_Planes_Entidades" PRIMARY KEY ("Cod");


--
-- Name: Usuarios PK_Usuarios_Auth; Type: CONSTRAINT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."Usuarios"
    ADD CONSTRAINT "PK_Usuarios_Auth" PRIMARY KEY ("Entidad", "Cod", "Usuario");


--
-- Name: IX_Archivos_Carpeta; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Archivos_Carpeta" ON documentos."Archivos" USING btree ("Carpeta");


--
-- Name: IX_Archivos_CodigoDocumento; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Archivos_CodigoDocumento" ON documentos."Archivos" USING btree ("CodigoDocumento");


--
-- Name: IX_Archivos_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Archivos_Estado" ON documentos."Archivos" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_Archivos_Hash; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Archivos_Hash" ON documentos."Archivos" USING btree ("Hash");


--
-- Name: IX_Archivos_SubidoPor; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Archivos_SubidoPor" ON documentos."Archivos" USING btree ("SubidoPor");


--
-- Name: IX_Carpetas_CarpetaPadre; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Carpetas_CarpetaPadre" ON documentos."Carpetas" USING btree ("CarpetaPadre");


--
-- Name: IX_Carpetas_CodigoExpediente; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Carpetas_CodigoExpediente" ON documentos."Carpetas" USING btree ("CodigoExpediente");


--
-- Name: IX_Carpetas_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Carpetas_Estado" ON documentos."Carpetas" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_Carpetas_SerieRaiz; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Carpetas_SerieRaiz" ON documentos."Carpetas" USING btree ("SerieRaiz");


--
-- Name: IX_Carpetas_TRD; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Carpetas_TRD" ON documentos."Carpetas" USING btree ("TRD");


--
-- Name: IX_Carpetas_TipoCarpeta; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Carpetas_TipoCarpeta" ON documentos."Carpetas" USING btree ("TipoCarpeta");


--
-- Name: IX_Certificados_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Certificados_Estado" ON documentos."Certificados_Usuarios" USING btree ("Estado") WHERE (("Estado")::text = 'ACTIVO'::text);


--
-- Name: IX_Certificados_Usuario; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Certificados_Usuario" ON documentos."Certificados_Usuarios" USING btree ("Usuario", "Entidad");


--
-- Name: IX_Entidades_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Entidades_Estado" ON documentos."Entidades" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_Entidades_Nit; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Entidades_Nit" ON documentos."Entidades" USING btree ("Nit");


--
-- Name: IX_Evidencias_Firmante; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Evidencias_Firmante" ON documentos."Evidencias_Firma" USING btree ("Firmante");


--
-- Name: IX_Firmantes_Solicitud; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Firmantes_Solicitud" ON documentos."Firmantes_Solicitud" USING btree ("Solicitud");


--
-- Name: IX_Firmantes_Usuario; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Firmantes_Usuario" ON documentos."Firmantes_Solicitud" USING btree ("Usuario", "Entidad", "Estado");


--
-- Name: IX_Firmas_Archivo; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Firmas_Archivo" ON documentos."Firmas_Archivos" USING btree ("Archivo");


--
-- Name: IX_Firmas_Usuario; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Firmas_Usuario" ON documentos."Firmas_Archivos" USING btree ("Usuario");


--
-- Name: IX_Historial_Fecha; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Historial_Fecha" ON documentos."Historial_Acciones" USING btree ("Fecha");


--
-- Name: IX_Historial_Tabla; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Historial_Tabla" ON documentos."Historial_Acciones" USING btree ("Tabla");


--
-- Name: IX_Historial_Usuario; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Historial_Usuario" ON documentos."Historial_Acciones" USING btree ("Usuario");


--
-- Name: IX_Notificaciones_FechaCreacion; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Notificaciones_FechaCreacion" ON documentos."Notificaciones" USING btree ("FechaCreacion" DESC);


--
-- Name: IX_Notificaciones_SolicitudFirma; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Notificaciones_SolicitudFirma" ON documentos."Notificaciones" USING btree ("SolicitudFirmaRef") WHERE ("SolicitudFirmaRef" IS NOT NULL);


--
-- Name: IX_Notificaciones_Usuario_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Notificaciones_Usuario_Estado" ON documentos."Notificaciones" USING btree ("UsuarioDestino", "Entidad", "Estado");


--
-- Name: IX_Notificaciones_Usuario_NoLeido; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Notificaciones_Usuario_NoLeido" ON documentos."Notificaciones" USING btree ("UsuarioDestino", "Entidad") WHERE ("FechaLeido" IS NULL);


--
-- Name: IX_OTP_Firmante; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_OTP_Firmante" ON documentos."Codigos_OTP" USING btree ("Firmante", "Usado");


--
-- Name: IX_Oficinas_Entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Oficinas_Entidad" ON documentos."Oficinas" USING btree ("Entidad");


--
-- Name: IX_Oficinas_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Oficinas_Estado" ON documentos."Oficinas" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_Oficinas_OficinaPadre; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Oficinas_OficinaPadre" ON documentos."Oficinas" USING btree ("OficinaPadre");


--
-- Name: IX_Oficinas_TRD_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Oficinas_TRD_Estado" ON documentos."Oficinas_TRD" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_Oficinas_TRD_Oficina; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Oficinas_TRD_Oficina" ON documentos."Oficinas_TRD" USING btree ("Oficina");


--
-- Name: IX_Oficinas_TRD_TRD; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Oficinas_TRD_TRD" ON documentos."Oficinas_TRD" USING btree ("TRD");


--
-- Name: IX_Oficinas_TRD_Unique; Type: INDEX; Schema: documentos; Owner: -
--

CREATE UNIQUE INDEX "IX_Oficinas_TRD_Unique" ON documentos."Oficinas_TRD" USING btree ("Oficina", "TRD", "Entidad");


--
-- Name: IX_Prestamos_Carpeta; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Prestamos_Carpeta" ON documentos."Prestamos_Expedientes" USING btree ("Carpeta");


--
-- Name: IX_Prestamos_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Prestamos_Estado" ON documentos."Prestamos_Expedientes" USING btree ("Estado");


--
-- Name: IX_Radicados_Activo; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_Activo" ON documentos."Radicados" USING btree ("Activo") WHERE ("Activo" = true);


--
-- Name: IX_Radicados_Anexos_Entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_Anexos_Entidad" ON documentos."Radicados_Anexos" USING btree ("Entidad");


--
-- Name: IX_Radicados_Anexos_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_Anexos_Estado" ON documentos."Radicados_Anexos" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_Radicados_Anexos_Radicado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_Anexos_Radicado" ON documentos."Radicados_Anexos" USING btree ("Radicado");


--
-- Name: IX_Radicados_AnioRadicado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_AnioRadicado" ON documentos."Radicados" USING btree ("AnioRadicado");


--
-- Name: IX_Radicados_Entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_Entidad" ON documentos."Radicados" USING btree ("Entidad");


--
-- Name: IX_Radicados_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_Estado" ON documentos."Radicados" USING btree ("Estado");


--
-- Name: IX_Radicados_FechaVencimiento; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_FechaVencimiento" ON documentos."Radicados" USING btree ("FechaVencimiento");


--
-- Name: IX_Radicados_NumeroRadicado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_NumeroRadicado" ON documentos."Radicados" USING btree ("NumeroRadicado");


--
-- Name: IX_Radicados_Numero_Entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE UNIQUE INDEX "IX_Radicados_Numero_Entidad" ON documentos."Radicados" USING btree ("NumeroRadicado", "Entidad");


--
-- Name: IX_Radicados_OficinaDestino; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_OficinaDestino" ON documentos."Radicados" USING btree ("OficinaDestino");


--
-- Name: IX_Radicados_Tercero; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_Tercero" ON documentos."Radicados" USING btree ("Tercero");


--
-- Name: IX_Radicados_TipoComunicacion; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_TipoComunicacion" ON documentos."Radicados" USING btree ("TipoComunicacion");


--
-- Name: IX_Radicados_UsuarioDestino; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Radicados_UsuarioDestino" ON documentos."Radicados" USING btree ("UsuarioDestino");


--
-- Name: IX_Roles_Usuarios_Oficina; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Roles_Usuarios_Oficina" ON documentos."Roles_Usuarios" USING btree ("Oficina");


--
-- Name: IX_Roles_Usuarios_Usuario; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Roles_Usuarios_Usuario" ON documentos."Roles_Usuarios" USING btree ("Usuario");


--
-- Name: IX_SolicitudesFirma_Archivo; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_SolicitudesFirma_Archivo" ON documentos."Solicitudes_Firma" USING btree ("Archivo", "Entidad");


--
-- Name: IX_SolicitudesFirma_CodigoVerif; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_SolicitudesFirma_CodigoVerif" ON documentos."Solicitudes_Firma" USING btree ("CodigoVerificacion");


--
-- Name: IX_SolicitudesFirma_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_SolicitudesFirma_Estado" ON documentos."Solicitudes_Firma" USING btree ("Estado", "Entidad");


--
-- Name: IX_SolicitudesFirma_Solicitante; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_SolicitudesFirma_Solicitante" ON documentos."Solicitudes_Firma" USING btree ("SolicitadoPor", "Entidad");


--
-- Name: IX_Solicitudes_Vencimiento; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Solicitudes_Vencimiento" ON documentos."Solicitudes_Firma" USING btree ("FechaVencimiento") WHERE ((("Estado")::text = ANY ((ARRAY['PENDIENTE'::character varying, 'EN_PROCESO'::character varying])::text[])) AND ("FechaVencimiento" IS NOT NULL));


--
-- Name: IX_TRD_Entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_TRD_Entidad" ON documentos."Tablas_Retencion_Documental" USING btree ("Entidad");


--
-- Name: IX_TRD_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_TRD_Estado" ON documentos."Tablas_Retencion_Documental" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_TRD_TRDPadre; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_TRD_TRDPadre" ON documentos."Tablas_Retencion_Documental" USING btree ("TRDPadre");


--
-- Name: IX_Terceros_Correo; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Terceros_Correo" ON documentos."Terceros" USING btree ("Correo");


--
-- Name: IX_Terceros_Entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Terceros_Entidad" ON documentos."Terceros" USING btree ("Entidad");


--
-- Name: IX_Terceros_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Terceros_Estado" ON documentos."Terceros" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_Terceros_NombreCompleto; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Terceros_NombreCompleto" ON documentos."Terceros" USING btree ("NombreCompleto");


--
-- Name: IX_Terceros_NumeroDocumento; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Terceros_NumeroDocumento" ON documentos."Terceros" USING btree ("NumeroDocumento");


--
-- Name: IX_Trazabilidad_Entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Trazabilidad_Entidad" ON documentos."Radicados_Trazabilidad" USING btree ("Entidad");


--
-- Name: IX_Trazabilidad_Fecha; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Trazabilidad_Fecha" ON documentos."Radicados_Trazabilidad" USING btree ("Fecha");


--
-- Name: IX_Trazabilidad_OficinaDestino; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Trazabilidad_OficinaDestino" ON documentos."Radicados_Trazabilidad" USING btree ("OficinaDestino");


--
-- Name: IX_Trazabilidad_Radicado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Trazabilidad_Radicado" ON documentos."Radicados_Trazabilidad" USING btree ("Radicado");


--
-- Name: IX_Usuarios_Documento; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Usuarios_Documento" ON documentos."Usuarios" USING btree ("Documento");


--
-- Name: IX_Usuarios_Entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Usuarios_Entidad" ON documentos."Usuarios" USING btree ("Entidad");


--
-- Name: IX_Usuarios_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Usuarios_Estado" ON documentos."Usuarios" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_Usuarios_Usuario; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Usuarios_Usuario" ON documentos."Usuarios" USING btree ("Usuario");


--
-- Name: IX_Versiones_Archivo; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Versiones_Archivo" ON documentos."Versiones_Archivos" USING btree ("Archivo");


--
-- Name: IX_Versiones_Archivo_Version; Type: INDEX; Schema: documentos; Owner: -
--

CREATE UNIQUE INDEX "IX_Versiones_Archivo_Version" ON documentos."Versiones_Archivos" USING btree ("Archivo", "Version");


--
-- Name: IX_Versiones_Estado; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX "IX_Versiones_Estado" ON documentos."Versiones_Archivos" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: UQ_Usuarios_Cod_Entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE UNIQUE INDEX "UQ_Usuarios_Cod_Entidad" ON documentos."Usuarios" USING btree ("Cod", "Entidad");


--
-- Name: idx_archivos_estado_upload; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX idx_archivos_estado_upload ON documentos."Archivos" USING btree ("EstadoUpload") WHERE (("EstadoUpload")::text = 'PENDIENTE'::text);


--
-- Name: idx_carpetas_entidad; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX idx_carpetas_entidad ON documentos."Carpetas" USING btree ("Entidad") WHERE ("Estado" = true);


--
-- Name: idx_carpetas_entidad_tipo; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX idx_carpetas_entidad_tipo ON documentos."Carpetas" USING btree ("Entidad", "TipoCarpeta") WHERE ("Estado" = true);


--
-- Name: idx_carpetas_oficina; Type: INDEX; Schema: documentos; Owner: -
--

CREATE INDEX idx_carpetas_oficina ON documentos."Carpetas" USING btree ("Oficina") WHERE ("Estado" = true);


--
-- Name: IX_Entidades_Nit; Type: INDEX; Schema: usuarios; Owner: -
--

CREATE INDEX "IX_Entidades_Nit" ON usuarios."Entidades" USING btree ("Nit");


--
-- Name: IX_Planes_Entidades_Entidad; Type: INDEX; Schema: usuarios; Owner: -
--

CREATE INDEX "IX_Planes_Entidades_Entidad" ON usuarios."Planes_Entidades" USING btree ("Entidad");


--
-- Name: IX_Planes_Entidades_Estado; Type: INDEX; Schema: usuarios; Owner: -
--

CREATE INDEX "IX_Planes_Entidades_Estado" ON usuarios."Planes_Entidades" USING btree ("Estado") WHERE ("Estado" = true);


--
-- Name: IX_Usuarios_Documento; Type: INDEX; Schema: usuarios; Owner: -
--

CREATE INDEX "IX_Usuarios_Documento" ON usuarios."Usuarios" USING btree ("Documento");


--
-- Name: IX_Usuarios_Entidad; Type: INDEX; Schema: usuarios; Owner: -
--

CREATE INDEX "IX_Usuarios_Entidad" ON usuarios."Usuarios" USING btree ("Entidad");


--
-- Name: IX_Usuarios_Entidad_Usuario; Type: INDEX; Schema: usuarios; Owner: -
--

CREATE UNIQUE INDEX "IX_Usuarios_Entidad_Usuario" ON usuarios."Usuarios" USING btree ("Entidad", "Usuario");


--
-- Name: IX_Usuarios_Usuario; Type: INDEX; Schema: usuarios; Owner: -
--

CREATE INDEX "IX_Usuarios_Usuario" ON usuarios."Usuarios" USING btree ("Usuario");


--
-- Name: Archivos TR_Archivos_NotificarIndice; Type: TRIGGER; Schema: documentos; Owner: -
--

CREATE TRIGGER "TR_Archivos_NotificarIndice" AFTER INSERT OR DELETE OR UPDATE ON documentos."Archivos" FOR EACH ROW EXECUTE FUNCTION documentos."F_NotificarCambioIndice"();


--
-- Name: Archivos TR_Archivos_NotifyNavIndex; Type: TRIGGER; Schema: documentos; Owner: -
--

CREATE TRIGGER "TR_Archivos_NotifyNavIndex" AFTER INSERT OR DELETE OR UPDATE OF "Estado" ON documentos."Archivos" FOR EACH ROW EXECUTE FUNCTION documentos."F_NotifyNavIndex_Archivos"();


--
-- Name: Carpetas TR_Carpetas_NotificarIndice; Type: TRIGGER; Schema: documentos; Owner: -
--

CREATE TRIGGER "TR_Carpetas_NotificarIndice" AFTER INSERT OR DELETE OR UPDATE ON documentos."Carpetas" FOR EACH ROW EXECUTE FUNCTION documentos."F_NotificarCambioCarpeta"();


--
-- Name: Carpetas TR_Carpetas_NotifyNavIndex; Type: TRIGGER; Schema: documentos; Owner: -
--

CREATE TRIGGER "TR_Carpetas_NotifyNavIndex" AFTER INSERT OR DELETE OR UPDATE ON documentos."Carpetas" FOR EACH ROW EXECUTE FUNCTION documentos."F_NotifyNavIndex"();


--
-- Name: TRIGGER "TR_Carpetas_NotifyNavIndex" ON "Carpetas"; Type: COMMENT; Schema: documentos; Owner: -
--

COMMENT ON TRIGGER "TR_Carpetas_NotifyNavIndex" ON documentos."Carpetas" IS 'Notifica cambios en carpetas al servicio NavIndex para invalidar caché';


--
-- Name: Evidencias_Firma TR_Evidencias_AppendOnly; Type: TRIGGER; Schema: documentos; Owner: -
--

CREATE TRIGGER "TR_Evidencias_AppendOnly" BEFORE DELETE OR UPDATE ON documentos."Evidencias_Firma" FOR EACH ROW EXECUTE FUNCTION documentos."F_PrevenirModificacionEvidencias"();


--
-- Name: Notificaciones TR_Notificacion_Creada; Type: TRIGGER; Schema: documentos; Owner: -
--

CREATE TRIGGER "TR_Notificacion_Creada" AFTER INSERT ON documentos."Notificaciones" FOR EACH ROW EXECUTE FUNCTION documentos."F_TriggerNuevaNotificacion"();


--
-- Name: Radicados TR_Radicados_HoraRadicacion; Type: TRIGGER; Schema: documentos; Owner: -
--

CREATE TRIGGER "TR_Radicados_HoraRadicacion" BEFORE INSERT OR UPDATE OF "FechaRadicacion" ON documentos."Radicados" FOR EACH ROW EXECUTE FUNCTION documentos."TRG_Radicados_HoraRadicacion"();


--
-- Name: Solicitudes_Firma TR_SolicitudFirma_Creada; Type: TRIGGER; Schema: documentos; Owner: -
--

CREATE TRIGGER "TR_SolicitudFirma_Creada" AFTER INSERT ON documentos."Solicitudes_Firma" FOR EACH ROW EXECUTE FUNCTION documentos."F_TriggerNuevaSolicitudFirma"();


--
-- Name: Codigos_OTP Codigos_OTP_Firmante_fkey; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Codigos_OTP"
    ADD CONSTRAINT "Codigos_OTP_Firmante_fkey" FOREIGN KEY ("Firmante") REFERENCES documentos."Firmantes_Solicitud"("Cod") ON DELETE CASCADE;


--
-- Name: Configuracion_Firma Configuracion_Firma_Entidad_fkey; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Configuracion_Firma"
    ADD CONSTRAINT "Configuracion_Firma_Entidad_fkey" FOREIGN KEY ("Entidad") REFERENCES usuarios."Entidades"("Cod");


--
-- Name: Evidencias_Firma Evidencias_Firma_Firmante_fkey; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Evidencias_Firma"
    ADD CONSTRAINT "Evidencias_Firma_Firmante_fkey" FOREIGN KEY ("Firmante") REFERENCES documentos."Firmantes_Solicitud"("Cod") ON DELETE CASCADE;


--
-- Name: Radicados_Anexos FK_Radicados_Anexos_Radicado; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Radicados_Anexos"
    ADD CONSTRAINT "FK_Radicados_Anexos_Radicado" FOREIGN KEY ("Radicado", "Entidad") REFERENCES documentos."Radicados"("Cod", "Entidad");


--
-- Name: Radicados FK_Radicados_Tercero; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Radicados"
    ADD CONSTRAINT "FK_Radicados_Tercero" FOREIGN KEY ("Tercero", "Entidad") REFERENCES documentos."Terceros"("Cod", "Entidad");


--
-- Name: Radicados_Trazabilidad FK_Trazabilidad_Radicado; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Radicados_Trazabilidad"
    ADD CONSTRAINT "FK_Trazabilidad_Radicado" FOREIGN KEY ("Radicado", "Entidad") REFERENCES documentos."Radicados"("Cod", "Entidad");


--
-- Name: Firmantes_Solicitud Firmantes_Solicitud_CertificadoUsado_fkey; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Firmantes_Solicitud"
    ADD CONSTRAINT "Firmantes_Solicitud_CertificadoUsado_fkey" FOREIGN KEY ("CertificadoUsado") REFERENCES documentos."Certificados_Usuarios"("Cod");


--
-- Name: Firmantes_Solicitud Firmantes_Solicitud_Solicitud_fkey; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Firmantes_Solicitud"
    ADD CONSTRAINT "Firmantes_Solicitud_Solicitud_fkey" FOREIGN KEY ("Solicitud") REFERENCES documentos."Solicitudes_Firma"("Cod") ON DELETE CASCADE;


--
-- Name: Notificaciones Notificaciones_TipoNotificacion_fkey; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Notificaciones"
    ADD CONSTRAINT "Notificaciones_TipoNotificacion_fkey" FOREIGN KEY ("TipoNotificacion") REFERENCES documentos."Tipos_Notificacion"("Cod");


--
-- Name: Oficinas_TRD Oficinas_TRD_TRD_fkey; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Oficinas_TRD"
    ADD CONSTRAINT "Oficinas_TRD_TRD_fkey" FOREIGN KEY ("TRD") REFERENCES documentos."Tablas_Retencion_Documental"("Cod");


--
-- Name: Roles_Usuarios Roles_Usuarios_Rol_fkey; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Roles_Usuarios"
    ADD CONSTRAINT "Roles_Usuarios_Rol_fkey" FOREIGN KEY ("Rol") REFERENCES documentos."Roles"("Cod");


--
-- Name: Transferencias_Detalle Transferencias_Detalle_Transferencia_fkey; Type: FK CONSTRAINT; Schema: documentos; Owner: -
--

ALTER TABLE ONLY documentos."Transferencias_Detalle"
    ADD CONSTRAINT "Transferencias_Detalle_Transferencia_fkey" FOREIGN KEY ("Transferencia") REFERENCES documentos."Transferencias_Documentales"("Cod");


--
-- Name: Entidades Entidades_Bd_fkey; Type: FK CONSTRAINT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."Entidades"
    ADD CONSTRAINT "Entidades_Bd_fkey" FOREIGN KEY ("Bd") REFERENCES usuarios."BasesDatos"("Cod");


--
-- Name: Planes_Entidades Planes_Entidades_Entidad_fkey; Type: FK CONSTRAINT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."Planes_Entidades"
    ADD CONSTRAINT "Planes_Entidades_Entidad_fkey" FOREIGN KEY ("Entidad") REFERENCES usuarios."Entidades"("Cod");


--
-- Name: Planes_Entidades Planes_Entidades_Plan_fkey; Type: FK CONSTRAINT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."Planes_Entidades"
    ADD CONSTRAINT "Planes_Entidades_Plan_fkey" FOREIGN KEY ("Plan") REFERENCES usuarios."Planes"("Cod");


--
-- Name: Usuarios Usuarios_Entidad_fkey; Type: FK CONSTRAINT; Schema: usuarios; Owner: -
--

ALTER TABLE ONLY usuarios."Usuarios"
    ADD CONSTRAINT "Usuarios_Entidad_fkey" FOREIGN KEY ("Entidad") REFERENCES usuarios."Entidades"("Cod");


--
-- PostgreSQL database dump complete
--

