using System;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using GestorEnvios.Services;
using GestorEnvios.Views;

namespace GestorEnvios
{
    public partial class MainWindow : Window
    {
        private readonly DataProcessor _processor;

        public MainWindow()
        {
            InitializeComponent();
            _processor = new DataProcessor();
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
                    statusBarCount.Text = $"{count} registros (Data)";
                    
                    btnProcesar.IsEnabled = true;
                    txtStatus.Text = "✅ Data cargada";
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

                var count = _processor.Models.Resultados.Count;
                var count200 = _processor.Models.Resultados.Count(r => r.Secuencia == 200);
                var count100 = _processor.Models.Resultados.Count(r => r.Secuencia == 100);
                
                btnProcesar.IsEnabled = true;

                statusBarText.Text = $"✅ Proceso completado. {count} registros";
                statusBarCount.Text = $"{count} registros (200: {count200}, 100: {count100})";
                txtStatus.Text = "✅ Completado";

                MessageBox.Show($"Proceso terminado. Los datos se han guardado en la tabla.\n\n" +
                               $"Total: {count} registros\n" +
                               $"Secuencia 200: {count200}\n" +
                               $"Secuencia 100: {count100}", 
                               "Proceso Completado", MessageBoxButton.OK, MessageBoxImage.Information);
                
                // Abrir la vista previa automáticamente
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
                txtArchivo.Text = "Selecciona el archivo Excel que contiene las 3 hojas...";
                
                btnProcesar.IsEnabled = false;
                
                statusBarText.Text = "🗑️ Datos limpiados. Selecciona un archivo nuevamente";
                statusBarCount.Text = "0 registros";
                statusBarErrores.Text = "";
                txtStatus.Text = "✅ Listo";
            }
        }

        private void AbrirVistaPrevia()
        {
            if (_processor.Models.Resultados.Any())
            {
                var preview = new PreviewWindow(_processor.Models.Resultados.ToList());
                preview.Owner = this;
                preview.ShowDialog();  // <-- CAMBIADO: ahora es modal
            }
            else
            {
                MessageBox.Show("No hay datos para mostrar en la vista previa.", "Sin datos", 
                              MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}