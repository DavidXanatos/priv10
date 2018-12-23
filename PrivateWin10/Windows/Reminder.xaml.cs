using System;
using System.Collections.Generic;
using System.Diagnostics;
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

/*
    Mind the LICENSE and don't mess with this file!
*/

namespace PrivateWin10.Windows
{
    /// <summary>
    /// Interaction logic for Reminder.xaml
    /// </summary>
    public partial class Reminder : Window
    {
        public Reminder()
        {
            InitializeComponent();

            this.Title = string.Format("{0} v{1} - Support Reminder", App.mName, App.mVersion);

            lblTitle.Content = App.mName;
            lblVerNum.Content = App.mVersion;
        }

        private void LblSupport_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Process.Start("https://github.com/DavidXanatos/priv10/wiki/Support");
            this.DialogResult = true;
        }

        private void LblClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
