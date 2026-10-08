using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GRIF.Entities;
using Microsoft.Win32;

namespace GRIF.Services
{
    public class TemplateService
    {
        private readonly string _folderPath;
        private readonly string[] _files;
        private readonly ITemplateValidator _validator;

        public TemplateService(string folderName, string documentName, ITemplateValidator validator)
        {
            string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName, documentName);

            if (Directory.Exists(folderPath))
            {
                this._folderPath = folderPath;
                this._files = Directory.GetFiles(this._folderPath, "*.docx", SearchOption.TopDirectoryOnly);
                if (_files.Length == 0)
                    throw new Exception($"В папке шаблона {folderPath} нет ни одного шаблона");
            }
            else
                throw new Exception($"Папки шаблона {folderPath} не существует");

            _validator = validator;
        }

        public (Template Template, ValidationResult ValidationResult) GetNewestTemplate()
        {
            FileInfo newestFile = _files.Select(f => new FileInfo(f)).OrderByDescending(f => f.LastWriteTime).First();
            var resValidation = Validate(newestFile.FullName);
            var template = new Template(this._folderPath, newestFile.FullName);
            return (template, resValidation);
        }

        public (Template Template, ValidationResult ValidationResult) ChooseTemplate()
        {
            var dialog = new OpenFileDialog();
            dialog.Filter = "Word documents |*.doc;*.docx";
            dialog.InitialDirectory = this._folderPath;
            dialog.Multiselect = false;
            dialog.RestoreDirectory = true;

            if (dialog.ShowDialog() == true)
            {
                if (!string.IsNullOrEmpty(dialog.FileName))
                {
                    string selectedDir = System.IO.Path.GetDirectoryName(dialog.FileName) ?? "";

                    // Запрещаем загружать файлы из любой другой папки, кроме папки с шаблонами
                    if (selectedDir != this._folderPath)
                        throw new Exception($"Нельзя загрузить выбранный файл. Выберите файл из папки:\n{this._folderPath}");
                }
            }

            var resValidation = Validate(dialog.FileName);
            var template = new Template(this._folderPath, dialog.FileName);
            return  (template, resValidation);
        }

        public string GetFolderPath()
        {
            return this._folderPath;
        }

        private ValidationResult Validate(string fileName)
        {
            var result = _validator.Validate(fileName);
            return result;
        }
    }
}
