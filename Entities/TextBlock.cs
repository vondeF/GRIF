using System;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.Net.WebSockets;

namespace GRIF.Entities
{
    /// <summary>
    /// Блок текста
    /// </summary>
    public class TextBlock : DocumentBlock
    {
        private int _numberOfEditing;
        public int NumberOfEditing
        {
            get { return _numberOfEditing; } 
            set
            {
                _numberOfEditing = value;
                NumberOfEditingBackup = value;
            }
        }
        private int NumberOfEditingBackup { get; set; }
        public string FilePath { get; set; }
        private string FileBackupPath { get; set; }
        public string BookmarkName { get; set; }
        
        public TextBlock(string path, int numberOfEditing, string bookmarkName, string displayName)
        {
            FilePath = path;
            TurnOnDisplayOfChanges();
            FileBackupPath = CreateBackupFile();
            _numberOfEditing = numberOfEditing;
            BookmarkName = bookmarkName;
            DisplayName = displayName;
        }

        public TextBlock()
        {

        }

        private string CreateBackupFile()
        {
            string newFilePath = FilePath.Replace(".docx", "_backup.docx");
            File.Copy(FilePath, newFilePath, true);
            return newFilePath;
        }

        private void TurnOnDisplayOfChanges()
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(FilePath, true))
            {
                var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                var settingsPart = mainPart.DocumentSettingsPart;
                if (settingsPart == null)
                {
                    settingsPart = mainPart.AddNewPart<DocumentSettingsPart>();
                    settingsPart.Settings = new DocumentFormat.OpenXml.Wordprocessing.Settings();
                }
                var settings = settingsPart.Settings;

                // Включаем трек изменений
                var trackRevisions = new DocumentFormat.OpenXml.Wordprocessing.TrackRevisions();
                settings.PrependChild(trackRevisions);
                settingsPart.Settings.Save();
            }
        }

        public void TurnOffDisplayOfChanges()
        {
            using (var document = WordprocessingDocument.Open(FilePath, true))
            {
                var mainPart = document.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                var body = mainPart.Document.Body ?? throw new InvalidOperationException("Body отсутствует в документе.");
                RemoveRevisions(body);

                var settingsPart = document.MainDocumentPart?.GetPartsOfType<DocumentSettingsPart>()?.FirstOrDefault();
                if (settingsPart != null && settingsPart.Settings != null)
                {
                    var trackRevisionsElement = settingsPart.Settings.Elements<TrackRevisions>().FirstOrDefault();
                    if (trackRevisionsElement != null)
                        trackRevisionsElement.Remove(); // Убираем элемент TrackRevisions

                    settingsPart.Settings.Save(); // Сохраняем обновленные настройки
                }
            }
        }

        private void RemoveRevisions(OpenXmlElement element)
        {
            // Обрабатываем каждый узел и удаляем элементы, ассоциированные с изменениями
            List<OpenXmlElement> elementsToRemove = new List<OpenXmlElement>();

            foreach (var child in element.ChildElements.ToList())
            {
                switch (child.LocalName)
                {
                    case "del":       // Элементы удаления
                    case "ins":       // Элементы вставки
                    case "moveFrom":  // Начало перемещения
                    case "moveTo":    // Конец перемещения
                    case "comment":   // Примечания
                    case "proofErr":  // Ошибки проверки орфографии
                    case "customXml": // Расширения XML
                        elementsToRemove.Add(child);
                        break;
                    default:
                        // Рекурсивно обрабатываем вложенные элементы
                        RemoveRevisions(child);
                        break;
                }
            }

            // Удаляем найденные элементы ревизий
            foreach (var el in elementsToRemove)
            {
                el.Remove();
            }
        }

        public void RestoreOriginalVersion()
        {
            File.Copy(FileBackupPath, FilePath, true);
            NumberOfEditing = NumberOfEditingBackup;
        }

        /// <summary>
        /// Проверяет, изменен ли документ
        /// </summary>
        /// <returns></returns>
        public bool IsEdited()
        {
            using (WordprocessingDocument docMain = WordprocessingDocument.Open(FilePath, true))
            {
                var elementsMain = docMain?.MainDocumentPart?.Document?.Body?.Descendants().ToList();
                using (WordprocessingDocument docBackup = WordprocessingDocument.Open(FileBackupPath, true))
                {
                    var elementsBackup = docBackup?.MainDocumentPart?.Document?.Body?.Descendants().ToList();

                    //если добавился какой-то элемент
                    if (elementsMain?.Count() != elementsBackup?.Count())
                        return true;
                    else
                    {
                        for (int i = 0; i < elementsMain?.Count(); i++)
                        {
                            //если какой-то элемент изменился
                            if (elementsMain[i].InnerXml != elementsBackup[i].InnerXml)
                                return true;
                        }
                    }
                }
            }
            return false;
        }


        public override void WriteAll(string path)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(path, true))
            {
                var mainPartDoc = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                Body bodyDoc = mainPartDoc.Document.Body ?? throw new InvalidOperationException("Body отсутствует в документе.");

                using (WordprocessingDocument currentFile = WordprocessingDocument.Open(FilePath, false))
                {
                    Body bodyCurrentFile = currentFile.MainDocumentPart?.Document.Body ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе."); ;
                    var elementsOfCurrentFile = bodyCurrentFile.Descendants();

                    // Копируем все элементы
                    foreach (var elem in elementsOfCurrentFile.Where(x => x is Paragraph && !(x.Parent is TableCell) || x is Table || x is Picture))
                    {
                        var clonedElement = elem.CloneNode(true);
                        bodyDoc.Append(clonedElement);
                    }
                    mainPartDoc.Document.Save();
                }
            }
            //TurnOffDisplayOfChanges(path);
            AcceptRevisionsAutomaticallyAndSave(path);
        }

        public void TurnOffDisplayOfChanges(string path)
        {
            using (var document = WordprocessingDocument.Open(path, true))
            {
                var mainPartDoc = document.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                Body bodyDoc = mainPartDoc.Document.Body ?? throw new InvalidOperationException("Body отсутствует в документе.");

                AcceptRevisionsManually(mainPartDoc.Document.Body);

                // Убираем настройку TrackRevisions
                var settingsPart = mainPartDoc.GetPartsOfType<DocumentSettingsPart>()?.FirstOrDefault();
                if (settingsPart != null && settingsPart.Settings != null)
                {
                    var trackRevisionsElement = settingsPart.Settings.Elements<TrackRevisions>().FirstOrDefault();
                    if (trackRevisionsElement != null)
                    {
                        trackRevisionsElement.Remove();
                    }
                    settingsPart.Settings.Save();
                }

                document.Save();
            }
        }

        private void AcceptRevisionsAutomaticallyAndSave(string path)
        {
            using (var document = WordprocessingDocument.Open(path, true))
            {
                OpenXmlPowerTools.RevisionAccepter.AcceptRevisions(document);
            }
        }

        private void AcceptRevisionsManually(OpenXmlElement element)
        {
            var children = element.ChildElements.ToList();

            foreach (var child in children)
            {
                switch (child.LocalName)
                {
                    case "del":
                        // Удаляем элемент, помеченный как удалённый
                        child.Remove();
                        break;

                    case "ins":
                        // Оставляем содержимое вставленного элемента
                        var parent = child.Parent;
                        if (parent != null)
                        {
                            foreach (var innerChild in child.ChildElements.ToList())
                            {
                                innerChild.Remove();
                                parent.InsertBefore(innerChild, child);
                            }
                            child.Remove();
                        }
                        break;

                    case "moveFrom":      // Удаление перемещённого текста
                        child.Remove();
                        break;

                    case "moveTo":        // Оставляем содержимое перемещённого текста
                        parent = child.Parent;
                        if (parent != null)
                        {
                            foreach (var innerChild in child.ChildElements.ToList())
                            {
                                innerChild.Remove();
                                parent.InsertBefore(innerChild, child);
                            }
                            child.Remove();
                        }
                        break;

                    case "comment":
                    case "proofErr":
                    case "customXml":
                        // Просто удаляем эти элементы, они не влияют на содержимое
                        child.Remove();
                        break;

                    default:
                        // Рекурсивно обрабатываем вложенные элементы
                        AcceptRevisionsManually(child);
                        break;
                }
            }
        }

        public override void WriteDiff(string path)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(path, true))
            {
                var mainPartDoc = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                Body bodyDoc = mainPartDoc.Document.Body ?? throw new InvalidOperationException("Body отсутствует в документе.");

                using (WordprocessingDocument currentFile = WordprocessingDocument.Open(FilePath, false))
                {
                    Body bodyCurrentFile = currentFile.MainDocumentPart?.Document.Body ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе."); ;
                    var paragraphs = bodyCurrentFile.Elements<Paragraph>();

                    bool isChanged = false;
                    foreach (var p in paragraphs)
                    {
                        // Если в разделе есть изменения текста
                        if (!isChanged)
                            isChanged = HasRevisions(p);

                        if (isChanged)
                            break;
                    }

                    if (isChanged)
                    {
                        bodyDoc.Append(new Paragraph(new Run(new RunProperties(new Bold()), new Text($"Раздел \"{DisplayName}\" изложить в следующей редакции"))));
                        var elementsOfCurrentFile = bodyCurrentFile.Descendants();

                        // Копируем все элементы
                        foreach (var elem in elementsOfCurrentFile.Where(x => x is Paragraph && !(x.Parent is TableCell) || x is Table || x is Picture))
                        {
                            var clonedElement = elem.CloneNode(true);
                            bodyDoc.Append(clonedElement);
                        }
                    }
                    mainPartDoc.Document.Save();
                }
            }
        }


        /// <summary>
        /// Проверка, есть ли изменения в параграфе
        /// </summary>
        /// <param name="paragraph"></param>
        /// <returns></returns>
        public bool HasRevisions(Paragraph paragraph)
        {
            foreach (var child in paragraph.Descendants())
            {
                switch (child.LocalName)
                {
                    case "del":      // Элементы удаления
                    case "ins":      // Элементы вставки
                    case "moveFrom": // Начало перемещения
                    case "moveTo":   // Конец перемещения
                    case "comment":  // Примечания
                                     //case "proofErr": // Ошибки проверки орфографии
                                     //case "customXml":// Расширения XML
                        return true;
                }
            }
            return false;
        }
    }
}
