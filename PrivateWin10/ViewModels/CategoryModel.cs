using PrivateWin10.Controls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PrivateWin10.ViewModels
{
    public class CategoryModel
    {
        public ObservableCollection<Category> Categorys { get; set; }

        public CategoryModel()
        {
            Categorys = new ObservableCollection<Category>();

            HashSet<string> knownCats = new HashSet<string>();
            foreach (Program entry in App.itf.GetPrograms())
            {
                if(entry.config.Category != null && entry.config.Category.Length > 0)
                    knownCats.Add(entry.config.Category);
            }

            foreach (string cat in knownCats)
                Categorys.Add(new Category() { Content = cat, Tag = cat, Groupe = Translate.fmt("cat_cats") });

            Categorys.Add(new Category() { SpecialCat = Category.Special.SetNone, Content = Translate.fmt("cat_none"), Tag = "", Groupe = Translate.fmt("cat_other") });
            Categorys.Add(new Category() { SpecialCat = Category.Special.AddNew, Content = Translate.fmt("cat_new"), Tag = true, Groupe = Translate.fmt("cat_other") });

        }

        public class Category : ContentControl
        {
            public enum Special
            {
                No = 0,
                AddNew,
                SetNone,
                Separator
            }
            public Special SpecialCat = Special.No;
            public string Groupe { get; set; }
        }

        public IEnumerable GetCategorys()
        {
            ListCollectionView lcv = new ListCollectionView(Categorys);
            lcv.GroupDescriptions.Add(new PropertyGroupDescription("Groupe"));
            return lcv;
        }
    }
}
