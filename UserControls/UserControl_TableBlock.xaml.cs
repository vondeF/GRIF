using DocumentFormat.OpenXml.Drawing;
using GRIF.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GRIF.UserControls
{
    public partial class UserControl_TableBlock : UserControl
    {
        public UserControl_TableBlock()
        {
            InitializeComponent();
            Loaded += UserControl_Loaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel_UserControl_TableBlock vm)
            {
                GenerateColumns(vm.ColumnHeaders);
            }
        }

        private void GenerateColumns(List<string> headers)
        {
            TableDataGrid.Columns.Clear();

            if (!this.Resources.Contains("SingleColumnRowConverter"))
            {
                throw new InvalidOperationException("Конвертер SingleColumnRowConverter не найден в ресурсах UserControl.");
            }

            var converterObj = this.Resources["SingleColumnRowConverter"];
            if (converterObj is not IValueConverter columnConverter)
            {
                throw new InvalidOperationException("Ресурс SingleColumnRowConverter не реализует интерфейс IValueConverter.");
            }

            // Базовый стиль для всех ячеек
            var baseStyle = new Style(typeof(TextBlock));
            baseStyle.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.Wrap));
            baseStyle.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            baseStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left));

            // Стиль для ПЕРВОЙ колонки с триггером (Объединенная ячейка на все столбцы типа "Определено в class")
            var firstColumnStyle = new Style(typeof(TextBlock), baseStyle);
            var trigger = new DataTrigger
            {
                Binding = new Binding("Columns") { Converter = columnConverter },
                Value = true
            };
            trigger.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            trigger.Setters.Add(new Setter(TextBlock.ForegroundProperty, Application.Current.TryFindResource("PrimaryTextBrush") as Brush ?? Brushes.DarkRed));
            firstColumnStyle.Triggers.Add(trigger);

            // Генерация колонок данных
            for (int i = 0; i < headers.Count(); i++)
            {
                var column = new DataGridTextColumn
                {
                    Header = headers[i],
                    Binding = new Binding($"Columns[{i}]"),
                    Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                    ElementStyle = (i == 0) ? firstColumnStyle : baseStyle
                };
                TableDataGrid.Columns.Add(column);
            }

            // Колонка "Примечание" — обычный стиль
            var footnoteColumn = new DataGridTextColumn
            {
                Header = "Примечание",
                Binding = new Binding("FootnoteDisplay"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star),
                ElementStyle = baseStyle
            };
            TableDataGrid.Columns.Add(footnoteColumn);
        }
    }
}
