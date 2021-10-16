using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Terminal_IO.ViewModels;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
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
    public sealed partial class CharacteristicsPage : Page
    {
        private CharacteristicViewModel selectedCharacteristic;

        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(CharacteristicListViewModel), typeof(CharacteristicsPage), new PropertyMetadata(CharacteristicListViewModel.Instance));

        public CharacteristicListViewModel ViewModel
        {
            get { return (CharacteristicListViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public CharacteristicsPage()
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
            var result = await selectedCharacteristic.PrepareToWork();
            if (result != true)
            {
                NotifyUser("Descriptor read failed.", NotifyType.ErrorMessage);
            }

            // Enable/disable operations based on the GattCharacteristicProperties.
            EnableCharacteristicPanels(selectedCharacteristic.Characteristic.CharacteristicProperties);
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
            SetVisibility(ValueChangedSubscribeToggle, properties.HasFlag(GattCharacteristicProperties.Indicate) ||
                                                       properties.HasFlag(GattCharacteristicProperties.Notify));
        }

        private async void CharacteristicReadButton_Click()
        {
            // BT_Code: Read the actual value from the device by using Uncached.
            GattReadResult result = await selectedCharacteristic.Characteristic.ReadValueAsync(BluetoothCacheMode.Uncached);
            if (result.Status == GattCommunicationStatus.Success)
            {
                string formattedResult = selectedCharacteristic.FormatValueByPresentation(result.Value);
                NotifyUser($"Read result: {formattedResult}", NotifyType.StatusMessage);
            }
            else
            {
                NotifyUser($"Read failed: {result.Status}", NotifyType.ErrorMessage);
            }
        }

        private async void CharacteristicWriteButton_Click()
        {

        }

        private async void CharacteristicWriteButtonInt_Click()
        {

        }

        private async void ValueChangedSubscribeToggle_Click()
        {

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
