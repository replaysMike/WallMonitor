using Avalonia.Controls;
using SystemMonitor.Desktop.ViewModels;

namespace SystemMonitor.Desktop.Views
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
