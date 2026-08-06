using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;

namespace GestorEnvios.Controls
{
    public partial class FilterHeader : UserControl
    {
        public event EventHandler<FilterChangedEventArgs>? FilterChanged;
        public event EventHandler<SortEventArgs>? SortRequested;
        
        private List<string> _allItems = new List<string>();
        private string _columnName = "";
        private bool _isFilterActive = false;
        private string _currentFilterText = "";
        private List<string> _selectedItems = new List<string>();

        public FilterHeader()
        {
            InitializeComponent();
        }

        public void SetColumnName(string columnName)
        {
            _columnName = columnName;
            HeaderText.Text = columnName;
        }

        public void SetValues(List<string> values)
        {
            _allItems = values.Distinct().OrderBy(v => v).ToList();
            _selectedItems = new List<string>(_allItems);
        }

        private void HeaderButton_Click(object sender, RoutedEventArgs e)
        {
            CargarValores();
            FilterPopup.IsOpen = !FilterPopup.IsOpen;
        }

        private void CargarValores()
        {
            SearchBox.Text = "";
            _currentFilterText = "";
            
            ItemsControl.Items.Clear();
            
            if (_allItems == null || !_allItems.Any())
            {
                ItemsControl.Items.Add(new FilterItem { Value = "(Sin datos)", IsSelected = false });
                return;
            }
            
            // Crear items con CheckBox
            foreach (var item in _allItems)
            {
                var isSelected = _isFilterActive && _selectedItems.Contains(item);
                // Si no hay filtro activo, seleccionar todos
                if (!_isFilterActive)
                    isSelected = true;
                    
                ItemsControl.Items.Add(new FilterItem { Value = item, IsSelected = isSelected });
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var search = SearchBox.Text?.ToLower() ?? "";
            _currentFilterText = search;
            
            // Guardar selección actual
            var selectedValues = new HashSet<string>();
            foreach (FilterItem item in ItemsControl.Items)
            {
                if (item.IsSelected)
                    selectedValues.Add(item.Value);
            }
            
            // Filtrar items
            var filteredItems = _allItems
                .Where(v => v.ToLower().Contains(search))
                .ToList();
            
            // Reconstruir lista
            ItemsControl.Items.Clear();
            foreach (var item in filteredItems)
            {
                var isSelected = selectedValues.Contains(item);
                ItemsControl.Items.Add(new FilterItem { Value = item, IsSelected = isSelected });
            }
        }

        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FilterItem item in ItemsControl.Items)
            {
                item.IsSelected = true;
            }
        }

        private void ClearAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (FilterItem item in ItemsControl.Items)
            {
                item.IsSelected = false;
            }
        }

        private void ApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = new List<string>();
            foreach (FilterItem item in ItemsControl.Items)
            {
                if (item.IsSelected)
                    selectedItems.Add(item.Value);
            }
            
            FilterPopup.IsOpen = false;
            
            _selectedItems = selectedItems;
            _isFilterActive = selectedItems.Count > 0 && selectedItems.Count < _allItems.Count;
            
            if (_isFilterActive)
            {
                FilterIndicator.Text = "●";
                FilterIndicator.Foreground = System.Windows.Media.Brushes.Yellow;
                FilterIndicator.ToolTip = "Filtro activo";
            }
            else
            {
                FilterIndicator.Text = "";
                FilterIndicator.ToolTip = "";
            }
            
            FilterChanged?.Invoke(this, new FilterChangedEventArgs
            {
                ColumnName = _columnName,
                SelectedValues = selectedItems,
                IsFilterActive = _isFilterActive
            });
        }

        private void SortAscending_Click(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = false;
            SortRequested?.Invoke(this, new SortEventArgs
            {
                ColumnName = _columnName,
                Direction = SortDirection.Ascending
            });
        }

        private void SortDescending_Click(object sender, RoutedEventArgs e)
        {
            FilterPopup.IsOpen = false;
            SortRequested?.Invoke(this, new SortEventArgs
            {
                ColumnName = _columnName,
                Direction = SortDirection.Descending
            });
        }

        public void ClearFilter()
        {
            _isFilterActive = false;
            _selectedItems = new List<string>(_allItems);
            FilterIndicator.Text = "";
            FilterIndicator.ToolTip = "";
        }
    }

    // Clase para los items con CheckBox
    public class FilterItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public string Value { get; set; } = "";
        
        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
                }
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class FilterChangedEventArgs : EventArgs
    {
        public string ColumnName { get; set; } = "";
        public List<string> SelectedValues { get; set; } = new List<string>();
        public bool IsFilterActive { get; set; }
    }

    public class SortEventArgs : EventArgs
    {
        public string ColumnName { get; set; } = "";
        public SortDirection Direction { get; set; }
    }

    public enum SortDirection
    {
        Ascending,
        Descending
    }
}