using System;
using System.Collections.Generic;
using System.Linq;
using GestorEnvios.Models;

namespace GestorEnvios.Services
{
    public class DataProcessor
    {
        private readonly ExcelService _excelService;
        public DataModels Models { get; set; }
        public List<string> Errores { get; private set; } = new();
        public List<string> CiudadesNoEncontradas { get; private set; } = new();
        public List<string> EntregasNoEncontradas { get; private set; } = new();

        public DataProcessor()
        {
            _excelService = new ExcelService();
            Models = new DataModels();
        }

        public void LoadDataFromSingleFile(string filePath)
        {
            Models.DataRecords.Clear();
            Models.EnviosRecords.Clear();
            Models.CentrosRecords.Clear();
            Errores.Clear();
            CiudadesNoEncontradas.Clear();
            EntregasNoEncontradas.Clear();

            var hojas = _excelService.ReadAllSheets(filePath);

            foreach (var hoja in hojas)
            {
                var nombreHoja = hoja.Key;
                var datos = hoja.Value;

                if (nombreHoja.ToLower().Contains("data") || nombreHoja.ToLower().Contains("datos"))
                {
                    CargarData(datos);
                }
                else if (nombreHoja.ToLower().Contains("envios") || nombreHoja.ToLower().Contains("envío"))
                {
                    CargarEnvios(datos);
                }
                else if (nombreHoja.ToLower().Contains("centros") || nombreHoja.ToLower().Contains("destino"))
                {
                    CargarCentros(datos);
                }
            }

            if (Models.DataRecords.Count == 0 && hojas.Count >= 1)
                CargarData(hojas.ElementAt(0).Value);
            if (Models.EnviosRecords.Count == 0 && hojas.Count >= 2)
                CargarEnvios(hojas.ElementAt(1).Value);
            if (Models.CentrosRecords.Count == 0 && hojas.Count >= 3)
                CargarCentros(hojas.ElementAt(2).Value);
        }

        private void CargarData(List<Dictionary<string, object>> dataRecords)
        {
            foreach (var row in dataRecords)
            {
                var values = row.Values.ToList();
                if (values.Count >= 21)
                {
                    var pesoOriginal = TryParseDouble(values[10]);
                    var cajasOriginal = TryParseCount(values[12]);
                    
                    Models.DataRecords.Add(new EnvioData
                    {
                        ShipmentNumber = values[0]?.ToString(),
                        Delivery = values[1]?.ToString(),
                        Name = values[2]?.ToString(),
                        ShipmentType = values[3]?.ToString(),
                        ShipToParty = values[4]?.ToString(),
                        VehicleType = values[5]?.ToString(),
                        Description = values[6]?.ToString(),
                        ServiceAgent = values[7]?.ToString(),
                        Name1 = values[8]?.ToString(),
                        DeliveryDate = values[9]?.ToString(),
                        Weight = NormalizarPeso(TryParseDouble(values[10])),
                        Volume = TryParseDouble(values[11]),
                        Count = cajasOriginal,
                        OrderNumber = values[13]?.ToString(),
                        Street = values[14]?.ToString(),
                        City = values[15]?.ToString(),
                        Fecha1 = values[16]?.ToString(),
                        Fecha2 = values[17]?.ToString(),
                        Tipo1 = values[18]?.ToString(),
                        Tipo2 = values[19]?.ToString(),
                        Factura = values[20]?.ToString()
                    });
                }
            }
        }

        private void CargarEnvios(List<Dictionary<string, object>> enviosRecords)
        {
            foreach (var row in enviosRecords)
            {
                var values = row.Values.ToList();
                if (values.Count >= 17)
                {
                    Models.EnviosRecords.Add(new EnvioData
                    {
                        Delivery = values[0]?.ToString(),
                        IdCarga = values[6]?.ToString(),
                        Secuencia = TryParseIntNull(values[7]),
                        Vhc = values[15]?.ToString(),
                        CostoEstandar = TryParseDoubleNull(values[16])
                    });
                }
            }
        }

        private void CargarCentros(List<Dictionary<string, object>> centrosRecords)
        {
            foreach (var row in centrosRecords)
            {
                var values = row.Values.ToList();
                if (values.Count >= 8)
                {
                    Models.CentrosRecords.Add(new CentroDestino
                    {
                        Centro = values[0]?.ToString(),
                        CiudadDestino = values[1]?.ToString(),
                        Ciudad = values[2]?.ToString()?.ToUpper()?.Trim(),
                        Depto = values[3]?.ToString(),
                        CodigoParaRecogidas = values[7]?.ToString()
                    });
                }
            }
        }

        public void ProcessData()
        {
            Models.Resultados.Clear();
            Errores.Clear();
            CiudadesNoEncontradas.Clear();
            EntregasNoEncontradas.Clear();

            ValidarCiudades();

            if (CiudadesNoEncontradas.Any())
            {
                Errores.AddRange(CiudadesNoEncontradas);
                return;
            }

            ProcesarDatos();
            AgregarCentroDestino();
            EliminarDuplicadosFactura();
            LimpiarSecuencias100();
        }

