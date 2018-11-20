using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using Windows.Devices.Usb;

namespace HoloLensDetector
{
    /// <summary>
    /// メインページ
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        Windows.UI.Core.CoreDispatcher dispatcher;
        public static DeviceWatcher watcher = null;
        public static int count = 0;
        public static DeviceInformation[] interfaces = new DeviceInformation[100];
        public static bool isEnumerationComplete = false;
        public static string StopStatus = null;

        /// <summary>
        /// デバイスの監視開始
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        void WatchDevices(object sender, RoutedEventArgs eventArgs)
        {
            UInt32 vid = 0x045E; //ベンダーId
            UInt32 pid = 0x0652; //プロジェクトId

            var aps = UsbDevice.GetDeviceSelector(vid, pid); //クエリを生成する

            try
            {
                dispatcher = Window.Current.CoreWindow.Dispatcher;

                //HoloLensのみ検出するように設定
                watcher = DeviceInformation.CreateWatcher(aps, null);

                watcher.Added += watcher_Added;
                watcher.Removed += watcher_Removed;
                watcher.Updated += watcher_Updated;
                watcher.EnumerationCompleted += watcher_EnumerationCompleted;
                watcher.Stopped += watcher_Stopped;
                watcher.Start();

                OutputText.Text = "デバイス一覧の検出を開始します.";
            }
            catch (ArgumentException)
            {
                OutputText.Text = "Caught ArgumentException. Failed to create watcher.";
            }
        }

        /// <summary>
        /// 監視の停止
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="eventArgs"></param>
        void StopWatcher(object sender, RoutedEventArgs eventArgs)
        {
            try
            {
                if (watcher.Status == Windows.Devices.Enumeration.DeviceWatcherStatus.Stopped)
                {
                    StopStatus = "The enumeration is already stopped.";
                }
                else
                {
                    watcher.Stop();
                    OutputText.Text = "検出を停止しました.";
                }
            }
            catch (ArgumentException)
            {
                OutputText.Text = "Caught ArgumentException. Failed to stop watcher.";
            }
        }

        /// <summary>
        /// デバイスの検出時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="deviceInterface"></param>
        async void watcher_Added(DeviceWatcher sender, DeviceInformation deviceInterface)
        {
            interfaces[count] = deviceInterface;
            count += 1;
            if (isEnumerationComplete)
            {
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    OutputText.Text = $"{deviceInterface.Name} が検出されました。";
                    DisplayDeviceInterfaceArray();
                });
            }
        }

        async void watcher_Updated(DeviceWatcher sender, DeviceInformationUpdate devUpdate)
        {
            int count2 = 0;
            foreach (var deviceInterface in interfaces)
            {
                if (count2 < count)
                {
                    if (interfaces[count2].Id == devUpdate.Id)
                    {
                        //Update the element.
                        interfaces[count2].Update(devUpdate);
                    }

                }
                count2 += 1;
            }

            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, DisplayDeviceInterfaceArray);
        }

        /// <summary>
        /// デバイス取り出し時の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="devUpdate"></param>
        async void watcher_Removed(DeviceWatcher sender, DeviceInformationUpdate devUpdate)
        {
            int count2 = 0;
            //Convert interfaces array to a list (IList).
            var interfaceList = new List<DeviceInformation>(interfaces);

            var removedDevice = string.Empty;

            foreach (var deviceInterface in interfaces)
            {
                if (count2 < count)
                {
                    if (interfaces[count2].Id == devUpdate.Id)
                    {
                        removedDevice = interfaces[count2].Name;

                        //Remove the element.
                        interfaceList.RemoveAt(count2);
                    }

                }
                count2 += 1;
            }
            //Convert the list back to the interfaces array.
            interfaces = interfaceList.ToArray();
            count -= 1;

            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                OutputText.Text = $"{removedDevice} が取り除かれました。";
                DisplayDeviceInterfaceArray();
            });
        }

        /// <summary>
        /// 一覧取得後の処理
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        async void watcher_EnumerationCompleted(DeviceWatcher sender, object args)
        {
            isEnumerationComplete = true;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, DisplayDeviceInterfaceArray);
        }

        /// <summary>
        /// 監視のストップ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        void watcher_Stopped(DeviceWatcher sender, object args)
        {
            if (watcher.Status == Windows.Devices.Enumeration.DeviceWatcherStatus.Aborted)
            {
                StopStatus = "Enumeration stopped unexpectedly. Click Watch to restart enumeration.";
            }
            else if (watcher.Status == Windows.Devices.Enumeration.DeviceWatcherStatus.Stopped)
            {
                StopStatus = "You requested to stop the enumeration. Click Watch to restart enumeration.";
            }
        }

        /// <summary>
        /// コレクション表示の更新
        /// </summary>
        void DisplayDeviceInterfaceArray()
        {
            DeviceInterfacesOutputList.Items?.Clear();
            int count2 = 0;
            foreach (var deviceInterface in interfaces)
            {
                if (count2 < count)
                {
                    DisplayDeviceInterface(deviceInterface);
                }
                count2 += 1;
            }
        }

        /// <summary>
        /// デバイス名を表示
        /// </summary>
        /// <param name="deviceInterface"></param>
        void DisplayDeviceInterface(DeviceInformation deviceInterface)
        {
            var id = "Id:" + deviceInterface.Id;
            var name = deviceInterface.Name;
            var isEnabled = "IsEnabled:" + deviceInterface.IsEnabled;

            var item = id + " is \n" + name + " and \n" + isEnabled;

            DeviceInterfacesOutputList.Items?.Add(item);
        }
    }
}
