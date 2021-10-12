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
            Id = deviceInfoIn.Id;
            Name = deviceInfoIn.Name;
            IsPaired = deviceInfoIn.Pairing.IsPaired;
        }

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
    }
}
