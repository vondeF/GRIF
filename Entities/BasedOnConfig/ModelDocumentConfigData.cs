using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Entities.BasedOnConfig
{
    public class ModelDocumentConfigData
    {
        public string? Tag { get; set; }
        public string Name { get; set; } = null!;
        public List<TableConfig> Tables { get; set; } = new();
    }

    public class TableConfig
    {
        // Свойство: имя закладки в Word-шаблоне, куда будет вставлена таблица
        public string BookmarkName { get; set; } = null!;

        // Свойство: отображаемое имя таблицы (например, "Абстрактные классы ИМ")
        public string DisplayName { get; set; } = null!;

        // Свойство: тег, используемый для поиска (может быть null)
        public string? Tag { get; set; }

        public string DataType { get; set; } = null!;
    }
}
