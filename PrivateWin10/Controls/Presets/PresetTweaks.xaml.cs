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
    /// Interaction logic for PresetTweaks.xaml
    /// </summary>
    public partial class PresetTweaks : UserControl
    {
        ControlList<TweakItemControl, TweakPreset.SingleTweak> TweakList;

        TweakPreset TweakPreset;

        public PresetTweaks()
        {
            InitializeComponent();

            TweakList = new ControlList<TweakItemControl, TweakPreset.SingleTweak>(this.itemScroll, (item) => 
                { 
                    var ctrl = new TweakItemControl(item);
                    ctrl.ItemChanged += Ctrl_ItemChanged;
                    return ctrl;
                }, (item) => item.TweakName);
        }

        private void Ctrl_ItemChanged(object sender, EventArgs e)
        {
            TweakItemControl ctrl = (TweakItemControl)sender;
            TweakPreset.Tweaks[ctrl.item.TweakName] = ctrl.item;
        }

        public void SetItem(TweakPreset tweakPreset)
        {
            TweakPreset = tweakPreset;

            groupName.Content = TweakPreset.TweakGroup;

            TweakList.UpdateItems(null);
            TweakList.UpdateItems(TweakPreset.Tweaks.Values.ToList());
        }
    }
}
