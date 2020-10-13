using MiscHelpers;
using QLicense;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PrivateWin10.Pages
{
    /// <summary>
    /// Interaction logic for ControlPage.xaml
    /// </summary>
    public partial class ControlPage : UserControl, IUserPage
    {
        ControlList<PresetControl, PresetGroup> PresetList;

        ControlList<PresetItemControl, PresetItem> PresetItemList;

        PresetGroup CurrentPreset = null;

        public ControlPage()
        {
            InitializeComponent();

            cmbUndo.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_undo_never"), Tag = 0 });
            cmbUndo.SelectedIndex = cmbUndo.Items.Count - 1; // default is 1h
            cmbUndo.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_undo", "5 min"), Tag = 5 * 60 });
            cmbUndo.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_undo", "15 min"), Tag = 15 * 60 });
            cmbUndo.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_undo", "1 h"), Tag = 60 * 60 });
            cmbUndo.Items.Add(new ComboBoxItem() { Content = Translate.fmt("lbl_undo", "24 h"), Tag = 24 * 60 * 60 });

            PresetList = new ControlList<PresetControl, PresetGroup>(this.presetScroll, (preset) => { return new PresetControl(preset); }, (preset) => preset.guid.ToString());
            PresetList.SingleSellection = false;

            PresetList.UpdateItems(App.presets.Presets.Values.ToList());

            PresetList.SelectionChanged += OnPresetGroupChanged;


            PresetItemList = new ControlList<PresetItemControl, PresetItem>(this.itemScroll, (item) => { return new PresetItemControl(item); }, (item) => item.guid.ToString());
            PresetItemList.SingleSellection = false;

            PresetItemList.SelectionChanged += OnPresetItemChanged;



            App.presets.PresetChange += OnPresetChanged;
        }

        public void OnShow()
        {
        }

        public void OnHide()
        {
        }

        public void OnClose()
        {
        }

        private void OnPresetGroupChanged(object sender, EventArgs e)
        {
            if (PresetList.SelectedItems.Count != 1)
                return;

            if (CurrentPreset != null && CurrentPreset.guid == PresetList.SelectedItems.First().preset.guid)
                return;

            CurrentPreset = PresetList.SelectedItems.First().preset.Clone();

            this.txtName.Text = CurrentPreset.Name;
            this.txtInfo.Text = CurrentPreset.Description;

            WpfFunc.CmbSelect(this.cmbUndo, CurrentPreset.AutoUndo.ToString());

            foreach(var item in CurrentPreset.Items.Values)
                item.Sync();

            PresetItemList.UpdateItems(CurrentPreset.Items.Values.ToList());
            CollapseAll();
        }

        private void CollapseAll()
        { 
            this.tweakItem.Visibility = Visibility.Collapsed;
            this.ruleItem.Visibility = Visibility.Collapsed;
            this.customItem.Visibility = Visibility.Collapsed;
        }

        private void BtnAddPreset_Click(object sender, RoutedEventArgs e)
        {
            InputWnd wnd = new InputWnd(Translate.fmt("msg_preset_name"), Translate.fmt("msg_preset_some"), App.Title);
            if (wnd.ShowDialog() != true || wnd.Value.Length == 0)
                return;

            var newPreset = new PresetGroup();
            newPreset.Name = wnd.Value;
            App.presets.UpdatePreset(newPreset);
        }

        private void BtnDelPreset_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show(Translate.fmt("msg_remove_items"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach(var preset in PresetList.SelectedItems)
                App.presets.RemovePreset(preset.preset.guid);
        }

        private void OnPresetItemChanged(object sender, EventArgs e)
        {
            if (PresetItemList.SelectedItems.Count != 1)
                return;

            PresetItem item = PresetItemList.SelectedItems.First().item;

            CollapseAll();
            if (item is TweakPreset)
            {
                this.tweakItem.Visibility = Visibility.Visible;
                this.tweakItem.SetItem(item as TweakPreset);
            }
            else if (item is FirewallPreset)
            {
                this.ruleItem.Visibility = Visibility.Visible;
                this.ruleItem.SetItem(item as FirewallPreset);
            }
            else if (item is CustomPreset)
            {
                this.customItem.Visibility = Visibility.Visible;
                this.customItem.SetItem(item as CustomPreset);
            }
        }

        private void OnPresetChanged(object sender, PresetManager.PresetChangeArgs args)
        {
            if (args.preset == null)
                PresetList.UpdateItems(App.presets.Presets.Values.ToList());
            else
                PresetList.UpdateItem(args.preset);
        }

        private void BtnApply_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null)
                return;

            CurrentPreset.Name = this.txtName.Text;
            CurrentPreset.Description = this.txtInfo.Text;

            CurrentPreset.AutoUndo = (int)(cmbUndo.SelectedItem as ComboBoxItem).Tag;

            App.presets.UpdatePreset(CurrentPreset);
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            CurrentPreset = null;
            OnPresetGroupChanged(null, null);
        }

        private void BtnAddPresetItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null)
                return;

            List<string> Categories = new List<string>();
            Categories.Add(PresetType.Tweak.ToString());
            Categories.Add(PresetType.Firewall.ToString());
            //Categories.Add(PresetType.Custom.ToString()); // todo:

            InputWnd wnd = new InputWnd(Translate.fmt("msg_preset_item"), Categories, "", false, App.Title);
            if (wnd.ShowDialog() != true || wnd.Value.Length == 0)
                return;

            PresetItem newItem = null;

            if (wnd.Value == PresetType.Tweak.ToString())
            {
                List<string> TweakNames = new List<string>();
                var tweaks = App.tweaks.GetAllGroups().ToList();
                foreach (var tweakGroup in tweaks)
                    TweakNames.Add(tweakGroup.Name);

                InputWnd wnd2 = new InputWnd(Translate.fmt("msg_preset_tweak"), TweakNames, "", true, App.Title);
                if (wnd2.ShowDialog() != true || wnd2.Value.Length == 0)
                    return;

                var tweak = tweaks.Find(x => x.Name.Equals(wnd2.Value));
                if (tweak == null)
                    return;
                
                TweakPreset item = new TweakPreset();   
                item.Name = tweak.Name;
                item.TweakGroup = tweak.Name;
                newItem = item;
            }
            else if (wnd.Value == PresetType.Firewall.ToString())
            {
                List<string> ProgNames = new List<string>();
                var progSets = App.client.GetPrograms();
                foreach (var progSet in progSets)
                    ProgNames.Add(progSet.config.Name);

                InputWnd wnd2 = new InputWnd(Translate.fmt("msg_preset_progset"), ProgNames, "", true, App.Title);
                if (wnd2.ShowDialog() != true || wnd2.Value.Length == 0)
                    return;

                ProgramSet prog = progSets.Find(x => x.config.Name.Equals(wnd2.Value));
                if (prog == null)
                    return;
                
                FirewallPreset item = new FirewallPreset();   
                item.Name = prog.config.Name;
                item.ProgSetId = prog.guid;
                newItem = item;
            }
            else if (wnd.Value == PresetType.Custom.ToString())
            {
                InputWnd wnd2 = new InputWnd(Translate.fmt("msg_preset_item_name"), "", App.Title);                
                if (wnd2.ShowDialog() != true || wnd2.Value.Length == 0)
                    return;

                CustomPreset item = new CustomPreset();   
                item.Name = wnd2.Value;
                newItem = item;
            }

            if (newItem != null)
            {
                if (!newItem.Sync())
                {
                    MessageBox.Show(Translate.fmt("msg_preset_item_failed"), App.Title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    return;
                }

                CurrentPreset.Items.Add(newItem.guid, newItem);

                PresetItemList.UpdateItems(CurrentPreset.Items.Values.ToList());
                CollapseAll();
            }
        }

        private void BtnDelPresetItem_Click(object sender, RoutedEventArgs e)
        {
            if (CurrentPreset == null)
                return;

            if (MessageBox.Show(Translate.fmt("msg_remove_items"), App.Title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
                return;

            foreach (var item in PresetItemList.SelectedItems)
                CurrentPreset.Items.Remove(item.item.guid);


            PresetItemList.UpdateItems(CurrentPreset.Items.Values.ToList());
            CollapseAll();
        }

        private void BtnSyncItems_Click(object sender, RoutedEventArgs e)
        {
            if (PresetItemList.SelectedItems.Count > 0)
            {
                PresetItem item = PresetItemList.SelectedItems.First().item;
                item.Sync(true);

                OnPresetItemChanged(null, null);
            }
        }

        public static string SelectTweakName()
        { 
            List<string> PresetNames = new List<string>();
            foreach (var preset in App.presets.Presets.Values)
                PresetNames.Add(preset.Name);

            InputWnd wnd = new InputWnd(Translate.fmt("msg_preset_select"), PresetNames, "", true, App.Title);
            if (wnd.ShowDialog() != true || wnd.Value.Length == 0)
                return null;

            return wnd.Value;
        }
    }
}
