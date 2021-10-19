﻿using ReactiveUI;
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
        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion

        public CharacteristicViewModel(GattCharacteristic gattCharacteristic)
        {
            Characteristic = gattCharacteristic;
            CharacteristicName = DisplayHelper.GetCharacteristicName(Characteristic);
        }

        private GattPresentationFormat presentationFormat;

        public GattCharacteristic Characteristic
        {
            get;
            set;
        }

        [Reactive]
        public string CharacteristicLatestValue
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


        public async Task<bool> PrepareToWork()
        {
            presentationFormat = null;
            if (Characteristic.PresentationFormats.Count > 0)
            {

                if (Characteristic.PresentationFormats.Count.Equals(1))
                {
                    // Get the presentation format since there's only one way of presenting it
                    presentationFormat = Characteristic.PresentationFormats[0];
                }
                else
                {
                    // It's difficult to figure out how to split up a characteristic and encode its different parts properly.
                    // In this case, we'll just encode the whole thing to a string to make it easy to print out.
                }
            }
            Debug.WriteLine(Characteristic.PresentationFormats.Count);
            var result = await Characteristic.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success)
            {
                return false;
            }
            else
            {
                return true;
            }
        }


        public string FormatValueByPresentation(IBuffer buffer, DataType dataType)
        {
            // BT_Code: For the purpose of this sample, this function converts only UInt32 and
            // UTF-8 buffers to readable text. It can be extended to support other formats if your app needs them.
            byte[] data;
            CryptographicBuffer.CopyToByteArray(buffer, out data);
            if (presentationFormat != null)
            {
                if (presentationFormat.FormatType == GattPresentationFormatTypes.UInt32 && data.Length >= 4)
                {
                    return BitConverter.ToInt32(data, 0).ToString();
                }
                else if (presentationFormat.FormatType == GattPresentationFormatTypes.UInt16 && data.Length >= 4)
                {
                    return BitConverter.ToInt16(data, 0).ToString();
                }
                else if (presentationFormat.FormatType == GattPresentationFormatTypes.Utf8)
                {
                    try
                    {
                        return Encoding.UTF8.GetString(data);
                    }
                    catch (ArgumentException)
                    {
                        return "(error: Invalid UTF-8 string)";
                    }
                }
                else
                {
                    // Add support for other format types as needed.
                    return "Unsupported format: " + CryptographicBuffer.EncodeToHexString(buffer);
                }
            }
            else if (data != null)
            {
                // We don't know what format to use. Let's try some well-known profiles, or default back to UTF-8.
                if (Characteristic.Uuid.Equals(GattCharacteristicUuids.HeartRateMeasurement))
                {
                    try
                    {
                        return "Heart Rate: " + ParseHeartRateValue(data).ToString();
                    }
                    catch (ArgumentException)
                    {
                        return "Heart Rate: (unable to parse)";
                    }
                }
                else if (Characteristic.Uuid.Equals(GattCharacteristicUuids.BatteryLevel))
                {
                    try
                    {
                        // battery level is encoded as a percentage value in the first byte according to
                        // https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.battery_level.xml
                        return "Battery Level: " + data[0].ToString() + "%";
                    }
                    catch (ArgumentException)
                    {
                        return "Battery Level: (unable to parse)";
                    }
                }
                // This is our custom calc service Result UUID. Format it like an Int
                else if (Characteristic.Uuid.Equals(Constants.ResultCharacteristicUuid))
                {
                    return BitConverter.ToInt32(data, 0).ToString();
                }
                // No guarantees on if a characteristic is registered for notifications.
                else if (Characteristic != null)
                {
                    /*
                    // This is our custom calc service Result UUID. Format it like an Int
                    if (Characteristic.Uuid.Equals(Constants.ResultCharacteristicUuid))
                    {
                        
                    }
                    */
                    //
                    if (dataType == DataType.Int32)
                    {
                        return BitConverter.ToInt32(data, 0).ToString();
                    }
                    else if (dataType == DataType.Utf8)
                    {
                        return Encoding.UTF8.GetString(data);
                    }
                    else if (dataType == DataType.Bytes)
                    {
                        string text = null;
                        foreach(byte b in data)
                        {
                            text += b.ToString("X2") + " ";
                        }
                        return text;
                    }
                    else
                    {
                        return "Unknown format";
                    }                  
                }
                else
                {                  
                    return "Unknown format";
                }
            }
            else
            {
                return "Empty data received";
            }
        }

        /// <summary>
        /// Process the raw data received from the device into application usable data,
        /// according the the Bluetooth Heart Rate Profile.
        /// https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.heart_rate_measurement.xml&u=org.bluetooth.characteristic.heart_rate_measurement.xml
        /// This function throws an exception if the data cannot be parsed.
        /// </summary>
        /// <param name="data">Raw data received from the heart rate monitor.</param>
        /// <returns>The heart rate measurement value.</returns>
        private static ushort ParseHeartRateValue(byte[] data)
        {
            // Heart Rate profile defined flag values
            const byte heartRateValueFormat = 0x01;

            byte flags = data[0];
            bool isHeartRateValueSizeLong = (flags & heartRateValueFormat) != 0;


            if (isHeartRateValueSizeLong)
            {

                return BitConverter.ToUInt16(data, 1);
            }
            else
            {
                return data[1];
            }
        }


        public async Task<bool> WriteBufferToSelectedCharacteristicAsync(IBuffer buffer)
        {
            try
            {
                // BT_Code: Writes the value from the buffer to the characteristic.
                var result = await Characteristic.WriteValueWithResultAsync(buffer);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_INVALID_PDU)
            {
                return false;
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED || ex.HResult == E_ACCESSDENIED)
            {
                return false;
            }
        }
    }
}