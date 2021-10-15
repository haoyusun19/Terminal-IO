﻿using ReactiveUI;
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
            GattSevice = gattDeviceService;
            Id = GattSevice.DeviceId;
            ServiceName = DisplayHelper.GetServiceName(GattSevice);
        }

        public GattDeviceService GattSevice { get; set; }

        [Reactive]
        public string ServiceName
        {
            get;
            set;
        }

        public string Id
        {
            get;
            set;
        }
    }
}
