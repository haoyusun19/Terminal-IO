﻿using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace Terminal_IO.ViewModels
{
    public class DeviceListViewModel : ReactiveObject
    {
        private DeviceWatcher deviceWatcher;

        public static DeviceListViewModel Instance { get; } = new Lazy<DeviceListViewModel>(() => new DeviceListViewModel()).Value;

        [Reactive]
        public ObservableCollection<DeviceViewModel> KnownDevices
        {
            get;
            set;
        }

        public DeviceListViewModel()
        {
            KnownDevices = new ObservableCollection<DeviceViewModel>();

            // Additional properties we would like about the device.
            // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // BT_Code: Example showing paired and non-paired in a single query.
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";

            deviceWatcher =
                    DeviceInformation.CreateWatcher(
                        aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);

            // Register event handlers before starting the watcher.
            //deviceWatcher.Added += DeviceWatcher_Added;
            //deviceWatcher.Updated += DeviceWatcher_Updated;
            //deviceWatcher.Removed += DeviceWatcher_Removed;
            //deviceWatcher.EnumerationCompleted += DeviceWatcher_EnumerationCompleted;
            //deviceWatcher.Stopped += DeviceWatcher_Stopped;
            // Start over with an empty collection.
            KnownDevices.Clear();

            // Start the watcher. Active enumeration is limited to approximately 30 seconds.
            // This limits power usage and reduces interference with other Bluetooth activities.
            // To monitor for the presence of Bluetooth LE devices for an extended period,
            // use the BluetoothLEAdvertisementWatcher runtime class. See the BluetoothAdvertisement
            // sample for an example.
            //deviceWatcher.Start();
        }

        private DeviceViewModel FindBluetoothLEDevice(string id)
        {
            foreach (DeviceViewModel bleDeviceDisplay in KnownDevices)
            {
                if (bleDeviceDisplay.Id == id)
                {
                    return bleDeviceDisplay;
                }
            }
            return null;
        }

        private void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            Debug.WriteLine(String.Format("Added {0}{1}", deviceInfo.Id, deviceInfo.Name));

            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == deviceWatcher)
            {
                // Make sure device isn't already present in the list.
                if (FindBluetoothLEDevice(deviceInfo.Id) == null)
                {
                    if (deviceInfo.Name != string.Empty)
                    {
                        
                        // If device has a friendly name display it immediately.
                        KnownDevices.Add(new DeviceViewModel(deviceInfo));
                    }                    
                }

            }
        }

        private void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            Debug.WriteLine(String.Format("Updated {0}{1}", deviceInfoUpdate.Id, ""));

            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == deviceWatcher)
            {
                DeviceViewModel bleDevice = FindBluetoothLEDevice(deviceInfoUpdate.Id);
                if (bleDevice != null)
                {
                    // Device is already being displayed - update UX.
                    bleDevice.Update(deviceInfoUpdate);
                    return;
                }              
            }
        }

        /*
        private void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {
            Debug.WriteLine(String.Format("Removed {0}{1}", deviceInfoUpdate.Id, ""));

            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == deviceWatcher)
            {
                // Find the corresponding DeviceInformation in the collection and remove it.
                DeviceViewModel bleDevice = FindBluetoothLEDevice(deviceInfoUpdate.Id);
                KnownDevices.Remove(bleDevice);
            }
        }

        /*
        private void DeviceWatcher_EnumerationCompleted(DeviceWatcher sender, object e)
        {
            
        }

        private void DeviceWatcher_Stopped(DeviceWatcher sender, object e)
        {
            
        }
        */

        public void StopBleDeviceWatcher()
        {
            if (deviceWatcher != null)
            {
                // Unregister the event handlers.
                deviceWatcher.Added -= DeviceWatcher_Added;
                deviceWatcher.Updated -= DeviceWatcher_Updated;
                //deviceWatcher.Removed -= DeviceWatcher_Removed;
                //deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                //deviceWatcher.Stopped -= DeviceWatcher_Stopped;
                // Stop the watcher.
                deviceWatcher.Stop();
            }
        }

        public void StartBleDeviceWatcher()
        {
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            //KnownDevices.Clear();
            // Start the watcher.
            deviceWatcher.Start();
        }
    }
}
