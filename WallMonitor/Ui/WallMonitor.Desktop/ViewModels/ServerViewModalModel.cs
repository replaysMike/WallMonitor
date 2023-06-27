using ReactiveUI;
using WallMonitor.Desktop.Models;

namespace WallMonitor.Desktop.ViewModels
{
    public class ServerViewModalModel : ViewModelBase
    {
        private Server? _server;
        public Server? Server
        {
            get => _server;
            set => this.RaiseAndSetIfChanged(ref _server, value);
        }

        private ServiceState? _selectedService;
        public ServiceState? SelectedService
        {
            get => _selectedService;
            set => this.RaiseAndSetIfChanged(ref _selectedService, value);
        }

        public int SelectedTimeScale
        {
            get => _selectedService?.TimeScale ?? 0;
            set
            {
                if (_selectedService != null)
                    _selectedService.TimeScale = value;
            }
        }


        public ServerViewModalModel()
        {
        }

        public ServerViewModalModel(Server server, ServiceState serviceState)
        {
            Server = server;
            SelectedService = serviceState;
            SelectedTimeScale = 0;
        }
    }
}
