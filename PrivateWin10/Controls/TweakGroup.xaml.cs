using CSharpControls.Wpf;
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

namespace PrivateWin10
{
    /// <summary>
    /// Interaction logic for TweakGroup.xaml
    /// </summary>
    public partial class TweakGroup : UserControl
    {
        public event RoutedEventHandler Click;
        public event RoutedEventHandler ReqSU;

        internal class TweakStat
        {
            public int total = 0;
            public int enabled = 0;
        }

        Group myGroup;

        Dictionary<TweakType, ContentControl> boxes = new Dictionary<TweakType, ContentControl>();

        public TweakGroup(Group group)
        {
            myGroup = group;

            InitializeComponent();

            Dictionary<TweakType, TweakStat> tweaks = new Dictionary<TweakType, TweakStat>();

            foreach (Tweak tweak in group.Tweaks)
            {
                if (!tweak.IsAvailable())
                    continue;

                tweak.StatusChanged += OnStatusChanged;

                TweakStat stat = null;
                if (!tweaks.TryGetValue(tweak.Type, out stat))
                {
                    stat = new TweakStat();
                    tweaks.Add(tweak.Type, stat);
                }

                stat.total++;
                if (tweak.Test())
                    stat.enabled++;
            }

            toggle.Click += new RoutedEventHandler(toggle_Checked);

            int height = 32;

            int i = 0;
            foreach (TweakType type in tweaks.Keys)
            {
                TweakStat stat = tweaks[type];

                ContentControl item;
                /*if (tweaks.Count > 1)
                {
                    CheckBox check = new CheckBox();
                    check.Click += new RoutedEventHandler(rect_Click);
                    check.Click += new RoutedEventHandler(check_Checked);
                    check.Tag = type;
                    item = check;
                }
                else*/
                {
                    item = new Label();
                    item.Padding = new Thickness(17, 0, 0, 0);
                    item.MouseDown += new MouseButtonEventHandler(rect_Click);
                }
                boxes.Add(type, item);

                item.Content = string.Format("{0}", Tweak.GetTypeStr(type));

                item.Height = 16;
                item.VerticalAlignment = VerticalAlignment.Top;
                item.HorizontalAlignment = HorizontalAlignment.Left;

                //check.IsEnabled

                checks.Children.Add(item);
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(item.Height);
                checks.RowDefinitions.Add(row);
                Grid.SetRow(item, i++);
                //Grid.SetColumn(item, 1);

                height += (int)item.Height;
            }

            this.Height = height;

            OnStatusChanged(null, null);

            toggle.Click += new RoutedEventHandler(rect_Click);
            rect.MouseDown += new MouseButtonEventHandler(rect_Click);
            label.MouseDown += new MouseButtonEventHandler(rect_Click);
        }

        private void toggle_Checked(object sender, RoutedEventArgs e)
        {
            if (!AdminFunc.IsAdministrator())
            {
                ReqSU?.Invoke(this, e);
                OnStatusChanged(null, null);
                return;
            }

            /*if ((sender as ToggleSwitch).IsChecked == null)
                (sender as ToggleSwitch).IsChecked = true;*/

            bool state = (bool)(sender as ToggleSwitch).IsChecked;

            foreach (Tweak tweak in myGroup.Tweaks)
            {
                if (!tweak.IsAvailable())
                    continue;

                if (state && (tweak.Sellected == null ? !tweak.Optional : (bool)tweak.Sellected))
                    tweak.Apply();
                else if(!state)
                    tweak.Undo();
            }
        }

        /*private void check_Checked(object sender, RoutedEventArgs e)
        {
            TweakType type = (TweakType)(sender as CheckBox).Tag;
            bool state = (bool)(sender as CheckBox).IsChecked;

            foreach (Tweak tweak in myGroup.Tweaks)
            {
                if (!tweak.IsAvailable())
                    continue;

                if (tweak.Type != type)
                    continue;

                if (state && (tweak.Sellected == null ? !tweak.Optional : (bool)tweak.Sellected))
                    tweak.Apply();
                else if(!state)
                    tweak.Undo();
            }
        }*/

        void OnStatusChanged(object sender, EventArgs arg)
        {
            Dictionary<TweakType, TweakStat> stats = new Dictionary<TweakType, TweakStat>();

            int active = 0;
            int selected = 0;
            foreach (Tweak tweak in myGroup.Tweaks)
            {
                if (!tweak.IsAvailable())
                    continue;

                if (tweak.Test())
                    active++;

                if(tweak.Sellected == null ? !tweak.Optional : (bool)tweak.Sellected)
                    selected++;

                TweakStat stat = null;
                if (!stats.TryGetValue(tweak.Type, out stat))
                {
                    stat = new TweakStat();
                    stats.Add(tweak.Type, stat);
                }

                stat.total++;
                if (tweak.Test())
                    stat.enabled++;

            }
            if (active == 0)
                toggle.IsChecked = false;
            else if (active >= selected)
                toggle.IsChecked = true;
            else
                toggle.IsChecked = null;


            foreach (TweakType type in stats.Keys)
            {
                TweakStat stat = stats[type];

                boxes[type].Content = string.Format("{0} {1}/{2}", Tweak.GetTypeStr(type), stat.enabled, stat.total);
                /*if (stat.enabled == 0)
                    boxes[type].IsChecked = false;
                else if (stat.enabled == stat.total)
                    boxes[type].IsChecked = true;
                else
                    boxes[type].IsChecked = null;*/
            }    
        }

        public void SetFocus(bool set = true)
        {
            this.rect.StrokeThickness = 2;
            this.rect.Stroke = set ? new SolidColorBrush(Color.FromArgb(255, 51, 153, 255)) : null;
            //this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 153, 204, 255) : Colors.Transparent);
            this.rect.Fill = new SolidColorBrush(set ? Color.FromArgb(255, 230, 240, 255) : Colors.Transparent);
        }

        private void rect_Click(object sender, RoutedEventArgs e)
        {
            Click?.Invoke(this, e);
        }
    }
}
