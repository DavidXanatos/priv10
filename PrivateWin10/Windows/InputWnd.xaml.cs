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

        public InputWnd(string prompt, List<string> items, string defValue = "", bool editable = true, string title = null)
        {
            InitializeComponent();
            if (title != null)
                this.Title = title;
            lblPrompt.Content = prompt;
            txtValue.Visibility = Visibility.Collapsed;
            cmbValue.Visibility = Visibility.Visible;
            cmbValue.IsEditable = editable;
            foreach (var item in items)
                cmbValue.Items.Add(new ComboBoxItem() { Content = item, Tag = item });
            if (!WpfFunc.CmbSelect(cmbValue, defValue))
                cmbValue.Text = defValue;
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (txtValue.Visibility == Visibility.Visible)
            {
                txtValue.SelectAll();
                txtValue.Focus();
            }
            else
                cmbValue.Focus();
        }

        public string Value
        {
            get {
                if (txtValue.Visibility == Visibility.Visible)
                    return txtValue.Text;
                return cmbValue.Text;
            }
        }
    }
}
