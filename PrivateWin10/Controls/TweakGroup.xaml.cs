using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        public Dictionary<string, TweakManager.Tweak> Tweaks = new Dictionary<string, TweakManager.Tweak>();

        public event RoutedEventHandler Click;
        public event RoutedEventHandler Toggle;
        //public event RoutedEventHandler ReqSU;

        public bool? oldValue;

        internal class TweakStat
        {
            public int total = 0;
            public int enabled = 0;
            public int undone = 0;
        }

        TweakStore.Group myGroup;

        Dictionary<TweakManager.TweakType, ContentControl> boxes = new Dictionary<TweakManager.TweakType, ContentControl>();

        public TweakGroup(TweakStore.Group group)
        {
            myGroup = group;

            InitializeComponent();

            HashSet<TweakManager.TweakType> tweakTypes = new HashSet<TweakManager.TweakType>();

            foreach (TweakManager.Tweak tweak in group.Tweaks.Values)
            {
                if (!tweak.IsAvailable())
                    continue;

                //tweak.StatusChanged += OnStatusChanged;

                if (!tweakTypes.Contains(tweak.Type))
                    tweakTypes.Add(tweak.Type);
            }

            toggle.Click += new RoutedEventHandler(toggle_Checked);

            int height = 32;

            int i = 0;
            foreach (TweakManager.TweakType type in tweakTypes)
            {

                ContentControl item;
                item = new Label();
                item.Padding = new Thickness(17, 0, 0, 0);
                item.MouseDown += new MouseButtonEventHandler(rect_Click);
                boxes.Add(type, item);

                item.Content = string.Format("{0}", TweakManager.Tweak.GetTypeStr(type));

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

            //OnStatusChanged(null, null);

            toggle.Click += new RoutedEventHandler(rect_Click);
            rect.MouseDown += new MouseButtonEventHandler(rect_Click);
            label.MouseDown += new MouseButtonEventHandler(rect_Click);
        }

        private void toggle_Checked(object sender, RoutedEventArgs e)
        {
            Toggle?.Invoke(this, e);

            /*if (!AdminFunc.IsAdministrator())
            {
                ReqSU?.Invoke(this, e);
                OnStatusChanged(null, null);
                return;
            }

            //if ((sender as ToggleSwitch).IsChecked == null)
            //    (sender as ToggleSwitch).IsChecked = true;

            bool state = (bool)(sender as ToggleButton).IsChecked;

            foreach (TweakManager.Tweak tweak in myGroup.Tweaks.Values)
            {
                if (!tweak.IsAvailable())
                    continue;

                if (state && (tweak.Sellected == null ? !tweak.Optional : (bool)tweak.Sellected))
                    tweak.Apply();
                else if(!state)
                    tweak.Undo();
            }*/
        }

        public bool? IsChecked { get { return toggle.IsChecked; } }

        public void Update()
        {
            Dictionary<TweakManager.TweakType, TweakStat> stats = new Dictionary<TweakManager.TweakType, TweakStat>();

            int active = 0;
            int changed = 0;
            foreach (TweakManager.Tweak tweak in myGroup.Tweaks.Values)
            {
                if (!tweak.IsAvailable())
                    continue;

                bool Status = tweak.Test();

                TweakStat stat = null;
                if (!stats.TryGetValue(tweak.Type, out stat))
                {
                    stat = new TweakStat();
                    stats.Add(tweak.Type, stat);
                }

                stat.total++;
                if (Status)
                {
                    stat.enabled++;
                    active++;
                }
                else if (tweak.State != TweakManager.Tweak.States.Unsellected)
                {
                    stat.undone++;
                    changed++;
                }

            }
            if (changed > 0)
                toggle.IsChecked = null;
            else if (active == 0)
                toggle.IsChecked = false;
            else 
                toggle.IsChecked = true;
            oldValue = toggle.IsChecked;

            foreach (TweakManager.TweakType type in stats.Keys)
            {
                TweakStat stat = stats[type];

                string aux = "";
                if (stat.undone != 0)
                    aux = Translate.fmt("tweak_undone", stat.undone);

                boxes[type].Content = string.Format("{0}: {1}/{2}{3}", TweakManager.Tweak.GetTypeStr(type), stat.enabled, stat.total, aux);
                //if (stat.enabled == 0)
                //    boxes[type].IsChecked = false;
                //else if (stat.enabled == stat.total)
                //    boxes[type].IsChecked = true;
                //else
                //    boxes[type].IsChecked = null;
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

        /*void OnStatusChanged(object sender, EventArgs arg)
        {
               Update(); 
        }*/

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
