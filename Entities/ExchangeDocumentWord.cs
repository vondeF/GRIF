using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using System.Windows;
using GRIF.Entities;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace GRIF.Entities
{
    public class ExchangeDocumentWord : ModelDocumentWord, IDocument
    {
        private readonly string _dirFull = "Текстовые версии Профилей обмена";
        private readonly string _dirDiff = "Изменения текстовых версий Профилей обмена";

        public ExchangeDocumentWord(string name): base(name) { }

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
            using (WordprocessingDocument newDoc = WordprocessingDocument.Open(path, true))
            {
                var bodyNewDoc = newDoc.MainDocumentPart!.Document.Body;
                var header = new Paragraph(
                                 new ParagraphProperties (new Justification { Val = JustificationValues.Center }),
                                 new Run(
                                    new RunProperties(new Bold()),
                                    new Text($"{Name}")
                                    ));
                bodyNewDoc!.Append(header);
            }
            
            foreach (var part in Parts)
            {
                part.WriteAllExchange(path);
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
                                    new Text($"Изменение {Name}")
                                    ));
                bodyNewDoc!.Append(header);
            }

            foreach (var part in Parts)
            {
                part.WriteDiff(path);
            }

            MessageBox.Show($"На основе текущей рабочей области создан набор изменений:\n{path}");
        }

    }
}
