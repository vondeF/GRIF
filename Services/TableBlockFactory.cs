using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Spreadsheet;
using GostObjectsClassLibrary;
using GostObjectsClassLibrary.GostDoc;
using GostObjectsClassLibrary.ProfileDoc;
using GRIF.Entities;
using GRIF.Entities.BasedOnConfig;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Services
{
    public interface ITableBlockFactory
    {
        TableBlock CreateTableBlock(TableConfig tableConfig, GostDoc gostDoc);
        TableBlock CreateTableBlockAttrs(ProfileClass exchangeClass);
        TableBlock CreateTableBlockAsocs(ProfileClass exchangeClass);

    }

    public class TableBlockFactory : ITableBlockFactory
    {
        public TableBlock CreateTableBlock(TableConfig tableConfig, GostDoc gostDoc)
        {
            var table = new TableBlock(tableConfig);

            // Вытаскиваем имя свойства по типу данных таблицы
            var dataType = TableModelDataTypes.Values.FirstOrDefault(x => x.Name == tableConfig.DataType);
            if (dataType == null)
                throw new NullReferenceException($"Не найден тип данных \"{tableConfig.DataType}\" таблицы \"{tableConfig.DisplayName}\"");
            var propertyName = dataType.PropertyName;
            
            // Инофрмация о свойстве, где содержаться нужные данные
            var propertyInfo = gostDoc.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
                throw new NullReferenceException($"Не найдено свойство \"{propertyName}\" для заполения таблицы \"{tableConfig.DisplayName}\"");
            
            // Вытаскиваем данные
            var value = propertyInfo.GetValue(gostDoc); // Данные для заполнения таблицы
            var basicProfileExt = tableConfig.Tag;
            if (value is IEnumerable<GostDocObject> data)
            {
                // Заполняем таблицу
                var target = basicProfileExt == null ? data.Where(x => String.IsNullOrEmpty(x.BasicProfileExt)) : data.Where(x => x.BasicProfileExt == basicProfileExt);
                foreach (var obj in target)
                {
                    Type objType = obj.GetType();
                    Type tableRowType = typeof(TableRow);
                    var constructor = tableRowType.GetConstructor(new[] { objType }); // Ищем подходящий конструктор

                    if (constructor != null)
                    {
                        var tableRow = constructor.Invoke(new object[] { obj });
                        table.AddRow((TableRow)tableRow);
                    }
                    else
                        throw new ArgumentException($"Конструктор TableRow({objType.Name}) не найден");
                }
            }
            return table;
        }


        public TableBlock CreateTableBlockAttrs(ProfileClass exchangeClass)
        {
            var table = new TableBlock($"Атрибуты {(exchangeClass.Stereotype == "cim" ? exchangeClass.Name : $"{exchangeClass.Stereotype}:{exchangeClass.Name}")}", "Атрибуты");
            var grouped = exchangeClass.Attributes.GroupBy(x => x.InheritanceLevel);
            foreach (var group in  grouped.OrderBy(x => x.Key))
            {
                var attrs = group.ToList();
                var className = attrs.First().ClassName ?? "";
                var classStereotype = attrs.First().ClassStereotype ?? "";

                table.AddRow(new TableRow(className, classStereotype, exchangeClass.ClassGuid!, nameof(ProfileAttribute), group.Key ?? 0));
                foreach(var attr in attrs.OrderBy(x => x.Name))
                {
                    table.AddRow(new TableRow(attr, exchangeClass.ClassGuid));
                }    
            }
            return table;
        }

        public TableBlock CreateTableBlockAsocs(ProfileClass exchangeClass)
        {
            
            var table = new TableBlock($"Ассоциации {(exchangeClass.Stereotype == "cim" ? exchangeClass.Name : $"{exchangeClass.Stereotype}:{exchangeClass.Name}")}", "Ассоциации");
            var grouped = exchangeClass.Associations.GroupBy(x => x.InheritanceLevel);

            foreach (var group in grouped.OrderBy(x => x.Key))
            {
                var asocs = group.ToList();
                var className = asocs.First().ClassName ?? "";
                var classStereotype = asocs.First().ClassStereotype ?? "";

                table.AddRow(new TableRow(className, classStereotype, exchangeClass.ClassGuid, nameof(ProfileAssociation), group.Key ?? 0));

                foreach (var asoc in asocs.OrderBy(x => x.Name))
                {
                    table.AddRow(new TableRow(asoc));
                }
            }
            return table;
        }
    }
}
