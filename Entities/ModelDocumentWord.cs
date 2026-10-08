using GRIF.Entities.BasedOnConfig;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System.DirectoryServices;
using GRIF.Services;

namespace GRIF.Entities
{
    public interface IDocument
    {
        string Name { get; }
        int NumberOfEditing { get; set; }
        public List<DocumentPart> Parts { get; set; }
        void WriteDiff();
        void WriteAll();
        void CreateNewTemplate(string path);
    }

    public class ModelDocumentWord : IDocument
    {
        // ===== Для EntityFramework =====
        public int Id { get; set; }
        // ===============================

        public string Name { get; set; } = String.Empty;
        public int NumberOfEditing { get; set; } = 0;
        public List<DocumentPart> Parts { get; set; } = new();
        private readonly string _dirFull = "Текстовые версии Профилей модели";
        private readonly string _dirDiff = "Изменения текстовых версий Профилей модели";

        public ModelDocumentWord(string name)
        {
            Name = name;
        }

        public ModelDocumentWord()
        {

        }

        public void WriteAll()
        {
            // Создание папки, если нет
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dir = Path.Combine(baseDir, _dirFull);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string dateTime = DateTime.Now.ToString("dd.MM.yyyy HH.mm");
            string fileName = $"Версия {Name} {dateTime}.docx";
            string path = Path.Combine(dir, fileName);

            CreateEmptyWord(path);
            foreach (var part in Parts)
            {
                part.WriteAll(path);
            }

            MessageBox.Show($"На основе текущей рабочей области создан файл:\n{path}");
        }

        public void WriteDiff()
        {
            // Создание папки, если нет
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string dir = Path.Combine(baseDir, _dirDiff);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string dateTime = DateTime.Now.ToString("dd.MM.yyyy HH.mm");
            string fileName = $"Изменения к {Name} {dateTime}.docx";
            string path = Path.Combine(dir, fileName);

            CreateEmptyWord(path);
            using (WordprocessingDocument newDoc = WordprocessingDocument.Open(path, true))
            {
                var bodyNewDoc = newDoc.MainDocumentPart!.Document.Body;
                var header = new Paragraph(
                                 new Run(
                                    new RunProperties(new Bold()),
                                    new Text($"Изменение {Name}"),
                                    new Break(),
                                    new Text("Утверждено и введено в действие Приказом Федерального агентства по техническому регулированию и метрологии от")
                                    ));
                var header2 = new Paragraph(
                              new ParagraphProperties(new Justification() { Val = JustificationValues.Right }),
                              new Run(
                                new RunProperties(new Bold()),
                                new Text($"Дата введения__________")));
                bodyNewDoc!.Append(header);
                bodyNewDoc!.Append(header2);
            }
                
            foreach (var part in Parts)
            {
                part.WriteDiff(path);
            }

            MessageBox.Show($"На основе текущей рабочей области создан набор изменений:\n{path}");
        }


        public void CreateEmptyWord(string path)
        {
            // Создание пустого документа и перенос всех настроек
            using (WordprocessingDocument newDoc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPartNewDoc = newDoc.AddMainDocumentPart();
                mainPartNewDoc.Document = new Document();
                mainPartNewDoc.Document.Body = new Body();
                Body bodyNewDoc = mainPartNewDoc.Document.Body;

                var firstTextBlock = Parts.FirstOrDefault(x => x.Blocks.Any(elem => elem is TextBlock))?.Blocks.OfType<TextBlock>().First();
                if (firstTextBlock == null)
                    throw new NullReferenceException();

                using (WordprocessingDocument currentFile = WordprocessingDocument.Open(firstTextBlock.FilePath, false))
                {
                    // Перенос всех частей со стилями и настройками
                    if (currentFile.ExtendedFilePropertiesPart != null)
                        newDoc.AddPart(currentFile.ExtendedFilePropertiesPart);
                    if (currentFile.CoreFilePropertiesPart != null)
                        newDoc.AddPart(currentFile.CoreFilePropertiesPart);
                    if (currentFile.MainDocumentPart?.DocumentSettingsPart != null)
                        mainPartNewDoc.AddPart(currentFile.MainDocumentPart.DocumentSettingsPart);
                    if (currentFile.MainDocumentPart?.StyleDefinitionsPart != null)
                        mainPartNewDoc.AddPart(currentFile.MainDocumentPart.StyleDefinitionsPart);
                    if (currentFile.MainDocumentPart?.ThemePart != null)
                        mainPartNewDoc.AddPart(currentFile.MainDocumentPart.ThemePart);
                    if (currentFile.MainDocumentPart?.FontTablePart != null)
                        mainPartNewDoc.AddPart(currentFile.MainDocumentPart.FontTablePart);
                    if (currentFile.MainDocumentPart?.WebSettingsPart != null)
                        mainPartNewDoc.AddPart(currentFile.MainDocumentPart.WebSettingsPart);
                    if (currentFile.MainDocumentPart?.CustomXmlParts?.FirstOrDefault() != null)
                        mainPartNewDoc.AddPart(currentFile.MainDocumentPart.CustomXmlParts.First());
                }
            }
        }

        public void CreateNewTemplate(string path)
        {
            CreateEmptyWord(path);
            foreach (var part in Parts)
            {
                part.WriteTemplate(path);
            }
        }
    }
}
