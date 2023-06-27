using Avalonia.Controls;
using Avalonia.Input;
using System;
using System.Diagnostics;
using WallMonitor.Desktop.Services;
using WallMonitor.Desktop.ViewModels;

namespace WallMonitor.Desktop.Views
{
    public partial class PaginatedView : UserControl
    {
        public static int PaginationHeight => 40;
        public MainWindowViewModel ViewModel => (MainWindowViewModel?)DataContext ?? new MainWindowViewModel();

        /// <summary>
        /// Get the current page of the paginated view
        /// </summary>
        public int CurrentPage => Carousel.SelectedIndex;

        public PaginatedView()
        {
            InitializeComponent();

            if (Design.IsDesignMode)
            {
                DataContext = new MainWindowViewModel();
            }
        }

        public void SwitchToPage(int pageNumber)
        {
            Debug.WriteLine($"Switching from {Carousel.SelectedIndex},{ViewModel.CurrentPage} to page {pageNumber}");
            //Carousel.SelectedIndex = pageNumber;
            ViewModel.CurrentPage = pageNumber;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            Debug.WriteLine("PaginatedView: DataContextChanged()");
            base.OnDataContextChanged(e);
        }

        private void Navigation_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (e.Source is not Button button) return;
            if (button.DataContext is ServerPageViewModel pageViewModel)
            {
                var navigateToPage = pageViewModel.PageNumber;
                ViewModel.CurrentPage = navigateToPage;
                ViewModel.UserClickedNavigation();
                //Carousel.SelectedIndex = ViewModel.CurrentPage = navigateToPage;
                AudioService.Instance.PlayClick();
            }
        }

        private void Navigation_Entered(object? sender, PointerEventArgs e)
        {
            if (e.Source is not Button button) return;
        }

        private void Navigation_Exited(object? sender, PointerEventArgs e)
        {
            if (e.Source is not Button button) return;
        }
    }
}
