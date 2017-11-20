using Microsoft.Toolkit.Uwp.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace NugetDataHelper
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        Regex _totalDownloadRegex;
        public MainPage()
        {
            this.InitializeComponent();
            _totalDownloadRegex = new Regex("([0-9,]*) total downloads");


            PackageTextBox.Text = @"Microsoft.Toolkit.Uwp
Microsoft.Toolkit.Uwp.UI
Microsoft.Toolkit.Uwp.UI.Animations
Microsoft.Toolkit.Uwp.UI.Controls
Microsoft.Toolkit.Uwp.Notifications
Microsoft.Toolkit.Uwp.Services
Microsoft.Toolkit
Microsoft.Toolkit.Uwp.Notifications.JavaScript
Microsoft.Toolkit.Uwp.DeveloperTools
Microsoft.Toolkit.Services
Microsoft.Toolkit.Uwp.Connectivity";
        }

        private async Task<NugetData> GetDetailedDataForPackage(string packageName)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var moreData = await client.GetStringAsync($"https://www.nuget.org/stats/reports/packages/{packageName}?groupby=Version");
                    var nugetData = JsonConvert.DeserializeObject<NugetData>(moreData);
                    nugetData.PackageName = packageName;

                    return nugetData;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async Task<BasicPackageDetails> GetBasicDataForPackage(string packageName)
        {
            try
            {
                var basicData = new BasicPackageDetails();

                using (var client = new HttpClient())
                {
                    var data = await client.GetStringAsync($"https://www.nuget.org/packages/{packageName}/");
                    var match = _totalDownloadRegex.Match(data);
                    if (match.Success)
                    {
                        basicData.TotalDownloads = int.Parse(match.Groups[1].Value.Replace(",", ""));
                    }
                }

                return basicData;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.IsEnabled = false;

            var items = new List<PackageData>();

            var networkCalls = new List<Task<NugetData>>();
            var basicNetworkCalls = new List<Task<BasicPackageDetails>>();

            var packageNames = PackageTextBox.Text.Split('\r');
            foreach (var packageName in packageNames)
            {
                networkCalls.Add(GetDetailedDataForPackage(packageName));
                basicNetworkCalls.Add(GetBasicDataForPackage(packageName));
            }

            await Task.WhenAll(Task.WhenAll(networkCalls), Task.WhenAll(basicNetworkCalls));

            for (var i = 0; i < networkCalls.Count; ++i)
            {
                var networkCall = networkCalls[i];
                var basicNetworkCall = basicNetworkCalls[i];

                var data = networkCall.Result;
                var basicData = basicNetworkCall.Result;

                if (data == null)
                {
                    continue;
                }

                var packageData = new PackageData();
                packageData.Name = data.PackageName;
                packageData.CombinedDownloads = data.Total;
                packageData.Details = basicData;

                List<DownloadsData> downloads = new List<DownloadsData>();

                foreach (var row in data.Table)
                {
                    var versionData = new DownloadsData()
                    {
                        Version = row[0].Data,
                        Downloads = int.Parse(row[1].Data)
                    };

                    downloads.Add(versionData);
                }

                packageData.Downloads = downloads;
                packageData.PercentageLatestVersion = (float)downloads.First().Downloads / (float)data.Total * 100;

                items.Add(packageData);
            }

            var combinedLatestPackageData = new PackageData();
            combinedLatestPackageData.Name = "Downloads for latest version of all packages";

            var combinedPackageData = new PackageData();
            combinedPackageData.Name = "Downloads for all versions of all packages";

            // get total
            foreach (var item in items)
            {
                combinedLatestPackageData.Downloads.Add(new DownloadsData()
                {
                    Downloads = item.Downloads.First().Downloads,
                    Version = item.Name.Split('.').Last()
                });

                combinedPackageData.Downloads.Add(new DownloadsData()
                {
                    Downloads = item.CombinedDownloads,
                    Version = item.Name.Split('.').Last()
                });
            }

            items.Add(combinedLatestPackageData);
            items.Add(combinedPackageData);

            ItemsContainer.ItemsSource = items;
            button.IsEnabled = true;
        }

        private async void Print(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            button.IsEnabled = false;

            RenderTargetBitmap rtb = new RenderTargetBitmap();
            await rtb.RenderAsync(ItemsContainer);

            DataPackage package = new DataPackage();
            package.RequestedOperation = DataPackageOperation.Copy;

            var pixels = await rtb.GetPixelsAsync();
            var stream = new InMemoryRandomAccessStream();

            var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.BmpEncoderId, stream);
            encoder.SetPixelData(
                BitmapPixelFormat.Bgra8,
                BitmapAlphaMode.Ignore,
                (uint)rtb.PixelWidth,
                (uint)rtb.PixelHeight,
                logicalDpi,
                logicalDpi,
                pixels.ToArray());

            await encoder.FlushAsync();
            package.SetBitmap(RandomAccessStreamReference.CreateFromStream(stream));

            Clipboard.SetContent(package);
            button.IsEnabled = true;
            Notification.Show("Image copied to clipboard", 3000);
        }
    }

    public class PackageData
    {
        public string Name { get; set; }
        public List<DownloadsData> Downloads { get; set; } = new List<DownloadsData>();

        public double PercentageLatestVersion { get; set; }
        public int CombinedDownloads { get; set; }
        public BasicPackageDetails Details { get; set; }
    }

    public class BasicPackageDetails
    {
        public int TotalDownloads { get; set; }
        public int DownloadsOfLatest { get; set; }
        public int AverageDownloadsPerDay { get; set; }
        public double PercentageOfLatestDownloads { get; set; }
    }

    public class DownloadsData
    {
        public string Version { get; set; }
        public int Downloads { get; set; }
    }
}
