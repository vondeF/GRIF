using GRIF.Entities.BasedOnConfig;
using GRIF.Entities;
using GRIF.Services;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using GRIF.DataBaseContexts;
using GostObjectsClassLibrary.ProfileDoc;
using System.Xml;

namespace GRIF.ViewModels
{
    public class ViewModel_MainWindow : INotifyPropertyChanged
    {
        // Сервис загрузки конфига ГОСТов
        private readonly IModelDocumentConfigService _configModelService;
        // Сервис загрузки конфига профилей обмена
        private readonly IExchangeDocumentConfigService _configExchangeService;
        // Фабрика для создания TemplateService
        private readonly ITemplateServiceFactory _templateServiceFactory;
        // Фабрика для создания ViewModel окна предварительного просмотра 
        private readonly IPreviewDocumentViewModelFactory _viewModelFactory;
        private readonly IDataBaseLoaderFactory _dataBaseLoaderFactory;

        // --- ДАННЫЕ ДЛЯ ПРОГРЕСС БАРА ---
        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }

        private static readonly string _startProgressText = "Создать";
        private string _progressText = _startProgressText;
        public string ProgressText
        {
            get => _progressText;
            set
            {
                _progressText = value;
                OnPropertyChanged();
            }
        }

        // --- ДАННЫЕ ДЛЯ ВКЛАДКИ ГОСТОВ ---
        public ObservableCollection<ModelDocumentConfigData> ModelDocuments { get; private set; } = new();
        private ModelDocumentConfigData? _selectedModelDocument;
        public ModelDocumentConfigData? SelectedModelDocument
        {
            get => _selectedModelDocument;
            set
            {
                _selectedModelDocument = value;
                OnPropertyChanged();   // Уведомляет UI об изменении свойства
            }
        }

        // --- ДАННЫЕ ДЛЯ ВКЛАДКИ ПРОФИЛЕЙ ОБМЕНА ---
        //public ObservableCollection<ExchangeDocumentConfigData> ExchangeDocuments { get; private set; } = new();
        //private ExchangeDocumentConfigData? _selectedExchangeDocument;
        //public ExchangeDocumentConfigData? SelectedExchangeDocument
        //{
        //    get => _selectedExchangeDocument;
        //    set
        //    {
        //        _selectedExchangeDocument = value;
        //        OnPropertyChanged();
        //    }
        //}
        public ObservableCollection<ExchangeDocumentDbData> ExchangeDocuments { get; private set; } = new();
        private ExchangeDocumentDbData? _selectedExchangeDocument;
        public ExchangeDocumentDbData? SelectedExchangeDocument
        {
            get => _selectedExchangeDocument;
            set
            {
                _selectedExchangeDocument = value;
                OnPropertyChanged();
            }
        }

        // --- КОМАНДЫ ---
        public AsyncRelayCommand CreateWithTemplate_ModelDocumentCommand { get; }
        public AsyncRelayCommand CreateWithTemplate_ExchangeDocumentCommand { get; }

        public ViewModel_MainWindow(IModelDocumentConfigService configModelService, IExchangeDocumentConfigService configExchangeService, ITemplateServiceFactory templateServiceFactory, IPreviewDocumentViewModelFactory viewModelFactory, IDataBaseLoaderFactory dataBaseLoaderFactory)
        {
            this._configModelService = configModelService;
            this._configExchangeService = configExchangeService;
            this._templateServiceFactory = templateServiceFactory;
            this._viewModelFactory = viewModelFactory;
            this._dataBaseLoaderFactory = dataBaseLoaderFactory;

            // Загружаем списки ГОСТов и профилей из конфигурационных файлов
            LoadModelDocumentsData();
            LoadExchangeDocumentsData();

            // Инициализируем команды
            CreateWithTemplate_ModelDocumentCommand = new AsyncRelayCommand(
                async (parameter) => await CreateDocumentWithTemplate(_selectedModelDocument),
                _ => SelectedModelDocument != null); // Может быть выполнена только если SelectedModelDocument != null

            CreateWithTemplate_ExchangeDocumentCommand = new AsyncRelayCommand(
                async (parameter) => await CreateExchangeDocumentWithoutTemplate(_selectedExchangeDocument!),
                _ => SelectedExchangeDocument != null);
        }

        /// <summary>
        /// Загружаем все ГОСТы из конфигурационного файла
        /// </summary>
        private void LoadModelDocumentsData()
        {
            ModelDocuments = this._configModelService.LoadConfigData();
        }

        /// <summary>
        /// Загружаем все профили информационного обмена из конфигурационного файла
        /// </summary>
        private void LoadExchangeDocumentsData()
        {
            //ExchangeDocuments = this._configExchangeService.LoadConfigData();

            using (ExchangeDownloadContext db = new ExchangeDownloadContext())
            {
                var list = db.GostProfiles.Select(x => new ExchangeDocumentDbData(x)).ToList();
                ExchangeDocuments = new ObservableCollection<ExchangeDocumentDbData>(list);
            }
        }

        /// <summary>
        /// Функция создания документа
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="documentConfigData"></param>
        private async Task CreateDocumentWithTemplate<T>(T documentConfigData)
        {
            IsLoading = true;
            ProgressText = "Загрузка с шаблоном...";
            try
            {
                var templateService = _templateServiceFactory.Create(documentConfigData!);
                var dbLoader = _dataBaseLoaderFactory.CreateLoader(documentConfigData!);
                var viewModel = await _viewModelFactory.CreateAsync(documentConfigData!, templateService, dbLoader);
                var previewWindow = new PreviewDocument(viewModel);

                ProgressText = "Загрузка завершена";
                IsLoading = false;

                previewWindow.ShowDialog();

                ProgressText = _startProgressText;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии на редактирование документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                IsLoading = false;
                ProgressText = _startProgressText;
            }
        }

        private async Task CreateExchangeDocumentWithoutTemplate(ExchangeDocumentDbData documentData)
        {
            IsLoading = true;
            ProgressText = "Загрузка без шаблона...";
            try
            {
                var templateService = _templateServiceFactory.Create(documentData!);
                var dbLoader = _dataBaseLoaderFactory.CreateLoader(documentData!);
                var viewModel = await _viewModelFactory.CreateAsync(documentData!, templateService, dbLoader);
                var previewWindow = new PreviewDocument(viewModel);

                ProgressText = "Загрузка завершена";
                IsLoading = false;

                previewWindow.ShowDialog();

                ProgressText = _startProgressText;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии на редактирование документа: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                IsLoading = false;
                ProgressText = _startProgressText;
            }
        }

        // Событие, которое вызывается при изменении свойства ViewModel
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            // Вызываем событие PropertyChanged с именем свойства, которое изменилось
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
