using GRIF.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRIF.Services
{
    public interface ITableBlockComparer
    {
        TableBlock Compare(TableBlock table1, TableBlock? table2);
    }

    public class TableBlockComparer : ITableBlockComparer
    {
        public TableBlock Compare(TableBlock table1, TableBlock? table2)
        {
            var resTable = table1;

            if (table2 == null)
            {
                foreach(var row1 in resTable.Rows)
                {
                    row1.Status = TableRow.RowStatus.Added;
                }
                resTable.Sort();
                return resTable;
            }

            // Убираем изменения относительно позапрошлой версии
            var rows2 = table2.Rows.Where(x => x.Status != TableRow.RowStatus.Deleted && x.Status != TableRow.RowStatus.EditedOld);

            var editedOldRows = new List<TableRow>();
            var deletedRows = rows2.Where(x => !table1.Rows.Any(y => y.HashCode == x.HashCode)); // удаленные строки - те, которые мы не смогли найти в текущей версии

            foreach (var row1 in resTable.Rows)
            {
                var row2 = table2.Rows.Where(x => x.Status != TableRow.RowStatus.Deleted && x.Status != TableRow.RowStatus.EditedOld).FirstOrDefault(x => x.HashCode == row1.HashCode);
                
                if (row2 == null) // Если такой строки не было в прошлой версии
                {
                    row1.Status = TableRow.RowStatus.Added;
                    continue;
                }
                if (row1.IsEqual(row2)) // Если точно такая же строка была в прошлой версии
                {
                    row1.Status = TableRow.RowStatus.Identical;
                    continue;
                }
                else // Если у строк одинаковый HashCode, но разные данные
                {
                    row1.Status = TableRow.RowStatus.EditedNew;
                    var editedOldRow = row2;
                    editedOldRow.Status = TableRow.RowStatus.EditedOld;
                    editedOldRows.Add(editedOldRow);
                } 
            }

            foreach (var row in editedOldRows)
            {
                resTable.AddRowDefault(row);
            }
            foreach (var row in deletedRows)
            {
                row.Status = TableRow.RowStatus.Deleted;
                resTable.AddRowDefault(row);
            }

            resTable.Sort();
            return resTable;
        }
    }
}
