using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal_IO.Service;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Terminal_IO.ViewModels
{
    public class ServiceViewModel : ReactiveObject
    {
        public ServiceViewModel(GattDeviceService gattDeviceService)
        {
            SeviceInformation = gattDeviceService;
            ServiceName = DisplayHelper.GetServiceName(SeviceInformation);
        }

        public GattDeviceService SeviceInformation { get; set; }

        [Reactive]
        public string ServiceName
        {
            get;
            set;
        }
    }
}
