using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal_IO.Service;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace Terminal_IO.ViewModels
{
    public class CharacteristicViewModel : ReactiveObject
    {
        

        public CharacteristicViewModel(GattCharacteristic gattCharacteristic)
        {
            Characteristic = gattCharacteristic;
            CharacteristicName = DisplayHelper.GetCharacteristicName(Characteristic);
        }


        public GattCharacteristic Characteristic
        {
            get;
            set;
        }

        
        [Reactive]
        public string CharacteristicName
        {
            get;
            set;
        }


        
        public string FormatValueByPresentation(IBuffer buffer)
        {
            // BT_Code: For the purpose of this sample, this function converts only UInt32 and
            // UTF-8 buffers to readable text. It can be extended to support other formats if your app needs them.
            byte[] data;
            string text = null;
            CryptographicBuffer.CopyToByteArray(buffer, out data);
            
            if (data != null)
            {                
                if (Characteristic.Uuid.Equals(new Guid("00002a50-0000-1000-8000-00805f9b34fb")))
                {
                    text += "0x" + data[4].ToString("X2") + data[3].ToString("X2");
                    return text;
                }               
                // No guarantees on if a characteristic is registered for notifications.               
                else
                {

                    /*
                    string text = null;
                    foreach (byte b in data)
                    {
                        //text += b.ToString();
                    }*/
                    char[] chars = new char[data.Length];
                    Decoder d = Encoding.UTF8.GetDecoder();
                    int textLen = d.GetChars(data, 0, data.Length, chars, 0);
                    if(textLen > 0)
                    {
                        text = new String(chars);
                    }
                    return text;
                }
            }
            else
            {
                return "Empty data received";
            }
        }            
    }
}
