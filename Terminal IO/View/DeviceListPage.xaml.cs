using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Terminal_IO.ViewModels;
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
    public sealed partial class DeviceListPage : Page
    {
        public static readonly DependencyProperty ViewModelProperty =
            DependencyProperty.Register(nameof(ViewModel), typeof(DeviceListViewModel), typeof(DeviceListPage), new PropertyMetadata(DeviceListViewModel.Instance));

        public DeviceListViewModel ViewModel
        {
            get { return (DeviceListViewModel)GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
        public DeviceListPage()
        {
            this.InitializeComponent();
        }
       

        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            
            var bleDevice = ResultsListView.SelectedItem as DeviceViewModel;

            // BT_Code: Pair the currently selected device.
            await bleDevice.DeviceInformation.Pairing.PairAsync();
            
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            
            ViewModel.StopBleDeviceWatcher();
            // Save the selected device's ID for use in other scenarios.
            var bleDevice = ResultsListView.SelectedItem as DeviceViewModel;
            if (bleDevice != null)
            {
                (Application.Current as App).SelectedBleDeviceId = bleDevice.Id;
                (Application.Current as App).SelectedBleDeviceName = bleDevice.Name;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.StartBleDeviceWatcher();
        }

        private void NextPageButton_Click(object sender, RoutedEventArgs e)
        {                         
            this.Frame.Navigate(typeof(ServicesPage));
        }
    }
}
