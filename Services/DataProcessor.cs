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

        // ✅ Lista cruda para el DataGrid del Plan B
        public List<EnvioRawView> EnviosRawView { get; private set; } = new();

        // ✅ Lista separada para los shiptos de la hoja Db_Shipto
        public List<CentroDestino> ShiptoRecords { get; private set; } = new();

        public DataProcessor()
        {
            _excelService = new ExcelService();
            Models = new DataModels();
        }

        // ============================================================
        // FLUJO NORMAL (3 hojas: Data + Envíos + Centros)
        // ============================================================
        public void LoadDataFromSingleFile(string filePath)
        {
            Models.DataRecords.Clear();
            Models.EnviosRecords.Clear();
            Models.CentrosRecords.Clear();
            ShiptoRecords.Clear();
            Errores.Clear();
            CiudadesNoEncontradas.Clear();
            EntregasNoEncontradas.Clear();
            EnviosRawView.Clear();

            var hojas = _excelService.ReadAllSheets(filePath);

            foreach (var hoja in hojas)
            {
                var nombreHoja = hoja.Key.ToLower();

                // Chequear "shipto" ANTES que "centros"/"destino"
                if (nombreHoja.Contains("shipto"))
                    continue;
                else if (nombreHoja.Contains("data") || nombreHoja.Contains("datos"))
                    CargarData(hoja.Value);
                else if (nombreHoja.Contains("envios") || nombreHoja.Contains("envío"))
                    CargarEnvios(hoja.Value);
                else if (nombreHoja.Contains("centros") || nombreHoja.Contains("destino"))
                    CargarCentros(hoja.Value);
            }

            // Fallback por posición
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
                    var name1 = values[8]?.ToString() ?? "";
                    var cajasOriginal = TryParseCount(values[12]);

                    Models.DataRecords.Add(new EnvioData
                    {
                        ShipmentNumber = NormalizarNumero(values[0]?.ToString() ?? ""),
                        Delivery = NormalizarNumero(values[1]?.ToString() ?? ""),
                        Name = values[2]?.ToString(),
                        ShipmentType = values[3]?.ToString(),
                        ShipToParty = values[4]?.ToString(),
                        VehicleType = values[5]?.ToString(),
                        Description = values[6]?.ToString(),
                        ServiceAgent = values[7]?.ToString(),
                        Name1 = name1,
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
                    var nombreCarrier = values[13]?.ToString() ?? "";

                    Models.EnviosRecords.Add(new EnvioData
                    {
                        Delivery = NormalizarNumero(values[0]?.ToString() ?? ""),
                        IdCarga = values[6]?.ToString(),
                        Secuencia = TryParseIntNull(values[7]),
                        Vhc = values[15]?.ToString(),
                        CostoEstandar = TryParseDoubleNull(values[16]),
                        Name1 = nombreCarrier
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

        // ============================================================
        // PLAN B: Cargar Envíos + Db_Shipto + Centros
        // - Match Ship-to: Envíos."ID ubicación destino" == Db_Shipto."Ship-to"
        // - Centro Destino: se calcula con la hoja Centros (igual que flujo normal)
        // ============================================================
        public void LoadDataFromEnviosOnly(string filePath)
        {
            Models.DataRecords.Clear();
            Models.EnviosRecords.Clear();
            Models.CentrosRecords.Clear();
            ShiptoRecords.Clear();
            Models.Resultados.Clear();
            Errores.Clear();
            CiudadesNoEncontradas.Clear();
            EntregasNoEncontradas.Clear();
            EnviosRawView.Clear();

            var hojas = _excelService.ReadAllSheets(filePath);

            List<Dictionary<string, object>> enviosSheet = new();
            List<Dictionary<string, object>> shiptoSheet = new();
            List<Dictionary<string, object>> centrosSheet = new();

            foreach (var hoja in hojas)
            {
                var nombre = hoja.Key.ToLower();

                // Chequear "shipto" ANTES que "centros"/"destino"
                if (nombre.Contains("shipto") || nombre.Contains("db_shipto"))
                    shiptoSheet = hoja.Value;
                else if (nombre.Contains("centros") || nombre.Contains("destino"))
                    centrosSheet = hoja.Value;
                else if (nombre.Contains("envio") || nombre.Contains("envío"))
                    enviosSheet = hoja.Value;
            }

            CargarEnviosPlanB(enviosSheet);
            CargarShiptoPlanB(shiptoSheet);
            CargarCentros(centrosSheet);

            // Diccionario Ship-to → CentroDestino (desde ShiptoRecords)
            var shiptoDict = ShiptoRecords
                .Where(c => !string.IsNullOrWhiteSpace(c.ShipTo))
                .GroupBy(c => c.ShipTo!)
                .ToDictionary(g => g.Key, g => g.First());

            // ============================================================
            // PRE-SCAN: por cada ID de envío, elegir el ID ubicación
            // destino que EXISTA en Db_Shipto
            // ============================================================
            var idUbicDestinoGanador = new Dictionary<string, string>();

            var filasPorIdEnvio = new Dictionary<string, List<string>>();
            foreach (var row in enviosSheet)
            {
                var values = row.Values.ToList();
                if (values.Count < 8) continue;

                var idEnvio = values[0]?.ToString()?.Trim() ?? "";
                var idUbicDestinoRaw = NormalizarNumero(values[2]?.ToString() ?? "");

                if (string.IsNullOrEmpty(idEnvio)) continue;

                if (!filasPorIdEnvio.ContainsKey(idEnvio))
                    filasPorIdEnvio[idEnvio] = new List<string>();

                filasPorIdEnvio[idEnvio].Add(idUbicDestinoRaw);
            }

            foreach (var kvp in filasPorIdEnvio)
            {
                var idEnvio = kvp.Key;
                var idsUbicDestino = kvp.Value;

                var idGanador = idsUbicDestino
                    .FirstOrDefault(id => !string.IsNullOrEmpty(id) && shiptoDict.ContainsKey(id));

                if (!string.IsNullOrEmpty(idGanador))
                {
                    idUbicDestinoGanador[idEnvio] = idGanador;
                }
                else
                {
                    var nombreDestino = enviosSheet
                        .Where(r => (r.Values.ToList()[0]?.ToString()?.Trim() ?? "") == idEnvio)
                        .Select(r => r.Values.ToList()[1]?.ToString())
                        .FirstOrDefault();

                    var listaIds = string.Join(", ", idsUbicDestino);
                    EntregasNoEncontradas.Add(
                        $"ID envío {idEnvio} ({nombreDestino}): ninguno de sus Ship-to [{listaIds}] existe en Db_Shipto.");

                    idUbicDestinoGanador[idEnvio] = idsUbicDestino.LastOrDefault() ?? "";
                }
            }

            // ============================================================
            // 1) Construir EnviosRawView (para DataGrid)
            // ============================================================
            var cacheStreetCity = new Dictionary<string, (string Street, string City)>();
            var cacheNombreDestinatario = new Dictionary<string, string>();

            foreach (var row in enviosSheet)
            {
                var values = row.Values.ToList();
                if (values.Count < 8) continue;

                var idEnvio = values[0]?.ToString()?.Trim() ?? "";

                string idUbicDestino = idUbicDestinoGanador.TryGetValue(idEnvio, out var ganador)
                    ? ganador
                    : NormalizarNumero(values[2]?.ToString() ?? "");

                string street;
                string city;
                string nombreDestinatario;

                if (!string.IsNullOrEmpty(idEnvio) &&
                    cacheStreetCity.TryGetValue(idEnvio, out var cached))
                {
                    street = cached.Street;
                    city = cached.City;
                    nombreDestinatario = cacheNombreDestinatario[idEnvio];
                }
                else
                {
                    street = "";
                    city = "";
                    nombreDestinatario = "";

                    if (!string.IsNullOrEmpty(idUbicDestino) &&
                        shiptoDict.TryGetValue(idUbicDestino, out var shipto))
                    {
                        street = shipto.ShiptoStreet ?? "";
                        city = shipto.ShiptoCity ?? "";
                        nombreDestinatario = shipto.ShiptoName1 ?? "";
                    }

                    if (!string.IsNullOrEmpty(idEnvio))
                    {
                        cacheStreetCity[idEnvio] = (street, city);
                        cacheNombreDestinatario[idEnvio] = nombreDestinatario;
                    }
                }

                EnviosRawView.Add(new EnvioRawView
                {
                    IdEnvio = values.Count > 0 ? values[0]?.ToString() : "",
                    NombreUbicacionDestino = string.IsNullOrEmpty(nombreDestinatario)
                                                ? (values.Count > 1 ? values[1]?.ToString() : "")
                                                : nombreDestinatario,
                    IdUbicacionDestino = idUbicDestino,
                    PesoKG = values.Count > 3 ? TryParseDouble(values[3]) : 0,
                    VolumenCUM = values.Count > 4 ? TryParseDouble(values[4]) : 0,
                    Piezas = values.Count > 5 ? TryParseCount(values[5]) : 0,
                    IdCarga = values.Count > 6 ? values[6]?.ToString() : "",
                    NumeroSecuencia = values.Count > 7 ? TryParseIntNull(values[7]) : null,
                    IdUbicacionOrigen = values.Count > 8 ? values[8]?.ToString() : "",
                    DireccionDestino = values.Count > 9 ? values[9]?.ToString() : "",
                    Corredor = values.Count > 10 ? values[10]?.ToString() : "",
                    FechaLlegada = values.Count > 11 ? values[11]?.ToString() : "",
                    EstadoOperativo = values.Count > 12 ? values[12]?.ToString() : "",
                    NombreCarrier = values.Count > 13 ? values[13]?.ToString() : "",
                    IdTransportista = values.Count > 14 ? values[14]?.ToString() : "",
                    Vhc = values.Count > 15 ? values[15]?.ToString() : "",
                    CostoEstandar = values.Count > 16 ? values[16]?.ToString() : "",
                    Street = street,
                    City = city
                });
            }

            // ============================================================
            // 2) Construir DataRecords (para procesamiento interno)
            // ============================================================
            var cacheStreetCityData = new Dictionary<string, (string Street, string City)>();

            foreach (var envio in Models.EnviosRecords)
            {
                var idEnvio = envio.ShipmentNumber?.Trim() ?? "";
                var idDestinoRaw = envio.Delivery?.Trim() ?? "";
                var nombreDestino = envio.Name?.Trim() ?? "";

                string idDestino = idUbicDestinoGanador.TryGetValue(idEnvio, out var ganadorData)
                    ? ganadorData
                    : idDestinoRaw;

                string street;
                string city;
                string name1Shipto = "";

                if (!string.IsNullOrEmpty(idEnvio) &&
                    cacheStreetCityData.TryGetValue(idEnvio, out var cached))
                {
                    street = cached.Street;
                    city = cached.City;
                }
                else
                {
                    street = "";
                    city = "";

                    if (!string.IsNullOrEmpty(idDestino) &&
                        shiptoDict.TryGetValue(idDestino, out var shipto))
                    {
                        street = shipto.ShiptoStreet ?? "";
                        city = shipto.ShiptoCity ?? "";
                        name1Shipto = shipto.ShiptoName1 ?? "";
                    }

                    if (!string.IsNullOrEmpty(idEnvio))
                        cacheStreetCityData[idEnvio] = (street, city);
                }

                if (string.IsNullOrWhiteSpace(city))
                    CiudadesNoEncontradas.Add($"Ship-to '{idDestino}' sin ciudad en Db_Shipto.");

                Models.DataRecords.Add(new EnvioData
                {
                    ShipmentNumber = envio.ShipmentNumber,
                    Delivery = idDestino,
                    ShipToParty = idDestino,
                    Name = string.IsNullOrEmpty(name1Shipto) ? nombreDestino : name1Shipto,
                    Street = street,
                    City = city,
                    Weight = envio.Weight,
                    Volume = envio.Volume,
                    Count = envio.Count,
                    DeliveryDate = envio.DeliveryDate,
                    Name1 = envio.Name1,
                    ServiceAgent = envio.ServiceAgent,
                    VehicleType = envio.VehicleType,
                    ShipmentType = "",
                    Description = "",
                    OrderNumber = "",
                    Fecha1 = "",
                    Fecha2 = "",
                    Tipo1 = "",
                    Tipo2 = "",
                    Factura = ""
                });
            }
        }

        private void CargarEnviosPlanB(List<Dictionary<string, object>> enviosRecords)
        {
            foreach (var row in enviosRecords)
            {
                var values = row.Values.ToList();
                if (values.Count < 8) continue;

                Models.EnviosRecords.Add(new EnvioData
                {
                    ShipmentNumber = NormalizarNumero(values[0]?.ToString() ?? ""),
                    Name = values[1]?.ToString()?.Trim(),
                    Delivery = NormalizarNumero(values[2]?.ToString() ?? ""),
                    Weight = values.Count > 3 ? TryParseDouble(values[3]) : 0,
                    Volume = values.Count > 4 ? TryParseDouble(values[4]) : 0,
                    Count = values.Count > 5 ? TryParseCount(values[5]) : 0,
                    IdCarga = values.Count > 6 ? values[6]?.ToString() : null,
                    Secuencia = values.Count > 7 ? TryParseIntNull(values[7]) : null,
                    DeliveryDate = values.Count > 11 ? values[11]?.ToString() : null,
                    Name1 = values.Count > 13 ? values[13]?.ToString()?.Trim() : "",
                    Vhc = values.Count > 15 ? values[15]?.ToString() : null,
                    CostoEstandar = values.Count > 16 ? TryParseDoubleNull(values[16]) : null
                });
            }
        }

        private void CargarShiptoPlanB(List<Dictionary<string, object>> shiptoRecords)
        {
            foreach (var row in shiptoRecords)
            {
                var values = row.Values.ToList();
                if (values.Count < 4) continue;

                ShiptoRecords.Add(new CentroDestino
                {
                    ShipTo = NormalizarNumero(values[0]?.ToString() ?? ""),
                    ShiptoName1 = values[1]?.ToString()?.Trim(),
                    ShiptoStreet = values[2]?.ToString()?.Trim(),
                    ShiptoCity = values[3]?.ToString()?.Trim()?.ToUpper()
                });
            }
        }

        // ============================================================
        // PLAN B: Mapear EnviosRawView → estructura estándar (EnvioData)
        // ✅ Weight dividido entre 1000
        // ============================================================
        public void ProcesarEnviosPlanB(string transportadoraFiltro = "")
        {
            Models.Resultados.Clear();
            Errores.Clear();

            var filas = EnviosRawView.AsEnumerable();

            if (!string.IsNullOrEmpty(transportadoraFiltro))
            {
                filas = filas.Where(f =>
                    f.NombreCarrier?.Trim() == "ICOLTRANS LTDA" ||
                    f.NombreCarrier?.Trim() == transportadoraFiltro);
            }

            foreach (var f in filas)
            {
                int? secuenciaFinal;

                if (f.NumeroSecuencia == 200)
                    secuenciaFinal = 200;
                else if (f.NumeroSecuencia == 100)
                    secuenciaFinal = null;
                else
                    continue;

                string fechaFormateada = FormatearFechaDDMMYYYY(f.FechaLlegada);

                Models.Resultados.Add(new EnvioData
                {
                    // #0 Shipment Number  ← ID de carga
                    ShipmentNumber = f.IdCarga,
                    // #1 Delivery         ← ID de envío
                    Delivery = f.IdEnvio,
                    // #2 Name
                    Name = f.NombreUbicacionDestino,
                    // #3 Shipment type
                    ShipmentType = "ZTM7",
                    // #4 Ship-to party
                    ShipToParty = f.IdUbicacionDestino,
                    // #5 Vehicle Type
                    VehicleType = f.Vhc,
                    // #6 Description
                    Description = "",
                    // #7 Service agent
                    ServiceAgent = f.IdTransportista,
                    // #8 Name 1
                    Name1 = f.NombreCarrier,
                    // #9 Delivery Date
                    DeliveryDate = f.FechaLlegada,
                    // #10 Weight  ← ✅ DIVIDIDO ENTRE 1000
                    Weight = (f.PesoKG ?? 0) / 1000.0,
                    // #11 Volume
                    Volume = f.VolumenCUM,
                    // #12 Count
                    Count = f.Piezas,
                    // #13 Order Number
                    OrderNumber = "",
                    // #14 Street
                    Street = f.Street,
                    // #15 City
                    City = f.City,
                    // #16 Fecha1
                    Fecha1 = fechaFormateada,
                    // #17 Fecha2
                    Fecha2 = fechaFormateada,
                    // #18 Tipo1
                    Tipo1 = "CS",
                    // #19 Tipo2
                    Tipo2 = "CS",
                    // #20 Factura     ← ID de envío
                    Factura = f.IdEnvio,

                    // Extras internos
                    Secuencia = secuenciaFinal,
                    IdCarga = f.IdCarga,
                    CostoEstandar = TryParseDoubleNull(f.CostoEstandar),
                    Vhc = f.Vhc
                });
            }

            // ✅ Calcular Centro Destino (igual que flujo normal)
            AgregarCentroDestino();
        }

        // ============================================================
        // Helper: convierte "3/09/2026 23:30" → "03.09.2026"
        // ============================================================
        private string FormatearFechaDDMMYYYY(string? fechaOriginal)
        {
            if (string.IsNullOrWhiteSpace(fechaOriginal))
                return "";

            var limpio = fechaOriginal.Trim();
            var parteFecha = limpio.Split(' ')[0];

            string[] formatos = { "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy",
                                  "yyyy-MM-dd", "d-M-yyyy", "dd-MM-yyyy",
                                  "d.M.yyyy", "dd.MM.yyyy" };

            if (DateTime.TryParseExact(parteFecha, formatos,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime fecha))
            {
                return fecha.ToString("dd.MM.yyyy");
            }

            if (DateTime.TryParse(parteFecha,
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out DateTime fechaLibre))
            {
                return fechaLibre.ToString("dd.MM.yyyy");
            }

            return limpio;
        }

        // ============================================================
        // PIPELINE
        // ============================================================
        public void ProcessData(string transportadoraFiltro = "")
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

            ProcesarDatos(transportadoraFiltro);
            AgregarCentroDestino();
            EliminarDuplicadosFactura();
            LimpiarSecuencias100();
        }

        private void ValidarCiudades()
        {
            var ciudadesValidas = Models.CentrosRecords
                .Where(c => !string.IsNullOrEmpty(c.Ciudad) || !string.IsNullOrEmpty(c.ShiptoCity))
                .Select(c => (c.Ciudad ?? c.ShiptoCity)!.ToUpper().Trim())
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

        private void ProcesarDatos(string transportadoraFiltro = "")
        {
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
                        CostoEstandar = envio.CostoEstandar,
                        Transportadora = envio.Name1 ?? ""
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
                        if (!string.IsNullOrEmpty(transportadoraFiltro))
                        {
                            var transportadora = envioInfo.Transportadora?.Trim() ?? "";
                            if (transportadora != "ICOLTRANS LTDA" && transportadora != transportadoraFiltro)
                                continue;
                        }

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
            if (Models.Resultados.All(r => string.IsNullOrWhiteSpace(r.Factura)))
                return;

            var dictFactura = new Dictionary<string, int>();
            var dictRepetidas = new HashSet<string>();

            foreach (var record in Models.Resultados)
            {
                var factura = record.Factura ?? "";
                var secuencia = record.Secuencia ?? 0;

                if (dictFactura.ContainsKey(factura))
                {
                    if (secuencia > dictFactura[factura])
                        dictFactura[factura] = secuencia;
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
                        Models.Resultados.RemoveAt(i);
                }
            }
        }

        private void LimpiarSecuencias100()
        {
            foreach (var record in Models.Resultados.Where(r => r.Secuencia == 100).ToList())
            {
                record.Secuencia = null;
                record.IdCarga = null;
            }
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

        public (int secuencia100, int secuencia200) ContarSecuencias()
        {
            int secuencia100 = 0;
            int secuencia200 = 0;

            foreach (var record in Models.Resultados)
            {
                if (!record.Secuencia.HasValue || record.Secuencia.Value == 100)
                    secuencia100++;
                else if (record.Secuencia.Value == 200)
                    secuencia200++;
            }

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

        // ============================================================
        // HELPERS
        // ============================================================
        private string NormalizarNumero(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor))
                return string.Empty;

            var limpio = valor.Trim().Replace(" ", "").Replace("-", "").Replace(".", "");

            if (long.TryParse(limpio, out long numeroLimpio))
                return numeroLimpio.ToString();

            return limpio;
        }

        private double TryParseDouble(object value)
        {
            if (value == null) return 0;

            var str = value.ToString()?.Trim() ?? "";
            str = str.Replace("$", "").Replace(" ", "").Replace(",", "");

            if (double.TryParse(str, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double result))
                return result;

            return 0;
        }

        private int? TryParseIntNull(object value)
        {
            if (value == null) return null;

            var str = value.ToString()?.Trim() ?? "";

            if (int.TryParse(str, out int result))
                return result;

            if (double.TryParse(str, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double dResult))
                return (int)dResult;

            return null;
        }

        private double? TryParseDoubleNull(object value)
        {
            if (value == null) return null;

            var str = value.ToString()?.Trim() ?? "";
            str = str.Replace("$", "").Replace(" ", "").Replace(",", "");

            if (double.TryParse(str, System.Globalization.NumberStyles.Any,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double result))
                return result;

            return null;
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
                return peso / 1000000.0;

            return peso;
        }
    }

    public class EnvioInfo
    {
        public string IdCarga { get; set; } = string.Empty;
        public int? Secuencia { get; set; }
        public string Vhc { get; set; } = string.Empty;
        public double? CostoEstandar { get; set; }
        public string Transportadora { get; set; } = string.Empty;
    }
}