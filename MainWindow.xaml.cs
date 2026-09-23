using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using GestorEnvios.Services;
using GestorEnvios.Views;
using GestorEnvios.Models;

namespace GestorEnvios
{
    // ================================================
    // CONVERTIDORES PARA EL EFECTO HOVER DE BOTONES
    // ================================================
    public class DarkenColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var color = brush.Color;
                return new SolidColorBrush(Color.FromRgb(
                    (byte)Math.Max(0, color.R - 30),
                    (byte)Math.Max(0, color.G - 30),
                    (byte)Math.Max(0, color.B - 30)
                ));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class DarkerColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var color = brush.Color;
                return new SolidColorBrush(Color.FromRgb(
                    (byte)Math.Max(0, color.R - 60),
                    (byte)Math.Max(0, color.G - 60),
                    (byte)Math.Max(0, color.B - 60)
                ));
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // ================================================
    // CLASE AUXILIAR PARA TRANSPORTADORA
    // ================================================
    public class TransportadoraItem
    {
        public string Key { get; set; } = string.Empty;
        public int Value { get; set; }
    }

    // ================================================
    // MAIN WINDOW
    // ================================================
    public partial class MainWindow : Window
    {
        private readonly DataProcessor _processor;
        private List<EnvioData>? _ultimosDatos;
        private List<EnvioData>? _datosFiltradosPorTransportadora;
        private bool _modoPlanB = false;

        private const string ICOLTRANS = "ICOLTRANS LTDA";

        public MainWindow()
        {
            InitializeComponent();
            _processor = new DataProcessor();

            btnVerVistaPrevia.Visibility = Visibility.Collapsed;

            chkFiltrarTransportadora.IsEnabled = false;
            cmbTransportadoras.IsEnabled = false;

            cmbTransportadoras.ItemsSource = new List<TransportadoraItem>();
        }

