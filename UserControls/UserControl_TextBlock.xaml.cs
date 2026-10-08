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
    /// <summary>
    /// Interaction logic for UserControl_TextBlock.xaml
    /// </summary>
    public partial class UserControl_TextBlock : UserControl
    {
        public UserControl_TextBlock()
        {
            InitializeComponent();
            Loaded += UserControl_Loaded;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ViewModel_UserControl_TextBlock vm)
            {
                vm.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(vm.FlowDocument))
                    {
                        RichTextBoxControl.Document = vm.FlowDocument;
                    }
                };
                RichTextBoxControl.Document = vm.FlowDocument;
            }
        }
    }
}
