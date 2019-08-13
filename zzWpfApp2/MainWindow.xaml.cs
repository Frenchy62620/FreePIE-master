using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Linq;
using System.ServiceProcess;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using ComboBox = System.Windows.Controls.ComboBox;

namespace zzWpfApp2
{
    public enum HairColor { White, Black, Brown, Red, Yellow };
    public partial class MainWindow : Window
    {
        
        public MainWindow()
        {

            var list = new List<int> { 1, 3, 5, 7, 8 };
            var c = list.Average();
            var list1 = new List<int> { 1, 2, 3, 4 };
           var cc = list.Aggregate((x, y) =>
           {
               System.Diagnostics.Debug.WriteLine($"x = {x}, y = {y}");
               return y;
           });
            int closest = list.Aggregate((x, y) => Math.Abs(x - c) < Math.Abs(y - c) ? x : y);
            int closest1 = list1.Aggregate((x, y) => Math.Abs(x - c) < Math.Abs(y - c) ? x : y);


            InitializeComponent();


            List<string> computers = new List<string> { Environment.MachineName, Environment.MachineName };
            TreeViewItem root = new TreeViewItem() { Title = "General Menu" };

            foreach (string computer in computers)
            {
                TreeViewItem childItem = new TreeViewItem() { Title = computer };
                
                foreach (ServiceController tempService in ServiceController.GetServices())
                {
                    TreeViewItem subchildItem = new TreeViewItem() {Title = tempService.DisplayName };                 
                    childItem.Items.Add(subchildItem);
                    subchildItem.Items.Add(new TreeViewItem() { Title = tempService.Status.ToString()});
                    subchildItem.Items.Add(new TreeViewItem() { Title = tempService.ServiceName});
                }
                root.Items.Add(childItem);
            }
            trvMenu.Items.Add(root);
        }

        public class TreeViewItem
        {
            public TreeViewItem()
            {
                this.Items = new ObservableCollection<TreeViewItem>();
            }

            public string Title { get; set; }

            public ObservableCollection<TreeViewItem> Items { get; set; }
        }

        public class DropDownTreeNode : System.Windows.Controls.TreeView
        {
            // *snip* Constructors go here

            private ComboBox m_ComboBox = new System.Windows.Controls.ComboBox();
            public ComboBox ComboBox
            {
                get
                {
                    this.m_ComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                    return this.m_ComboBox;
                }
                set
                {
                    this.m_ComboBox = value;
                    this.m_ComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                }
            }
        }
    }
}