        public void MostrarBotonVistaPrevia(bool mostrar)
        {
            Dispatcher.Invoke(() =>
            {
                btnVerVistaPrevia.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        private List<TransportadoraItem> ObtenerTransportadoras()
        {
            var transportadoras = new Dictionary<string, int>();

            // ✅ MODO PLAN B → usar EnviosRawView
            if (_modoPlanB && _processor.EnviosRawView.Any())
            {
                foreach (var record in _processor.EnviosRawView)
                {
                    var transportadora = record.NombreCarrier?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(transportadora))
                    {
                        if (transportadoras.ContainsKey(transportadora))
                            transportadoras[transportadora]++;
                        else
                            transportadoras[transportadora] = 1;
                    }
                }
            }
            else
            {
                // Flujo normal (Data + Envíos + Centros)
                var deliveriesData = _processor.Models.DataRecords
                    .Select(r => r.Delivery?.Trim() ?? "")
                    .Where(d => !string.IsNullOrEmpty(d))
                    .ToHashSet();

                foreach (var record in _processor.Models.EnviosRecords)
                {
                    var transportadora = record.Name1?.Trim() ?? "";
                    var delivery = record.Delivery?.Trim() ?? "";

                    if (!string.IsNullOrEmpty(transportadora) &&
                        !string.IsNullOrEmpty(delivery) &&
                        deliveriesData.Contains(delivery))
                    {
                        if (transportadoras.ContainsKey(transportadora))
                            transportadoras[transportadora]++;
                        else
                            transportadoras[transportadora] = 1;
                    }
                }
            }

            return transportadoras
                .OrderBy(x => x.Key)
                .Select(x => new TransportadoraItem { Key = x.Key, Value = x.Value })
                .ToList();
        }

        private void CargarTransportadoras()
        {
            var transportadoras = ObtenerTransportadoras();

            cmbTransportadoras.ItemsSource = transportadoras;
            chkFiltrarTransportadora.IsEnabled = transportadoras.Any();
            cmbTransportadoras.IsEnabled = false;

            if (transportadoras.Any())
                cmbTransportadoras.SelectedIndex = 0;
            else
            {
                cmbTransportadoras.ItemsSource = new List<TransportadoraItem>();
                chkFiltrarTransportadora.IsEnabled = false;
            }
        }

        // ============================================================
        // FLUJO NORMAL
        // ============================================================
        private void BtnSeleccionarArchivo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar archivo Excel con 3 hojas",
                Filter = "Archivos Excel|*.xlsx;*.xls"
            };

            if (dialog.ShowDialog() == true)
            {
                txtArchivo.Text = dialog.FileName;

                try
                {
                    statusBarText.Text = "Cargando vista previa de Data...";

                    _processor.LoadDataFromSingleFile(dialog.FileName);

                    _modoPlanB = false;

                    dgEnviosRaw.Visibility = Visibility.Collapsed;
                    dgDatos.Visibility = Visibility.Visible;

                    dgDatos.ItemsSource = null;
                    dgDatos.ItemsSource = _processor.Models.DataRecords;

                    var count = _processor.Models.DataRecords.Count;

                    statusBarText.Text = $"✅ Vista previa de Data cargada. {count} registros";

                    btnProcesar.IsEnabled = true;
                    btnLimpiar.IsEnabled = true;
                    txtStatus.Text = "✅ Data cargada";
                    statusBarErrores.Text = "";

                    MostrarBotonVistaPrevia(false);
                    CargarTransportadoras();

                    chkFiltrarTransportadora.IsChecked = false;
                    chkFiltrarTransportadora.IsEnabled = true;
                    cmbTransportadoras.IsEnabled = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar vista previa: {ex.Message}", "Error",
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // ============================================================
        // PLAN B: Cargar solo Envíos + Db_Shipto + Centros
        // ============================================================
        private void BtnCargarEnvios_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Seleccionar archivo con hojas Envíos y Db_Shipto",
                Filter = "Archivos Excel|*.xlsx;*.xls"
            };

            if (dialog.ShowDialog() != true) return;

            txtArchivo.Text = dialog.FileName;

            try
            {
                statusBarText.Text = "Cargando Envíos (Plan B)...";
                statusBarErrores.Text = "";

                _processor.LoadDataFromEnviosOnly(dialog.FileName);

                _modoPlanB = true;

                if (_processor.CiudadesNoEncontradas.Any())
                {
                    var mensaje = "Se detectaron las siguientes ciudades no encontradas:\n\n" +
                                  string.Join("\n", _processor.CiudadesNoEncontradas) +
                                  "\n\nValidar la hoja Db_Shipto y ejecutar nuevamente.";
                    MessageBox.Show(mensaje, "Validación de Ciudades",
                                    MessageBoxButton.OK, MessageBoxImage.Error);
                    txtStatus.Text = "❌ Error ciudades";
                    statusBarText.Text = "❌ Error de validación";
                    return;
                }

                if (_processor.EntregasNoEncontradas.Any())
                {
                    var mensaje = "Ship-to no encontrados en Db_Shipto:\n\n" +
                                  string.Join("\n", _processor.EntregasNoEncontradas);
                    MessageBox.Show(mensaje, "Validación de Ship-to",
                                    MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                dgEnviosRaw.ItemsSource = null;
                dgEnviosRaw.ItemsSource = _processor.EnviosRawView;

                dgEnviosRaw.Visibility = Visibility.Visible;
                dgDatos.Visibility = Visibility.Collapsed;

                _ultimosDatos = _processor.Models.Resultados.ToList();
                _datosFiltradosPorTransportadora = null;

                btnProcesar.IsEnabled = true;
                btnLimpiar.IsEnabled = true;
                txtStatus.Text = "✅ Cargado (Envíos)";
                statusBarText.Text = $"✅ Carga completada. {_processor.EnviosRawView.Count} registros mostrados";

                MessageBox.Show(
                    $"Carga terminada (Plan B - solo Envíos).\n\n" +
                    $"Registros mostrados: {_processor.EnviosRawView.Count}\n" +
                    $"Presiona 'Procesar Datos' para generar la vista previa.",
                    "Carga Completada", MessageBoxButton.OK, MessageBoxImage.Information);

                MostrarBotonVistaPrevia(false);
                CargarTransportadoras();

                chkFiltrarTransportadora.IsChecked = false;
                chkFiltrarTransportadora.IsEnabled = true;
                cmbTransportadoras.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "❌ Error";
                statusBarText.Text = "❌ Error";
            }
        }

        // ✅ Al marcar el check, filtra INMEDIATAMENTE con la transportadora ya seleccionada
        private void ChkFiltrarTransportadora_Checked(object sender, RoutedEventArgs e)
        {
            cmbTransportadoras.IsEnabled = true;
            AplicarFiltroTransportadora();
        }

        private void ChkFiltrarTransportadora_Unchecked(object sender, RoutedEventArgs e)
        {
            cmbTransportadoras.IsEnabled = false;

            // ✅ Plan B: restaurar todas las filas en dgEnviosRaw
            if (_modoPlanB)
            {
                dgEnviosRaw.ItemsSource = null;
                dgEnviosRaw.ItemsSource = _processor.EnviosRawView;

                statusBarText.Text = $"✅ Mostrando todos los registros. Total: {_processor.EnviosRawView.Count}";
                return;
            }

            // Flujo normal
            MostrarTodosLosDatos();
        }

        private void CmbTransportadoras_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (chkFiltrarTransportadora.IsChecked == true && cmbTransportadoras.SelectedItem != null)
                AplicarFiltroTransportadora();
        }

        // ✅ Filtro por transportadora (Plan B filtra dgEnviosRaw; flujo normal filtra dgDatos)
        private void AplicarFiltroTransportadora()
        {
            if (cmbTransportadoras.SelectedItem == null) return;

            var transportadoraSeleccionada = ((TransportadoraItem)cmbTransportadoras.SelectedItem).Key;

            // ========================================================
            // ✅ MODO PLAN B: filtrar directamente el dgEnviosRaw
            // ========================================================
            if (_modoPlanB)
            {
                var filtrados = _processor.EnviosRawView
                    .Where(r => r.NombreCarrier?.Trim() == transportadoraSeleccionada)
                    .ToList();

                dgEnviosRaw.ItemsSource = null;
                dgEnviosRaw.ItemsSource = filtrados;

                statusBarText.Text = $"🔍 Mostrando {filtrados.Count} registros de {transportadoraSeleccionada} (sin procesar)";
                return;
            }

            // ========================================================
            // FLUJO NORMAL
            // ========================================================
            if (_ultimosDatos == null || !_ultimosDatos.Any()) return;

            _datosFiltradosPorTransportadora = _ultimosDatos
                .Where(r => r.Name1?.Trim() == transportadoraSeleccionada)
                .ToList();

            dgDatos.ItemsSource = null;
            dgDatos.ItemsSource = _datosFiltradosPorTransportadora;

            statusBarText.Text = $"🔍 Mostrando {_datosFiltradosPorTransportadora.Count} registros de {transportadoraSeleccionada}";
        }

        private void MostrarTodosLosDatos()
        {
            if (_ultimosDatos != null)
            {
                dgDatos.ItemsSource = null;
                dgDatos.ItemsSource = _ultimosDatos;
                statusBarText.Text = $"✅ Mostrando todos los registros. Total: {_ultimosDatos.Count}";
            }
        }

        // ============================================================
        // PROCESAR DATOS
        // ============================================================
        private void BtnProcesar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnProcesar.IsEnabled = false;
                txtStatus.Text = "⏳ Procesando...";
                statusBarText.Text = "Procesando datos...";
                statusBarErrores.Text = "";

                string transportadoraSeleccionada = "";
                if (chkFiltrarTransportadora.IsChecked == true && cmbTransportadoras.SelectedItem != null)
                    transportadoraSeleccionada = ((TransportadoraItem)cmbTransportadoras.SelectedItem).Key;

                // ========================================================
                // ✅ MODO PLAN B: solo vista previa
                // ========================================================
                if (_modoPlanB)
                {
                    _processor.ProcesarEnviosPlanB(transportadoraSeleccionada);

                    _ultimosDatos = _processor.Models.Resultados.ToList();
                    _datosFiltradosPorTransportadora = null;

                    var (count, count100, count200) = _processor.ObtenerResumenCompleto();

                    btnProcesar.IsEnabled = true;
                    txtStatus.Text = "✅ Completado (Envíos)";
                    statusBarText.Text = $"✅ Proceso completado. {count} registros";

                    MessageBox.Show(
                        $"Proceso terminado (Plan B - Envíos mapeado a estructura estándar).\n\n" +
                        $"Total: {count} registros\n" +
                        $"Secuencia 200: {count200}\n" +
                        $"Secuencia 100 (sin número): {count100}",
                        "Proceso Completado", MessageBoxButton.OK, MessageBoxImage.Information);

                    MostrarBotonVistaPrevia(false);
                    AbrirVistaPrevia();
                    return;
                }

                // ========================================================
                // FLUJO NORMAL: solo vista previa
                // ========================================================
                _processor.ProcessData(transportadoraSeleccionada);

                if (_processor.CiudadesNoEncontradas.Any())
                {
                    var mensaje = "Se Detectaron Las Siguientes Ciudades No Encontradas:\n\n" +
                                 string.Join("\n", _processor.CiudadesNoEncontradas) +
                                 "\n\nPor Favor Validar Las Ciudades En La Hoja 'Data' Y Ejecutar Nuevamente El Proceso.";

                    MessageBox.Show(mensaje, "Error - En Validación De Ciudades",
                                  MessageBoxButton.OK, MessageBoxImage.Error);

                    btnProcesar.IsEnabled = true;
                    txtStatus.Text = "❌ Error ciudades";
                    return;
                }

                if (_processor.EntregasNoEncontradas.Any())
                {
                    var mensaje = "Se detectaron las siguientes inconsistencias:\n\n" +
                                 string.Join("\n", _processor.EntregasNoEncontradas);

                    MessageBox.Show(mensaje, "Validación de Cartaportes",
                                  MessageBoxButton.OK, MessageBoxImage.Warning);

                    _processor.Models.Resultados.Clear();
                    btnProcesar.IsEnabled = true;
                    txtStatus.Text = "❌ Error entregas";
                    return;
                }

                _ultimosDatos = _processor.Models.Resultados.ToList();
                _datosFiltradosPorTransportadora = null;

                var (countN, count100N, count200N) = _processor.ObtenerResumenCompleto();

                btnProcesar.IsEnabled = true;
                txtStatus.Text = "✅ Completado";
                statusBarText.Text = $"✅ Proceso completado. {countN} registros";

                MessageBox.Show($"Proceso terminado. Los datos se han guardado en la tabla.\n\n" +
                               $"Total: {countN} registros\n" +
                               $"Secuencia 200: {count200N}\n" +
                               $"Secuencia 100: {count100N}",
                               "Proceso Completado", MessageBoxButton.OK, MessageBoxImage.Information);

                MostrarBotonVistaPrevia(false);
                AbrirVistaPrevia();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                txtStatus.Text = "❌ Error";
                btnProcesar.IsEnabled = true;
            }
        }

