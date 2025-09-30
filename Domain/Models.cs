using System;
using System.Collections.Generic;

namespace Domain
{
    public enum TipoEquipo { Desconocido = 0, Conveyor = 1, Empacadora = 2, Etiquetadora = 3, Otro = 99 }

    public class Area
    {
        public Guid Id { get; set; }
        public string Nombre { get; set; } = "";
        public bool Activo { get; set; } = true;
        public ICollection<Equipo> Equipos { get; set; } = new List<Equipo>();
    }

    public class Equipo
    {
        public Guid Id { get; set; }
        public TipoEquipo Tipo { get; set; }
        public string Codigo { get; set; } = "";
        public Guid AreaId { get; set; }
        public Area Area { get; set; } = null!;
        public bool Activo { get; set; } = true;
        public ICollection<Label> Labels { get; set; } = new List<Label>();
    }

    public class Label
    {
        public Guid Id { get; set; }
        public Guid EquipoId { get; set; }
        public Equipo Equipo { get; set; } = null!;
        public DateTime FechaCreacionLabel { get; set; }
        public DateTime FechaActualizacionLabel { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public int Revision { get; set; } = 1;
        public string CreadoPor { get; set; } = "";
        public string? Observacion { get; set; }
        public string? FotoUrl { get; set; }
        public byte[]? FotoBlob { get; set; }
        public string? FotoContentType { get; set; }
        public ICollection<HistorialLabel> Historial { get; set; } = new List<HistorialLabel>();
    }

    public class HistorialLabel
    {
        public Guid Id { get; set; }
        public Guid LabelId { get; set; }
        public Label Label { get; set; } = null!;
        public DateTime FechaCambio { get; set; } = DateTime.UtcNow;
        public int? RevisionAnterior { get; set; }
        public int RevisionNueva { get; set; }
        public string Usuario { get; set; } = "";
        public string? Comentario { get; set; }
    }

    public class SuscripcionAlerta
    {
        public Guid Id { get; set; }
        public Guid? AreaId { get; set; }
        public Guid? EquipoId { get; set; }
        public Guid? LabelId { get; set; }
        public string Email { get; set; } = "";
        public int UmbralDias { get; set; } = 30;
        public bool Activo { get; set; } = true;
    }

    public enum EstadoVencimiento { Ok, Proximo, Vencido }

    public static class VencimientoHelper
    {
        public static EstadoVencimiento Calcular(DateTime vence, int umbral = 30)
        {
            var d = (vence.Date - DateTime.UtcNow.Date).TotalDays;
            if (d < 0) return EstadoVencimiento.Vencido;
            if (d <= umbral) return EstadoVencimiento.Proximo;
            return EstadoVencimiento.Ok;
        }
    }
}
