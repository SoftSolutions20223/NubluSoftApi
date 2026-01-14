namespace NubluSoft_Core.Models.DTOs
{
    // ==================== REQUESTS ====================

    /// <summary>
    /// Request para crear o actualizar un día festivo
    /// </summary>
    public class CrearDiaFestivoRequest
    {
        public DateTime Fecha { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EsFijo { get; set; }
        public string Pais { get; set; } = "Colombia";
    }

    /// <summary>
    /// Request para actualizar un día festivo existente
    /// </summary>
    public class ActualizarDiaFestivoRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public bool EsFijo { get; set; }
        public string Pais { get; set; } = "Colombia";
    }

    /// <summary>
    /// Request para calcular fecha con días hábiles
    /// </summary>
    public class CalcularDiasHabilesRequest
    {
        public DateTime FechaInicio { get; set; }
        public int DiasHabiles { get; set; }
    }

    /// <summary>
    /// Request para generar festivos de un año
    /// </summary>
    public class GenerarFestivosAnioRequest
    {
        public int Anio { get; set; }
        public bool SobreescribirExistentes { get; set; } = false;
    }

    /// <summary>
    /// Filtros para consultar días festivos
    /// </summary>
    public class FiltrosDiasFestivosRequest
    {
        public int? Anio { get; set; }
        public string? Pais { get; set; }
        public bool? SoloFijos { get; set; }
        public DateTime? FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }

    // ==================== RESPONSES ====================

    /// <summary>
    /// Día festivo
    /// </summary>
    public class DiaFestivoDto
    {
        public DateTime Fecha { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public bool EsFijo { get; set; }
        public string Pais { get; set; } = string.Empty;
        public string DiaSemana { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de verificación de día hábil
    /// </summary>
    public class VerificacionDiaHabilDto
    {
        public DateTime Fecha { get; set; }
        public bool EsDiaHabil { get; set; }
        public string? Motivo { get; set; }
        public string DiaSemana { get; set; } = string.Empty;
    }

    /// <summary>
    /// Resultado de cálculo de días hábiles
    /// </summary>
    public class CalculoDiasHabilesDto
    {
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public int DiasHabilesSolicitados { get; set; }
        public int DiasCalendarioTotal { get; set; }
        public int DiasFestivosEnRango { get; set; }
        public int FinesSemanasEnRango { get; set; }
    }

    /// <summary>
    /// Resumen de días festivos
    /// </summary>
    public class ResumenDiasFestivosDto
    {
        public int Anio { get; set; }
        public int TotalFestivos { get; set; }
        public int FestivosFijos { get; set; }
        public int FestivosMoviles { get; set; }
        public DiaFestivoDto? ProximoFestivo { get; set; }
        public IEnumerable<DiaFestivoDto> FestivosDelMes { get; set; } = new List<DiaFestivoDto>();
    }

    /// <summary>
    /// Resultado de operación CRUD
    /// </summary>
    public class ResultadoDiaFestivoDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public DiaFestivoDto? DiaFestivo { get; set; }
    }

    /// <summary>
    /// Resultado de generación masiva
    /// </summary>
    public class ResultadoGeneracionFestivosDto
    {
        public bool Exito { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public int FestivosCreados { get; set; }
        public int FestivosOmitidos { get; set; }
        public IEnumerable<string> Errores { get; set; } = new List<string>();
    }
}