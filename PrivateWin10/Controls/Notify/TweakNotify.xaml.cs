using PrivateWin10.Windows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using TweakEngine;

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

            // todo localize xxxxxxx <<<<<<<<<<<<<
            /*this.tweaksGrid.Columns[1].Header = Translate.fmt("lbl_name");
            this.tweaksGrid.Columns[2].Header = Translate.fmt("lbl_enabled");
            this.tweaksGrid.Columns[3].Header = Translate.fmt("lbl_action");
            this.tweaksGrid.Columns[4].Header = Translate.fmt("lbl_direction");
            this.tweaksGrid.Columns[5].Header = Translate.fmt("lbl_program");*/

            var Ignore = this.IgnoreSB.Content as TextBlock;
            var Restore = this.RestoreSB.Content as TextBlock;

            Ignore.Text = Translate.fmt("lbl_ignore");
            (this.IgnoreSB.MenuItemsSource[0] as MenuItem).Header = Translate.fmt("lbl_ignore_all");
            Restore.Text = Translate.fmt("lbl_restore");
            (this.RestoreSB.MenuItemsSource[0] as MenuItem).Header = Translate.fmt("lbl_restore_all");

            UpdateState();
        }

        public bool IsEmpty()
        {
            return tweaksGrid.Items.IsEmpty;
        }

        public bool Add(TweakManager.TweakEventArgs args)
        {
            if (args.state != TweakManager.TweakEventArgs.State.eChanged)
                return false;

            tweaksGrid.Items.Add(new TweakEntry(args));
            UpdateState();
            return true;
        }

        public void Closing()
        {

        }

        public void RemoveCurrent()
        {
            var item = tweaksGrid.SelectedItem as TweakEntry;
            tweaksGrid.SelectedIndex++;
            if (item != null)
                tweaksGrid.Items.Remove(item);
            UpdateState();
        }

        public void UpdateState()
        {
            this.IgnoreSB.IsEnabled = tweaksGrid.Items.IsEmpty ? false : true;
            this.RestoreSB.IsEnabled = tweaksGrid.Items.IsEmpty ? false : true;

            if (tweaksGrid.Items.IsEmpty)
                Emptied?.Invoke(this, new EventArgs());
            else if (tweaksGrid.SelectedItem == null)
                tweaksGrid.SelectedItem = tweaksGrid.Items[0];
        }

        private void BtnIgnoreAll_Click(object sender, RoutedEventArgs e)
        {
            tweaksGrid.Items.Clear();
            UpdateState();
        }

        private void BtnIgnore_Click(object sender, MouseButtonEventArgs e)
        {
            var item = tweaksGrid.SelectedItem as TweakEntry;
            if (item == null)
                return;

            RemoveCurrent();
        }

        private void BtnRestoreAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (TweakEntry item in tweaksGrid.Items)
                App.tweaks.ApplyTweak(item.Entry.tweak);

            tweaksGrid.Items.Clear();
            UpdateState();
        }

        private void BtnRestore_Click(object sender, MouseButtonEventArgs e)
        {
            var item = tweaksGrid.SelectedItem as TweakEntry;
            if (item != null)
                App.tweaks.ApplyTweak(item.Entry.tweak);

            RemoveCurrent();
        }

        public class TweakEntry : INotifyPropertyChanged
        {
            public TweakManager.TweakEventArgs Entry;
            DateTime EntryTimeStamp = DateTime.Now;

            public TweakEntry(TweakManager.TweakEventArgs entry)
            {
                Entry = entry;
            }
            
            public string Name { get { return Entry.tweak.Name; } }
            public string Group { get { return Entry.tweak.Group; } }
            public string Category { get { return Entry.tweak.Category; } }

            public string State { get { return Translate.fmt("str_changed"); } }

            public string TimeStamp { get { return EntryTimeStamp.ToString("HH:mm:ss"); } }

            #region INotifyPropertyChanged Members

            public event PropertyChangedEventHandler PropertyChanged;

            #endregion

            #region Private Helpers

            private void NotifyPropertyChanged(string propertyName)
            {
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }
            }

            #endregion
        }

        private void TweaksGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {

        }

        private void TweaksGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var item = tweaksGrid.SelectedItem as TweakEntry;
            if (item != null)
                info.Text = item.Entry.tweak.GetInfoStr();
        }
    }
}
