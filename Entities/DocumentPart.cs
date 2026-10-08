using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Entities
{
    public interface IWritableBlock
    {
        int NumberOfEditing { get; set; }
        string DisplayName { get; set; }
        void WriteAll(string path);
        void WriteDiff(string path);
    }


    /// <summary>
    /// Логическая часть документа, содержащая в себе как текст, так и такблицы с данными
    /// </summary>
    public class DocumentPart : IWritableBlock
    {
        // ===== Для EntityFramework =====
        public int Id { get; set; }
        public int ModelDocumentWordId { get; set; }
        [ForeignKey(nameof(ModelDocumentWordId))]
        public ModelDocumentWord? ModelDocumentWord { get; set; }
        // ===============================

        public int NumberOfEditing { get; set; } = 0;
        public string DisplayName { get; set; } = "";
        public List<DocumentBlock> Blocks { get; set; } = new();

        public DocumentPart()
        {

        }

        public void WriteAll(string path)
        {
            foreach (var block in Blocks)
            {
                block.WriteAll(path);
            }
        }

        public void WriteAllExchange (string path)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(path, true))
            {
                var mainPartDoc = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                Body bodyDoc = mainPartDoc.Document.Body ?? throw new InvalidOperationException("Body отсутствует в документе.");

                // Заголовок
                string text = this.DisplayName;
                int dotIndex = text.IndexOf('.');
                Paragraph paragraph = new Paragraph(
                    new ParagraphProperties(
                        new Justification() { Val = JustificationValues.Both }
                    )
                );
                if (dotIndex != -1)
                {
                    var boldText = text.Substring(0, dotIndex + 1);
                    var normalText = text.Substring(dotIndex + 1);

                    paragraph.Append(
                        new Run(new Text(boldText)) { RunProperties = new RunProperties(new Bold()) },
                        new Run(new Text(normalText))
                    );
                }
                else
                    paragraph.Append(new Run(new Text(text)));

                bodyDoc.Append(paragraph);
                mainPartDoc.Document.Save();
            }

            foreach (var block in Blocks)
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(path, true))
                {
                    var mainPartDoc = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                    Body bodyDoc = mainPartDoc.Document.Body ?? throw new InvalidOperationException("Body отсутствует в документе.");
                    bodyDoc.Append(new Paragraph(new Run(new Text(block.DisplayName))));
                    mainPartDoc.Document.Save();
                }

                block.WriteAll(path);
            }
        }

        public void WriteDiff(string path)
        {
            foreach (var block in Blocks)
            {
                block.WriteDiff(path);
            }
        }

        public void WriteTemplate(string path)
        {
            foreach (TextBlock block in Blocks.Where(x => x is TextBlock))
            {
                block.WriteAll(path);
            }
        }
    }
}
