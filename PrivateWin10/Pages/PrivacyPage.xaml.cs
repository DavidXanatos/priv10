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
using TweakEngine;

namespace PrivateWin10.Pages
{
    /// <summary>
    /// Interaction logic for PrivacyPage.xaml
    /// </summary>
    public partial class PrivacyPage : UserControl, IUserPage
    {
        //private Dictionary<Guid, TweakManager.Tweak> TweakList = null;

        //private Dictionary<string, ToggleButton> Categories = new Dictionary<string, ToggleButton>();
        private Dictionary<string, TweakGroup> Groups = new Dictionary<string, TweakGroup>();
        private Dictionary<string, TweakControl> Tweaks = new Dictionary<string, TweakControl>();

        public bool showAll = false; // todo

        public PrivacyPage()
        {
            InitializeComponent();

            App.tweaks.TweakChanged += OnTweakChange;
        }

        public void OnTweakChange(object sender, TweakManager.TweakEventArgs args)
        {
            // todo xxxxxxx   <<<<<<<<<<<< update display

            if(App.MainWnd != null)
                App.MainWnd.notificationWnd.NotifyTweak(args);
        }

        public void OnShow()
        {
            //App.tweaks.TestTweaks(); // that takes over half a second

            // initialize tweak list on first show
            if (this.catGrid.Children.Count > 0)
                return;

            foreach (TweakPresets.Category cat in App.tweaks.Categorys.Values)
            {
                ToggleButton item = new ToggleButton();
                item.Content = cat.Name;
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
                Grid.SetRow(item, this.catGrid.Children.Count - 1);
                //Grid.SetColumn(item, 1);
            }
        
            category_Click(this.catGrid.Children[0], null);
        }

        public void OnHide()
        {
        }

        public void OnClose()
        {
        }

        void category_Click(object sender, RoutedEventArgs e)
        {
            TweakPresets.Category cat = (TweakPresets.Category)(sender as ToggleButton).Tag;

            foreach (ToggleButton curBtn in this.catGrid.Children)
            {
                curBtn.IsChecked = curBtn == sender;
                curBtn.Background = new SolidColorBrush(((bool)curBtn.IsChecked) ? Color.FromArgb(255, 230, 240, 255) : Color.FromArgb(255, 235, 235, 235));
            }

            this.groupGrid.Children.Clear();
            this.groupGrid.RowDefinitions.Clear();

            this.tweakGrid.Children.Clear();
            this.tweakGrid.RowDefinitions.Clear();

            foreach (TweakPresets.Group group in cat.Groups.Values)
            {
                if (!showAll && !group.IsAvailable())
                    continue;

                TweakGroup item;
                if (!Groups.TryGetValue(group.Name, out item))
                {
                    item = new TweakGroup(group);
                    Groups.Add(group.Name, item);
                    //item.MouseDown += new MouseButtonEventHandler(group_Click);
                    item.Click += new RoutedEventHandler(group_Click);
                    item.Toggle += new RoutedEventHandler(group_Toggle);
                    //item.ReqSU += new RoutedEventHandler(req_su);

                    item.label.Content = group.Name;
                    item.Tag = group;
                    item.VerticalAlignment = VerticalAlignment.Top;
                    item.HorizontalAlignment = HorizontalAlignment.Stretch;
                    item.Margin = new Thickness(1, 1, 1, 1);

                    if (!group.IsAvailable())
                        item.toggle.IsEnabled = false;
                }

                item.Update(); // note: this tests all tweaks in the groupe

                this.groupGrid.Children.Add(item);
                RowDefinition row = new RowDefinition();
                row.Height = new GridLength(item.Height + 2);
                this.groupGrid.RowDefinitions.Add(row);
                Grid.SetRow(item, groupGrid.RowDefinitions.Count - 1);
                //Grid.SetColumn(item, 1);
            }
        }

