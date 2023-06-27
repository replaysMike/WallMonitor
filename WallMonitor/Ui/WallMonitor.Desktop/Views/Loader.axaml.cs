using Avalonia.Controls;
using WallMonitor.Desktop.ViewModels;

namespace WallMonitor.Desktop.Views
{
    public partial class Loader : UserControl
    {
        public Loader()
        {
            InitializeComponent();
            DataContext = new LoaderViewModel();
        }
    }
}
