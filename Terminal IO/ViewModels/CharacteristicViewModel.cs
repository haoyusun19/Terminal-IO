using ReactiveUI;
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
    }
}
