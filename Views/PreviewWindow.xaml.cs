using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorEnvios.Models;
using GestorEnvios.Services;
using GestorEnvios.Controls;

namespace GestorEnvios.Views
{
    public partial class PreviewWindow : Window
    {
        private List<EnvioData> _allData;
        private List<EnvioData> _filteredData;
        private readonly DataProcessor _processor;
        private Dictionary<string, FilterHeader> _filterHeaders = new Dictionary<string, FilterHeader>();
        private Dictionary<string, List<string>> _activeFilters = new Dictionary<string, List<string>>();

        public PreviewWindow(List<EnvioData> data)
        {
            InitializeComponent();
            
            _processor = new DataProcessor();
            
            if (data == null || !data.Any())
            {
                _allData = new List<EnvioData>();
                _filteredData = new List<EnvioData>();
            }
            else
            {
                _allData = data;
                _filteredData = new List<EnvioData>(data);
                
                // 🔹 CORREGIDO: Limpiar y agregar los datos uno por uno
                _processor.Models.Resultados.Clear();
                foreach (var item in _allData)
                {
                    _processor.Models.Resultados.Add(item);
                }
            }
            
            dgPreview.ItemsSource = _filteredData;
            ActualizarContador();
        }

        private void ActualizarContador()
        {
            // 🔹 Obtener el conteo de secuencias usando el método del DataProcessor
            var (secuencia100, secuencia200) = _processor.ContarSecuencias();
            var total = _filteredData.Count;
            
            // 🔹 Mostrar el contador con la información de secuencias
            txtCount.Text = $"Total: {total} registros | " +
                           $"Secuencia 100: {secuencia100} | " +
                           $"Secuencia 200: {secuencia200}";
        }

        private void DgPreview_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            // ✅ CORRECTO: Verificar cada propiedad individualmente
            if (e.PropertyName == "EsDuplicado" || e.PropertyName == "CentroCodigo")
            {
                e.Cancel = true;
                return;
            }

            var columnName = e.Column.Header?.ToString() ?? "";
            if (string.IsNullOrEmpty(columnName)) return;
            
            var filterHeader = new FilterHeader();
            filterHeader.SetColumnName(columnName);
            
            var propertyName = MapearPropiedad(columnName.Replace(" ", "").Replace("-", "").Replace(".", ""));
            var propiedad = typeof(EnvioData).GetProperty(propertyName);
            if (propiedad != null)
            {
                var valores = _allData
                    .Select(r => propiedad.GetValue(r)?.ToString() ?? "")
                    .Where(v => !string.IsNullOrEmpty(v))
                    .Distinct()
                    .OrderBy(v => v)
                    .ToList();
                
                filterHeader.SetValues(valores);
            }
            
            filterHeader.FilterChanged += (s, args) =>
            {
                if (args.IsFilterActive && args.SelectedValues.Any())
                {
                    _activeFilters[args.ColumnName] = args.SelectedValues;
                }
                else
                {
                    _activeFilters.Remove(args.ColumnName);
                }
                AplicarFiltros();
            };
            
            filterHeader.SortRequested += (s, args) =>
            {
                AplicarOrdenamiento(args.ColumnName, args.Direction);
            };
            
            _filterHeaders[columnName] = filterHeader;
            
            e.Column.Header = filterHeader;
            e.Column.Width = DataGridLength.SizeToCells;
            e.Column.MinWidth = 100;
        }

        private string MapearPropiedad(string nombreColumna)
        {
            var mapa = new Dictionary<string, string>
            {
                { "ShipmentNumber", "ShipmentNumber" },
                { "Delivery", "Delivery" },
                { "Name", "Name" },
                { "Shipmenttype", "ShipmentType" },
                { "Shiptoparty", "ShipToParty" },
                { "VehicleType", "VehicleType" },
                { "Description", "Description" },
                { "Serviceagent", "ServiceAgent" },
                { "Name1", "Name1" },
                { "DeliveryDate", "DeliveryDate" },
                { "Weight", "Weight" },
                { "Volume", "Volume" },
                { "Count", "Count" },
                { "OrderNumber", "OrderNumber" },
                { "Street", "Street" },
                { "City", "City" },
                { "Fecha1", "Fecha1" },
                { "Fecha2", "Fecha2" },
                { "Tipo1", "Tipo1" },
                { "Tipo2", "Tipo2" },
                { "Factura", "Factura" },
                { "Secuencia", "Secuencia" },
                { "Vhc", "Vhc" },
                { "CostoEstándar", "CostoEstandar" },
                { "IdCarga", "IdCarga" },
                { "CentroDestino", "CentroDestino" }
            };
            
            return mapa.ContainsKey(nombreColumna) ? mapa[nombreColumna] : nombreColumna;
        }

        private void AplicarFiltros()
        {
            if (_allData == null || !_allData.Any())
            {
                _filteredData = new List<EnvioData>();
                dgPreview.ItemsSource = _filteredData;
                ActualizarContador();
                return;
            }

            var filtrados = new List<EnvioData>(_allData);

            foreach (var filtro in _activeFilters)
            {
                var columna = filtro.Key;
                var valoresPermitidos = filtro.Value;
                
                if (valoresPermitidos == null || !valoresPermitidos.Any())
                    continue;
                
                var propertyName = MapearPropiedad(columna.Replace(" ", "").Replace("-", "").Replace(".", ""));
                var propiedad = typeof(EnvioData).GetProperty(propertyName);
                if (propiedad == null) continue;
                
                filtrados = filtrados.Where(r =>
                {
                    var valor = propiedad.GetValue(r)?.ToString() ?? "";
                    return valoresPermitidos.Contains(valor);
                }).ToList();
            }

            _filteredData = filtrados;
            dgPreview.ItemsSource = _filteredData;
            ActualizarContador();
        }

        private void AplicarOrdenamiento(string columnName, SortDirection direction)
        {
            if (_filteredData == null || !_filteredData.Any())
                return;
            
            var propertyName = MapearPropiedad(columnName.Replace(" ", "").Replace("-", "").Replace(".", ""));
            var propiedad = typeof(EnvioData).GetProperty(propertyName);
            if (propiedad == null) return;
            
            if (direction == SortDirection.Ascending)
            {
                _filteredData = _filteredData.OrderBy(r => propiedad.GetValue(r)?.ToString()).ToList();
            }
            else
            {
                _filteredData = _filteredData.OrderByDescending(r => propiedad.GetValue(r)?.ToString()).ToList();
            }
            
            dgPreview.ItemsSource = null;
            dgPreview.ItemsSource = _filteredData;
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_filteredData.Any())
                {
                    MessageBox.Show("No hay datos para exportar.", "Sin datos", 
                                  MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var excelService = new ExcelService();
                var downloadsPath = excelService.GetDownloadsPath();
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var fileName = $"Corte_Procter&Gamble_{timestamp}.xlsx";
                var filePath = System.IO.Path.Combine(downloadsPath, fileName);

                excelService.SaveExcel(filePath, _filteredData);
                
                MessageBox.Show($"✅ Archivo exportado exitosamente en:\n{filePath}", 
                              "Exportación Exitosa", 
                              MessageBoxButton.OK, 
                              MessageBoxImage.Information);
                
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al exportar: {ex.Message}", "Error", 
                              MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}