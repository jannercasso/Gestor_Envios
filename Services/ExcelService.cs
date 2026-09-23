using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GestorEnvios.Models;

namespace GestorEnvios.Services
{
    public class ExcelService
    {
        public ExcelService()
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        // ============================================================
        // Leer TODAS las hojas (incluye VeryHidden)
        // ============================================================
        public Dictionary<string, List<Dictionary<string, object>>> ReadAllSheets(string filePath)
        {
            var result = new Dictionary<string, List<Dictionary<string, object>>>();

            using var package = new ExcelPackage(new FileInfo(filePath));

            // ✅ Primera pasada: foreach normal (Visible + Hidden)
            foreach (var worksheet in package.Workbook.Worksheets)
            {
                if (worksheet.Dimension == null) continue;

                result[worksheet.Name] = LeerHoja(worksheet);
            }

            // ✅ Segunda pasada: intentar leer VeryHidden por índice
            //    con try/catch para que no crashee si EPPlus lanza excepción
            try
            {
                for (int i = 1; i <= package.Workbook.Worksheets.Count; i++)
                {
                    ExcelWorksheet worksheet;
                    try
                    {
                        worksheet = package.Workbook.Worksheets[i];
                    }
                    catch (IndexOutOfRangeException)
                    {
                        continue;
                    }

                    if (worksheet == null) continue;
                    if (worksheet.Dimension == null) continue;
                    if (result.ContainsKey(worksheet.Name)) continue;

                    result[worksheet.Name] = LeerHoja(worksheet);
                }
            }
            catch
            {
                // Ignorar errores de la segunda pasada
            }

            return result;
        }

        // ✅ Helper para leer una hoja
        private List<Dictionary<string, object>> LeerHoja(ExcelWorksheet worksheet)
        {
            var sheetData = new List<Dictionary<string, object>>();

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
                headers.Add(worksheet.Cells[1, col].Text);

            for (int row = 2; row <= rowCount; row++)
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= colCount; col++)
                    rowData[headers[col - 1]] = worksheet.Cells[row, col].Value ?? "";
                sheetData.Add(rowData);
            }

            return sheetData;
        }

        public List<Dictionary<string, object>> ReadExcel(string filePath)
        {
            var result = new List<Dictionary<string, object>>();

            using var package = new ExcelPackage(new FileInfo(filePath));
            var worksheet = package.Workbook.Worksheets[0];

            if (worksheet.Dimension == null)
                return result;

            int rowCount = worksheet.Dimension.Rows;
            int colCount = worksheet.Dimension.Columns;

            var headers = new List<string>();
            for (int col = 1; col <= colCount; col++)
                headers.Add(worksheet.Cells[1, col].Text);

            for (int row = 2; row <= rowCount; row++)
            {
                var rowData = new Dictionary<string, object>();
                for (int col = 1; col <= colCount; col++)
                    rowData[headers[col - 1]] = worksheet.Cells[row, col].Value ?? "";
                result.Add(rowData);
            }

            return result;
        }

        public void SaveExcel(string filePath, List<EnvioData> data)
        {
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("Datos_Procesados");

            var columnas = new Dictionary<string, Func<EnvioData, object?>>
            {
                { "Shipment Number", d => d.ShipmentNumber },
                { "Delivery", d => d.Delivery },
                { "Name", d => d.Name },
                { "Shipment Type", d => d.ShipmentType },
                { "Ship-to Party", d => d.ShipToParty },
                { "Vehicle Type", d => d.VehicleType },
                { "Description", d => d.Description },
                { "Service Agent", d => d.ServiceAgent },
                { "Name 1", d => d.Name1 },
                { "Delivery Date", d => d.DeliveryDate },
                { "Weight", d => d.Weight },
                { "Volume", d => d.Volume },
                { "Count", d => d.Count },
                { "Order Number", d => d.OrderNumber },
                { "Street", d => d.Street },
                { "City", d => d.City },
                { "Fecha1", d => d.Fecha1 },
                { "Fecha2", d => d.Fecha2 },
                { "Tipo1", d => d.Tipo1 },
                { "Tipo2", d => d.Tipo2 },
                { "Factura", d => d.Factura },
                { "Secuencia", d => d.Secuencia },
                { "Vhc", d => d.Vhc },
                { "Costo Estándar", d => d.CostoEstandar },
                { "Id Carga", d => d.IdCarga },
                { "Centro Destino", d => d.CentroCodigo ?? d.CentroDestino }
            };

            int col = 1;
            foreach (var kvp in columnas)
            {
                worksheet.Cells[1, col].Value = kvp.Key;
                worksheet.Cells[1, col].Style.Font.Bold = true;
                col++;
            }

            for (int row = 0; row < data.Count; row++)
            {
                col = 1;
                foreach (var kvp in columnas)
                {
                    var value = kvp.Value(data[row]);
                    worksheet.Cells[row + 2, col].Value = value?.ToString();
                    col++;
                }
            }

            worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            package.SaveAs(new FileInfo(filePath));
        }

        public string GetDownloadsPath()
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Downloads\\";
        }
    }
}