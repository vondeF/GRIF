using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.ExtendedProperties;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using GostObjectsClassLibrary;
using GostObjectsClassLibrary.GostDoc;
using GostObjectsClassLibrary.ProfileDoc;
using GRIF.Entities.BasedOnConfig;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Security.Cryptography;
using static GRIF.Entities.TableRow;
using System.ComponentModel;
using HtmlToOpenXml;
using AngleSharp.Css;
using System.Runtime.InteropServices;
using DocumentFormat.OpenXml.Drawing.Spreadsheet;

namespace GRIF.Entities
{
    /// <summary>
    /// Блок таблицы
    /// </summary>
    public class TableBlock : DocumentBlock
    {
        public string BookmarkName { get; private set; } = "";
       
        public List<string> ColumnHeaders { get; private set; }
        private List<int> indexOfBothJustificationColumns;
        public List<TableRow> Rows { get; set; } = new();
        private readonly string _dataType;

        // для профиля модели
        public TableBlock(TableConfig tableConfig)
        {
            BookmarkName = tableConfig.BookmarkName;
            DisplayName = tableConfig.DisplayName;
            _dataType = tableConfig.DataType;
            ColumnHeaders = TableModelDataTypes.Values.First(x => x.Name == _dataType).Headers;

            FillLiatOfJustificationColumns();
        }

        // для профиля обмена
        public TableBlock(string displayName, string dataType)
        {
            DisplayName = displayName;
            _dataType = dataType;
            ColumnHeaders = TableExchangeDataTypes.Values.First(x => x.Name == _dataType).Headers;

            FillLiatOfJustificationColumns();
        }

        void FillLiatOfJustificationColumns()
        {
            var bothJustificationColumns = ColumnHeaders.Select(x => x.ToLower()).Where(x => x.Contains("смысл") || x.Contains("определение") || x.Contains("ограничение") || x.Contains("описание")).ToList();
            indexOfBothJustificationColumns = new List<int>();
            foreach (var column in bothJustificationColumns)
            {
                int index = ColumnHeaders.FindIndex(x => x.ToLower() == column);
                if (index != -1)
                    indexOfBothJustificationColumns.Add(index);
            }
        }

        public TableBlock()
        {
        }

        /// <summary>
        /// Добавление строки
        /// </summary>
        /// <param name="row"></param>
        public void AddRow(TableRow row)
        {
            if (this._dataType == "Атрибуты справочных классов")
                row.Columns.RemoveAt(row.Columns.Count() - 1);

            this.Rows.Add(row);
        }

        public void AddRowDefault(TableRow row)
        {
            this.Rows.Add(row);
        }

        /// <summary>
        /// Сортировка строк по алфавиту
        /// </summary>
        public void Sort()
        {
            if (Rows.Count == 0)
                return;

            if (Rows.Any(x => x?.SourceType == ""))
                return;

            var firstRowType = Rows.First().SourceType;

            var sortedRows = new List<TableRow>();
            if (firstRowType == nameof(GostClass))
            {
                sortedRows = Rows.OrderBy(x => x.Columns[1].Replace("rf:", "")).ThenBy(x => x.Status).ToList();
            }
            else if (firstRowType == nameof(GostAttributeModel))
            {
                sortedRows = Rows.OrderBy(x => x.Columns[2].Replace("rf:", "")).ThenBy(x => x.Columns[1].Replace("rf:", "")).ThenBy(x => x.Status).ToList();
            }
            else if (firstRowType == nameof(GostAssociationModel))
            {
                sortedRows = Rows.OrderBy(x => x.Columns[1].Replace("rf:", "")).ThenBy(x => x.Columns[2].Replace("rf:", "")).ThenBy(x => x.Columns[3].Replace("rf:", "")).ThenBy(x => x.Status).ToList();
            }
            else if (firstRowType == nameof(ProfileAttribute))
            {
                sortedRows = Rows.OrderBy(x => x.AdditionalInfo.Split(";").First()).ThenBy(x => x.AdditionalInfo.Split(";").Last()).ThenBy(x => x.Columns[0].Split(":").Last()).ToList();
            }
            else if (firstRowType == nameof(ProfileAssociation))
            {
                sortedRows = Rows.OrderBy(x => x.AdditionalInfo.Split(";").First()).ThenBy(x => x.AdditionalInfo.Split(";").Last()).ThenBy(x => x.Columns[0].Split(":").Last()).ToList();
            }
            Rows.Clear();
            Rows.AddRange(sortedRows);
        }

