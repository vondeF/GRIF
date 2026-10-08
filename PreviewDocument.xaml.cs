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
using System.Windows.Shapes;

namespace GRIF
{
    /// <summary>
    /// Interaction logic for PreviewDocument.xaml
    /// </summary>
    public partial class PreviewDocument : Window
    {
        public PreviewDocument(ViewModel_PreviewDocument viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        private void Back_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
