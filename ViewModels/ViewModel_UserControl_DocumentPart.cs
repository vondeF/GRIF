using GRIF.Entities;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows;
using GRIF.UserControls;
using DocumentFormat.OpenXml.Wordprocessing;

namespace GRIF.ViewModels
{
    public class ViewModel_UserControl_DocumentPart : INotifyPropertyChanged
    {
        private readonly DocumentPart _documentPart;
        private readonly int _initialNumberOfEditing;
        public string DisplayName
        {
            get => _documentPart.DisplayName;
            set
            {
                _documentPart.DisplayName = value;
                OnPropertyChanged();
            }
        }
        public int NumberOfEditing
        {
            get => _documentPart.NumberOfEditing;
            set
            {
                _documentPart.NumberOfEditing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsModified));
            }
        }

        public bool IsModified => NumberOfEditing != _initialNumberOfEditing;

        public ObservableCollection<UserControl> Controls { get; } = new();

        public ViewModel_UserControl_DocumentPart(DocumentPart documentPart)
        {
            _documentPart = documentPart;
            _initialNumberOfEditing = documentPart.NumberOfEditing;

            foreach (var block in _documentPart.Blocks)
            {
                switch (block)
                {
                    case TableBlock tableBlock:
                        var viewModel1 = new ViewModel_UserControl_TableBlock(tableBlock);
                        var userControl1 = new UserControl_TableBlock();
                        userControl1.DataContext = viewModel1;
                        Controls.Add(userControl1);
                        break;
                    case GRIF.Entities.TextBlock textBlock:
                        var viewModel2 = new ViewModel_UserControl_TextBlock(textBlock);
                        var userControl2 = new UserControl_TextBlock();
                        userControl2.DataContext = viewModel2;
                        Controls.Add(userControl2);
                        break;
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
