using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
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

namespace BeitieSpliter
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            InitComboBoxes();
        }

        void InitComboBoxes()
        {
            for (int i = 1; i < 20; i++)
            {
                RowCount.Items.Add(i);
                ColumnCount.Items.Add(i);
            }
            RowCount.SelectedIndex = 6;
            ColumnCount.SelectedIndex = 3;
        }

        private void SetDirFilePath(string path)
        {
            if (path != null)
            {
                FileDirPath.Visibility = Visibility.Visible;
                FileDirPath.Text = path;
            }
            else
            {
                FileDirPath.Visibility = Visibility.Collapsed;
            }
        }

        private void OnImportBeitieDir(object sender, RoutedEventArgs e)
        {

        }

        private async void OnImportBeitieFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker();
            openPicker.ViewMode = PickerViewMode.Thumbnail;
            openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                SetDirFilePath("Picked photo: " + file.Path);
                IRandomAccessStream ir = await file.OpenAsync(FileAccessMode.Read);
                BitmapImage bi = new BitmapImage();
                await bi.SetSourceAsync(ir);
                CurrentPage.Source = bi;
            }
            else
            {
                SetDirFilePath(null);
            }
        }

        private void CoumnCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void RowCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }
    }
}