        void group_Click(object sender, RoutedEventArgs e)
        {
            TweakPresets.Group group = (TweakPresets.Group)(sender as TweakGroup).Tag;

            foreach (TweakGroup curBtn in this.groupGrid.Children)
                curBtn.SetFocus(curBtn == sender);

            this.tweakGrid.Children.Clear();
            this.tweakGrid.RowDefinitions.Clear();

            foreach (TweakList.Tweak tweak in group.Tweaks.Values)
            {
                if (!showAll && !tweak.IsAvailable())
                    continue;
                TweakControl item;
                if (!Tweaks.TryGetValue(group.Name + "|" + tweak.Name, out item))
                {
                    item = new TweakControl(tweak);
                    Tweaks.Add(group.Name + "|" + tweak.Name, item);
                    //item.MouseDown += new MouseButtonEventHandler(tweak_Click);
                    item.Click += new RoutedEventHandler(tweak_Click);
                    item.Toggle += new RoutedEventHandler(tweak_Toggle);
                    //item.ReqSU += new RoutedEventHandler(req_su);

                    item.label.Content = tweak.Name;
                    item.Tag = tweak;
                    item.VerticalAlignment = VerticalAlignment.Top;
                    item.HorizontalAlignment = HorizontalAlignment.Stretch;
                    item.Margin = new Thickness(1, 1, 1, 1);

                    if (!tweak.IsAvailable())
                        item.toggle.IsEnabled = false;
                }

                item.Update();

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
            TweakList.Tweak tweak = (TweakList.Tweak)(sender as TweakControl).Tag;

            foreach (TweakControl curBtn in this.tweakGrid.Children)
                curBtn.SetFocus(curBtn == sender);
        }

        void group_Toggle(object sender, RoutedEventArgs e)
        {
            TweakGroup item = sender as TweakGroup;
            TweakPresets.Group group = item.Tag as TweakPresets.Group;

            if (TestAdmin())
            {
                if (item.oldValue == null)
                {
                    int active_count = 0;
                    List<TweakList.Tweak> to_be_fixed = new List<TweakList.Tweak>();
                    foreach (TweakList.Tweak tweak in group.Tweaks.Values)
                    {
                        if (tweak.Status == true)
                            active_count++;
                        else if (tweak.State != TweakList.Tweak.States.Unsellected)
                            to_be_fixed.Add(tweak);
                    }

                    if (active_count == 0)
                        ToggleGroup(group, TweakList.Tweak.States.Unsellected);
                    else // fix tweaks
                    {
                        foreach (TweakList.Tweak tweak in to_be_fixed)
                            ApplyTweak(tweak, null);
                    }
                }
                else
                    ToggleGroup(group, (bool)item.IsChecked ? TweakList.Tweak.States.SelGroupe : TweakList.Tweak.States.Unsellected);
            }

            UpdateView();
        }

        void tweak_Toggle(object sender, RoutedEventArgs e)
        {
            TweakControl item = sender as TweakControl;
            TweakList.Tweak tweak = item.Tag as TweakList.Tweak;

            if (tweak.usrLevel == false || TestAdmin())
            {
                ToggleTweak(tweak, (bool)item.IsChecked ? TweakList.Tweak.States.Sellected : TweakList.Tweak.States.Unsellected);
            }

            UpdateView();
        }

        void category_dblClick(object sender, RoutedEventArgs e)
        {
            TweakPresets.Category cat = (TweakPresets.Category)(sender as ToggleButton).Tag;
            if (MessageBox.Show(string.Format("Apply all {0} tweaks?", cat.Name), "Private Win10", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            if (!TestAdmin())
                return;

            foreach (TweakPresets.Group group in cat.Groups.Values)
            {
                ToggleGroup(group, TweakList.Tweak.States.SelGroupe, true); 
            }

            UpdateView();
        }

        void ToggleGroup(TweakPresets.Group group, TweakList.Tweak.States value, bool bOnlyRecommended = false)
        {
            foreach (TweakList.Tweak tweak in group.Tweaks.Values)
            {
                if(bOnlyRecommended && tweak.Hint != TweakList.Tweak.Hints.Recommended)
                    continue;
                if (value != TweakList.Tweak.States.Unsellected && tweak.Hint == TweakList.Tweak.Hints.Optional) // skip optional tweaks, thay are usually eider dangerouse or redundant
                    continue;

                ToggleTweak(tweak, value);
            }
        }

        void ToggleTweak(TweakList.Tweak tweak, TweakList.Tweak.States value)
        {
            if (value == TweakList.Tweak.States.Unsellected)
                UndoTweak(tweak);
            else
                ApplyTweak(tweak, value != TweakList.Tweak.States.Sellected);
        }

        /*void req_su(object sender, RoutedEventArgs e)
        {
            TestAdmin();
        }*/

        bool TestAdmin()
        {
            if (HasAdministrator())
                return true;

            if (MessageBox.Show(Translate.fmt("msg_admin_prompt", App.Title), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                App.Restart(true);
            return false;
        }

        void UpdateView()
        {
            foreach (var item in this.groupGrid.Children)
            {
                (item as TweakGroup).Update();
            }

            foreach (var item in this.tweakGrid.Children)
            {
                (item as TweakControl).Update();
            }
        }


        public static bool ApplyTweak(TweakList.Tweak tweak, bool? byUser)
        {
            return App.tweaks.ApplyTweak(tweak, byUser);
        }
        
        public static bool UndoTweak(TweakList.Tweak tweak)
        {
            return App.tweaks.UndoTweak(tweak);
        }

        public static bool TestTweak(TweakList.Tweak tweak)
        {
            return App.tweaks.TestTweak(tweak);
        }

        public static bool HasAdministrator()
        {
            return AdminFunc.IsAdministrator() || App.client.IsConnected();
        }
    }
}
