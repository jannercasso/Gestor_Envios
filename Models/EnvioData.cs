namespace GestorEnvios.Models
{
    public class EnvioData
    {
        // Columnas de DATA (igual que tu macro)
        public string? ShipmentNumber { get; set; }
        public string? Delivery { get; set; }
        public string? Name { get; set; }
        public string? ShipmentType { get; set; }
        public string? ShipToParty { get; set; }
        public string? VehicleType { get; set; }
        public string? Description { get; set; }
        public string? ServiceAgent { get; set; }
        public string? Name1 { get; set; }
        public string? DeliveryDate { get; set; }
        public double? Weight { get; set; }
        public double? Volume { get; set; }
        public int? Count { get; set; }
        public string? OrderNumber { get; set; }
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? Fecha1 { get; set; }
        public string? Fecha2 { get; set; }
        public string? Tipo1 { get; set; }
        public string? Tipo2 { get; set; }
        public string? Factura { get; set; }
        
        // Columnas de ENVIOS
        public int? Secuencia { get; set; }
        public string? Vhc { get; set; }
        public double? CostoEstandar { get; set; }
        public string? IdCarga { get; set; }
        
        // Centro Destino (calculado)
        public string? CentroDestino { get; set; }
        
        // Para control de duplicados (como la macro)
        public bool EsDuplicado { get; set; }
    }

    // Clase auxiliar para almacenar datos de envío
    public class EnvioInfo
    {
        public string? IdCarga { get; set; }
        public int? Secuencia { get; set; }
        public string? Vhc { get; set; }
        public double? CostoEstandar { get; set; }
    }
}