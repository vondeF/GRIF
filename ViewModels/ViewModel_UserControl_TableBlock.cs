using GRIF.Entities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Controls;

namespace GRIF.ViewModels
{
    public class ViewModel_UserControl_TableBlock : INotifyPropertyChanged
    {
        private readonly TableBlock _tableBlock;
        private readonly int _initialNumberOfEditing;
        public string DisplayName => _tableBlock.DisplayName;
        public List<string> ColumnHeaders => _tableBlock.ColumnHeaders;

        public int NumberOfEditing
        {
            get => _tableBlock.NumberOfEditing;
            set
            {
                _tableBlock.NumberOfEditing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsModified));
            }
        }

        public bool IsModified => NumberOfEditing != _initialNumberOfEditing;

        // Observable коллекция для DataGrid
        public ObservableCollection<TableRow> AllRows { get; }
        public ICollectionView FilteredRows { get; }

        // Фильтры
        private bool _showIdentical = true;
        private bool _showAdded = true;
        private bool _showDeleted = true;
        private bool _showEditedNew = true;
        private bool _showEditedOld = true;

        public bool ShowIdentical
        {
            get => _showIdentical;
            set
            {
                _showIdentical = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public bool ShowAdded
        {
            get => _showAdded;
            set
            {
                _showAdded = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public bool ShowDeleted
        {
            get => _showDeleted;
            set
            {
                _showDeleted = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public bool ShowEditedNew
        {
            get => _showEditedNew;
            set
            {
                _showEditedNew = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public bool ShowEditedOld
        {
            get => _showEditedOld;
            set
            {
                _showEditedOld = value;
                OnPropertyChanged();
                ApplyFilter();
            }
        }

        public RelayCommand ClearFiltersCommand { get; }

        public ViewModel_UserControl_TableBlock(TableBlock tableBlock)
        {
            _tableBlock = tableBlock;
            _initialNumberOfEditing = tableBlock.NumberOfEditing;

            // Заполняем коллекцию строками
            AllRows = new ObservableCollection<TableRow>(tableBlock.Rows);
            FilteredRows = CollectionViewSource.GetDefaultView(AllRows);

            // Устанавливаем фильтр
            FilteredRows.Filter = FilterRows;

            ClearFiltersCommand = new RelayCommand(ClearFilters);
        }

        private bool FilterRows(object item)
        {
            if (item is TableRow row)
            {
                return row.Status switch
                {
                    TableRow.RowStatus.Identical => ShowIdentical,
                    TableRow.RowStatus.Added => ShowAdded,
                    TableRow.RowStatus.Deleted => ShowDeleted,
                    TableRow.RowStatus.EditedNew => ShowEditedNew,
                    TableRow.RowStatus.EditedOld => ShowEditedOld,
                    _ => true
                };
            }
            return false;
        }

        private void ApplyFilter()
        {
            FilteredRows.Refresh();
        }

        public void ClearFilters(object? parameter)
        {
            ShowIdentical = true;
            ShowAdded = true;
            ShowDeleted = true;
            ShowEditedNew = true;
            ShowEditedOld = true;

            FilteredRows.SortDescriptions.Clear();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
