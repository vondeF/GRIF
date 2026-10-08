using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace GRIF.Entities
{
    public class Template
    {
        public string TemplateFolderPath { get; set; } = null!;
        public string TemplateFilePath { get; set; } = null!;
        private string _temporaryFolder;

        public Template(string templateFolderPath, string templateFilePath)
        {
            TemplateFolderPath = templateFolderPath;
            TemplateFilePath = templateFilePath;
            _temporaryFolder = CreateTemporaryFolder();
            ClearTemporaryFolder();
        }

        /// <summary>
        /// Создает папку для хранения временных файлов
        /// </summary>
        /// <returns></returns>
        private string CreateTemporaryFolder()
        {
            // Определение пути к папке сборки проекта
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            // Формирование полного пути к создаваемой папке
            string tempFolderPath = Path.Combine(baseDir, "Temporary word files");

            // Проверка существования папки и создание, если её ещё нет
            if (!Directory.Exists(tempFolderPath))
            {
                Directory.CreateDirectory(tempFolderPath);
            }
            return tempFolderPath;
        }

        /// <summary>
        /// Очистка папки с временными файлами
        /// </summary>
        private void ClearTemporaryFolder()
        {
            string[] filePaths = Directory.GetFiles(_temporaryFolder);
            foreach (string filePath in filePaths)
            {
                File.Delete(filePath);
            }
        }

        /// <summary>
        /// Разделяет шаблон на временные файлы по установленным закладкам
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public List<TextBlock> DivideByBookmarks()
        {
            var textBlocks = new List<TextBlock>();
            using (WordprocessingDocument doc = WordprocessingDocument.Open(TemplateFilePath, false))
            {
                var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");

                // Удаление системных закладок, которые начинаются с _
                var bookmarks = mainPart.Document.Descendants<BookmarkStart>().ToList();
                foreach (var bookmarkStart in bookmarks)
                {
                    if (bookmarkStart.Name?.ToString().StartsWith("_") == true)
                    {
                        var bookmarkEnd = mainPart.Document.Descendants<BookmarkEnd>()
                            .FirstOrDefault(be => be.Id == bookmarkStart.Id);
                        bookmarkStart.Remove();
                        bookmarkEnd?.Remove();
                    }
                }

                var bookmarkParagraphs = mainPart.Document
                    .Descendants<BookmarkStart>()
                    .Select(x => x.Parent)
                    .OfType<Paragraph>()
                    .ToArray();

                if (!bookmarkParagraphs.Any())
                {
                    ProcessAndAddTextBlock(doc, mainPart.Document.Elements().ToList(), textBlocks, "");
                    return textBlocks;
                }

                // 1. Содержимое до первой закладки
                var elementsBeforeFirstBookmark = bookmarkParagraphs[0]
                    .ElementsBefore()
                    .Where(IsValidElement)
                    .ToList();
                ProcessAndAddTextBlock(doc, elementsBeforeFirstBookmark, textBlocks, GetBookmarkName(bookmarkParagraphs[0]));

                // 2. Содержимое между закладками
                for (int i = 0; i + 1 < bookmarkParagraphs.Length; i++)
                {
                    var elementsBetweenBookmarks = bookmarkParagraphs[i]
                        .ElementsAfter()
                        .TakeWhile(e => e != bookmarkParagraphs[i + 1])
                        .Where(IsValidElement)
                        .ToList();

                    var bookmarkName = GetBookmarkName(bookmarkParagraphs[i]);

                    if (bookmarkParagraphs[i].InnerText.ToLower().Contains("таблица"))
                    {
                        ProcessAndAddTextBlock(doc, new List<OpenXmlElement> { bookmarkParagraphs[i] }, textBlocks, bookmarkName);
                        bookmarkName = bookmarkName + " промежуточная";
                    }
                    else
                        elementsBetweenBookmarks.Insert(0, bookmarkParagraphs[i]);

                    ProcessAndAddTextBlock(doc, elementsBetweenBookmarks, textBlocks, bookmarkName);
                }

                // 3. Содержимое после последней закладки
                var elementsAfterLastBookmark = bookmarkParagraphs.Last()
                    .ElementsAfter()
                    .Where(IsValidElement)
                    .ToList();
                elementsAfterLastBookmark.Insert(0, bookmarkParagraphs.Last());
                ProcessAndAddTextBlock(doc, elementsAfterLastBookmark, textBlocks, GetBookmarkName(bookmarkParagraphs.Last()));
            }
            return textBlocks;
        }

        private static readonly Func<OpenXmlElement, bool> IsValidElement = x => x != null && (x is Paragraph || x is Table || x is Picture);

        private string GetBookmarkName(Paragraph paragraph)
        {
            return paragraph.Descendants<BookmarkStart>().FirstOrDefault()?.Name?.Value ?? "";
        }

        private void ProcessAndAddTextBlock(WordprocessingDocument doc, List<OpenXmlElement> elements, List<TextBlock> textBlocks, string bookmarkName)
        {
            var res = GetElementsWithoutEditingNote(elements);
            string displayName = res.Elements.FirstOrDefault()?.InnerText ?? "";
            string path = Path.Combine(_temporaryFolder, $"{textBlocks.Count}.docx");
            CreateFileOnBookmark(doc, res.Elements, path);
            textBlocks.Add(new TextBlock(path, res.NumberOfEditing, bookmarkName, displayName));
        }

        /// <summary>
        /// Создает файл на основе документа и списка необходимых элементов
        /// </summary>
        /// <param name="sourceDoc"></param>
        /// <param name="elements"></param>
        /// <param name="path"></param>
        private void CreateFileOnBookmark(WordprocessingDocument sourceDoc, List<OpenXmlElement> elements, string path)
        {
            using (WordprocessingDocument newDoc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPartNewDoc = newDoc.AddMainDocumentPart();
                mainPartNewDoc.Document = new Document();
                mainPartNewDoc.Document.Body = new Body();

                // Перенос всех частей со стилями и настройками
                if (sourceDoc.ExtendedFilePropertiesPart != null)
                    newDoc.AddPart(sourceDoc.ExtendedFilePropertiesPart);
                if (sourceDoc.CoreFilePropertiesPart != null)
                    newDoc.AddPart(sourceDoc.CoreFilePropertiesPart);
                if (sourceDoc.MainDocumentPart?.DocumentSettingsPart != null)
                    mainPartNewDoc.AddPart(sourceDoc.MainDocumentPart.DocumentSettingsPart);
                if (sourceDoc.MainDocumentPart?.StyleDefinitionsPart != null)
                    mainPartNewDoc.AddPart(sourceDoc.MainDocumentPart.StyleDefinitionsPart);
                if (sourceDoc.MainDocumentPart?.ThemePart != null)
                    mainPartNewDoc.AddPart(sourceDoc.MainDocumentPart.ThemePart);
                if (sourceDoc.MainDocumentPart?.FontTablePart != null)
                    mainPartNewDoc.AddPart(sourceDoc.MainDocumentPart.FontTablePart);
                if (sourceDoc.MainDocumentPart?.WebSettingsPart != null)
                    mainPartNewDoc.AddPart(sourceDoc.MainDocumentPart.WebSettingsPart);
                if (sourceDoc.MainDocumentPart?.CustomXmlParts?.FirstOrDefault() != null)
                    mainPartNewDoc.AddPart(sourceDoc.MainDocumentPart.CustomXmlParts.First());

                // Перенос всех элементов внутри Body
                foreach (var elem in elements)
                {
                    var clonedElement = elem.CloneNode(true);
                    mainPartNewDoc.Document.Body.Append(clonedElement);
                }
                mainPartNewDoc.Document.Save();
            }
        }

        /// <summary>
        /// Находит номер текущего изменения блока, сохраняет его и удаляет строку с пометкой из ворда
        /// </summary>
        /// <param name="elementsBetweenBookmarks"></param>
        /// <returns></returns>
        private (List<OpenXmlElement> Elements, int NumberOfEditing) GetElementsWithoutEditingNote(List<OpenXmlElement> elementsBetweenBookmarks)
        {
            int numberOfEditing = 0;
            List<OpenXmlElement> elements = new List<OpenXmlElement>();
            foreach (var el in elementsBetweenBookmarks)
            {
                // Если нашли пометку о редактировании у раздела
                if (el is Paragraph p && p.InnerText.Contains("Измененная редакция") && !p.InnerText.Contains("Таблица"))
                {
                    // Находим все числа в строке
                    var numbers = Regex.Matches(p.InnerText, @"\d+")
                        .Cast<Match>()
                        .Select(m => m.Value)
                        .ToList();

                    // Номер изменения
                    string lastNumber = numbers.LastOrDefault();
                    if (lastNumber != null)
                    {
                        numberOfEditing = int.Parse(lastNumber);
                    }
                }
                else if (el is Paragraph p1 && p1.InnerText.Contains("Измененная редакция") && p1.InnerText.Contains("Таблица"))
                {
                    // Игнорируем изменения таблиц
                }
                else
                {
                    elements.Add(el);
                }
            }
            return (elements, numberOfEditing);
        }
    }
}
