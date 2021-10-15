using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal_IO.Service;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Terminal_IO.ViewModels
{
    public class CharacteristicViewModel : ReactiveObject
    {
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
    }
}
