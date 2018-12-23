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

namespace PrivateWin10.Pages
{
    /// <summary>
    /// Interaction logic for PrivacyPage.xaml
    /// </summary>
    public partial class PrivacyPage : UserControl, IUserPage
    {
        private Dictionary<string, TweakControl> myTweaks = new Dictionary<string, TweakControl>();
        private Dictionary<string, TweakGroupe> myGroupes = new Dictionary<string, TweakGroupe>();

        public bool showAll = false; // todo

        public PrivacyPage()
        {
            InitializeComponent();
        }

        public void OnShow()
        {
            for (int i = 0; i < App.tweaks.Categorys.Count; ++i)
            {
                Category cat = App.tweaks.Categorys[i];
                ToggleButton item = new ToggleButton();
                item.Content = cat.Label;
                item.Tag = cat;
                item.VerticalAlignment = VerticalAlignment.Top;
                item.HorizontalAlignment = HorizontalAlignment.Stretch;
                item.Height = 32;
                item.Background = new SolidColorBrush(Color.FromArgb(255, 235, 235, 235));
                item.FontWeight = FontWeights.Bold;
                item.Margin = new Thickness(1, 1, 1, 1);
                item.Click += new RoutedEventHandler(category_Click);
                item.MouseDoubleClick += new MouseButtonEventHandler(category_dblClick);
                item.Style = (Style)FindResource(ToolBar.ToggleButtonStyleKey);

                if (!showAll && !cat.IsAvailable())
                    item.IsEnabled = false;

                this.catGrid.Children.Add(item);
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(item.Height + 2);
                this.catGrid.RowDefinitions.Add(row);
                Grid.SetRow(item, i);
                //Grid.SetColumn(item, 1);
            }

            category_Click(this.catGrid.Children[0], null);
        }

        public void OnHide()
        {
            this.catGrid.Children.Clear();
            this.catGrid.RowDefinitions.Clear();
        }

        public void OnClose()
        {
        }

        void category_dblClick(object sender, RoutedEventArgs e)
        {
            Category cat = (Category)(sender as ToggleButton).Tag;
            if (MessageBox.Show(string.Format("Apply all {0} tweaks?", cat.Label), "Private Win10", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            for (int i = 0; i < cat.Groupes.Count; ++i)
            {
                Groupe groupe = cat.Groupes[i];
                if (!groupe.IsAvailable())
                    continue;

                foreach (Tweak tweak in groupe.Tweaks)
                {
                    if (!tweak.IsAvailable())
                        continue;

                    if (tweak.Sellected == null ? !tweak.Optional : (bool)tweak.Sellected)
                        tweak.Apply();
                }
            }
        }

        void category_Click(object sender, RoutedEventArgs e)
        {
            Category cat = (Category)(sender as ToggleButton).Tag;

            foreach (ToggleButton curBtn in this.catGrid.Children)
            {
                curBtn.IsChecked = curBtn == sender;
                curBtn.Background = new SolidColorBrush(((bool)curBtn.IsChecked) ? Color.FromArgb(255, 230, 240, 255) : Color.FromArgb(255, 235, 235, 235));
            }

            this.groupeGrid.Children.Clear();
            this.groupeGrid.RowDefinitions.Clear();

            this.tweakGrid.Children.Clear();
            this.tweakGrid.RowDefinitions.Clear();

            for (int i = 0; i < cat.Groupes.Count; ++i)
            {
                Groupe groupe = cat.Groupes[i];
                if (!showAll && !groupe.IsAvailable())
                    continue;
                TweakGroupe item;
                if (!myGroupes.TryGetValue(groupe.Label, out item))
                {
                    item = new TweakGroupe(groupe);
                    myGroupes.Add(groupe.Label, item);
                    //item.MouseDown += new MouseButtonEventHandler(groupe_Click);
                    item.Click += new RoutedEventHandler(groupe_Click);
                    item.ReqSU += new RoutedEventHandler(req_su);
                }
                item.label.Content = groupe.Label;
                item.Tag = groupe;
                item.VerticalAlignment = VerticalAlignment.Top;
                item.HorizontalAlignment = HorizontalAlignment.Stretch;
                item.Margin = new Thickness(1, 1, 1, 1);

                if (!groupe.IsAvailable())
                    item.toggle.IsEnabled = false;

                this.groupeGrid.Children.Add(item);
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(item.Height + 2);
                this.groupeGrid.RowDefinitions.Add(row);
                Grid.SetRow(item, groupeGrid.RowDefinitions.Count - 1);
                //Grid.SetColumn(item, 1);
            }
        }

        void groupe_Click(object sender, RoutedEventArgs e)
        {
            Groupe groupe = (Groupe)(sender as TweakGroupe).Tag;

            foreach (TweakGroupe curBtn in this.groupeGrid.Children)
                curBtn.SetFocus(curBtn == sender);

            this.tweakGrid.Children.Clear();
            this.tweakGrid.RowDefinitions.Clear();

            for (int i = 0; i < groupe.Tweaks.Count; ++i)
            {
                Tweak tweak = groupe.Tweaks[i];
                if (!showAll && !tweak.IsAvailable())
                    continue;
                TweakControl item;
                if (!myTweaks.TryGetValue(groupe.Label + "|" + tweak.Label, out item))
                {
                    item = new TweakControl(tweak);
                    myTweaks.Add(groupe.Label + "|" + tweak.Label, item);
                    //item.MouseDown += new MouseButtonEventHandler(tweak_Click);
                    item.Click += new RoutedEventHandler(tweak_Click);
                    item.ReqSU += new RoutedEventHandler(req_su);
                }
                item.label.Content = tweak.Label;
                item.Tag = tweak;
                item.VerticalAlignment = VerticalAlignment.Top;
                item.HorizontalAlignment = HorizontalAlignment.Stretch;
                item.Margin = new Thickness(1, 1, 1, 1);

                if (!tweak.IsAvailable())
                    item.toggle.IsEnabled = false;

                this.tweakGrid.Children.Add(item);
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(item.Height + 2);
                this.tweakGrid.RowDefinitions.Add(row);
                Grid.SetRow(item, tweakGrid.RowDefinitions.Count - 1);
                //Grid.SetColumn(item, 1);
            }
        }

        void tweak_Click(object sender, RoutedEventArgs e)
        {
            Tweak tweak = (Tweak)(sender as TweakControl).Tag;

            foreach (TweakControl curBtn in this.tweakGrid.Children)
                curBtn.SetFocus(curBtn == sender);

            // todo:
        }

        void req_su(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_admin_prompt", App.mName), App.mName, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                App.Restart(true);
        }
    }
}
