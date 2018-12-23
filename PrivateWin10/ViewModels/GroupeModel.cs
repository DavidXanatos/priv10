using PrivateWin10.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PrivateWin10.ViewModels
{
    public class GroupeModel
    {
        private static GroupeModel mInstance = null;
        public static GroupeModel GetInstance()
        {
            if (mInstance == null)
                mInstance = new GroupeModel();
            return mInstance;
        }

        public ObservableCollection<ContentControl> Groupes { get; set; }

        public GroupeModel()
        {
            Groupes = new ObservableCollection<ContentControl>();

            HashSet<string> knownGroupes = new HashSet<string>();
            foreach (FirewallRule rule in App.itf.GetRules())
            {
                if(rule.Grouping != null && rule.Grouping.Length > 0)
                    knownGroupes.Add(rule.Grouping);
            }

            foreach (string groupe in knownGroupes)
            {
                string temp = groupe;
                if (temp.Substring(0, 1) == "@")
                    temp = MiscFunc.GetResourceStr(temp);

                Groupes.Add(new ContentControl() { Tag = groupe, Content = temp});
            }
        }
    }
}
