using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GRIF.Entities.BasedOnConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Services
{
    public interface ITemplateValidator
    {
        ValidationResult Validate(string fileName);
    }
    public class ValidationResult
    {
        public bool IsValidated { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
    }

    public class GostDocumentTemplateValidator : ITemplateValidator
    {
        private readonly ModelDocumentConfigData _configData;
        public GostDocumentTemplateValidator(ModelDocumentConfigData configData)
        {
            this._configData = configData;
        }

        public ValidationResult Validate(string fileName)
        {
            var res = new ValidationResult { IsValidated = true };

            var bookmarksConfig = _configData.Tables.Select(x => x.BookmarkName);
            string?[] bookmarksTemplate;

            using (WordprocessingDocument doc = WordprocessingDocument.Open(fileName, false))
            {
                var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");

                bookmarksTemplate = mainPart.Document
                    .Descendants<BookmarkStart>()
                    .Where(x => x.Name != null)
                    .Select(x => x.Name!.Value)
                    .ToArray();
            }

            var dif = bookmarksConfig.Except(bookmarksTemplate).ToArray();

            if (dif.Length == 0)
                return res;
            else
                res.IsValidated = false;

            foreach (var bookmark in dif)
            {
                res.Warnings.Add($"Закладка таблицы \"{bookmark}\" отсутсвует в шаблоне.");
            }

            return res;
        }
    }

    public class ExchangeDocumentTemplateValidator : ITemplateValidator
    {
        public ValidationResult Validate(string fileName)
        {
            return new ValidationResult { IsValidated = true };
        }
    }
}
