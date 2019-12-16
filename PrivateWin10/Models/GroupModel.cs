using PrivateWin10.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PrivateWin10
{
    public class GroupModel
    {
        private static GroupModel mInstance = null;
        public static GroupModel GetInstance()
        {
            if (mInstance == null)
                mInstance = new GroupModel();
            return mInstance;
        }

        public ObservableCollection<ContentControl> Groups { get; set; }

        public GroupModel()
        {
            Groups = new ObservableCollection<ContentControl>();

            HashSet<string> knownGroups = new HashSet<string>();
            Dictionary<Guid, List<FirewallRuleEx>> rules = App.client.GetRules();
            foreach (var ruleSet in rules)
            {
                foreach (FirewallRule rule in ruleSet.Value)
                {
                    if (rule.Grouping != null && rule.Grouping.Length > 0)
                    {
                        string temp = App.GetResourceStr(rule.Grouping);
                        if (temp.Substring(0, 1) == "@")
                            continue; // dont list unresolved names
                        knownGroups.Add(temp);
                    }
                }
            }

            foreach (string group in knownGroups)
                Groups.Add(new ContentControl() { Tag = group, Content = group});
        }

        public IEnumerable GetGroups()
        {
            ListCollectionView lcv = new ListCollectionView(Groups);
            lcv.SortDescriptions.Add(new SortDescription("Content", ListSortDirection.Ascending));
            return lcv;
        }
    }
}