        public override void WriteAll(string path)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(path, true))
            {
                var mainPartDoc = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                var converter = new HtmlConverter(mainPartDoc);
                Body bodyDoc = mainPartDoc.Document.Body ?? throw new InvalidOperationException("Body отсутствует в документе.");
                if(mainPartDoc.NumberingDefinitionsPart == null)
                    mainPartDoc.AddNewPart<NumberingDefinitionsPart>();
                if (mainPartDoc.NumberingDefinitionsPart.Numbering == null)
                {
                    // Инициализируем объект нумерации
                    mainPartDoc.NumberingDefinitionsPart.Numbering = new Numbering();
                    mainPartDoc.NumberingDefinitionsPart.Numbering.Save();
                }

                // Создаем таблицy
                Table table = CreateEmptyTable();

                // Строка Заголовков
                var headerRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                foreach (var elem in ColumnHeaders)
                {
                    headerRow.Append(
                        new TableCell(
                            new Paragraph(
                                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                                new Run(new Text(elem)))));
                }
                table.Append(headerRow);

                // Примечания
                var footnoteRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                TableCell footnoteCell = new TableCell();
                footnoteCell.TableCellProperties = new TableCellProperties(
                    new GridSpan { Val = ColumnHeaders.Count() });

                // Строки таблицы после заголовков
                var footNotesnumbers = new HashSet<int?>();
                foreach (var elem in Rows.Where(x => x.Status != RowStatus.Deleted && x.Status != RowStatus.EditedOld))
                {
                    var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                    for (int i = 0; i < elem.Columns.Count() ; i++)
                    {
                        string text = elem.Columns[i];
                        string footnoteLink = "";
                        if (elem.Footnote?.Column - 1 == i)
                        {
                            footnoteLink = $" {elem.Footnote!.Number})";

                            if (!footNotesnumbers.Contains(elem.Footnote.Number))
                            {
                                footnoteCell.Append(
                                    new Paragraph(
                                        new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                                        new Run(new Text($"{elem.Footnote.Number}) {elem.Footnote.Text}"))));
                            }

                            footNotesnumbers.Add(elem.Footnote.Number);
                        }

                        //надстрочное начертание для примечания
                        Run footnoteLinkRun = new Run(new Text(footnoteLink));
                        RunProperties runProperties = new RunProperties(
                            new VerticalTextAlignment { Val = VerticalPositionValues.Superscript }
                        );
                        footnoteLinkRun.RunProperties = runProperties;

                        // парсим html тэги
                        var cell = new TableCell();
                        var elements = converter.Parse(text);
                        elements.Last().Append(footnoteLinkRun);
                        foreach (var el in elements)
                        {
                            if (!indexOfBothJustificationColumns.Contains(i))
                                el.PrependChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
                            else
                                el.PrependChild(new ParagraphProperties(new Justification { Val = JustificationValues.Both }));
                            cell.Append(el);
                        }
                        row.Append(cell);
                        //if (text.Contains(">") && text.Contains("</"))
                        //{
                        //    var cell = new TableCell();
                        //    var elements = converter.Parse(text);
                        //    elements.Last().Append(footnoteLinkRun);
                        //    foreach (var el in elements)
                        //    {
                        //        if (!indexOfLeftJustificationColumns.Contains(i))
                        //            el.PrependChild(new ParagraphProperties(new Justification { Val = JustificationValues.Center }));
                        //        cell.Append(el);
                        //    }
                        //    row.Append(cell);
                        //}
                        //else
                        //{
                        //    row.Append(
                        //        new TableCell(
                        //            new Paragraph(
                        //                new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                        //                new Run(new Text(text)),
                        //                footnoteLinkRun)));
                        //}
                        //row.Append(
                        //    new TableCell(
                        //        new Paragraph(
                        //            new ParagraphProperties(new Justification { Val = JustificationValues.Center }),
                        //            new Run(new Text(text)),
                        //            footnoteLinkRun)));
                    }

                    if (ColumnHeaders.Count() > elem.Columns.Count())
                        MergeCells(row.Elements<TableCell>().Last());

                    table.Append(row);
                }

                if (footnoteCell.Descendants<Paragraph>().Any())
                {
                    SortParagraphsInTableCellByNumber(footnoteCell);
                    footnoteRow.Append(footnoteCell);
                    table.Append(footnoteRow);
                }

                // добавление таблицы в текст
                bodyDoc.Append(table);
                bodyDoc.Append(new Paragraph());
            }
        }

        public void SortParagraphsInTableCellByNumber(TableCell cell)
        {
            // Получаем все прямые дочерние параграфы ячейки
            var paragraphs = cell.Elements<Paragraph>().ToList();

            if (paragraphs.Count <= 1)
                return; // Нечего сортировать

            // Сортируем по числу в начале строки
            var sortedParagraphs = paragraphs
                .OrderBy(p => ExtractNumberFromParagraph(p))
                .ToList();

            // Удаляем старые параграфы
            foreach (var p in paragraphs)
            {
                p.Remove();
            }

            // Добавляем отсортированные параграфы обратно
            foreach (var p in sortedParagraphs)
            {
                cell.AppendChild(p);
            }
        }

        private int ExtractNumberFromParagraph(Paragraph p)
        {
            var text = p.InnerText?.Trim();

            if (string.IsNullOrEmpty(text))
                return 0;

            var match = Regex.Match(text, @"^(\d+)");

            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }

            return 0; // Если не найдено число — считаем как 0
        }


        private Table CreateEmptyTable()
        {
            Table table = new Table();
            TableProperties tableProps = new TableProperties(
                new TableWidth { Width = "100%", Type = TableWidthUnitValues.Pct },
                new TableJustification { Val = TableRowAlignmentValues.Center },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new BottomBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new LeftBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new RightBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4, Color = "000000" }
                )
            );
            table.Append(tableProps);

            return table;
        }

        public override void WriteDiff(string path)
        {
            using (WordprocessingDocument doc = WordprocessingDocument.Open(path, true))
            {
                var mainPartDoc = doc.MainDocumentPart ?? throw new InvalidOperationException("MainDocumentPart отсутствует в документе.");
                Body bodyDoc = mainPartDoc.Document.Body ?? throw new InvalidOperationException("Body отсутствует в документе.");

                // Если есть добавленные строки, то надо под них сделать таблицу
                if (Rows.Count(x => x.Status == RowStatus.Added) > 0)
                {
                    bodyDoc.Append(new Paragraph(new Run(new Text($"Таблица \"{DisplayName}\" дополняется строками:"))));
                    bodyDoc.Append(GetDiffTableOnStatus(RowStatus.Added));
                }

                // Описание изменения таблицы
                foreach (var elem in GetDiffDiscription())
                {
                    if (elem is Paragraph p)
                        bodyDoc.Append(p);
                    else if (elem is Table t)
                        bodyDoc.Append(t);
                }
            }
        }

        private Table GetDiffTableOnStatus(RowStatus status)
        {
            // Создаем таблицy
            Table table = CreateEmptyTable();

            // Строки таблицы после заголовков
            foreach (var elem in Rows.Where(x => x.Status == status))
            {
                var row = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
                for (int i = 0; i < elem.Columns.Count(); i++)
                {
                    string text = elem.Columns[i];
                    string footnoteLink = "";
                    if (elem.Footnote?.Column - 1 == i)
                        footnoteLink = $" {elem.Footnote.Number})";

                    //надстрочное начертание для примечания
                    Run footnoteLinkRun = new Run(new Text(footnoteLink));
                    RunProperties runProperties = new RunProperties(
                        new VerticalTextAlignment { Val = VerticalPositionValues.Superscript }
                    );
                    footnoteLinkRun.RunProperties = runProperties;

                    var justification = JustificationValues.Center;
                    if (indexOfBothJustificationColumns.Contains(i))
                        justification = JustificationValues.Both;

                    row.Append(
                        new TableCell(
                            new Paragraph(
                                new ParagraphProperties(new Justification { Val = justification }),
                                new Run(new Text(text)),
                                footnoteLinkRun)));
                }

                if (ColumnHeaders.Count() > elem.Columns.Count())
                    MergeCells(row.Elements<TableCell>().Last());

                table.Append(row);
            }
            return table;
        }

        private void MergeCells(TableCell cell)
        {
            var cellProps = cell.GetFirstChild<TableCellProperties>();
            if (cellProps == null)
            {
                cellProps = new TableCellProperties();
                cell.PrependChild(cellProps);
            }
            cellProps.GridSpan = new GridSpan { Val = ColumnHeaders.Count() };

            var paragraph = cell.Elements<Paragraph>().First();
            var paragraphProps = paragraph.GetFirstChild<ParagraphProperties>();
            if (paragraphProps == null)
            {
                paragraphProps = new ParagraphProperties();
                paragraph.PrependChild(paragraphProps);
            }
            paragraphProps.Justification = new Justification { Val = JustificationValues.Left };
        }

        private List<OpenXmlElement> GetDiffDiscription()
        {
            var editedOldDeletedRows = Rows.Where(x => x.Status == RowStatus.EditedOld || x.Status == RowStatus.Deleted).ToList();
            var editedNewRows = Rows.Where(x => x.Status == RowStatus.EditedNew).ToList();
            var deletedRows = Rows.Where(x => x.Status == RowStatus.Deleted).ToList();

            var addedFootnotes = Rows.Where(x => x.Status == RowStatus.Added && x.Footnote != null).Select(x => x.Footnote).ToList();
            var deletedFootnotes = Rows.Where(x => x.Status == RowStatus.Deleted && x.Footnote != null).Select(x => x.Footnote).ToList();
            var editedFootnotes = new List<(RowFootnote oldFootnote, RowFootnote newFootnote)>();

            var result = new List<OpenXmlElement>();

            if (!editedOldDeletedRows.Any())
                return result;
            else if (editedOldDeletedRows.Any())
                result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"Для таблицы \"{DisplayName}\" произвести изменение следующих строк:"))));

            int startNumDeleted = -1;
            int endNumDeleted = -1;
            foreach (var rowOld in editedOldDeletedRows)
            {
                 
                if (rowOld.Status == RowStatus.Deleted)
                {
                    if (startNumDeleted == -1) // начинаем последовательность
                    {
                        startNumDeleted = FindNumberOfRow(rowOld);
                        endNumDeleted = startNumDeleted;
                    }
                    else // продолжаем последовательность
                        endNumDeleted = FindNumberOfRow(rowOld);
                    continue;
                }
                if (startNumDeleted != -1) // завершаем последовательность
                {
                    result.Add(GetParagraphDeletedRows(startNumDeleted, endNumDeleted));
                    startNumDeleted = -1;
                    endNumDeleted = -1;
                }

                var rowNew = editedNewRows.FirstOrDefault(x => x.HashCode == rowOld.HashCode);
                if (rowNew == null)
                    throw new NullReferenceException($"Для строки с HashCode = {rowOld.HashCode} должна быть найдена измененная строка");
                
                // меняется значение в столбце
                for (int j = 0; j < rowOld.Columns.Count(); j++)
                {
                    var valueOld = rowOld.Columns[j];
                    var valueNew = rowNew.Columns[j];

                    if (valueOld != valueNew)
                        result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"{FindNumberOfRow(rowOld)} строка. В графе \"{ColumnHeaders[j]}\" заменить \"{valueOld}\" на \"{valueNew}\"."))));
                }

                if (rowOld.Footnote == null && rowNew.Footnote == null)
                    continue;

                // удаляется примечание
                if (rowOld.Footnote != null && rowNew.Footnote == null)
                {
                    deletedFootnotes.Add(rowOld.Footnote);
                    result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"{FindNumberOfRow(rowOld)} строка. В графе \"{ColumnHeaders[(rowOld.Footnote.Column - 1)]}\" удалить примечание {rowOld.Footnote.Number})."))));
                }
                    
                // добавляется примечание
                if (rowOld.Footnote == null && rowNew.Footnote != null)
                {
                    addedFootnotes.Add(rowNew.Footnote);
                    result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"{FindNumberOfRow(rowOld)} строка. В графе \"{ColumnHeaders[(rowNew.Footnote.Column - 1)]}\" добавить примечание {rowNew.Footnote.Number})."))));
                }
                
                if (rowOld.Footnote != null && rowNew.Footnote != null)
                {
                    
                    // меняется столбец примечания
                    if (rowOld.Footnote.Column != rowNew.Footnote.Column)
                    {
                        result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"{FindNumberOfRow(rowOld)} строка. В графе \"{ColumnHeaders[(rowOld.Footnote.Column - 1)]}\" удалить примечание {rowOld.Footnote.Number})."))));
                        result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"{FindNumberOfRow(rowOld)} строка. В графе \"{ColumnHeaders[(rowNew.Footnote.Column - 1)]}\" добавить примечание {rowNew.Footnote.Number})."))));
                    }
                    // меняется номер примечания
                    else if (rowOld.Footnote.Number != rowNew.Footnote.Number)
                    {
                        editedFootnotes.Add((rowOld.Footnote, rowNew.Footnote));
                        result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"{FindNumberOfRow(rowOld)} строка. В графе \"{ColumnHeaders[(rowOld.Footnote.Column - 1)]}\" изменить номер примечания с {rowOld.Footnote.Number} на {rowNew.Footnote.Number}"))));
                    }

                    // меняется текст примечания
                    if (rowOld.Footnote.Text != rowNew.Footnote.Text)
                        editedFootnotes.Add((rowOld.Footnote, rowNew.Footnote));
                }
            }
            // выводим последнюю удаленную последовательность
            if (startNumDeleted != -1)
                result.Add(GetParagraphDeletedRows(startNumDeleted, endNumDeleted));

            //// Если меняется примечание
            //if (isFootnoteTableEdited)
            //{
            //    result.Add(new Paragraph(new Run(new Text($"Примечания к таблице \"{DisplayName}\" изложить в следующей редакции:"))));
            //    result.Add(GetFootnoteTable());
            //}

            // Убираем из удаленных примечаний те, которые есть в строках Added, EditedNew, Identical (то есть по факту существуют)
            var rowsToCheckDeletedFootnotes = Rows.Where(x => (x.Status == RowStatus.Added || x.Status == RowStatus.EditedNew || x.Status == RowStatus.Identical) && x.Footnote != null);
            var footnotesToCheckDeletedFootnotes = rowsToCheckDeletedFootnotes.Select(x => x.Footnote).ToList();
            deletedFootnotes = deletedFootnotes.Where(x => !footnotesToCheckDeletedFootnotes.Any(y => y.Number == x.Number && y.Text == x.Text)).ToList();

            // Убираем из добавленных примечаний те, которые есть итак в Deleted, EditedNew, EditedOld и Identical (то есть они существовали)
            var rowsToCheckAddedFootnotes = Rows.Where(x => (x.Status == RowStatus.Deleted || x.Status == RowStatus.EditedNew || x.Status == RowStatus.EditedOld || x.Status == RowStatus.Identical) && x.Footnote != null);
            var footnotesToCheckAddedFootnotes = rowsToCheckAddedFootnotes.Select(x => x.Footnote).ToList();
            addedFootnotes = addedFootnotes.Where(x => !footnotesToCheckAddedFootnotes.Any(y => y.Number == x.Number && y.Text == x.Text)).ToList();

            // Выводим изменения по примечаниям
            if (addedFootnotes.Any())
            {
                result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"Таблицу \"{DisplayName}\" дополнить {(addedFootnotes.Count() == 1 ? "сноской" : "сносками")}:"))));
                foreach (var footnote in addedFootnotes)
                    result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"\"{footnote.Number}) {footnote.Text}\""))));
            }
            if (deletedFootnotes.Any())
            {
                result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"Из таблицы \"{DisplayName}\" удалить {(deletedFootnotes.Count() == 1 ? "сноску" : "сноски")}:"))));
                foreach (var footnote in deletedFootnotes)
                    result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"\"{footnote.Number}) {footnote.Text}\""))));
            }
            if (editedFootnotes.Any())
            {
                foreach (var footnote in editedFootnotes)
                    result.Add(new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"В таблице \"{DisplayName}\" заменить слова  \"{footnote.oldFootnote.Number}) {footnote.oldFootnote.Text}\" на \"{footnote.newFootnote.Number}) {footnote.newFootnote.Text}\""))));
            }
            return result;
        }

        private Paragraph GetParagraphDeletedRows(int startNumDeleted, int endNumDeleted)
        {
            var map = new Dictionary<int, string>()
            {
                { 1, "первую"},
                { 2, "вторую"},
                { 3, "третью"},
                { 4, "четвертую"},
                { 5, "пятую"},
                { 6, "шестую"},
                { 7, "седьмую"},
                { 8, "восьмую"},
                { 9, "девятую"}
            };

            if (startNumDeleted == endNumDeleted)
            {
                string val = startNumDeleted.ToString();

                if (map.TryGetValue(startNumDeleted, out var stringName))
                    val = stringName;
                return new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"{val} строку исключить.")));
            }
            else
                return new Paragraph(new ParagraphProperties(new Justification { Val = JustificationValues.Both }), new Run(new Text($"{startNumDeleted} – {endNumDeleted} строки исключить.")));
        }

        /// <summary>
        /// Находит номер строки в старой таблице
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="gostTable"></param>
        /// <param name="row"></param>
        /// <returns></returns>
        private int FindNumberOfRow(TableRow row)
        {
            var rowsOfOldTable = Rows.Where(x => x.Status != RowStatus.EditedNew && x.Status != RowStatus.Added).ToList();
            int n = rowsOfOldTable.IndexOf(row);
            return n + 1; // +1 тк с нуля начинается индексация
        }

        private Table GetFootnoteTable()
        {
            Table table = CreateEmptyTable();
            var footnoteRow = new DocumentFormat.OpenXml.Wordprocessing.TableRow();
            TableCell footnoteCell = new TableCell();
            footnoteCell.TableCellProperties = new TableCellProperties(
                new GridSpan { Val = ColumnHeaders.Count() });

            var footNotesnumbers = new HashSet<int?>();
            foreach (var elem in Rows.Where(x => x.Status != RowStatus.Deleted && x.Status != RowStatus.EditedOld))
            {
                if (elem.Footnote == null)
                    continue;

                if (!footNotesnumbers.Contains(elem.Footnote.Number))
                {
                    footnoteCell.Append(
                        new Paragraph(
                            new ParagraphProperties(new Justification { Val = JustificationValues.Left }),
                            new Run(new Text($"{elem.Footnote.Number}) {elem.Footnote.Text}"))));
                }
                footNotesnumbers.Add(elem.Footnote.Number);
            }

            if (footnoteCell.Descendants<Paragraph>().Any())
            {
                SortParagraphsInTableCellByNumber(footnoteCell);
                footnoteRow.Append(footnoteCell);
                table.Append(footnoteRow);
            }
            return table;
        }
    }

    /// <summary>
    /// Строка таблицы
    /// </summary>
    public class TableRow
    {
        // ===== Для EntityFramework =====
        public int Id { get; set; }
        public int TableBlockId { get; set; }
        [ForeignKey(nameof(TableBlockId))]
        public TableBlock? TableBlock { get; set; }
        public int? FootnoteId { get; set; }
        [ForeignKey(nameof(FootnoteId))]
        // ===============================


        // Ячейки строки
        public List<string> Columns { get; private set; } = new List<string>();

        // Примечание к строке
        public RowFootnote? Footnote { get; private set; } = null;

        // Тип элемента, на основе которого создана строка
        public string SourceType { get; private set; }

        public string AdditionalInfo { get; private set; } = "";

        // Дополнительный столбец для отображения изменения примечания в интерфейсе
        public string FootnoteDisplay =>
        Footnote != null
            ? $"Столбец №{Footnote.Column} | {Footnote.Number}) {Footnote.Text}"
            : string.Empty;

        public enum RowStatus
        {
            None = 0,
            Identical = 1,
            Added = 2,
            Deleted = 3,
            EditedNew = 4,
            EditedOld = 5
        }

        public RowStatus Status { get; set; } = RowStatus.None;

        // HashCode строки для сравнения
        public int HashCode { get; private set; }

        // --- НА ОСНОВЕ СУЩЕСТВУЮЩЕЙ СТРОКИ ---
        //public TableRow(TableRow row)
        //{
        //    Columns = new List<string>(row.Columns);
        //    Footnote = row.Footnote;
        //    Status = row.Status;
        //}

        public TableRow()
        {

        }


        // --- ПРОФИЛИ МОДЕЛИ ---
        public TableRow(GostClass obj)
        {
            Columns.Add(!String.IsNullOrEmpty(obj.Definition) ? WebUtility.HtmlDecode(obj.Definition.Trim().TrimEnd('.')) : "-"); // Смысловое определение
            Columns.Add(obj.Stereotype == "rf" ? $"{obj.Stereotype}:{obj.Name}" : obj.Name); // Наименование класса

            // Наименование родительского класса
            if (obj.ParentClassName == null)
                Columns.Add("-");
            else if (obj.ParentClassStereotype == "rf")
                Columns.Add($"{obj.ParentClassStereotype}:{obj.ParentClassName}");
            else
                Columns.Add(obj.ParentClassName);

            Footnote = obj.Footnote == null ? null : new RowFootnote(obj.Footnote);
            HashCode = ComputeStableHashCode(obj.ClassGuid, "");
            SourceType = nameof(GostClass);
        }

        public TableRow(GostAttributeModel obj)
        {
            Columns.Add(!String.IsNullOrEmpty(obj.Definition) ? WebUtility.HtmlDecode(obj.Definition.Trim().TrimEnd('.')) : "-"); // Смысловое определение
            Columns.Add((obj.Stereotype == "rf" && obj.ClassStereotype != "rf") ? $"{obj.Stereotype}:{obj.Name}" : obj.Name); // Наименование атрибута
            Columns.Add(obj.ClassStereotype == "rf" ? $"{obj.ClassStereotype}:{obj.ClassName}" : obj.ClassName); // Наименование класса

            string typeName = "";
            if (obj.TypeName?.Contains("gost.ru") == true)
                typeName = $"rf:{(obj.TypeName?.Split('#').Last() ?? "")}";
            else
                typeName = obj.TypeName?.Split('#').Last() ?? "";
            Columns.Add(ConvertTypeRussian(typeName, obj.TypeOfTypeName)); // Тип значения

            Footnote = obj.Footnote == null ? null : new RowFootnote(obj.Footnote);
            HashCode = ComputeStableHashCode(obj.ClassGuid, Columns[1]); // HashCode из UID класса и наименования атрибута
            SourceType = nameof(GostAttributeModel);
        }

        public TableRow(GostAssociationModel obj)
        {
            Columns.Add(!String.IsNullOrEmpty(obj.Definition) ? WebUtility.HtmlDecode(obj.Definition.Trim().TrimEnd('.')) : "-"); // Смысловое определение
            Columns.Add(obj.StartClassStereotype == "rf" ? $"{obj.StartClassStereotype}:{obj.StartClassName}" : obj.StartClassName); // Наименование класса начала
            Columns.Add(obj.EndClassStereotype == "rf" ? $"{obj.EndClassStereotype}:{obj.EndClassName}" : obj.EndClassName); // Наименование класса конца
            Columns.Add(obj.Stereotype == "rf" ? $"{obj.Stereotype}:{obj.Name}" : obj.Name); // Наименование ассоциации
            Columns.Add(obj.Multiplicity1); // Множественность

            Footnote = obj.Footnote == null ? null : new RowFootnote(obj.Footnote);
            HashCode = ComputeStableHashCode(obj.AssociationGuid, Columns[3]);  // HashCode из UID ассоциации и наименования ассоциации
            SourceType = nameof(GostAssociationModel);
        }

        // --- ПРОФИЛИ ОБМЕНА ---
        public TableRow(string className, string classStereotype, Guid classGuid, string sourceType, int inheritanceLevel)
        {
            Columns.Add($"Определено в: {(classStereotype == "cim" ? className : $"{classStereotype}:{className}")}");
            HashCode = ComputeStableHashCode(classGuid, Columns[0]);
            //HashCode = ComputeStableHashCode("123", Columns[0]);
            Footnote = null;
            SourceType = sourceType;
            AdditionalInfo = $"{inheritanceLevel};{className};1";
        }

        public TableRow(ProfileAttribute obj, Guid classGuid)
        {
            Columns.Add($"{(obj.Stereotype == "cim" ? obj.Name : $"{obj.Stereotype}:{obj.Name}")}"); // Имя атрибута
            Columns.Add((obj.IsMandatory ?? false) ? "1..1" : "0..1"); // Обязательность атрибута
            Columns.Add(obj.TypeName?.Split('#').Last() ?? ""); // Тип данных
            Columns.Add(!String.IsNullOrEmpty(obj.Definition) ? WebUtility.HtmlDecode(obj.Definition.Trim().TrimEnd('.')) : "-"); // Описание и ограничения

            Footnote = obj.Footnote == null ? null : new RowFootnote(obj.Footnote);
            HashCode = ComputeStableHashCode(classGuid, Columns[0]); // HashCode из UID класса и наименования атрибута
            SourceType = nameof(ProfileAttribute);
            AdditionalInfo = $"{obj.InheritanceLevel};{obj.ClassName};2";
        }

        public TableRow(ProfileAssociation obj)
        {
            Columns.Add($"{(obj.Stereotype == "cim" ? obj.Name : $"{obj.Stereotype}:{obj.Name}")}"); // Имя ассоциации
            Columns.Add(string.IsNullOrEmpty(obj.Multiplicity2) ? "-" : obj.Multiplicity2); // Множественность (от)
            Columns.Add(obj.Multiplicity1); // Множественность (к)
            Columns.Add($"{(obj.EndClassStereotype == "cim" ? obj.EndClassName : $"{obj.EndClassStereotype}:{obj.EndClassName}")}"); // Конечный класс ассоциации
            Columns.Add(!String.IsNullOrEmpty(obj.Definition) ? WebUtility.HtmlDecode(obj.Definition.Trim().TrimEnd('.')) : "-"); // Описание и ограничения

            Footnote = obj.Footnote == null ? null : new RowFootnote(obj.Footnote);
            HashCode = ComputeStableHashCode(obj.AssociationGuid, Columns[0]); // HashCode из UID ассоциации, наименования ассоциации
            SourceType = nameof(ProfileAssociation);
            AdditionalInfo = $"{obj.InheritanceLevel};{obj.ClassName};2";
        }


        /// <summary>
        /// Преобразует тип из типа, который находится XML, в человекочитаемый тип для документа
        /// </summary>
        /// <param name="typeName"></param>
        /// <param name="typeOfTypeName"></param>
        /// <returns></returns>
        private static string ConvertTypeEnglish(string typeName, string typeOfTypeName)
        {
            if (typeOfTypeName == "Compound" || 
                typeOfTypeName == "Enumeration" ||
                typeOfTypeName == "Primitive" ||
                typeOfTypeName == "CIMDatatype")
                return typeName;

            return "Unknown";
        }

        private static string ConvertTypeRussian(string typeName, string typeOfTypeName)
        {
            Dictionary<string, string> _primitives = new Dictionary<string, string>()
            {
                {"Integer", "Целое"},
                {"Float", "Вещественный"},
                {"Boolean", "Логическое"},
                {"String", "Строка"},
                {"UUID", "UUID"},
                {"Time", "Время"},
                {"Date", "Дата"},
                {"DateTime", "ДатаВремя"},
                {"MonthDay", "ДеньМесяца" }
            };

            if (typeOfTypeName == "Primitive")
            {
                if (_primitives.ContainsKey(typeName))
                    return _primitives[typeName];
                return typeName;
            }
            else if (typeOfTypeName == "CIMDatatype")
                return "Вещественный";
            else if (typeOfTypeName == "Compound" ||
                typeOfTypeName == "Enumeration")
                return typeName;

            return "Unknown";
        }

        /// <summary>
        /// Проверка равенства строк
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public bool IsEqual(TableRow row)
        {
            bool isEqual = true;
            for (int i = 0; i < Columns.Count(); i++)
            {
                if (Columns[i] != row.Columns[i])
                {
                    isEqual = false;
                }
            }
            if (Footnote?.Number != row.Footnote?.Number
                || Footnote?.Column != row.Footnote?.Column
                || Footnote?.Text != row.Footnote?.Text)
            {
                isEqual = false;
            }
            return isEqual;
        }

        public static int ComputeStableHashCode(Guid guid, string columnValue)
        {
            // Создаём детерминированную строку для хэширования
            string input = $"{guid:N}|{columnValue ?? string.Empty}";

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                // Берём первые 4 байта как int (безопасно для сравнения)
                return BitConverter.ToInt32(hashBytes, 0);
            }
        }

        public static int ComputeStableHashCode(string columnValue1, string columnValue2)
        {
            // Создаём детерминированную строку для хэширования
            string input = $"{columnValue1 ?? string.Empty}|{columnValue2 ?? string.Empty}";

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                // Берём первые 4 байта как int (безопасно для сравнения)
                return BitConverter.ToInt32(hashBytes, 0);
            }
        }
    }

    public class RowFootnote
    {
        // ===== Для EntityFramework =====
        public int Id { get; set; }
        public int TableRowId { get; set; }
        [ForeignKey(nameof(TableRowId))]
        public TableRow? TableRow { get; set; }
        // ===============================

        public Guid Uid { get; set; }
        public int Number { get; set; }
        public int Column { get; set; }
        public string Text { get; set; }

        public RowFootnote()
        {

        }

        public RowFootnote(TableFootnote footnote)
        {
            Uid = footnote.FootNoteId;
            Number = footnote.Number ?? 0;
            Column = footnote.Column ?? 0;
            Text = footnote.Text ?? "";
        }
    }
}
