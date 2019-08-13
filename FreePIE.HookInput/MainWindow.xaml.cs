using System.IO.MemoryMappedFiles;
using System.Threading;
using System.Windows;

namespace FreePIE.HookInput
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            var hookstatus = int.Parse(App.mArgs[0]);
            InitializeComponent();
            if (hookstatus == 0)
                MessageBox.Show("no hook activated (see plugins option), the program will stop", "Hook Error");
            AllInputsCommander AIC = new AllInputsCommander(hookstatus);
            AIC.StartLoop();
            //System.Windows.Application.Current.Shutdown();
        }
    }
}
