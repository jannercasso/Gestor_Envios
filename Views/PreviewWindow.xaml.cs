using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using GestorEnvios.Models;
using GestorEnvios.Services;

namespace GestorEnvios.Views
{
    public partial class PreviewWindow : Window
    {
        private List<EnvioData> _allData;
        private readonly DataProcessor _processor;

        public PreviewWindow(List<EnvioData> data)
        {
            InitializeComponent();
            
            _processor = new DataProcessor();
            
            if (data == null || !data.Any())
            {
                _allData = new List<EnvioData>();
            }
            else
            {
                _allData = data;
            }
            
            dgPreview.ItemsSource = _allData;
            txtCount.Text = $"Total: {_allData.Count} registros";
        }

        private void BtnExportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_allData.Any())
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

                excelService.SaveExcel(filePath, _allData);
                
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