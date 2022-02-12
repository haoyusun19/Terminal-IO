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
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace Terminal_IO.Service
{
    public class InitialService 
    {
        private readonly Guid TerminalIO = new Guid("0000fefb-0000-1000-8000-00805f9b34fb");
        private readonly Guid UARTDataRX = new Guid("00000001-0000-1000-8000-008025000000");
        private readonly Guid UARTDataTX = new Guid("00000002-0000-1000-8000-008025000000");
        private readonly Guid UARTCreditsRX = new Guid("00000003-0000-1000-8000-008025000000");
        private readonly Guid UARTCreditsTX = new Guid("00000004-0000-1000-8000-008025000000");

        private BluetoothLEDevice _bluetoothLeDevice;
        private GattDeviceService _terminalIOService;

        public GattCharacteristic UARTDataRXCharacteristic
        {
            get;
            set;
        }
        public GattCharacteristic UARTDataTXCharacteristic
        {
            get;
            set;
        }
        public GattCharacteristic UARTCreditsRXCharacteristic
        {
            get;
            set;
        }
        public GattCharacteristic UARTCreditsTXCharacteristic
        {
            get;
            set;
        }

        public InitialService()
        {
            
        }

        public async Task GetServices(string deviceId)
        {
            //Sevices.Clear();
            _bluetoothLeDevice = await BluetoothLEDevice.FromIdAsync(deviceId);

            if (_bluetoothLeDevice != null)
            {
                // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
                // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
                // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
                GattDeviceServicesResult result = await _bluetoothLeDevice.GetGattServicesForUuidAsync(TerminalIO, BluetoothCacheMode.Uncached);
                Debug.WriteLine(result.Status);
                if (result.Status == GattCommunicationStatus.Success)
                {
                    var services = result.Services;
                    foreach (var service in services)
                    {
                        Debug.WriteLine(String.Format("Added {0}", service.Uuid));
                        _terminalIOService = service;   
                    }                    
                }
            }
            else
            {
                Debug.WriteLine("Can not get device");
            }
        }

        public async Task GetCharacteristics()
        {
            if(_terminalIOService != null)
            {
                var accessStatus = await _terminalIOService.RequestAccessAsync();

                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    var result = await _terminalIOService.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);

                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        var characteristics = result.Characteristics;
                        foreach (var characteristic in characteristics)
                        {
                            if (characteristic.Uuid == UARTDataRX)
                            {
                                UARTDataRXCharacteristic = characteristic;
                            }
                            else if (characteristic.Uuid == UARTDataTX)
                            {
                                UARTDataTXCharacteristic = characteristic;
                            }
                            else if (characteristic.Uuid == UARTCreditsRX)
                            {
                                UARTCreditsRXCharacteristic = characteristic;
                            }
                            else if (characteristic.Uuid == UARTCreditsTX)
                            {
                                UARTCreditsTXCharacteristic = characteristic;
                            }
                            Debug.WriteLine(String.Format("Added {0}", characteristic.Uuid));
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Can not get characteristic");
                    }
                }
                else
                {
                    Debug.WriteLine("Can not get Service");
                }
            }
            else
            {
                Debug.WriteLine("Can not get Service");
            }
        }

        public string FormatValueByPresentation(IBuffer buffer, Guid guid)
        {
            
            byte[] data;
            string text = null;
            CryptographicBuffer.CopyToByteArray(buffer, out data);
            
            if (data != null)
            {                
                if (guid == new Guid("00000004-0000-1000-8000-008025000000"))
                {
                    foreach (byte b in data)
                    {
                        text += b.ToString("G");
                    }
                    return text;
                }             
                else if(guid == new Guid("00000002-0000-1000-8000-008025000000"))
                {                                      
                    char[] chars = new char[data.Length];
                    Decoder d = Encoding.UTF8.GetDecoder();
                    int textLen = d.GetChars(data, 0, data.Length, chars, 0);
                    if (textLen > 0)
                    {
                        text = new String(chars);
                    }
                    return text;
                }
                else
                {
                    return "Unkown format";
                }
            }
            else
            {
                return "Empty data received";
            }
        }

        public void Clear()
        {
            _bluetoothLeDevice?.Dispose();
            _bluetoothLeDevice = null;
            _terminalIOService?.Dispose();
            _terminalIOService = null;
        }
    }
}
