using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Security.Policy;
using System.Windows;

namespace zzWpfApp1
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<User> Users { get; set; }
        public MainWindow()
        {
            List<User> users = new List<User>();
            users.Add(new User() { Name = "Donald Duck", ReaderType = ReaderType.Chief });
            users.Add(new User() { Name = "Mimmi Mouse", ReaderType = ReaderType.Staff });
            users.Add(new User() { Name = "Goofy", ReaderType = ReaderType.Officer });
            Users = new ObservableCollection<User>(users);
            InitializeComponent();
            //DataContext = new MainWindowViewModel();
            //DataContext = this;
            //Flag = true;
        }
        //private void ButtonClick(object sender, RoutedEventArgs e)
        //{
        //    Flag = true;
        //    OnPropertyChanged("Flag");
        //}
        public bool Flag { get; set; }
        //public event PropertyChangedEventHandler PropertyChanged;
        //protected void OnPropertyChanged(string property)
        //{
        //    PropertyChanged(this, new PropertyChangedEventArgs(property));
        //}
    }
}

