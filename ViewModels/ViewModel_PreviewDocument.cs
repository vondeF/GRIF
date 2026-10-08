using DocumentFormat.OpenXml.Wordprocessing;
using GRIF.Entities;
using GRIF.Services;
using GRIF.UserControls;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Spreadsheet;
using GRIF.Entities.BasedOnConfig;

namespace GRIF.ViewModels
{
    public class ViewModel_PreviewDocument : INotifyPropertyChanged
    {
        private IDocument? _document;
        private readonly TemplateService _templateService;
        private readonly IDocumentFactory _documentFactory;
        private readonly object _documentConfigData;
        private int _initialNumberOfEditing;
        private IDataBaseLoader _dataBaseLoader;
        private (Template Template, GRIF.Services.ValidationResult ValidationResult) _templateCreation;

        public Task InitializationTask { get; private set; }

        private int _numberOfEditing = 0;
        public int NumberOfEditing
        {
            get => _numberOfEditing;
            set
            {
                _numberOfEditing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsModified));
            }
        }

        public bool IsModified => NumberOfEditing != _initialNumberOfEditing;

        private string _displayName = "";
        public string DisplayName 
        { 
            get => _displayName; 
            set
            {
                _displayName = value;
                OnPropertyChanged();
            } 
        }

        private string _title = "";
        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged();
            }
        }

        private string _information = "";
        public string Information
        {
            get => _information;
            set
            {
                _information = value;
                OnPropertyChanged();
            }
        }

        private string _templatePath = "";
        public string TemplatePath
        {
            get => _templatePath;
            set
            {
                _templatePath = value;
                OnPropertyChanged();
            }
        }

        private bool _isValidationSuccesfull = true;
        public bool IsValidationSuccesfull
        {
            get => _isValidationSuccesfull;
            set
            {
                _isValidationSuccesfull = value;
                OnPropertyChanged();
                if (_isValidationSuccesfull)
                    Title = "Предварительный просмотр ";
                else
                    Title = "Валидация данный не пройдена ";
            }
        }

        private bool _areTemplateElementsVisible = true;
        public bool AreTemplateElementsVisible
        {
            get => _areTemplateElementsVisible;
            set
            {
                _areTemplateElementsVisible = value;
                OnPropertyChanged();
            }
        }
        public bool IsVisible => IsValidationSuccesfull && AreTemplateElementsVisible;

        public ObservableCollection<UserControl> Controls { get; } = new();
        public AsyncRelayCommand LoadTemplateCommand { get; }
        public RelayCommand SaveTemplateCommand { get; }
        public RelayCommand WriteDiffCommand { get; }
        public RelayCommand WriteAllCommand { get; }
        public AsyncRelayCommand SaveToDbCommand { get; }
        public AsyncRelayCommand DeleteLastVersionFromDbCommand { get; }

        public ViewModel_PreviewDocument(object documentConfigData, TemplateService templateService, IDocumentFactory documentFactory, IDataBaseLoader dataBaseLoader)
        {
            _documentFactory = documentFactory;
            _templateService = templateService;
            _documentConfigData = documentConfigData;
            _dataBaseLoader = dataBaseLoader;

            // Сборка документа
            if (documentConfigData is ExchangeDocumentDbData)
                AreTemplateElementsVisible = false;
            _templateCreation = _templateService.GetNewestTemplate();
            InitializationTask = InitializeAsync();

            LoadTemplateCommand = new AsyncRelayCommand(LoadTemplateAsync);
            SaveTemplateCommand = new RelayCommand(SaveTemplate);
            WriteDiffCommand = new RelayCommand(WriteDiff);
            WriteAllCommand = new RelayCommand(WriteAll);
            SaveToDbCommand = new AsyncRelayCommand(SaveToDbAsync);
            DeleteLastVersionFromDbCommand = new AsyncRelayCommand(DeleteLastVersionFromDbAsync);
        }

        private async Task InitializeAsync()
        {
            try
            {
                await CreateAndVisualizeDocAsync(_templateCreation);
                TemplatePath = $"Файл шаблона:\n{_templateCreation.Template.TemplateFilePath}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task CreateAndVisualizeDocAsync((Template Template, GRIF.Services.ValidationResult ValidationResult) templateCreation)
        {
            Controls.Clear();

            IsValidationSuccesfull = templateCreation.ValidationResult.IsValidated;
            ShowValidationResult(templateCreation.ValidationResult);
            if (templateCreation.ValidationResult.IsValidated)
            {
                _document = await _documentFactory.CreateAsync(templateCreation.Template, _documentConfigData, _dataBaseLoader);
                _displayName = _document.Name;
                _initialNumberOfEditing = _document.NumberOfEditing;
                _numberOfEditing = _document.NumberOfEditing;

                // Вывод в UI
                foreach (var part in _document.Parts)
                {
                    var viewModel = new ViewModel_UserControl_DocumentPart(part);
                    var userControl = new UserControl_DocumentPart();
                    userControl.DataContext = viewModel;
                    Controls.Add(userControl);
                }
            }
        }

        private void ShowValidationResult(GRIF.Services.ValidationResult validationResult)
        {
            Information = "Результат валидации\n\n";
            if (validationResult.Errors.Any())
            {
                Information += "Ошибки:\n";
                Information += String.Join("\n", validationResult.Errors.Select(error => " - " + error));
            }

            if (validationResult.Warnings.Any())
            {
                Information += "Предупреждения:\n";
                Information += String.Join("\n", validationResult.Warnings.Select(warn => " - " + warn));
            }
        }

        private async Task LoadTemplateAsync(object? parameter)
        {
           _templateCreation = _templateService.ChooseTemplate();
            await InitializeAsync();
        }

        private void SaveTemplate(object? parameter)
        {
            var folderPath = _templateService.GetFolderPath();
            string dateTime = DateTime.Now.ToString("dd.MM.yyyy HH.mm");
            string fileName = $"Шаблон {_document!.Name} {dateTime}.docx";

            var dialog = new SaveFileDialog();
            dialog.Title = "Сохраните файл";
            dialog.InitialDirectory = folderPath;
            dialog.FileName = fileName;
            dialog.Filter = "Word documents |*.doc;*.docx";

            if (dialog.ShowDialog() == true)
            {
                if (Path.GetDirectoryName(dialog.FileName) != folderPath)
                    MessageBox.Show($"Файл не сохранен. Сохраните файл в папке\n{folderPath}");
            }

            if (Path.GetDirectoryName(dialog.FileName) == folderPath)
                _document!.CreateNewTemplate(dialog.FileName);
        }

        private void WriteDiff(object? parameter)
        {
            _document?.WriteDiff();
        }

        private void WriteAll(object? parameter)
        {
            _document?.WriteAll();
        }

        private async Task SaveToDbAsync(object? parameter)
        {
            var result = MessageBox.Show(
                           "Вы уверены, что хотите сохранить текущую версию документа в базе данных?",
                           "Сообщение",
                           MessageBoxButton.YesNo,
                           MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                if (_document == null)
                    return;
                if (_documentConfigData is ModelDocumentConfigData)
                    await _dataBaseLoader.UploadAsync((ModelDocumentWord)_document);
                if (_documentConfigData is ExchangeDocumentDbData)
                    await _dataBaseLoader.UploadAsync((ExchangeDocumentWord)_document);
                await InitializeAsync();
            }    
        }

        private async Task DeleteLastVersionFromDbAsync(object? parameter)
        {
            var result = MessageBox.Show(
                           "Вы уверены, что хотите удалить текущую версию документа в базе данных?",
                           "Сообщение",
                           MessageBoxButton.YesNo,
                           MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                await _dataBaseLoader.DeleteLastVersionAsync();
                await InitializeAsync();
            }    
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
