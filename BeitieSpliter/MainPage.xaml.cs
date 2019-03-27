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
using Windows.UI.Popups;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BeitieSpliter
{
    public sealed class BeitieGrids
    {
        public Point OriginPoint;
        public Point MaxPoint;
        public List<float> Widths = new List<float>();
        public List<float> Heights = new List<float>();
    }

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
        public CanvasBitmap cvsBmp;

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
            cvsBmp = await CanvasBitmap.LoadAsync(creator, iras);


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
        BeitieGrids BtGrids = new BeitieGrids();
        int ColumnNumber = -1;
        int RowNumber = -1;
        int PenWidth = 2;
        Color PenColor = Colors.White;

        public MainPage()
        {
            this.InitializeComponent();
            InitControls();
        }
        private void ColumnIllegalHandler(IUICommand command)
        {

            ColumnCount.SelectedIndex = 3;
        }
        private void RowIllegalHandler(IUICommand command)
        {
            RowCount.SelectedIndex = 6;
        }
        private async void ShowMessageDlg(string msg, bool isColumn)
        {
            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(msg);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            if (isColumn)
            {
                messageDialog.Commands.Add(new UICommand(
                    "关闭", new UICommandInvokedHandler(this.ColumnIllegalHandler)));
            }
            else
            {

                messageDialog.Commands.Add(new UICommand(
                    "关闭", new UICommandInvokedHandler(this.RowIllegalHandler)));
            }

            // Set the command that will be invoked by default
            messageDialog.DefaultCommandIndex = 0;

            // Set the command to be invoked when escape is pressed
            messageDialog.CancelCommandIndex = 1;

            // Show the message dialog
            await messageDialog.ShowAsync();
        }

        public int GetColumnCount()
        {
            if (ColumnCount.Text == "")
            {
                return -1;
            }

            int columns = -1;
            try
            {
                columns = int.Parse(ColumnCount.Text);
            }
            catch
            {
                ShowMessageDlg("列数非法: " + ColumnCount.Text, true);
            }
            return columns;
        }

        public int GetRowCount()
        {
            if (RowCount.Text == "")
            {
                return -1;
            }

            int columns = -1;
            try
            {
                columns = int.Parse(RowCount.Text);
            }
            catch
            {
                ShowMessageDlg("行数非法: " + RowCount.Text, false);
            }
            return columns;
        }

        void InitControls()
        {
            for (int i = 1; i < 20; i++)
            {
                RowCount.Items.Add(i);
                ColumnCount.Items.Add(i);
            }
            RowCount.SelectedIndex = 8;
            ColumnCount.SelectedIndex = 5;

            CurrentPage.Height = ImageScrollViewer.Height;
            CurrentPage.Width = ImageScrollViewer.Width;
        }

        private void InitGrids()
        {
            ColumnNumber = GetColumnCount();
            RowNumber = GetRowCount();

            CurrentPage.Height = CurrentBtImage.resolutionY;
            CurrentPage.Width = CurrentBtImage.resolutionX;

            BtGrids.OriginPoint = new Point(0, 0);
            BtGrids.MaxPoint = new Point(CurrentPage.Width, CurrentPage.Height);

            int GridNumber = ColumnNumber * RowNumber;
            int GridHeight = (int)(CurrentPage.Height / RowNumber);
            int GridWidth = (int)(CurrentPage.Width / ColumnNumber);

            BtGrids.Heights.Clear();
            BtGrids.Widths.Clear();
            for (int i = 0; i < GridNumber; i++)
            {
                BtGrids.Heights.Add(GridHeight);
                BtGrids.Widths.Add(GridWidth);
            }
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
                InitGrids();
                CurrentPage.Invalidate();
            }
            else
            {
                SetDirFilePath(null);
            }
        }
        
        private void AssignPoint(ref Point dst, ref Point src)
        {
            dst.X = src.X;
            dst.Y = src.Y;
        }
        private void DrawLine(CanvasDrawingSession draw, Point p1, Point p2, Color clr)
        {
            Debug.WriteLine("DrawLine: ({0:0},{1:0})->({2:0},{3:0})\n", (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
            draw.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, clr);
        }

        private void PageDrawLines(CanvasDrawingSession draw)
        {
            Point LeftTopPnt = new Point();
            Point LeftBottomPnt = new Point(1,2);
            Point RightTopPnt = new Point();
            Point RightBottomPnt = new Point();
            Point RowStartPnt = new Point();

            int GridNumber = ColumnNumber * RowNumber;


            int index = 0;
            AssignPoint(ref LeftTopPnt, ref BtGrids.OriginPoint);
            for (int i = 0; i < RowNumber; i++)
            {
                AssignPoint(ref RowStartPnt, ref LeftTopPnt);
                for (int j = 0; j < ColumnNumber; j++)
                {
                    index = i * ColumnNumber + j;
                    AssignPoint(ref RightBottomPnt, ref LeftTopPnt);
                    RightBottomPnt.X += BtGrids.Widths[index];
                    RightBottomPnt.Y += BtGrids.Heights[index];

                    AssignPoint(ref LeftBottomPnt, ref LeftTopPnt);
                    LeftBottomPnt.Y += BtGrids.Heights[index];


                    AssignPoint(ref RightTopPnt, ref LeftTopPnt);
                    RightTopPnt.X += BtGrids.Widths[index];

                    // draw Rectangle
                    DrawLine(draw, LeftTopPnt, LeftBottomPnt, PenColor);
                    DrawLine(draw, LeftBottomPnt, RightBottomPnt, PenColor);
                    DrawLine(draw, RightBottomPnt, RightTopPnt, PenColor);
                    DrawLine(draw, RightTopPnt, LeftTopPnt, PenColor);
                    
                    AssignPoint(ref LeftTopPnt, ref RightTopPnt);
                }
                AssignPoint(ref LeftTopPnt, ref RowStartPnt);
                LeftTopPnt.Y += BtGrids.Heights[i * ColumnNumber];
            }
            
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
            draw.DrawImage(CurrentBtImage.cvsBmp);
            PageDrawLines(draw);
        }

        private void Page_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(String.Format("Page_OnUnloaded called"));
            CurrentPage.RemoveFromVisualTree();
            CurrentPage = null;
        }

        private void ColumnCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ColumnNumber = GetColumnCount();
        }


        private void RowCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RowNumber = GetRowCount();
        }
    }
}
