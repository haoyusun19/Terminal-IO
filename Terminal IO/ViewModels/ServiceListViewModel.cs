using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Terminal_IO.ViewModels
{
    public class ServiceListViewModel : ReactiveObject
    {
        public static ServiceListViewModel Instance { get; } = new Lazy<ServiceListViewModel>(() => new ServiceListViewModel()).Value;

        public BluetoothLEDevice BluetoothLeDevice
        {
            get;
            set;
        }

        [Reactive]
        public ObservableCollection<ServiceViewModel> Sevices
        {
            get;
            set;
        }

        public ServiceListViewModel()
        {
            Sevices = new ObservableCollection<ServiceViewModel>();
        }

        public async Task GetServices(string deviceId)
        {
            Sevices.Clear();
            BluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceId);

            if (BluetoothLeDevice != null)
            {
                // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
                GattDeviceServicesResult result = await BluetoothLeDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    var services = result.Services;
                    foreach (var service in services)
                    {
                        Debug.WriteLine(String.Format("Added {0}", service.DeviceId));
                        Sevices.Add(new ServiceViewModel(service));
                    }                    
                }
            }
            else
            {
                Debug.WriteLine("Can not get device");
            }
        }

        public void ClearBluetoothLEDevice()
        {
            BluetoothLeDevice?.Dispose();
            BluetoothLeDevice = null;
        }
    }
}
