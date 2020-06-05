using PrivateWin10.Windows;
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

namespace PrivateWin10.Controls
{
    /// <summary>
    /// Interaction logic for TweakNotify.xaml
    /// </summary>
    public partial class TweakNotify : UserControl, INotificationTab
    {
        public event EventHandler<EventArgs> Emptied;

        public TweakNotify()
        {
            InitializeComponent();
        }

        public bool IsEmpty()
        {
            return true;
        }

        public bool Add(TweakManager.TweakEventArgs args)
        {
            return true;
        }

        public void Closing()
        {

        }
    }
}
