using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Terminal_IO.ViewModels;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Terminal_IO.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DeviceListPage : Page
    {       

        public ObservableCollection<DeviceViewModel> KnownDevices = new ObservableCollection<DeviceViewModel>();

        private DeviceWatcher deviceWatcher;

        public DeviceListPage()
        {
            // Additional properties we would like about the device.
            // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
            string[] requestedProperties = { "System.Devices.Aep.DeviceAddress", "System.Devices.Aep.IsConnected", "System.Devices.Aep.Bluetooth.Le.IsConnectable" };

            // BT_Code: Example showing paired and non-paired in a single query.
            string aqsAllBluetoothLEDevices = "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\" AND System.ItemNameDisplay:~~\"" + "BM+S50" + "\")";

            deviceWatcher =
                    DeviceInformation.CreateWatcher(
                        aqsAllBluetoothLEDevices,
                        requestedProperties,
                        DeviceInformationKind.AssociationEndpoint);
            KnownDevices.Clear();
            this.InitializeComponent();
        }

        private void StartBleDeviceWatcher()
        {
            deviceWatcher.Added += DeviceWatcher_Added;
            deviceWatcher.Updated += DeviceWatcher_Updated;
            deviceWatcher.Removed += DeviceWatcher_Removed;
            deviceWatcher.Start();
        }

        private void StopBleDeviceWatcher()
        {
            if (deviceWatcher != null)
            {
                // Unregister the event handlers.
                deviceWatcher.Added -= DeviceWatcher_Added;
                deviceWatcher.Updated -= DeviceWatcher_Updated;
                deviceWatcher.Removed -= DeviceWatcher_Removed;
                //deviceWatcher.EnumerationCompleted -= DeviceWatcher_EnumerationCompleted;
                //deviceWatcher.Stopped -= DeviceWatcher_Stopped;
                // Stop the watcher.
                deviceWatcher.Stop();
            }
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

        private async void DeviceWatcher_Added(DeviceWatcher sender, DeviceInformation deviceInfo)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
            });
            
        }

        private async void DeviceWatcher_Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {          
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
            });
        }

        private async void DeviceWatcher_Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInfoUpdate)
        {          
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Debug.WriteLine(String.Format("Removed {0}{1}", deviceInfoUpdate.Id, ""));

                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == deviceWatcher)
                {
                    // Find the corresponding DeviceInformation in the collection and remove it.
                    DeviceViewModel bleDevice = FindBluetoothLEDevice(deviceInfoUpdate.Id);
                    KnownDevices.Remove(bleDevice);
                }
            });
        }

        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            
            var bleDevice = ResultsListView.SelectedItem as DeviceViewModel;

            // BT_Code: Pair the currently selected device.
            await bleDevice.DeviceInformation.Pairing.PairAsync();
            
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {          
            StopBleDeviceWatcher();
            // Save the selected device's ID for use in other scenarios.
            var bleDevice = ResultsListView.SelectedItem as DeviceViewModel;
            if (bleDevice != null)
            {
                (Application.Current as App).SelectedBleDeviceId = bleDevice.Id;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            StartBleDeviceWatcher();
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {                         
            this.Frame.Navigate(typeof(WorkPage));
        }
    }
}
