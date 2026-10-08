using DocumentFormat.OpenXml.Presentation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Entities
{
    public static class TableModelDataTypes
    {
        public static DataType[] Values =
        {
            new DataType("Абстрактные классы", "classesAbstract", new List<string> {"Смысловое определение абстрактного класса", "Имя класса (англ.)", "Имя вышестоящего класса (англ.)"}),
            new DataType("Основные классы", "classesMain", new List<string> { "Смысловое определение основного класса", "Имя класса (англ.)", "Имя вышестоящего класса (англ.)"}),
            new DataType("Атрибуты", "attributesAbstractAndMain", new List<string> {"Смысловое назначение атрибута", "Имя атрибута (англ.)", "Имя класса атрибута (англ.)", "Тип данных"}),
            new DataType("Ассоциации", "associationsAbstractAndMain", new List<string> {"Смысловое назначение ассоциации", "Начальный класс", "Конечный класс", "Имя ассоциации (англ.)", "Множественность"}),
            new DataType("Структурные классы", "classesCompound", new List<string> {"Смысловое определение структурного класса", "Имя класса (англ.)", "Имя вышестоящего класса (англ.)"}),
            new DataType("Атрибуты структурных классов", "attributesCompound", new List<string> {"Смысловое назначение атрибута", "Имя атрибута (англ.)", "Имя класса атрибута (англ.)", "Тип данных"}),
            new DataType("Справочные классы", "classesEnum", new List<string> { "Смысловое определение справочного класса", "Имя класса (англ.)", "Имя вышестоящего класса (англ.)"}),
            new DataType("Атрибуты справочных классов", "attributesEnum", new List<string> {"Смысловое назначение атрибута", "Имя атрибута (англ.)", "Имя класса атрибута (англ.)"}),
            new DataType("Базовые классы", "classesPrimitive", new List<string> {"Смысловое определение класса", "Имя класса (англ.)", "Имя вышестоящего класса (англ.)"}),
            new DataType("Типы данных CIM", "classesCimDatatype", new List<string> {"Смысловое определение класса", "Имя класса (англ.)", "Имя вышестоящего класса (англ.)"}),
            new DataType("Атрибуты типов данных CIM", "attributesCimDatatype", new List<string> {"Смысловое назначение атрибута", "Имя атрибута (англ.)", "Имя класса атрибута (англ.)", "Тип данных"})
        };
    }
    public static class TableExchangeDataTypes
    {
        public static DataType[] Values =
        { // TODO: добавить имя свойства
            new DataType("Атрибуты", "", new List<string> { "Имя атрибута (англ.)", "Обязательность атрибута", "Тип данных", "Описание и ограничения"}),
            new DataType("Ассоциации", "", new List<string> {"Наименование ассоциации", "Множественность (от)", "Множественность (к)", "Конечный класс ассоциации", "Описание и ограничения"}),
        };
    }

    public class DataType
    {
        public string Name { get; set; }
        public string PropertyName { get; set; }
        public List<string> Headers { get; set; }
        public DataType(string name, string propertyName, List<string> headers) 
        {
            Name = name;
            PropertyName = propertyName;
            Headers = headers;        
        }
    }
}
