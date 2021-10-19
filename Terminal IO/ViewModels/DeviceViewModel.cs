using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace Terminal_IO.ViewModels
{
    public class DeviceViewModel : ReactiveObject
    {
        public DeviceViewModel(DeviceInformation deviceInfoIn)
        {
            DeviceInformation = deviceInfoIn;
            Id = DeviceInformation.Id;
            Name = DeviceInformation.Name;
            IsPaired = DeviceInformation.Pairing.IsPaired;
            IsConnected = (bool?)DeviceInformation.Properties["System.Devices.Aep.IsConnected"] == true;
            IsConnected = (bool?)DeviceInformation.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true;
            IsUnPaired = !DeviceInformation.Pairing.IsPaired;           
        }

        public DeviceInformation DeviceInformation { get; set; }

        [Reactive]
        public string Id
        {
            get;
            set;
        }

        [Reactive]
        public string Name
        {
            get;
            set;
        }

        [Reactive]
        public bool IsPaired
        {
            get;
            set;
        }

        [Reactive]
        public bool IsConnected
        {
            get;
            set;
        }

        [Reactive]
        public bool IsConnectable
        {
            get;
            set;
        }

        public bool IsUnPaired
        {
            get;
            set;
        }

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation.Update(deviceInfoUpdate);
            Id = DeviceInformation.Id;
            Name = DeviceInformation.Name;
            IsPaired = DeviceInformation.Pairing.IsPaired;
            IsConnected = (bool?)DeviceInformation.Properties["System.Devices.Aep.IsConnected"] == true;
            IsConnected = (bool?)DeviceInformation.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] == true;
            IsUnPaired = !DeviceInformation.Pairing.IsPaired;
        }
    }
}
