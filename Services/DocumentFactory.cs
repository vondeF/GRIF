using GostObjectsClassLibrary.GostDoc;
using GostObjectsClassLibrary.ProfileDoc;
using GRIF.Entities;
using GRIF.Entities.BasedOnConfig;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Services
{
    public interface IDocumentFactory
    {
        Task<IDocument> CreateAsync(Template template, object documentConfigData, IDataBaseLoader dataBaseLoader);
    }

    public class DocumentFactory : IDocumentFactory
    {
        private readonly ITableBlockFactory _tableBlockFactory;
        private readonly ITableBlockComparer _tableBlockComparer;

        public DocumentFactory(ITableBlockFactory tableBlockFactory, ITableBlockComparer tableBlockComparer)
        {
            _tableBlockFactory = tableBlockFactory;
            _tableBlockComparer = tableBlockComparer;
        }

        public async Task<IDocument> CreateAsync(Template template, object documentConfigData, IDataBaseLoader dataBaseLoader)
        {
            switch (documentConfigData)
            {
                case ModelDocumentConfigData gost:
                    var document = new ModelDocumentWord(gost.Name);

                    // 1. Разделяем шаблон и получаем список TextBlock
                    var textBlocks = template.DivideByBookmarks();

                    // 2. Загружаем данные из БД
                    var data = await dataBaseLoader.DownloadAsync<GostDoc>(gost.Tag);
                    var dataPrevious = await dataBaseLoader.DownloadGRIFAsync<ModelDocumentWord>(gost.Name);

                    document.NumberOfEditing = dataPrevious?.NumberOfEditing ?? 0;

                    // 3. Cоздаем и заполняем DocumentPart
                    var tableBookmarks = gost.Tables.Select(x => x.BookmarkName).ToList();
                    DocumentPart? currentDocumentPart = null;
                    foreach(var textBlock in textBlocks)
                    {
                        // Текстовый блок с заголовком таблицы (добавлем его и таблицу к существующему DocumentPart)
                        if (tableBookmarks.Contains(textBlock.BookmarkName))
                        {
                            if (currentDocumentPart == null)
                                currentDocumentPart = new DocumentPart();
                            
                            // Номер изм текстовой части, связанной с таблицей
                            currentDocumentPart.Blocks.Add(textBlock);
                            if (currentDocumentPart.NumberOfEditing < textBlock.NumberOfEditing)
                                currentDocumentPart.NumberOfEditing = textBlock.NumberOfEditing;

                            // Создаем таблицу
                            var tableBlock = _tableBlockFactory.CreateTableBlock(gost.Tables.First(x => x.BookmarkName == textBlock.BookmarkName), data);
                            var tableBlockPrevious = dataPrevious?.Parts?
                                .FirstOrDefault(x => x.Blocks.Any(y => y is TableBlock table && table.DisplayName == tableBlock.DisplayName))?.Blocks
                                .FirstOrDefault(y => y is TableBlock table && table.DisplayName == tableBlock.DisplayName);
                            var resultTableBlock = _tableBlockComparer.Compare(tableBlock, tableBlockPrevious == null ? null : tableBlockPrevious as TableBlock);

                            // Номер изм таблицы
                            if (resultTableBlock.Rows.Any(x => x.Status != TableRow.RowStatus.Identical))
                            {
                                resultTableBlock.NumberOfEditing = (dataPrevious?.NumberOfEditing ?? 0) + 1;
                                if (currentDocumentPart.NumberOfEditing < resultTableBlock.NumberOfEditing)
                                    currentDocumentPart.NumberOfEditing = resultTableBlock.NumberOfEditing;
                            }

                            currentDocumentPart.Blocks.Add(resultTableBlock);
                        }
                        else if (textBlock.BookmarkName.Contains(" промежуточная"))
                        {
                            currentDocumentPart!.Blocks.Add(textBlock);
                            if (currentDocumentPart.NumberOfEditing < textBlock.NumberOfEditing)
                                currentDocumentPart.NumberOfEditing = textBlock.NumberOfEditing;
                        }    
                        // Текстовый блок раздела (завершаем формирование DocumentPart, добавлем его к документу и формируем новый DocumentPart)
                        else
                        {
                            if (currentDocumentPart != null)
                                document.Parts.Add(currentDocumentPart);
                            currentDocumentPart = new DocumentPart();
                            currentDocumentPart.DisplayName = textBlock.DisplayName;
                            currentDocumentPart.Blocks.Add(textBlock);
                            currentDocumentPart.NumberOfEditing = textBlock.NumberOfEditing;
                        }
                    }
                    return document;

                case ExchangeDocumentConfigData exchange:
                    break;

                case ExchangeDocumentDbData exchangeDb:

                    var exchangeTextBlocks = template.DivideByBookmarks();

                    var exchangeDocument = new ExchangeDocumentWord(exchangeDb.Name);
                    var exchangeData = await dataBaseLoader.DownloadAsync<ProfileDoc>(exchangeDb.profileTag);
                    var exchangeDataPrevious = await dataBaseLoader.DownloadGRIFAsync<ExchangeDocumentWord>(exchangeDb.Name);

                    exchangeDocument.NumberOfEditing = exchangeDataPrevious?.NumberOfEditing ?? 0;

                    var currentExchangeDocumentPart = new DocumentPart();
                    currentExchangeDocumentPart.Blocks.Add(exchangeTextBlocks.First());
                    exchangeDocument.Parts.Add(currentExchangeDocumentPart);

                    // создаем таблицы
                    foreach (var exchangeClass in exchangeData.classes.OrderBy(x => x.Name))
                    {
                        currentExchangeDocumentPart = new DocumentPart();
                        if (string.IsNullOrEmpty(exchangeClass.Definition))
                            currentExchangeDocumentPart.DisplayName = $"{(exchangeClass.Stereotype == "cim" ? exchangeClass.Name : $"{exchangeClass.Stereotype}:{exchangeClass.Name}")}";
                        else
                            currentExchangeDocumentPart.DisplayName = $"{(exchangeClass.Stereotype == "cim" ? exchangeClass.Name : $"{exchangeClass.Stereotype}:{exchangeClass.Name}")}. {exchangeClass.Definition}";

                        var tableAttrs = _tableBlockFactory.CreateTableBlockAttrs(exchangeClass);
                        var tableBlockAttrsPrevious = exchangeDataPrevious?.Parts ?
                            .FirstOrDefault(x => x.Blocks.Any(y => y is TableBlock table && table.DisplayName == tableAttrs.DisplayName))?.Blocks
                            .FirstOrDefault(y => y is TableBlock table && table.DisplayName == tableAttrs.DisplayName);
                        var resultTableBlockAttrs = _tableBlockComparer.Compare(tableAttrs, tableBlockAttrsPrevious == null ? null : tableBlockAttrsPrevious as TableBlock);

                        var tableAsocs = _tableBlockFactory.CreateTableBlockAsocs(exchangeClass);
                        var tableBlockAsocsPrevious = exchangeDataPrevious?.Parts?
                            .FirstOrDefault(x => x.Blocks.Any(y => y is TableBlock table && table.DisplayName == tableAsocs.DisplayName))?.Blocks
                            .FirstOrDefault(y => y is TableBlock table && table.DisplayName == tableAsocs.DisplayName);
                        var resultTableBlockAsocs = _tableBlockComparer.Compare(tableAsocs, tableBlockAsocsPrevious == null ? null : tableBlockAsocsPrevious as TableBlock);

                        if (resultTableBlockAttrs.Rows.Count() != 0)
                            currentExchangeDocumentPart.Blocks.Add(resultTableBlockAttrs);
                        if (resultTableBlockAsocs.Rows.Count() != 0)
                            currentExchangeDocumentPart.Blocks.Add(resultTableBlockAsocs);
                        exchangeDocument.Parts.Add(currentExchangeDocumentPart);
                    }
                    return exchangeDocument;
            }
            return null;
        }
    }
}
