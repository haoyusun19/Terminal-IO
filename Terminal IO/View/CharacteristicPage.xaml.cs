using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Terminal_IO.Service;
using Terminal_IO.ViewModels;
using Windows.Devices.Bluetooth;
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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Terminal_IO.View
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class CharacteristicPage : Page
    {
        #region Error Codes
        readonly int E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED = unchecked((int)0x80650003);
        readonly int E_BLUETOOTH_ATT_INVALID_PDU = unchecked((int)0x80650004);
        readonly int E_ACCESSDENIED = unchecked((int)0x80070005);
        readonly int E_DEVICE_NOT_AVAILABLE = unchecked((int)0x800710df); // HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE)
        #endregion

        private bool subscribedForNotifications = false;
        private bool subscribedForIndications = false;
        private DataType datatype;

        private CharacteristicViewModel selectedCharacteristic;

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(CharacteristicListViewModel), typeof(CharacteristicPage), new PropertyMetadata(CharacteristicListViewModel.Instance));

        public CharacteristicListViewModel ViewModel
        {
            get { return (CharacteristicListViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public CharacteristicPage()
        {
            this.InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            await ViewModel.GetCharacteristics((Application.Current as App).SelectedBleServiceId);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            ViewModel.ClearGattDeviceService();
        }

        private void PreviousPageButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(ServicesPage));
        }

        private async void CharacteristicList_SelectionChanged()
        {
            selectedCharacteristic = ResultsListView.SelectedItem as CharacteristicViewModel;
            if (selectedCharacteristic != null)
            {
                var result = await selectedCharacteristic.PrepareToWork();
                if (result != true)
                {
                    NotifyUser("Descriptor read failed.", NotifyType.ErrorMessage);
                }
                NotifyUser("UUID of the selected Characteristic " + selectedCharacteristic.Characteristic.Uuid.ToString(), NotifyType.StatusMessage);
                // Enable/disable operations based on the GattCharacteristicProperties.
                EnableCharacteristicPanels(selectedCharacteristic.Characteristic.CharacteristicProperties);
            }          
        }

        private void SetVisibility(UIElement element, bool visible)
        {
            element.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private void EnableCharacteristicPanels(GattCharacteristicProperties properties)
        {
            // BT_Code: Hide the controls which do not apply to this characteristic.
            SetVisibility(CharacteristicReadButton, properties.HasFlag(GattCharacteristicProperties.Read));

            SetVisibility(CharacteristicWritePanel,
                properties.HasFlag(GattCharacteristicProperties.Write) ||
                properties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse));
            CharacteristicWriteValue.Text = "";
            SetVisibility(ActivateNotification, properties.HasFlag(GattCharacteristicProperties.Notify));
            SetVisibility(ActivateIndication, properties.HasFlag(GattCharacteristicProperties.Indicate));
        }

        private async void CharacteristicReadButton_Click()
        {
            // BT_Code: Read the actual value from the device by using Uncached.
            GattReadResult result = await selectedCharacteristic.Characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
            if (result.Status == GattCommunicationStatus.Success)
            {
                string formattedResult = selectedCharacteristic.FormatValueByPresentation(result.Value, datatype);
                NotifyUser($"Read result: {formattedResult}", NotifyType.StatusMessage);
            }
            else
            {
                NotifyUser($"Read failed: {result.Status}", NotifyType.ErrorMessage);
            }
        }

        private void CharacteristicWriteButton_Click()
        {
            if (!String.IsNullOrEmpty(CharacteristicWriteValue.Text))
            {
                var writeBuffer = CryptographicBuffer.ConvertStringToBinary(CharacteristicWriteValue.Text,
                    BinaryStringEncoding.Utf8);
                datatype = DataType.Utf8;
                WriteBufferToSelectedCharacteristicAsync(writeBuffer);
            }
            else
            {
                NotifyUser("No data to write to device", NotifyType.ErrorMessage);
            }
        }

        private void CharacteristicWriteButtonByte_Click()
        {
            if (!String.IsNullOrEmpty(CharacteristicWriteValue.Text))
            {
                var isValidValue = Byte.TryParse(CharacteristicWriteValue.Text, NumberStyles.HexNumber,
                    null as IFormatProvider, out byte readValue);
                if (isValidValue)
                {
                    var writer = new DataWriter();
                    writer.ByteOrder = ByteOrder.LittleEndian;
                    writer.WriteByte(readValue);
                    datatype = DataType.Bytes;
                    WriteBufferToSelectedCharacteristicAsync(writer.DetachBuffer());
                }
                else
                {
                    NotifyUser("Data to write has to be an byte in hexformat", NotifyType.ErrorMessage);
                }
            }
            else
            {
                NotifyUser("No data to write to device", NotifyType.ErrorMessage);
            }
        }

        private void CharacteristicWriteButtonByteArray_Click()
        {          
            if (!String.IsNullOrEmpty(CharacteristicWriteValue.Text))
            {
                string[] bytes = CharacteristicWriteValue.Text.Split(' ');
                var writer = new DataWriter();
                foreach (var word in bytes)
                {
                    var isValidValue = Byte.TryParse(word, NumberStyles.HexNumber,
                        null as IFormatProvider, out byte readValue);
                    if (isValidValue)
                    {                       
                        writer.ByteOrder = ByteOrder.LittleEndian;
                        writer.WriteByte(readValue);
                        datatype = DataType.Bytes;                       
                    }
                    else
                    {
                        NotifyUser("Data to write has to be an byte in hexformat, like ff ff ff", NotifyType.ErrorMessage);
                    }
                }
                WriteBufferToSelectedCharacteristicAsync(writer.DetachBuffer());
            }
            else
            {
                NotifyUser("No data to write to device", NotifyType.ErrorMessage);
            }
        }

        private void CharacteristicWriteButtonInt_Click()
        {
            if (!String.IsNullOrEmpty(CharacteristicWriteValue.Text))
            {
                var isValidValue = Int32.TryParse(CharacteristicWriteValue.Text, out int readValue);
                if (isValidValue)
                {
                    var writer = new DataWriter();
                    writer.ByteOrder = ByteOrder.LittleEndian;
                    writer.WriteInt32(readValue);
                    datatype = DataType.Int32;
                    WriteBufferToSelectedCharacteristicAsync(writer.DetachBuffer());
                }
                else
                {
                    NotifyUser("Data to write has to be an int32", NotifyType.ErrorMessage);
                }
            }
            else
            {
                NotifyUser("No data to write to device", NotifyType.ErrorMessage);
            }
        }

        private async void WriteBufferToSelectedCharacteristicAsync(IBuffer buffer)
        {
            try
            {
                // BT_Code: Writes the value from the buffer to the characteristic.
                var result = await selectedCharacteristic.Characteristic.WriteValueWithResultAsync(buffer);

                if (result.Status == GattCommunicationStatus.Success)
                {
                    NotifyUser("Successfully wrote value to device", NotifyType.StatusMessage);
                }
                else
                {
                    NotifyUser($"Write failed: {result.Status}", NotifyType.ErrorMessage);
                }
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_INVALID_PDU)
            {
                NotifyUser(ex.Message, NotifyType.ErrorMessage);
            }
            catch (Exception ex) when (ex.HResult == E_BLUETOOTH_ATT_WRITE_NOT_PERMITTED || ex.HResult == E_ACCESSDENIED)
            {
                NotifyUser(ex.Message, NotifyType.ErrorMessage);
            }
        }

        private async void ActivateNotification_Click()
        {
            if (!subscribedForNotifications)
            {
                // initialize status
                GattCommunicationStatus status = GattCommunicationStatus.Unreachable;
                var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Notify;
                
                try
                {
                    // BT_Code: Must write the CCCD in order for server to send indications.
                    // We receive them in the ValueChanged event handler.
                    status = await selectedCharacteristic.Characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);

                    if (status == GattCommunicationStatus.Success)
                    {
                        AddValueChangedHandler();
                        subscribedForNotifications = true;
                        ActivateNotification.Content = "Deactivate the Notification";
                        NotifyUser("Successfully subscribed for value changes", NotifyType.StatusMessage);
                    }
                    else
                    {
                        NotifyUser($"Error registering for value changes: {status}", NotifyType.ErrorMessage);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // This usually happens when a device reports that it support indicate, but it actually doesn't.
                    NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
            }
            else
            {
                try
                {
                    // BT_Code: Must write the CCCD in order for server to send notifications.
                    // We receive them in the ValueChanged event handler.
                    // Note that this sample configures either Indicate or Notify, but not both.
                    var result = await
                            selectedCharacteristic.Characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                GattClientCharacteristicConfigurationDescriptorValue.None);
                    if (result == GattCommunicationStatus.Success)
                    {                       
                        RemoveValueChangedHandler();
                        subscribedForNotifications = false;
                        ActivateNotification.Content = "Activate the Notification";
                        NotifyUser("Successfully un-registered for notifications", NotifyType.StatusMessage);
                    }
                    else
                    {
                        NotifyUser($"Error un-registering for notifications: {result}", NotifyType.ErrorMessage);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // This usually happens when a device reports that it support notify, but it actually doesn't.
                    NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
            }
        }

        private async void ActivateIndication_Click()
        {
            if (!subscribedForIndications)
            {
                // initialize status
                GattCommunicationStatus status = GattCommunicationStatus.Unreachable;
                var cccdValue = GattClientCharacteristicConfigurationDescriptorValue.Indicate;

                try
                {
                    // BT_Code: Must write the CCCD in order for server to send indications.
                    // We receive them in the ValueChanged event handler.
                    status = await selectedCharacteristic.Characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(cccdValue);

                    if (status == GattCommunicationStatus.Success)
                    {
                        AddValueChangedHandler();
                        subscribedForIndications = true;
                        ActivateIndication.Content = "Deactivate the Indication";
                        NotifyUser("Successfully subscribed for value changes", NotifyType.StatusMessage);
                    }
                    else
                    {
                        NotifyUser($"Error registering for value changes: {status}", NotifyType.ErrorMessage);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // This usually happens when a device reports that it support indicate, but it actually doesn't.
                    NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
            }
            else
            {
                try
                {
                    // BT_Code: Must write the CCCD in order for server to send notifications.
                    // We receive them in the ValueChanged event handler.
                    // Note that this sample configures either Indicate or Notify, but not both.
                    var result = await
                            selectedCharacteristic.Characteristic.WriteClientCharacteristicConfigurationDescriptorAsync(
                                GattClientCharacteristicConfigurationDescriptorValue.None);
                    if (result == GattCommunicationStatus.Success)
                    {
                        RemoveValueChangedHandler();
                        subscribedForIndications = false;
                        ActivateIndication.Content = "Activate the Indication";
                        NotifyUser("Successfully un-registered for indications", NotifyType.StatusMessage);
                    }
                    else
                    {
                        NotifyUser($"Error un-registering for indications: {result}", NotifyType.ErrorMessage);
                    }
                }
                catch (UnauthorizedAccessException ex)
                {
                    // This usually happens when a device reports that it support notify, but it actually doesn't.
                    NotifyUser(ex.Message, NotifyType.ErrorMessage);
                }
            }
        }

        private void AddValueChangedHandler()
        {
            if (!subscribedForNotifications)
            {
                selectedCharacteristic.Characteristic.ValueChanged += Characteristic_ValueChanged;               
            }
        }

        private void RemoveValueChangedHandler()
        {
            if (subscribedForNotifications)
            {
                selectedCharacteristic.Characteristic.ValueChanged -= Characteristic_ValueChanged;
            }
        }

        private async void Characteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // BT_Code: An Indicate or Notify reported that the value has changed.
            // Display the new value with a timestamp.
            var newValue = selectedCharacteristic.FormatValueByPresentation(args.CharacteristicValue, DataType.Bytes);
            var message = $"Value at {DateTime.Now:hh:mm:ss.FFF}: {newValue}";


            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () => {
                    selectedCharacteristic.CharacteristicLatestValue = message;
                });
        }

        private void NotifyUser(string strMessage, NotifyType type)
        {
            // If called from the UI thread, then update immediately.
            // Otherwise, schedule a task on the UI thread to perform the update.
            if (Dispatcher.HasThreadAccess)
            {
                UpdateStatus(strMessage, type);
            }
            else
            {
                var task = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => UpdateStatus(strMessage, type));
            }
        }

        private void UpdateStatus(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }

            StatusBlock.Text = strMessage;

            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
            }
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };
    }
}
