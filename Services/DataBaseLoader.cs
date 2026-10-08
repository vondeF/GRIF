using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Office2010.Excel;
using DocumentFormat.OpenXml.Spreadsheet;
using GostObjectsClassLibrary;
using GostObjectsClassLibrary.GostDoc;
using GostObjectsClassLibrary.ProfileDoc;
using GRIF.DataBaseContexts;
using GRIF.Entities;
using GRIF.Entities.BasedOnConfig;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace GRIF.Services
{
    public interface IDataBaseLoader
    {
        Task<T> DownloadAsync<T>(string tag);
        Task<T> DownloadGRIFAsync<T>(string Name);
        Task UploadAsync<T>(T doc);
        Task DeleteLastVersionAsync();
    }

    public class ModelDocumentDataBaseLoader : IDataBaseLoader
    {
        public async Task<T> DownloadAsync<T>(string tag)
        {
            if (typeof(T) != typeof(GostDoc))
                throw new NotSupportedException($"Тип {typeof(T)} не поддерживается");


            int gostNumber = int.Parse(tag ?? "0");
            GostDoc? gostDoc;
            using (GostDownloadContext db = new GostDownloadContext())
            {
                gostDoc = await db.GostDocs
                    .Where(x => x.gostNumber == gostNumber)
                    .Include(x => x.classesAbstract).ThenInclude(c => c.Footnote)
                    .Include(x => x.classesMain).ThenInclude(c => c.Footnote)
                    .Include(x => x.attributesAbstractAndMain).ThenInclude(c => c.Footnote)
                    .Include(x => x.associationsAbstractAndMain).ThenInclude(c => c.Footnote)
                    .Include(x => x.classesEnum).ThenInclude(c => c.Footnote)
                    .Include(x => x.attributesEnum).ThenInclude(c => c.Footnote)
                    .Include(x => x.classesCompound).ThenInclude(c => c.Footnote)
                    .Include(x => x.attributesCompound).ThenInclude(c => c.Footnote)
                    .Include(x => x.classesPrimitive).ThenInclude(c => c.Footnote)
                    .Include(x => x.classesCimDatatype).ThenInclude(c => c.Footnote)
                    .Include(x => x.attributesCimDatatype).ThenInclude(c => c.Footnote)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();

                if (gostDoc == null)
                    throw new NullReferenceException($"В БД нет данных для заполнения ГОСТа с номером {gostNumber}");

                // Для приложений (тк классы, атр и асс описаны также в других ГОСТах - поэтому в БД номер последнего ГОСТа)
                if (gostNumber == 2)
                {
                    var extraData = await db.GostDocs
                        .Where(x => x.gostNumber != 2)
                        .Include(x => x.classesAbstract).ThenInclude(c => c.Footnote)
                        .Include(x => x.classesMain).ThenInclude(c => c.Footnote)
                        .Include(x => x.attributesAbstractAndMain).ThenInclude(c => c.Footnote)
                        .Include(x => x.associationsAbstractAndMain).ThenInclude(c => c.Footnote)
                        .AsSplitQuery()
                        .ToArrayAsync();

                    var newClassesAbstract = new List<GostClass>();
                    var newClassesMain = new List<GostClass>();
                    var newAttr = new List<GostAttributeModel>();
                    var newAsoc = new List<GostAssociationModel>();
                    foreach (var data in extraData)
                    {
                        newClassesAbstract.AddRange(data.classesAbstract.Where(x => !string.IsNullOrEmpty(x.BasicProfileExt)));
                        newClassesMain.AddRange(data.classesMain.Where(x => !string.IsNullOrEmpty(x.BasicProfileExt)));
                        newAttr.AddRange(data.attributesAbstractAndMain.Where(x => !string.IsNullOrEmpty(x.BasicProfileExt)));
                        newAsoc.AddRange(data.associationsAbstractAndMain.Where(x => !string.IsNullOrEmpty(x.BasicProfileExt)));
                    }

                    gostDoc.classesAbstract.AddRange(newClassesAbstract);
                    gostDoc.classesMain.AddRange(newClassesMain);
                    gostDoc.attributesAbstractAndMain.AddRange(newAttr);
                    gostDoc.associationsAbstractAndMain.AddRange(newAsoc);
                }
            }
            return (T)(object)gostDoc;
        }

        public async Task<T> DownloadGRIFAsync<T>(string profileName)
        {
            if (typeof(T) != typeof(ModelDocumentWord))
                throw new NotSupportedException($"Тип {typeof(T)} не поддерживается");

            ModelDocumentWord? doc;
            using (GostUploadContext db = new GostUploadContext())
            {
                doc = await db.ModelDocumentWords
                    .Include(x => x.Parts)
                        .ThenInclude(p => p.Blocks)
                            .ThenInclude(block => (block as TableBlock)!.Rows)
                                .ThenInclude(r => r.Footnote)
                    .FirstOrDefaultAsync(x => x.Name == profileName);
            }
            return (T)(object)doc;
        }

        public async Task UploadAsync<T>(T doc)
        {
            if (typeof(T) != typeof(ModelDocumentWord))
                throw new NotSupportedException($"Тип {typeof(T)} не поддерживается");

            using (GostUploadContext db = new GostUploadContext())
            {
                var modelDoc = doc as ModelDocumentWord;
                db.ModelDocumentWords.Add(modelDoc);
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteLastVersionAsync()
        {
            using (GostUploadContext db = new GostUploadContext())
            {
                var lastDoc = await db.ModelDocumentWords
                    .Include(x => x.Parts)
                        .ThenInclude(p => p.Blocks)
                            .ThenInclude(block => (block as TableBlock)!.Rows)
                                .ThenInclude(row => row.Footnote)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (lastDoc != null)
                {
                    //var footnotesToDelete = lastDoc.Parts
                    //    .SelectMany(p => p.Blocks)
                    //    .OfType<TableBlock>()
                    //    .SelectMany(tb => tb.Rows)
                    //    .Where(r => r.Footnote != null)
                    //    .Select(r => r.Footnote!)
                    //    .ToList();

                    //db.TableFootnotes.RemoveRange(footnotesToDelete);

                    db.ModelDocumentWords.Remove(lastDoc);
                    await db.SaveChangesAsync();          
                }
            }
        }
    }

    public class ExchangeDocumentDataBaseLoader : IDataBaseLoader
    {
        public async Task<T> DownloadAsync<T>(string tag)
        {
            if (typeof(T) != typeof(ProfileDoc))
                throw new NotSupportedException($"Тип {typeof(T)} не поддерживается");

            ProfileDoc? profile;
            using (ExchangeDownloadContext db = new ExchangeDownloadContext())
            {
                profile = db.GostProfiles
                    .Where(x => x.profileTag == tag)
                    .Include(x => x.classes)
                        .ThenInclude(c => c.Attributes)
                            .ThenInclude(a => a.Footnote)
                    .Include(x => x.classes)
                        .ThenInclude(c => c.Associations)
                            .ThenInclude(a => a.Footnote)
                    .FirstOrDefault();
            }
            return (T)(object)profile;
        }

        public async Task<T> DownloadGRIFAsync<T>(string gostName)
        {
            if (typeof(T) != typeof(ExchangeDocumentWord))
                throw new NotSupportedException($"Тип {typeof(T)} не поддерживается");

            ExchangeDocumentWord? doc;
            using (ExchangeUploadContext db = new ExchangeUploadContext())
            {
                doc = await db.ModelDocumentWord
                    .Include(x => x.Parts)
                        .ThenInclude(p => p.Blocks)
                            .ThenInclude(block => (block as TableBlock)!.Rows)
                                .ThenInclude(r => r.Footnote)
                    .FirstOrDefaultAsync(x => x.Name == gostName); // Последний записанный в БД ГОСТ
            }
            return (T)(object)doc;
        }

        public async Task UploadAsync<T>(T doc)
        {
            if (typeof(T) != typeof(ExchangeDocumentWord))
                throw new NotSupportedException($"Тип {typeof(T)} не поддерживается");

            using (ExchangeUploadContext db = new ExchangeUploadContext())
            {
                var exchangeDoc = doc as ExchangeDocumentWord;
                db.ModelDocumentWord.Add(exchangeDoc);
                await db.SaveChangesAsync();
            }
        }

        public async Task DeleteLastVersionAsync()
        {
            using (ExchangeUploadContext db = new ExchangeUploadContext())
            {
                var lastDoc = await db.ModelDocumentWord
                    .Include(x => x.Parts)
                        .ThenInclude(p => p.Blocks)
                            .ThenInclude(block => (block as TableBlock)!.Rows)
                                .ThenInclude(row => row.Footnote)
                    .OrderByDescending(x => x.Id)
                    .FirstOrDefaultAsync();

                if (lastDoc != null)
                {
                    //var footnotesToDelete = lastDoc.Parts
                    //    .SelectMany(p => p.Blocks)
                    //    .OfType<TableBlock>()
                    //    .SelectMany(tb => tb.Rows)
                    //    .Where(r => r.Footnote != null)
                    //    .Select(r => r.Footnote!)
                    //    .ToList();

                    //db.TableFootnotes.RemoveRange(footnotesToDelete);

                    db.ModelDocumentWord.Remove(lastDoc);
                    await db.SaveChangesAsync();
                }
            }
        }
    }
}
