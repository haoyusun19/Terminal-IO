using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace Terminal_IO.ViewModels
{
    public class CharacteristicListViewModel : ReactiveObject
    {
        public static CharacteristicListViewModel Instance { get; } = new Lazy<CharacteristicListViewModel>(() => new CharacteristicListViewModel()).Value;

        public GattDeviceService GattDeviceService
        {
            get;
            set;
        }

        [Reactive]
        public ObservableCollection<CharacteristicViewModel> Characteristics
        {
            get;
            set;
        }

        public CharacteristicListViewModel()
        {
            Characteristics = new ObservableCollection<CharacteristicViewModel>();
        }

        public async Task GetCharacteristics(string deviceId)
        {
            Characteristics.Clear();
            GattDeviceService = await GattDeviceService.FromIdAsync(deviceId);

            if (GattDeviceService != null)
            {
                var accessStatus = await GattDeviceService.RequestAccessAsync();

                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                    // and the new Async functions to get the characteristics of unpaired devices as well. 
                    var result = await GattDeviceService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);

                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        var characteristics = result.Characteristics;
                        foreach (var characteristic in characteristics)
                        {
                            Debug.WriteLine(String.Format("Added {0}", characteristic.Uuid));
                            Characteristics.Add(new CharacteristicViewModel(characteristic));
                        }
                    }
                }                   
            }
            else
            {
                Debug.WriteLine("Can not get service");
            }
        }
             

        public void ClearGattDeviceService()
        {
            GattDeviceService?.Dispose();
            GattDeviceService = null;
        }
    }
}