        private void BtnLimpiar_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("¿Limpiar todos los datos?", "Confirmar",
                                       MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _processor.Models.Resultados.Clear();
                _processor.Models.DataRecords.Clear();
                _processor.Models.EnviosRecords.Clear();
                _processor.Models.CentrosRecords.Clear();
                _processor.Errores.Clear();
                _processor.CiudadesNoEncontradas.Clear();
                _processor.EntregasNoEncontradas.Clear();
                _processor.EnviosRawView.Clear();
                _processor.ShiptoRecords.Clear();

                dgDatos.ItemsSource = null;
                dgEnviosRaw.ItemsSource = null;

                dgEnviosRaw.Visibility = Visibility.Collapsed;
                dgDatos.Visibility = Visibility.Visible;

                txtArchivo.Text = "Seleccionar Documento";

                btnProcesar.IsEnabled = false;
                btnLimpiar.IsEnabled = false;

                statusBarText.Text = "🗑️ Datos limpiados. Selecciona un archivo nuevamente";
                statusBarErrores.Text = "";
                txtStatus.Text = "✅ Listo";

                MostrarBotonVistaPrevia(false);
                _ultimosDatos = null;
                _datosFiltradosPorTransportadora = null;
                _modoPlanB = false;

                chkFiltrarTransportadora.IsEnabled = false;
                chkFiltrarTransportadora.IsChecked = false;
                cmbTransportadoras.IsEnabled = false;
                cmbTransportadoras.ItemsSource = new List<TransportadoraItem>();
            }
        }

        private void BtnVerVistaPrevia_Click(object sender, RoutedEventArgs e)
        {
            if (_ultimosDatos != null && _ultimosDatos.Any())
            {
                MostrarBotonVistaPrevia(false);

                var preview = new PreviewWindow(_ultimosDatos);
                preview.Owner = this;
                preview.ShowDialog();
            }
            else
            {
                MessageBox.Show("No hay datos para mostrar en la vista previa.", "Sin datos",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
                MostrarBotonVistaPrevia(false);
            }
        }

        private void AbrirVistaPrevia()
        {
            if (_processor.Models.Resultados.Any())
            {
                var preview = new PreviewWindow(_processor.Models.Resultados.ToList());
                preview.Owner = this;
                preview.ShowDialog();
            }
            else
            {
                MessageBox.Show("No hay datos para mostrar en la vista previa.", "Sin datos",
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}