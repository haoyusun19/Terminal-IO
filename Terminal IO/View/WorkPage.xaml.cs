using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Terminal_IO.Service;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace Terminal_IO.View
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class WorkPage : Page
    {
        private InitialService _initialService;

        public WorkPage()
        {
            _initialService = new InitialService();
            this.InitializeComponent();
        }

        private async void WriteToUARTCreditsRX()
        {
            var writer = new DataWriter();
            writer.ByteOrder = ByteOrder.LittleEndian;
            writer.WriteByte(Byte.MaxValue);
            await _initialService.UARTCreditsRXCharacteristic.WriteValueWithResultAsync(writer.DetachBuffer());              
        }

        private async void ActivateNotification()
        {
            GattCommunicationStatus status = GattCommunicationStatus.Unreachable;
            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;

            status = await _initialService.UARTDataTXCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);

            if (status == GattCommunicationStatus.Success)
            {
                AddValueChangedHandler1();                
            }
            else
            {
                Notify("Unreachable.", NotifyType.StatusMessage);
            }
        }

        private async void ActivateIndication()
        {
            GattCommunicationStatus status = GattCommunicationStatus.Unreachable;
            var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;

            status = await _initialService.UARTCreditsTXCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);

            if (status == GattCommunicationStatus.Success)
            {
                AddValueChangedHandler2();
            }
            else
            {
                Notify("Unreachable.", NotifyType.StatusMessage);
            }
        }


        private void AddValueChangedHandler1()
        {
            _initialService.UARTDataTXCharacteristic.ValueChanged += Characteristic_ValueChanged1;
        }

        private void AddValueChangedHandler2()
        {
            _initialService.UARTCreditsTXCharacteristic.ValueChanged += Characteristic_ValueChanged2;
        }

        private void Characteristic_ValueChanged1(GattCharacteristic sender, GattValueChangedEventArgs args)
        {           
            string newValue = _initialService.FormatValueByPresentation(args.CharacteristicValue, _initialService.UARTDataTXCharacteristic.Uuid);
            Notify(newValue, NotifyType.ReceivedMessage);
        }

        private void Characteristic_ValueChanged2(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            string newValue = _initialService.FormatValueByPresentation(args.CharacteristicValue, _initialService.UARTCreditsTXCharacteristic.Uuid);
            Notify(newValue, NotifyType.NumerOfData);
        }
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            BackButton.IsEnabled = false;
            WriteButton.IsEnabled = false;
            await _initialService.GetServices((Application.Current as App).SelectedBleDeviceId);
            await _initialService.GetCharacteristics();
            if(_initialService.UARTCreditsRXCharacteristic != null)
            {
                ActivateIndication();
                ActivateNotification();
                WriteToUARTCreditsRX();
            }
            else
            {
                Notify("Unreachable.", NotifyType.StatusMessage);
                _initialService.Clear();
                this.Frame.Navigate(typeof(DeviceListPage));
            }
            WriteButton.IsEnabled = true;
            BackButton.IsEnabled = true;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            _initialService.Clear();
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {            
            this.Frame.Navigate(typeof(DeviceListPage));
        }

        private async void CharacteristicWriteButton_Click()
        {
            if (!String.IsNullOrEmpty(CharacteristicWriteValue.Text))
            {
                var writeBuffer = CryptographicBuffer.ConvertStringToBinary(CharacteristicWriteValue.Text,
                    BinaryStringEncoding.Utf8);
                await _initialService.UARTDataRXCharacteristic.WriteValueWithResultAsync(writeBuffer);
                Notify(CharacteristicWriteValue.Text, NotifyType.SendedMessage);
            }
            else
            {
                Notify("Text can not be empty.", NotifyType.StatusMessage);
            }
        }

        private void Notify(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.HasThreadAccess)
            {
                Update(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => Update(strMessage, type));
            }
        }

        private void Update(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.SendedMessage:
                    SendedText.Text += strMessage;
                    break;
                case NotifyType.ReceivedMessage:
                    ReceivedText.Text += strMessage;
                    break;
                case NotifyType.NumerOfData:
                    NumerOfData.Text = strMessage;
                    break;
                case NotifyType.StatusMessage:
                    StatusLabel.Text = strMessage;
                    break;
            }           
        }

        public enum NotifyType
        {
            SendedMessage,
            ReceivedMessage,
            NumerOfData,
            StatusMessage
        };
    }
}
