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

namespace PrivateWin10
{
    /// <summary>
    /// Interaction logic for InputWnd.xaml
    /// </summary>
    public partial class InputWnd : Window
    {
        public InputWnd(string prompt, string defValue = "", string title = null)
        {
            InitializeComponent();
            if (title != null)
                this.Title = title;
            lblPrompt.Content = prompt;
            txtValue.Text = defValue;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtValue.SelectAll();
            txtValue.Focus();
        }

        public string Value
        {
            get { return txtValue.Text; }
        }
    }
}