        private void ValidarCiudades()
        {
            var ciudadesValidas = Models.CentrosRecords
                .Where(c => !string.IsNullOrEmpty(c.Ciudad))
                .Select(c => c.Ciudad?.ToUpper().Trim())
                .ToHashSet();

            for (int i = 0; i < Models.DataRecords.Count; i++)
            {
                var record = Models.DataRecords[i];
                var ciudad = record.City?.ToUpper().Trim() ?? "";

                if (!string.IsNullOrEmpty(ciudad) && !ciudadesValidas.Contains(ciudad))
                {
                    CiudadesNoEncontradas.Add($"Ciudad '{record.City}' No Existe O Mal Escrita Revisar la (Fila {i + 2}).");
                }
            }
        }

        private void ProcesarDatos()
        {
            // Crear diccionario de envíos por Delivery
            var enviosDict = new Dictionary<string, List<EnvioInfo>>();
            
            foreach (var envio in Models.EnviosRecords)
            {
                if (!string.IsNullOrEmpty(envio.Delivery))
                {
                    if (!enviosDict.ContainsKey(envio.Delivery))
                        enviosDict[envio.Delivery] = new List<EnvioInfo>();
                    
                    enviosDict[envio.Delivery].Add(new EnvioInfo
                    {
                        IdCarga = envio.IdCarga ?? string.Empty,
                        Secuencia = envio.Secuencia,
                        Vhc = envio.Vhc ?? string.Empty,
                        CostoEstandar = envio.CostoEstandar
                    });
                }
            }

            foreach (var dataRecord in Models.DataRecords)
            {
                var delivery = dataRecord.Delivery ?? "";

                if (enviosDict.ContainsKey(delivery))
                {
                    var enviosList = enviosDict[delivery];
                    
                    foreach (var envioInfo in enviosList)
                    {
                        if (envioInfo.Secuencia.HasValue && envioInfo.Secuencia.Value != 0)
                        {
                            var secuencia = envioInfo.Secuencia.Value;
                            
                            if (secuencia == 200 || secuencia == 100)
                            {
                                var registro = CrearRegistro(dataRecord, envioInfo, secuencia);
                                Models.Resultados.Add(registro);
                            }
                        }
                    }
                }
                else
                {
                    EntregasNoEncontradas.Add($"Cartaporte {delivery} no encontrado. Validar el Cuadro de Planeación Correcto.");
                }
            }

            if (EntregasNoEncontradas.Any())
            {
                Errores.AddRange(EntregasNoEncontradas);
            }
        }

        private void AgregarCentroDestino()
        {
            // Diccionario para guardar código y nombre
            var centrosDict = new Dictionary<string, (string Codigo, string Nombre)>();
            
            foreach (var centro in Models.CentrosRecords)
            {
                if (!string.IsNullOrEmpty(centro.Ciudad) && !centrosDict.ContainsKey(centro.Ciudad))
                {
                    var codigo = centro.CodigoParaRecogidas ?? "";
                    var nombre = centro.Centro ?? "Validar_Ciudad_Destino";
                    
                    centrosDict[centro.Ciudad] = (codigo, nombre);
                }
            }

            foreach (var record in Models.Resultados.Where(r => r.Secuencia == 200))
            {
                var ciudad = record.City?.ToUpper().Trim() ?? "";
                
                if (!string.IsNullOrEmpty(ciudad) && centrosDict.ContainsKey(ciudad))
                {
                    var info = centrosDict[ciudad];
                    record.CentroDestino = info.Nombre;
                    record.CentroCodigo = info.Codigo;
                }
                else
                {
                    record.CentroDestino = "Validar_Ciudad_Destino";
                    record.CentroCodigo = "";
                }
            }
        }

        private void EliminarDuplicadosFactura()
        {
            var dictFactura = new Dictionary<string, int>();
            var dictRepetidas = new HashSet<string>();

            foreach (var record in Models.Resultados)
            {
                var factura = record.Factura ?? "";
                var secuencia = record.Secuencia ?? 0;

                if (dictFactura.ContainsKey(factura))
                {
                    if (secuencia > dictFactura[factura])
                    {
                        dictFactura[factura] = secuencia;
                    }
                    dictRepetidas.Add(factura);
                }
                else
                {
                    dictFactura[factura] = secuencia;
                }
            }

            for (int i = Models.Resultados.Count - 1; i >= 0; i--)
            {
                var record = Models.Resultados[i];
                var factura = record.Factura ?? "";
                var secuencia = record.Secuencia ?? 0;

                if (dictRepetidas.Contains(factura))
                {
                    if (secuencia < dictFactura[factura])
                    {
                        Models.Resultados.RemoveAt(i);
                    }
                }
            }
        }

