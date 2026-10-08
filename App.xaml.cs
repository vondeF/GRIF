using GRIF.DataBaseContexts;
using GRIF.Services;
using GRIF.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;

namespace GRIF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider _serviceProvider = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>(); // Создание MainWindow с помощью DI
            mainWindow.Show();
        }

        /// <summary>
        /// Регистрируем все необходимые сервисы
        /// </summary>
        /// <param name="services"></param>
        private void ConfigureServices(ServiceCollection services)
        {
            // Сервисы загрузки конфигурационных файлов
            services.AddSingleton<IModelDocumentConfigService>(provider => new ModelDocumentConfigService(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Конфигурационные файлы\\Профили модели.json")));
            services.AddSingleton<IExchangeDocumentConfigService>(provider => new ExchangeDocumentConfigService(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Конфигурационные файлы\\Профили обмена.json")));
            
            // Фабрики
            services.AddSingleton<ITemplateServiceFactory, TemplateServiceFactory>();
            services.AddSingleton<IDocumentFactory, DocumentFactory>();
            services.AddSingleton<ITableBlockFactory, TableBlockFactory>();
            services.AddTransient<IPreviewDocumentViewModelFactory, PreviewDocumentViewModelFactory>();

            // БД сервисы
            services.AddSingleton<IDataBaseLoaderFactory, DataBaseLoaderFactory>();
            services.AddSingleton<ModelDocumentDataBaseLoader>();
            services.AddSingleton<ExchangeDocumentDataBaseLoader>();
            services.AddSingleton<IDataBaseLoader, ModelDocumentDataBaseLoader>();
            services.AddSingleton<ITableBlockComparer, TableBlockComparer>();

            // Окна и ViewModel
            services.AddTransient<ViewModel_MainWindow>();
            services.AddTransient<ViewModel_PreviewDocument>();
            services.AddTransient<MainWindow>();
            services.AddTransient<PreviewDocument>();
        }
    }
}