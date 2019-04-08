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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Windows.Graphics.Display;
using OpenCVBridge;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;

/* Open CV: */
//using EMGU.CV;
//using Emgu.CV.CvEnum;
//using Emgu.CV.Structure;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BeitieSpliter
{
    public sealed class Common
    {
        public static void Sleep(int msTime)
        {
            AutoResetEvent h = new AutoResetEvent(false);
            h.WaitOne(msTime);
        }
    }
    
    public sealed class BeitieElement
    {
        public BeitieElement(BeitieElementType t, string cont)
        {
            type = t;
            content = cont;
        }
        public enum BeitieElementType
        {
            Zi,
            Quezi,
            Yinzhang,
            Kongbai,
            Other
        }
        public BeitieElementType type;
        public string content;
    }

    public sealed class BeitieGrids
    {
        public double DrawHeight = 1.0;
        public double DrawWidth = 1.0;
        public Point OriginPoint = new Point(0, 0);
        public int Columns = 0;
        public int Rows = 0;
        public List<float> Widths = new List<float>();
        public List<float> Heights = new List<float>();
        public List<BeitieElement> Elements = new List<BeitieElement>();
        public Rect GetRectangle(int row, int col)
        {
            Point leftTopPnt = OriginPoint;
            for (int i = 0; i < col; i++)
            {
                int index = ((row-1) * Rows) + i;
                leftTopPnt.X += Widths[i];
            }
            for (int i = 0; i < row; i++)
            {
                int index = ((col-1) * Columns) + i;
                leftTopPnt.Y += Heights[i];
            }
            Point rightBottomPnt = leftTopPnt;
            rightBottomPnt.X += Widths[(row * Columns) + col];
            rightBottomPnt.Y += Heights[(col * Rows) + row];

            return new Rect(leftTopPnt, rightBottomPnt);
        }
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
    public class ColorBoxItem
    {
        public Color Value { get; set; }
        public string Text { get; set; }
        public ColorBoxItem(Color Value, string Text)
        {
            this.Value = Value;
            this.Text = Text;
        }
    }


    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private Color BKGD_COLOR = Colors.White;   //画布背景色
        BeitieImage CurrentBtImage;
        BeitieGrids BtGrids = new BeitieGrids();
        int ColumnNumber = -1;
        int RowNumber = -1;
        float PenWidth = 0;
        Color PenColor = Colors.White;
        Thickness PageMargin = new Thickness();

        private ObservableCollection<ColorBoxItem> _ColorBoxItems = new ObservableCollection<ColorBoxItem>();
        public ObservableCollection<ColorBoxItem> ColorBoxItems {
            get { return _ColorBoxItems; }
            set { Set(ref _ColorBoxItems, value); }
        }

        private ColorBoxItem _ColorBoxSelectedItem = null;
        public ColorBoxItem ColorBoxSelectedItem { get { return _ColorBoxSelectedItem; } set { Set(ref _ColorBoxSelectedItem, value); } }


        public event PropertyChangedEventHandler PropertyChanged;

        private void Set<T>(ref T storage, T value, [CallerMemberName]string propertyName = null)
        {
            if (Equals(storage, value))
            {
                return;
            }
            storage = value;
            OnPropertyChanged(propertyName);
        }

        private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        public MainPage()
        {
            this.InitializeComponent();
            InitControls();
            InitMaps();
        }
        private void ColumnIllegalHandler(IUICommand command)
        {

            ColumnCount.SelectedIndex = 3;
        }
        private void RowIllegalHandler(IUICommand command)
        {
            RowCount.SelectedIndex = 6;
        }

        private async void ShowMessageDlg(string msg, UICommandInvokedHandler handler)
        {
            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(msg);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            if (handler != null)
            {
                messageDialog.Commands.Add(new UICommand(
                    "关闭", handler));
            }
            else
            {
                messageDialog.Commands.Add(new UICommand(
                   "关闭", null));
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
                ShowMessageDlg("列数非法: " + ColumnCount.Text, new UICommandInvokedHandler(ColumnIllegalHandler));
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
                ShowMessageDlg("行数非法: " + RowCount.Text, new UICommandInvokedHandler(RowIllegalHandler));
            }
            return columns;
        }

        void InitControls()
        {
            // 添加颜色
            ColorBoxItems.Add(new ColorBoxItem(Colors.White, "白色"));
            ColorBoxItems.Add(new ColorBoxItem(Colors.Red, "红色"));
            ColorBoxItems.Add(new ColorBoxItem(Colors.Black, "黑色"));
            ColorBoxItems.Add(new ColorBoxItem(Colors.Yellow, "黄色"));
            ColorBoxItems.Add(new ColorBoxItem(Colors.Blue, "蓝色"));
            ColorBoxSelectedItem = ColorBoxItems.FirstOrDefault(f => f.Text == "白色");

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

        private void InitDrawParameters()
        {
            PenColor = ColorBoxSelectedItem.Value;
            PenWidth = float.Parse(PenWidthBox.Text);

            ColumnNumber = GetColumnCount();
            RowNumber = GetRowCount();
            InitPageMargin();


            CurrentPage.Height = CurrentBtImage.resolutionY;
            CurrentPage.Width = CurrentBtImage.resolutionX;

            BtGrids.Columns = ColumnNumber;
            BtGrids.Rows = RowNumber;
            BtGrids.DrawHeight = CurrentPage.Height - PageMargin.Top - PageMargin.Bottom;
            BtGrids.DrawWidth = CurrentPage.Width - PageMargin.Left - PageMargin.Right;

            BtGrids.OriginPoint = new Point(PageMargin.Left, PageMargin.Top);

            int GridNumber = ColumnNumber * RowNumber;
            float GridHeight = (float)(BtGrids.DrawHeight / RowNumber);
            float GridWidth = (float)(BtGrids.DrawWidth / ColumnNumber);

            BtGrids.Heights.Clear();
            BtGrids.Widths.Clear();
            for (int i = 0; i < GridNumber; i++)
            {
                BtGrids.Heights.Add(GridHeight);
                BtGrids.Widths.Add(GridWidth);
            }
            Debug.WriteLine("Image Parameter:\n col/row: ({0},{1}), resolution: ({2:0},{3:0})\n " +
                "PageMargin:({4},{5},{6},{7}", ColumnNumber, RowNumber, CurrentBtImage.resolutionX,
                CurrentBtImage.resolutionY, PageMargin.Left, PageMargin.Top, PageMargin.Right, PageMargin.Bottom);
        }

        private void RefreshPage()
        {
            RefreshPage(0);
        }

        private void RefreshPage(int delayMs)
        {
            var t = Task.Run(() =>
            {
                if (delayMs > 0)
                {
                    Common.Sleep(delayMs);
                }
                CurrentPage.Invalidate();
            }
           );
            t.Wait();
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
                SetDirFilePath("图片: " + file.Path);
                CurrentBtImage = new BeitieImage(CurrentPage, file);
                InitDrawParameters();
                ParsePageText();
                RefreshPage(1);
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
        private void DrawLine(CanvasDrawingSession draw, Point p1, Point p2)
        {
            //Debug.WriteLine("DrawLine: ({0:0},{1:0})->({2:0},{3:0})\n", (float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y);
            draw.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, PenColor, PenWidth);
        }

        private void PageDrawLines(CanvasDrawingSession draw)
        {
            Point LeftTopPnt = new Point();
            Point LeftBottomPnt = new Point();
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
                    DrawLine(draw, LeftTopPnt, LeftBottomPnt);
                    DrawLine(draw, LeftBottomPnt, RightBottomPnt);
                    DrawLine(draw, RightBottomPnt, RightTopPnt);
                    DrawLine(draw, RightTopPnt, LeftTopPnt);

                    AssignPoint(ref LeftTopPnt, ref RightTopPnt);
                }
                AssignPoint(ref LeftTopPnt, ref RowStartPnt);
                LeftTopPnt.Y += BtGrids.Heights[i * ColumnNumber];
            }

        }

        private bool IsColumnRowValid()
        {
            if ((RowNumber == -1) ||
               (ColumnNumber == -1))
            {
                return false;
            }
            return true;
        }

        private bool IsParametersValid()
        {
            if (!IsColumnRowValid())
            {
                return false;
            }
            if (PenWidth <= 0)
            {
                return false;
            }
            if ((CurrentPage.Height <= 0) ||
                (CurrentPage.Width <= 0))
            {
                return false;
            }
            return true;
        }

        private void CurrentPage_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            Debug.WriteLine(String.Format("CurrentPage_OnDraw called"));

            var draw = args.DrawingSession;

            draw.Clear(Colors.Gray);
            if (CurrentBtImage == null)
            {
                draw.DrawText("请选择书法字帖图片!", new Vector2(100, 100), Colors.Black);
                return;
            }
            if (!IsParametersValid())
            {
                draw.Clear(Colors.Black);
                draw.DrawText("参数错误，请更改参数后重试!", new Vector2(100, 100), Colors.Red);
                return;
            }
            if (CurrentBtImage.cvsBmp == null)
            {
                draw.Clear(Colors.Black);
                draw.DrawText("图片正在加载中...", new Vector2(100, 100), Colors.Blue);
                RefreshPage();
                return;
            }

            draw.DrawImage(CurrentBtImage.cvsBmp);
            PageDrawLines(draw);
            ImageScrollViewer.Scale = new Vector3(1, 1, 1);
        }

        private void Page_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(String.Format("Page_OnUnloaded called"));
            CurrentPage.RemoveFromVisualTree();
            CurrentPage = null;
        }

        void UpdateColumnCount()
        {
            int columns = GetColumnCount();
            if (columns == ColumnNumber)
            {
                return;
            }
            ColumnNumber = columns;
            if (IsColumnRowValid())
            {
                InitDrawParameters();
            }
            RefreshPage();
        }
        private void ColumnCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateColumnCount();
        }

        private void ColumnCount_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateColumnCount();
        }
        void UpdateRowCount()
        {

            int rows = GetRowCount();
            if (rows == RowNumber)
            {
                return;
            }
            RowNumber = rows;
            if (IsColumnRowValid())
            {
                InitDrawParameters();
            }
            CurrentPage.Invalidate();
        }
        private void RowCount_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateRowCount();
        }
        private void RowCount_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateRowCount();
        }

        private void PenColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PenColor = ColorBoxSelectedItem.Value;
            CurrentPage.Invalidate();
        }

        private void PenWidthBox_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void PageMargin_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private void InitPageMargin()
        {
            string pattern = "([\\d\\.]+),?";
            MatchCollection mc = Regex.Matches(PageMarginsBox.Text, pattern);
            if (mc.Count == 1)
            {
                double oneForAllMargin = double.Parse(mc.ElementAt(0).Value);
                PageMargin = new Thickness(oneForAllMargin, oneForAllMargin, oneForAllMargin, oneForAllMargin);
            }
            else
            {
                double[] margins = { 0.0, 0.0, 0, 0 };
                for (int i = 0; i < mc.Count; i++)
                {
                    margins[i] = double.Parse(mc.ElementAt(i).Value);
                }
                PageMargin = new Thickness(margins[0], margins[1], margins[2], margins[3]);
            }
        }

        private void PageMargin_LostFocus(object sender, RoutedEventArgs e)
        {
            string TotalPattern = "([\\d\\.]+),?([\\d\\.]?),?([\\d\\.]?),?([\\d\\.]?)";
            var textbox = (TextBox)sender;
            if (Regex.IsMatch(textbox.Text, TotalPattern) && textbox.Text != "")
            {
                InitDrawParameters();
                CurrentPage.Invalidate();
            }
            else
            {
                PenWidth = 0;
                textbox.Text = "";
                ShowMessageDlg("Invalid margin: " + textbox.Text, null);
            }
        }

        private void PenWidthBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textbox = (TextBox)sender;
            if (Regex.IsMatch(textbox.Text, "^[\\d]+\\.?[\\d]?$") && textbox.Text != "")
            {
                PenWidth = float.Parse(PenWidthBox.Text);
            }
            else
            {
                PenWidth = 0;
                int pos = textbox.SelectionStart - 1;
                textbox.Text = textbox.Text.Remove(pos, 1);
                textbox.SelectionStart = pos;
                ShowMessageDlg("Invalid pen width: " + textbox.Text, null);
            }
            CurrentPage.Invalidate();
        }

        async void SaveWholePage(string name)
        {
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder folder = await applicationFolder.CreateFolderAsync("Picture", CreationCollisionOption.OpenIfExists);
            StorageFile saveFile = await folder.CreateFileAsync(name, CreationCollisionOption.ReplaceExisting);
            RenderTargetBitmap bitmap = new RenderTargetBitmap();
            await bitmap.RenderAsync(CurrentPage);
            var pixelBuffer = await bitmap.GetPixelsAsync();
            byte[] bytes = pixelBuffer.ToArray();
            using (var fileStream = await saveFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, fileStream);
                encoder.SetPixelData(BitmapPixelFormat.Bgra8,
                                    BitmapAlphaMode.Ignore,
                                    (uint)bitmap.PixelWidth,
                                    (uint)bitmap.PixelHeight,
                                    DisplayInformation.GetForCurrentView().LogicalDpi,
                                    DisplayInformation.GetForCurrentView().LogicalDpi,
                                    pixelBuffer.ToArray());

                await encoder.FlushAsync();
            }
        }

        async Task<SoftwareBitmap> GetSoftwareBitmap(StorageFile inputFile)
        {
            SoftwareBitmap softwareBitmap;
            using (IRandomAccessStream stream = await inputFile.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.JpegDecoderId, stream);

                // Get the SoftwareBitmap representation of the file
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            }

            return softwareBitmap;
        }

        async Task<SoftwareBitmap> GetSoftwareBitmap(string dir, string name)
        {
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder folder = await applicationFolder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists);
            StorageFile inputFile = await folder.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);

            return await GetSoftwareBitmap(inputFile);
        }


        enum OperationType
        {
            Crop,
            Blur,
            HoughLines,
            Contours,
            Histogram,
            MotionDetector
        }
        private OpenCVHelper _helper = new OpenCVHelper();

        void UpdateCanvasBmp(CanvasBitmap bmp)
        {
            CurrentBtImage.cvsBmp = bmp;
            CurrentBtImage.resolutionY = (float)bmp.Bounds.Height;
            CurrentBtImage.resolutionX = (float)bmp.Bounds.Width;
            Debug.WriteLine("New Bitmap: {0:0},{1:0}", CurrentBtImage.resolutionX, CurrentBtImage.resolutionY);
            InitDrawParameters();
        }

        private async void SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, string dir, string filename)
        {
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder folder = await applicationFolder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists);
            StorageFile outputFile = await folder.CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);

            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite))
            {
                // Create an encoder with the desired format
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, stream);

                // Set the software bitmap
                encoder.SetSoftwareBitmap(softwareBitmap);

                // Set additional encoding parameters, if needed
                //encoder.BitmapTransform.ScaledWidth = 320;
                //encoder.BitmapTransform.ScaledHeight = 240;
                //encoder.BitmapTransform.Rotation = Windows.Graphics.Imaging.BitmapRotation.Clockwise90Degrees;
                //encoder.BitmapTransform.InterpolationMode = BitmapInterpolationMode.Fant;
                encoder.IsThumbnailGenerated = true;

                try
                {
                    await encoder.FlushAsync();
                }
                catch (Exception err)
                {
                    const int WINCODEC_ERR_UNSUPPORTEDOPERATION = unchecked((int)0x88982F81);
                    switch (err.HResult)
                    {
                        case WINCODEC_ERR_UNSUPPORTEDOPERATION:
                            // If the encoder does not support writing a thumbnail, then try again
                            // but disable thumbnail generation.
                            encoder.IsThumbnailGenerated = false;
                            break;
                        default:
                            throw;
                    }
                }

                if (encoder.IsThumbnailGenerated == false)
                {
                    await encoder.FlushAsync();
                }
            }
        }
        void SaveSingleCropImage(SoftwareBitmap input, Rect roi, string album, string filename)
        {
            SoftwareBitmap croppedBmp = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
                       (int)roi.Width, (int)roi.Height, BitmapAlphaMode.Premultiplied);

            _helper.Crop(input, croppedBmp, (int)roi.Left, (int)roi.Top, (int)roi.Width, (int)roi.Height);


            Debug.WriteLine("Save Single Crop Image: ({0:0},{3:0},{4:0},{5:0}), -> {1}\\{2}", roi.X, album, filename,
                roi.Y, roi.Width, roi.Height);
            SaveSoftwareBitmapToFile(croppedBmp, album, filename);
        }

        private async void OnSaveSplitImages(object sender, RoutedEventArgs e)
        {
            if (CurrentBtImage == null)
            {
                ShowMessageDlg("请选择书法碑帖图片!", null);
                return;
            }
            string album = TieAlbum.Text;
            SoftwareBitmap inputBitmap = await GetSoftwareBitmap(CurrentBtImage.file);
            int size = BtGrids.Elements.Count;
            // 从左到右，自上而下
            int counter = 0;

            if (album == "")
            {
                System.DateTime currentTime = System.DateTime.Now;
                string filename;
                filename = currentTime.ToString("yyyyMMdd_HHmmss");
                album = filename;
            }
            for (int i = BtGrids.Columns-1; i >= 0; i--)
            {
                for (int j = 0; j < BtGrids.Rows; j++)
                {
                    Rect roi = BtGrids.GetRectangle(j, i);
                    BeitieElement element = BtGrids.Elements[counter];
                    string filename = string.Format("{0}-{1}.jpg", counter, element.content);

                    if (element.type == BeitieElement.BeitieElementType.Kongbai)
                    {
                        continue;
                    }
                    try
                    {
                        SaveSingleCropImage(inputBitmap, roi, album, filename);
                        counter++;
                    }
                    catch (Exception err)
                    {
                        Debug.WriteLine(err.ToString());
                    }
                }
            }
        }
        private List<char> IGNORED_CHARS = new List<char>();
        
        private void InitMaps()
        {
            IGNORED_CHARS.Add(',');
            IGNORED_CHARS.Add('.');
            IGNORED_CHARS.Add(';');

            IGNORED_CHARS.Add('，');
            IGNORED_CHARS.Add('。');
            IGNORED_CHARS.Add('；');
            IGNORED_CHARS.Add('、');
        }

        private void SetStatisticsOfPageText()
        {
            int totalElements = BtGrids.Elements.Count;
            int CharCount = 0;
            int LostCharCount = 0;
            int SpaceCount = 0;
            int SealCount = 0;
            int OtherCount = 0;
            for (int i = 0; i < totalElements; i++)
            {
                switch (BtGrids.Elements[i].type)
                {
                    case BeitieElement.BeitieElementType.Kongbai:
                        SpaceCount++;
                        break;
                    case BeitieElement.BeitieElementType.Zi:
                        CharCount++;
                        break;
                    case BeitieElement.BeitieElementType.Quezi:
                        LostCharCount++;
                        break;
                    case BeitieElement.BeitieElementType.Yinzhang:
                        SealCount++;
                        break;
                    default:
                        OtherCount++;
                        break;
                }
            }
            TextParsedData.Text = string.Format("字：{0},阙字({5})：{1}, 印章({6})：{2},空白({7}): {3}, 其他({8}): {4}",
                CharCount, LostCharCount, SealCount, SpaceCount, OtherCount,
                "{缺}", "{印:}", "{}", "{XX}");
        }
        private void ParsePageText()
        {
            string txt = PageText.Text;
            int length = txt.Length;
            char single;
            bool specialTypeDetected = false;
            StringBuilder sb = new StringBuilder();

            BtGrids.Elements.Clear();
            for (int i = 0; i < length; i++)
            {
                single = txt[i];
                if (IGNORED_CHARS.Contains(single))
                {
                    continue;
                }
                else if (single == '□')
                {
                    BtGrids.Elements.Add(new BeitieElement(BeitieElement.BeitieElementType.Kongbai, new string(single, 1)));
                }
                else if (single == '{')
                {
                    specialTypeDetected = true;
                }
                else if (single == '}')
                {
                    specialTypeDetected = false;
                    string name = sb.ToString();
                    BeitieElement.BeitieElementType type;
                    if (name.Contains("印"))
                    {
                        type = BeitieElement.BeitieElementType.Yinzhang;
                    }
                    else if (name.Length == 0)
                    {
                        type = BeitieElement.BeitieElementType.Kongbai;
                    }
                    else if (name.Contains("缺"))
                    {
                        type = BeitieElement.BeitieElementType.Quezi;
                    }
                    else
                    {
                        type = BeitieElement.BeitieElementType.Other;
                    }
                    BtGrids.Elements.Add(new BeitieElement(type, name));
                    sb.Clear();
                }
                else if (specialTypeDetected)
                {
                    sb.Append(single);
                }
                else
                {
                    BtGrids.Elements.Add(new BeitieElement(BeitieElement.BeitieElementType.Zi, new string(single, 1)));
                }
            }
            SetStatisticsOfPageText();
        }

        private void PageText_LostFocus(object sender, RoutedEventArgs e)
        {
            ParsePageText();
        }

        private async void BtnMore_Clicked(object sender, RoutedEventArgs e)
        {
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(GridsConfig), null);
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId);
        }
    }
}
