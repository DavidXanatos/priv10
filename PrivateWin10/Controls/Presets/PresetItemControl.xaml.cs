using MiscHelpers;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrivateWin10
{
    /// <summary>
    /// Interaction logic for ProcessControl.xaml
    /// </summary>
    public partial class PresetItemControl : UserControl, IControlItem<PresetItem>
    {
        public event RoutedEventHandler Click;

        public PresetItem item;

        public PresetItemControl(PresetItem item)
        {
            InitializeComponent();

            DoUpdate(item);

            rect.MouseDown += new MouseButtonEventHandler(rect_Click);
            //name.MouseDown += new MouseButtonEventHandler(rect_Click);
            name.PreviewMouseDown += new MouseButtonEventHandler(rect_Click);
            //progGrid.PreviewMouseDown += new MouseButtonEventHandler(rect_Click);
            icon.MouseDown += new MouseButtonEventHandler(rect_Click);
            info.MouseDown += new MouseButtonEventHandler(rect_Click);

            //toggle.Click += new RoutedEventHandler(toggle_Click);
        }

        int SuspendChange = 0;

        public void DoUpdate(PresetItem item)
        {
            this.item = item;

            SuspendChange++;

            ImgFunc.GetIconAsync(item.GetIcon(), icon.Width, (ImageSource src) => {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(new Action(() =>
                    {
                        icon.Source = src;
                    }));
                }
                return 0;
            });

            //name.Content = process.Name;
            name.Text = item.Name;

            /*info.Text = preset.Description;

            toggle.IsChecked = preset.State;
            */

            SuspendChange--;
        }

        /*private void toggle_Click(object sender, RoutedEventArgs e)
        {
            
        }*/

        private bool mHasFocus = false;

        public bool GetFocus() { return mHasFocus; }

        public void SetFocus(bool set = true)
        {
            this.rect.StrokeThickness = 2;
            this.rect.Stroke = set ? new SolidColorBrush(Color.FromArgb(255, 51, 153, 255)) : null;
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 153, 204, 255) : Colors.Transparent);
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 230, 240, 255) : Colors.Transparent);

            mHasFocus = set;
        }

        private void rect_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }

        private void name_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            if (!name.IsReadOnly)
            {
                //name.BorderBrush = Brushes.Transparent;
                name.IsReadOnly = true;

                item.Name = name.Text;
            }
        }

        private void name_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            //mBorderBrush = name.BorderBrush;
            name.IsReadOnly = false;
        }

        private void name_KeyDown(object sender, KeyEventArgs e)
        {
            if (SuspendChange > 0)
                return;

            if (e.Key == Key.Enter || e.Key == Key.Escape)
            {
                //name.BorderBrush = Brushes.Transparent;
                name.IsReadOnly = true;
                if (e.Key == Key.Enter)
                {
                    item.Name = name.Text;
                }
            }
        }

        private void icon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (SuspendChange > 0)
                return;
            
            if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2) // double click
            {
                string iconFile = ProgramControl.OpenIconPicker(item.Icon);
                if (iconFile != null)
                {
                    item.Icon = iconFile;
                    //App.presets.UpdatePreset(preset);
                    icon.Source = ImgFunc.GetIcon(item.Icon, icon.Width);
                }
            }
        }
        
    }
}
