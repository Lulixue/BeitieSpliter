using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
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
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using System.Diagnostics;
using Windows.UI.Core;
using System.Threading.Tasks;
using System.Threading;
using System.Text;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BeitieSpliter
{
    public sealed class BeitieImage
    {
        private void Sleep(int msTime)
        {
            AutoResetEvent h = new AutoResetEvent(false);
            h.WaitOne(msTime);
        }
        public BeitieImage(CanvasControl cvs, StorageFile f)
        {
            creator = cvs;
            file = f;
            Init();
            if (resolutionX < 100)
            {
                resolutionX = (float)size.Width;
                resolutionY = (float)size.Height;
            }
        }
        public CanvasControl creator;
        public StorageFile file;
        public float resolutionX;
        public float resolutionY;
        public Size size;
        public string contents;
        public IRandomAccessStream iras;
        private BinaryReader stmReader;
        private Stream stream;
        public CanvasBitmap canvasBmp;

        public void Init()
        {
            var t = Task.Run(() =>
                {
                    GetJpgSize(file.Path, out size, out resolutionX, out resolutionY);
                }
            );
            t.Wait();


            //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            //() =>
            //{

            //});
            //getJpgSize(file.Path, out size, out resolutionX, out resolutionY);
        }


        public async void GetFileText()
        {
            //contents = await FileIO.ReadTextAsync(file);


            stream = await file.OpenStreamForReadAsync();
            stmReader = new BinaryReader(stream, Encoding.ASCII);

            iras = await file.OpenAsync(FileAccessMode.Read);
            canvasBmp = await CanvasBitmap.LoadAsync(creator, iras);


            //var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
            //ulong size = stream.Size;
            //using (var inputStream = stream.GetInputStreamAt(0))
            //{
            //    // We'll add more code here in the next step.
            //    using (var dataReader = new Windows.Storage.Streams.DataReader(inputStream))
            //    {
            //        uint numBytesLoaded = await dataReader.LoadAsync((uint)size);
            //        string text = dataReader.ReadString(numBytesLoaded);

            //    }
            //}

        }
        

        public int GetJpgSize(string FileName, out Size JpgSize, out float Wpx, out float Hpx)
        {
            JpgSize = new Size(0, 0);
            Wpx = 0; Hpx = 0;
            int rx = 0;

            GetFileText();
            while (stmReader == null)
            {
                Sleep(50);
            }
            
            stream.Seek(0, SeekOrigin.Begin);

            int ff = stmReader.ReadByte();
            int type = stmReader.ReadByte();

            if (ff != 0xff || type != 0xd8)
            {//非JPG文件
                stmReader.Dispose();
                Debug.WriteLine("Error -> File({3}) Not JPG: {0:H}{1:H}", ff, type, file.Name);
                return rx;
            }
            long ps = 0;
            do
            {
                do
                {
                    ff = stmReader.ReadByte();
                    if (ff < 0) //文件结束
                    {
                        stmReader.Dispose();
                        Debug.WriteLine("End of File, position: {0}", stream.Position);
                        return rx;
                    }
                } while (ff != 0xff);

                do
                {
                    type = stmReader.ReadByte();
                } while (type == 0xff);

                //MessageBox.Show(ff.ToString() + "," + type.ToString(), stmReader.Position.ToString());
                ps = stream.Position;
                switch (type)
                {
                    case 0x00:
                    case 0x01:
                    case 0xD0:
                    case 0xD1:
                    case 0xD2:
                    case 0xD3:
                    case 0xD4:
                    case 0xD5:
                    case 0xD6:
                    case 0xD7:
                        break;
                    case 0xc0: //SOF0段
                        ps = stmReader.ReadByte() * 256;
                        ps = stream.Position + ps + stmReader.ReadByte() - 2; //加段长度

                        stmReader.ReadByte(); //丢弃精度数据
                        //高度
                        JpgSize.Height = stmReader.ReadByte() * 256;
                        JpgSize.Height = JpgSize.Height + stmReader.ReadByte();
                        //宽度
                        JpgSize.Width = stmReader.ReadByte() * 256;
                        JpgSize.Width = JpgSize.Width + stmReader.ReadByte();
                        //后面信息忽略
                        if (rx != 1 && rx < 3) rx = rx + 1;
                        break;
                    case 0xe0: //APP0段
                        ps = stmReader.ReadByte() * 256;
                        ps = stream.Position + ps + stmReader.ReadByte() - 2; //加段长度

                        stream.Seek(5, SeekOrigin.Current); //丢弃APP0标记(5bytes)
                        stream.Seek(2, SeekOrigin.Current); //丢弃主版本号(1bytes)及次版本号(1bytes)
                        int units = stmReader.ReadByte(); //X和Y的密度单位,units=0：无单位,units=1：点数/英寸,units=2：点数/厘米

                        //水平方向(像素/英寸)分辨率
                        Wpx = stmReader.ReadByte() * 256;
                        Wpx = Wpx + stmReader.ReadByte();
                        if (units == 2) Wpx = (float)(Wpx * 2.54); //厘米变为英寸
                        //垂直方向(像素/英寸)分辨率
                        Hpx = stmReader.ReadByte() * 256;
                        Hpx = Hpx + stmReader.ReadByte();
                        if (units == 2) Hpx = (float)(Hpx * 2.54); //厘米变为英寸
                        //后面信息忽略
                        if (rx != 2 && rx < 3) rx = rx + 2;
                        break;

                    default: //别的段都跳过////////////////
                        ps = stmReader.ReadByte() * 256;
                        ps = stream.Position + ps + stmReader.ReadByte() - 2; //加段长度
                        break;
                }
                if (ps + 1 >= stream.Length) //文件结束
                {
                    stmReader.Dispose();
                    return rx;
                }
                stream.Position = ps; //移动指针
            } while (type != 0xda); // 扫描行开始
            stmReader.Dispose();
            return rx;
        }
    }

    public sealed partial class MainPage : Page
    {
        private Color BKGD_COLOR = Colors.White;   //画布背景色
        BeitieImage CurrentBtImage;

        public MainPage()
        {
            this.InitializeComponent();
            InitControls();
        }

        void InitControls()
        {
            for (int i = 1; i < 20; i++)
            {
                RowCount.Items.Add(i);
                ColumnCount.Items.Add(i);
            }
            RowCount.SelectedIndex = 6;
            ColumnCount.SelectedIndex = 3;

            CurrentPage.Height = ImageScrollViewer.Height;
            CurrentPage.Width = ImageScrollViewer.Width;
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

        private void SavePage(StorageFile file)
        {

        }
        
        private void DrawPage(StorageFile file)
        {
            
        }


        

        private async void OnImportBeitieFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };

            openPicker.FileTypeFilter.Add(".jpg");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".png");
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                // Application now has read/write access to the picked file
                SetDirFilePath("Picked photo: " + file.Path);
                CurrentBtImage = new BeitieImage(CurrentPage, file);
                
                CurrentPage.Height = CurrentBtImage.resolutionY;
                CurrentPage.Width = CurrentBtImage.resolutionX;
                CurrentPage.Invalidate();

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
        private void PageDrawLines(CanvasDrawingSession draw)
        {
            int beginX = 0, endX = 0;
            int beginY = 0, endY = 0;

            // 画横线
        }
      
        private void CurrentPage_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            Debug.WriteLine(String.Format("CurrentPage_OnDraw called"));
              
            var draw = args.DrawingSession;

            if (CurrentBtImage == null)
            {
                draw.DrawText("请选择书法字帖图片!", new Vector2(100, 100), Colors.Black);
                return;
            }
            draw.DrawImage(CurrentBtImage.canvasBmp);

        }

        private void Page_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(String.Format("Page_OnUnloaded called"));
            CurrentPage.RemoveFromVisualTree();
            CurrentPage = null;
        }
    }
}
