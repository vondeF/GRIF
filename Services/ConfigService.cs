using GRIF.Entities.BasedOnConfig;
using GRIF.Entities;
using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace GRIF.Services
{
    public interface IModelDocumentConfigService
    {
        /// <summary>
        /// Возвращает коллекцию объектов, которые содержат конфигурационные данные документов ГОСТ
        /// </summary>
        /// <returns></returns>
        ObservableCollection<ModelDocumentConfigData> LoadConfigData();
    }

    public interface IExchangeDocumentConfigService
    {
        /// <summary>
        /// Возвращает коллекцию объектов, которые содержат конфигурационные данные документов профилей обмена
        /// </summary>
        /// <returns></returns>
        ObservableCollection<ExchangeDocumentConfigData> LoadConfigData();
    }

    /// <summary>
    /// Общий сервис загрузки конфигурационного файла
    /// </summary>
    public abstract class ConfigService<T>
    {
        protected readonly string _configPath;

        public ConfigService(string configPath)
        {
            if (string.IsNullOrWhiteSpace(configPath))
                throw new ArgumentException("Путь к конфигурационному файлу не может быть пустым.", nameof(configPath));

            if (!File.Exists(configPath))
                throw new FileNotFoundException($"Конфигурационный файл не найден: {configPath}");

            _configPath = configPath;
        }

        public ObservableCollection<T> LoadConfigData()
        {
            var json = File.ReadAllText(_configPath);

            // Проверяем, что файл не пуст
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidOperationException($"Конфигурационный файл пуст: {_configPath}");
            try
            {
                // Десериализуем в Root-объект
                var root = JsonSerializer.Deserialize<ConfigRoot<T>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // Проверяем, что десериализация не вернула null
                if (root == null)
                    throw new JsonException($"Не удалось десериализовать JSON-файл: {_configPath}. Формат файла некорректен.");

                // Вызываем валидацию, если нужно
                ValidateConfigData(root.Items);

                return new ObservableCollection<T>(root.Items);
            }
            catch (JsonException ex)
            {
                throw new JsonException($"Ошибка десериализации JSON-файла: {_configPath}. {ex.Message}", ex);
            }
        }

        // Абстрактный метод для валидации
        protected virtual void ValidateConfigData(List<T> items) { }
    }

    /// <summary>
    /// Сервис загрузки конфигурационного файла ГОСТа
    /// </summary>
    public class ModelDocumentConfigService : ConfigService<ModelDocumentConfigData>, IModelDocumentConfigService
    {
        public ModelDocumentConfigService(string configPath) : base(configPath)
        {
        }

        protected override void ValidateConfigData(List<ModelDocumentConfigData> items)
        {
            foreach (var gost in items)
            {
                foreach (var table in gost.Tables)
                {
                    if (!TableModelDataTypes.Values.Select(x => x.Name).Contains(table.DataType))
                    {
                        throw new InvalidOperationException($"Недопустимое значение DataType ({table.DataType}) у таблицы \"{table.DisplayName}\" для \"{gost.Name}\".\n\nПуть к конфигурационному файлу: {_configPath}.\n\nДопустимые значения DataType:\n{string.Join(",\n", TableModelDataTypes.Values.Select(x => x.Name))}.");
                    }
                }
            }
        }

    }

    /// <summary>
    /// Сервис загрузки конфигурационного файла профиля обмена
    /// </summary>
    public class ExchangeDocumentConfigService : ConfigService<ExchangeDocumentConfigData>, IExchangeDocumentConfigService
    {
        public ExchangeDocumentConfigService(string configPath) : base(configPath)
        {
        }
    }

    public class ConfigRoot<T>
    {
        public List<T> Items { get; set; } = new();
    }
}
