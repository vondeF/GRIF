using DocumentFormat.OpenXml.Packaging;
using GRIF.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using OpenXmlWP = DocumentFormat.OpenXml.Wordprocessing;
using System.IO;

namespace GRIF.ViewModels
{
    public class ViewModel_UserControl_TextBlock : INotifyPropertyChanged
    {
        private readonly TextBlock _textBlock;
        private readonly int _initialNumberOfEditing;

        public string DisplayName => _textBlock.DisplayName;
        public int NumberOfEditing
        {
            get => _textBlock.NumberOfEditing;
            set
            {
                _textBlock.NumberOfEditing = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsModified));
            }
        }

        public bool IsModified => NumberOfEditing != _initialNumberOfEditing;

        private FlowDocument _flowDocument = new FlowDocument();
        public FlowDocument FlowDocument
        {
            get => _flowDocument;
            set
            {
                _flowDocument = value;
                OnPropertyChanged();
            }
        }

        private bool _isEdited = false;
        public bool IsEdited
        {
            get => _isEdited;
            set
            {
                _isEdited = value;
                OnPropertyChanged();
            }
        }

        public AsyncRelayCommand EditCommand { get; }
        public RelayCommand RestoreCommand { get; }

        public ViewModel_UserControl_TextBlock(TextBlock textBlock)
        {
            _textBlock = textBlock;
            _initialNumberOfEditing = textBlock.NumberOfEditing;

            EditCommand = new AsyncRelayCommand(EditAsync);
            RestoreCommand = new RelayCommand(Restore);

            _flowDocument = CreateFlowDocument(_textBlock.FilePath);
        }

        private async Task EditAsync(object? parameter)
        {
            Process? wordProcess = Process.Start(new ProcessStartInfo
            {
                FileName = _textBlock.FilePath,
                UseShellExecute = true,
            });
            await Task.Delay(1000); // задержка, чтобы ОС открыла файл ворд

            if (wordProcess != null)
            {
                await WaitForFileRelease(_textBlock.FilePath);
                FlowDocument = CreateFlowDocument(_textBlock.FilePath);
                IsEdited = _textBlock.IsEdited();
            }
        }

        private async Task WaitForFileRelease(string filePath)
        {
            while (true)
            {
                try
                {
                    // Пробуем открыть файл (т.е. проверяем, что он не занят)
                    using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, true))
                    {
                        // Если удалось — файл свободен
                        break;
                    }
                }
                catch (IOException)
                {
                    //  Файл занят — ждём и пробуем снова
                    await Task.Delay(100);
                }
            }
        }

        private void Restore(object? parameter)
        {
            _textBlock.RestoreOriginalVersion();
            FlowDocument = CreateFlowDocument(_textBlock.FilePath);
            IsEdited = _textBlock.IsEdited();
        }

        private FlowDocument CreateFlowDocument(string path)
        {
            var flowDocument = new FlowDocument();
            using (WordprocessingDocument doc = WordprocessingDocument.Open(path, false))
            {
                MainDocumentPart mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                var body = mainPart.Document.Body;
                var elements = body.Descendants();

                // Для случаев, когда идут несколько разделителей строк подряд
                // Если один переход на новую строку, то мы его игнорируем, тк между параграфами итак ставится перевод строки
                // На каждые два перехода строки надо делать один параграф (вообще получается избыточно, тк получается автоматически переход+параграф+переход, но пока забьем)
                int number_of_consecutive_empty_paragraph = 0;
                foreach (var elem in elements)
                {
                    // Обработка параграфа
                    if (elem is OpenXmlWP.Paragraph p)
                    {
                        // Если параграф не пустой (тк при отображении WPF делать разделения между параграфами итак)
                        if (p.Descendants<OpenXmlWP.Run>().Any(x => x.InnerText.Replace(" ", "") != ""))
                        {
                            number_of_consecutive_empty_paragraph = 0;
                            var winParagraph = new Paragraph();
                            ApplyParagraphFormatting(winParagraph, p);

                            foreach (var el in p.Descendants<OpenXmlWP.Run>())
                            {
                                if (el.Ancestors<OpenXmlWP.DeletedRun>().Any())
                                    continue; // пропускаем, если внутри DeletedRun
                                ProcessRunElement(el, winParagraph);
                            }
                            flowDocument.Blocks.Add(winParagraph);
                        }
                        else
                        {
                            // Если встречаем второй подряд пустой абзац
                            if (number_of_consecutive_empty_paragraph > 0)
                            {
                                flowDocument.Blocks.Add(new Paragraph());
                                number_of_consecutive_empty_paragraph = 0;
                            }
                            number_of_consecutive_empty_paragraph++;
                        }
                    }
                    // Обработка таблицы
                    else if (elem is OpenXmlWP.Table)
                    {
                        //TODO: добавить обработчик таблиц
                    }
                }
            }
            return flowDocument;
        }

        /// <summary>
        /// Обработка Run
        /// </summary>
        /// <param name="run"></param>
        /// <param name="winParagraph"></param>
        private static void ProcessRunElement(OpenXmlWP.Run run, Paragraph winParagraph)
        {
            // Создаем Run для каждого текстового фрагмента с его форматированием
            if (run.InnerText != null)
            {
                Run winRun = new Run(run.InnerText);
                winParagraph.Inlines.Add(winRun);
            }
        }

        /// <summary>
        /// Перенос форматирования параграфа
        /// </summary>
        /// <param name="winParagraph"></param>
        /// <param name="wordParagraph"></param>
        private static void ApplyParagraphFormatting(Paragraph winParagraph, OpenXmlWP.Paragraph wordParagraph)
        {
            var paragraphProperties = wordParagraph.ParagraphProperties;
            if (paragraphProperties == null) return;

            // Выравнивание
            if (paragraphProperties.Justification != null)
            {
                var justificationValue = paragraphProperties.Justification.Val.Value;

                if (justificationValue == OpenXmlWP.JustificationValues.Center) { winParagraph.TextAlignment = TextAlignment.Center; }
                else if (justificationValue == OpenXmlWP.JustificationValues.Right) { winParagraph.TextAlignment = TextAlignment.Right; }
                else if (justificationValue == OpenXmlWP.JustificationValues.Both) { winParagraph.TextAlignment = TextAlignment.Justify; }
                else if (justificationValue == OpenXmlWP.JustificationValues.Left) { winParagraph.TextAlignment = TextAlignment.Left; }
                else { winParagraph.TextAlignment = TextAlignment.Left; }
            }

            // Отступы
            if (paragraphProperties.Indentation != null)
            {
                if (paragraphProperties.Indentation.Left != null)
                {
                    if (int.TryParse(paragraphProperties.Indentation.Left, out int leftIndent))
                    {
                        winParagraph.Margin = new Thickness(leftIndent / 20.0, 0, 0, 0); // 20 twips = 1 point
                    }
                }
            }

            // Межстрочный интервал
            if (paragraphProperties.SpacingBetweenLines != null)
            {
                if (paragraphProperties.SpacingBetweenLines.Line != null)
                {
                    if (int.TryParse(paragraphProperties.SpacingBetweenLines.Line, out int lineSpacing))
                    {
                        // Приблизительное преобразование
                        double spacing = lineSpacing / 240.0; // 240 twips = 1 line
                        winParagraph.LineHeight = spacing * 12; // Примерное значение
                    }
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
