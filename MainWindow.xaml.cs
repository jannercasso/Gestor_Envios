using System;
using System.Linq;
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
    // MAIN WINDOW
    // ================================================
    
    public partial class MainWindow : Window
    {
        private readonly DataProcessor _processor;
        private List<EnvioData> _ultimosDatos; // ✅ Guardar los datos para volver a mostrarlos

        public MainWindow()
        {
            InitializeComponent();
            _processor = new DataProcessor();
            
            // ✅ Ocultar botón de vista previa al inicio
            btnVerVistaPrevia.Visibility = Visibility.Collapsed;
        }

        // ✅ Método público para que PreviewWindow pueda mostrar/ocultar el botón
        public void MostrarBotonVistaPrevia(bool mostrar)
        {
            Dispatcher.Invoke(() =>
            {
                btnVerVistaPrevia.Visibility = mostrar ? Visibility.Visible : Visibility.Collapsed;
            });
        }

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
                    
                    dgDatos.ItemsSource = null;
                    dgDatos.ItemsSource = _processor.Models.DataRecords;
                    
                    var count = _processor.Models.DataRecords.Count;
                    
                    statusBarText.Text = $"✅ Vista previa de Data cargada. {count} registros";
                    
                    btnProcesar.IsEnabled = true;
                    btnLimpiar.IsEnabled = true;
                    txtStatus.Text = "✅ Data cargada";
                    statusBarErrores.Text = "";
                    
                    // ✅ Ocultar botón de vista previa al cargar nuevos datos
                    MostrarBotonVistaPrevia(false);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al cargar vista previa: {ex.Message}", "Error", 
                                  MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnProcesar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnProcesar.IsEnabled = false;
                txtStatus.Text = "⏳ Procesando...";
                statusBarText.Text = "Procesando datos...";
                statusBarErrores.Text = "";

                _processor.ProcessData();

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
                    dgDatos.ItemsSource = null;
                    dgDatos.ItemsSource = _processor.Models.DataRecords;
                    btnProcesar.IsEnabled = true;
                    txtStatus.Text = "❌ Error entregas";
                    return;
                }

                dgDatos.ItemsSource = null;
                dgDatos.ItemsSource = _processor.Models.Resultados;

                var (count, count100, count200) = _processor.ObtenerResumenCompleto();
                
                btnProcesar.IsEnabled = true;

                statusBarText.Text = $"✅ Proceso completado. {count} registros";
                txtStatus.Text = "✅ Completado";

                // ✅ Guardar los datos para poder volver a mostrarlos
                _ultimosDatos = _processor.Models.Resultados.ToList();

                MessageBox.Show($"Proceso terminado. Los datos se han guardado en la tabla.\n\n" +
                               $"Total: {count} registros\n" +
                               $"Secuencia 200: {count200}\n" +
                               $"Secuencia 100: {count100}", 
                               "Proceso Completado", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // ✅ Ocultar botón de vista previa (por si acaso)
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
                
                dgDatos.ItemsSource = null;
                txtArchivo.Text = "Seleccionar Documento";
                
                btnProcesar.IsEnabled = false;
                btnLimpiar.IsEnabled = false;
                
                statusBarText.Text = "🗑️ Datos limpiados. Selecciona un archivo nuevamente";
                statusBarErrores.Text = "";
                txtStatus.Text = "✅ Listo";
                
                // ✅ Ocultar botón de vista previa al limpiar
                MostrarBotonVistaPrevia(false);
                _ultimosDatos = null;
            }
        }

        // ✅ Evento del botón "Ver Vista Previa"
        private void BtnVerVistaPrevia_Click(object sender, RoutedEventArgs e)
        {
            if (_ultimosDatos != null && _ultimosDatos.Any())
            {
                // ✅ Ocultar el botón antes de abrir la vista previa
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