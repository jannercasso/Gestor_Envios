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
    }
}