        private void LimpiarSecuencias100()
        {
            int contador100 = Models.Resultados.Count(r => r.Secuencia == 100);
            System.Diagnostics.Debug.WriteLine($"Registros con secuencia 100 antes de limpiar: {contador100}");
            
            foreach (var record in Models.Resultados.Where(r => r.Secuencia == 100).ToList())
            {
                record.Secuencia = null;
                record.IdCarga = null;
            }
            
            int contadorNull = Models.Resultados.Count(r => !r.Secuencia.HasValue);
            System.Diagnostics.Debug.WriteLine($"Registros con secuencia null después de limpiar: {contadorNull}");
        }

        private EnvioData CrearRegistro(EnvioData data, EnvioInfo envioInfo, int secuencia)
        {
            return new EnvioData
            {
                ShipmentNumber = data.ShipmentNumber,
                Delivery = data.Delivery,
                Name = data.Name,
                ShipmentType = data.ShipmentType,
                ShipToParty = data.ShipToParty,
                VehicleType = data.VehicleType,
                Description = data.Description,
                ServiceAgent = data.ServiceAgent,
                Name1 = data.Name1,
                DeliveryDate = data.DeliveryDate,
                Weight = data.Weight,
                Volume = data.Volume,
                Count = data.Count,
                OrderNumber = data.OrderNumber,
                Street = data.Street,
                City = data.City,
                Fecha1 = data.Fecha1,
                Fecha2 = data.Fecha2,
                Tipo1 = data.Tipo1,
                Tipo2 = data.Tipo2,
                Factura = data.Factura,
                Secuencia = secuencia,
                Vhc = envioInfo.Vhc,
                CostoEstandar = envioInfo.CostoEstandar,
                IdCarga = envioInfo.IdCarga
            };
        }

        /// <summary>
        /// Cuenta las secuencias 100 (incluyendo los registros con secuencia null que originalmente eran 100)
        /// </summary>
        public (int secuencia100, int secuencia200) ContarSecuencias()
        {
            int secuencia100 = 0;
            int secuencia200 = 0;
            
            foreach (var record in Models.Resultados)
            {
                // Los null y los 100 cuentan como secuencia 100
                if (!record.Secuencia.HasValue || record.Secuencia.Value == 100)
                {
                    secuencia100++;
                }
                else if (record.Secuencia.Value == 200)
                {
                    secuencia200++;
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"Conteo final - Secuencia100: {secuencia100}, Secuencia200: {secuencia200}");
            
            return (secuencia100, secuencia200);
        }

        public (int total, int secuencia100, int secuencia200) ObtenerResumenCompleto()
        {
            int total = Models.Resultados.Count;
            var (secuencia100, secuencia200) = ContarSecuencias();
            
            return (total, secuencia100, secuencia200);
        }

        public void SaveResults(string filePath)
        {
            _excelService.SaveExcel(filePath, Models.Resultados.ToList());
        }

        public string GetDefaultSavePath()
        {
            var downloadsPath = _excelService.GetDownloadsPath();
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            return $"{downloadsPath}Corte_Procter&Gamble_{timestamp}.xlsx";
        }

        private double TryParseDouble(object value)
        {
            if (value == null) return 0;
            return double.TryParse(value.ToString(), out double result) ? result : 0;
        }

        private int TryParseInt(object value)
        {
            if (value == null) return 0;
            return int.TryParse(value.ToString(), out int result) ? result : 0;
        }

        private int? TryParseIntNull(object value)
        {
            if (value == null) return null;
            return int.TryParse(value.ToString(), out int result) ? result : (int?)null;
        }

        private double? TryParseDoubleNull(object value)
        {
            if (value == null) return null;
            return double.TryParse(value.ToString(), out double result) ? result : (double?)null;
        }

        private int TryParseCount(object value)
        {
            if (value == null) return 0;
            
            var strValue = value.ToString()?.Trim() ?? "";
            
            if (int.TryParse(strValue, out int intResult))
                return intResult;
            
            if (double.TryParse(strValue, System.Globalization.NumberStyles.Any, 
                                System.Globalization.CultureInfo.InvariantCulture, 
                                out double doubleResult))
            {
                if (doubleResult < 0.001) return 0;
                return (int)Math.Ceiling(doubleResult);
            }
            
            return 0;
        }

        private double NormalizarPeso(double peso)
        {
            if (peso <= 0) return 0;
            
            if (Math.Abs(peso % 1) < 0.0001)
            {
                return peso / 1000000.0;
            }
            
            return peso;
        }
    }

    // Clase auxiliar para almacenar información de envíos
    public class EnvioInfo
    {
        public string IdCarga { get; set; } = string.Empty;
        public int? Secuencia { get; set; }
        public string Vhc { get; set; } = string.Empty;
        public double? CostoEstandar { get; set; }
    }
}