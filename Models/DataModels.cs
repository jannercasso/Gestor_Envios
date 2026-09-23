using System.Collections.ObjectModel;

namespace GestorEnvios.Models
{
    public class DataModels
    {
        public ObservableCollection<EnvioData> DataRecords { get; set; } = new();
        public ObservableCollection<EnvioData> EnviosRecords { get; set; } = new();
        public ObservableCollection<EnvioData> Resultados { get; set; } = new();
        public ObservableCollection<CentroDestino> CentrosRecords { get; set; } = new();
    }

    public class CentroDestino
    {
        public string? Centro { get; set; }
        public string? CiudadDestino { get; set; }
        public string? Ciudad { get; set; }
        public string? Depto { get; set; }
        public string? CodigoParaRecogidas { get; set; }

        // ✅ Solo para Plan B (hoja Db_Shipto)
        public string? ShipTo { get; set; }
        public string? ShiptoName1 { get; set; }
        public string? ShiptoStreet { get; set; }
        public string? ShiptoCity { get; set; }
    }

    // ================================================================
    // Vista cruda de la hoja Envíos + Street/City (Plan B)
    // ================================================================
    public class EnvioRawView
    {
        public string? IdEnvio { get; set; }
        public string? NombreUbicacionDestino { get; set; }
        public string? IdUbicacionDestino { get; set; }
        public double? PesoKG { get; set; }
        public double? VolumenCUM { get; set; }
        public int? Piezas { get; set; }
        public string? IdCarga { get; set; }
        public int? NumeroSecuencia { get; set; }
        public string? IdUbicacionOrigen { get; set; }
        public string? DireccionDestino { get; set; }
        public string? Corredor { get; set; }
        public string? FechaLlegada { get; set; }
        public string? EstadoOperativo { get; set; }
        public string? NombreCarrier { get; set; }
        public string? IdTransportista { get; set; }
        public string? Vhc { get; set; }
        public string? CostoEstandar { get; set; }

        // ✅ Traídas de Db_Shipto
        public string? Street { get; set; }
        public string? City { get; set; }
    }
}