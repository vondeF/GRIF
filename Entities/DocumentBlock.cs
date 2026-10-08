using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Entities
{
    /// <summary>
    /// Класс любого блока внутри логической части
    /// </summary>
    public abstract class DocumentBlock: IWritableBlock
    {
        // ===== Для EntityFramework =====
        public int Id { get; set; }
        public int DocumentPartId { get; set; }
        [ForeignKey(nameof(DocumentPartId))]
        public DocumentPart? DocumentPart { get; set; }
        // ===============================

        public DocumentBlock() 
        {

        }

        public DocumentBlock(string displayName)
        {
            DisplayName = displayName;
        }

        // Реализация интерфейса
        public int NumberOfEditing { get; set; }
        public string DisplayName { get; set; } = "";
        public abstract void WriteAll(string path);
        public abstract void WriteDiff(string path);


    }
}
