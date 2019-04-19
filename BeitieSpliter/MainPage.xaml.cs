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
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using Windows.UI.Xaml.Automation.Peers;

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

        public static async void ShowMessageDlg(string msg, UICommandInvokedHandler handler)
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
    }
    
    public sealed class BeitieElement
    {
        public BeitieElement(BeitieElementType t, string cont, int n)
        {
            type = t;
            content = cont;
            this.no = n;
        }
        public bool NeedAddNo()
        {
            return no != -1;
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
        public int row = -1;
        public int col = -1;
        public int no = -1;
    }
    public class BeitieGridRect
    {
        public BeitieGridRect(Rect rect)
        {
            rc = rect;
            revised = false;
        }
        public BeitieGridRect(Rect rect, bool rev)
        {
            rc = rect;
            revised = rev;
        }

        public Rect rc = new Rect();
        public bool revised = false;
    }

    public sealed class BeitieGrids : ICloneable
    {
        public float angle = 0;
        public float PenWidth = 0;
        public List<Color> BackupColors = new List<Color>();
        public List<Color> ContrastColors = new List<Color>();
        public Color PenColor = Colors.Red;
        public Thickness PageMargin = new Thickness();
        public BeitieImage BtImageParent = null;
        public double DrawHeight = 1.0;
        public double DrawWidth = 1.0;
        public Point OriginPoint = new Point(0, 0);
        public int Columns = 0;
        public int Rows = 0;
        public StorageFile ImageFile = null;
        public StorageFile RotateFile = null;
        public List<BeitieGridRect> ElementRects = new List<BeitieGridRect>();

        public List<BeitieElement> Elements = new List<BeitieElement>();

        public bool IsImageRotated() { return angle != 0; }

        public Rect GetMaxRectangle(int minRow, int minCol, int maxRow, int maxCol)
        {
            Point pntLt = new Point(GetRowLeft(minRow, minCol, maxCol), GetColTop(minCol, minRow, maxRow));
            Point pntRb = new Point(GetRowRight(maxRow, minCol, maxCol), GetColBottom(maxCol, minRow, maxRow));

            return new Rect(pntLt, pntRb);
        }
        public double GetRowRight(int row, int MinCol, int MaxCol)
        {
            double maxRight = 0;

            for (int i = MinCol; i <= MaxCol; i++)
            {
                double right = GetRectangle(row, i).Right;
                if (right > maxRight)
                {
                    maxRight = right;
                }
            }
            return maxRight;
        }

        public double GetRowLeft(int row, int MinCol, int MaxCol)
        {
            double minLeft = 10000.0;

            for (int i = MinCol; i <= MaxCol; i++)
            {
                double left = GetRectangle(row, i).Left;
                if (left < minLeft)
                {
                    minLeft = left;
                }
            }
            return minLeft;
        }

        public double GetColTop(int col, int MinRow, int MaxRow)
        {
            double minTop = 10000.0;

            for (int i = MinRow; i <= MaxRow; i++)
            {
                double top = GetRectangle(i, col).Top;
                if (top < minTop)
                {
                    minTop = top;
                }
            }
            return minTop;
        }
        public double GetColBottom(int col, int MinRow, int MaxRow)
        {
            double maxBottom = 0.0;

            for (int i = MinRow; i <= MaxRow; i++)
            {
                double bottom = GetRectangle(i, col).Bottom;
                if (bottom > maxBottom)
                {
                    maxBottom = bottom;
                }
            }
            return maxBottom;
        }

        public int ToIndex(int row, int col)
        {
            Debug.Assert(row > 0);
            Debug.Assert(col > 0);
            int index = (row - 1) * Columns + col - 1;

            return index;
        }
        public BeitieGridRect GetElement(int row, int col)
        {
            return ElementRects[ToIndex(row, col)];
        }
        public Rect GetRectangle(int row, int col)
        {
            return ElementRects[ToIndex(row, col)].rc;
        }
        public bool GetRevised(int row, int col)
        {
            return ElementRects[ToIndex(row, col)].revised;
        }
        // 古籍：自上而下，从右到左
        // 现代：自左而右，从上到下
        public int GetIndex(int row, int col, bool oldStyle)
        {
            Debug.Assert(row > 0);
            Debug.Assert(col > 0);
            if (!oldStyle)
            {
                return ToIndex(row, col);
            }
            else
            {
                col = Columns - col;
                int index = col * Rows + row - 1;
                return index;
            }
        }
        public bool IndexToRowCol(int index, ref int row, ref int col, bool oldStyle)
        {
            if (!oldStyle)
            {
                row = index / Columns + 1;
                col = index % Columns + 1;
            }
            else
            {
                col = Columns - index / Rows;
                row = index % Rows + 1;
            }
            Debug.Assert(row > 0);
            Debug.Assert(col > 0);
            return true;
        }

        public object Clone()
        {
            // 浅拷贝，没有拷贝内存
            return this.MemberwiseClone();
            // 需要深拷贝
            // Color不能被serialized
            
            //using (MemoryStream memStream = new MemoryStream())
            //{
            //    BinaryFormatter binaryFormatter = new BinaryFormatter(null,
            //         new StreamingContext(StreamingContextStates.Clone));
            //    binaryFormatter.Serialize(memStream, this);
            //    memStream.Seek(0, SeekOrigin.Begin);
            //    return binaryFormatter.Deserialize(memStream);
            //}

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
        public bool PageTextConfirmed = false;

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
                Debug.WriteLine("Error -> File({2}) Not JPG: {0}{1}", ff, type, file.Name);
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
        public BeitieGrids BtGrids = new BeitieGrids();
        int ColumnNumber = -1;
        int RowNumber = -1;
        public event EventHandler SaveSplitted;

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
            this.SaveSplitted += new EventHandler(this.OnSaveSplitImagesDelegate);
        }
        private void ColumnIllegalHandler(IUICommand command)
        {

            ColumnCount.SelectedIndex = 3;
        }
        private void RowIllegalHandler(IUICommand command)
        {
            RowCount.SelectedIndex = 6;
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
                Common.ShowMessageDlg("列数非法: " + ColumnCount.Text, new UICommandInvokedHandler(ColumnIllegalHandler));
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
               Common.ShowMessageDlg("行数非法: " + RowCount.Text, new UICommandInvokedHandler(RowIllegalHandler));
            }
            return columns;
        }
        List<ColorBoxItem> LightColorItems = new List<ColorBoxItem>();
        List<ColorBoxItem> DarkColorItems = new List<ColorBoxItem>();
        
        void UpdateBackupColors()
        {
            BtGrids.BackupColors.Clear();
            bool NeedContinue = true;
            foreach (ColorBoxItem item in LightColorItems)
            {
                if (item.Value == BtGrids.PenColor)
                {
                    NeedContinue = false;
                    continue;
                }
                BtGrids.BackupColors.Add(item.Value);
            }

            if (!NeedContinue)
            {
                return;
            }

            BtGrids.BackupColors.Clear();
            foreach (ColorBoxItem item in DarkColorItems)
            {
                if (item.Value == BtGrids.PenColor)
                {
                    NeedContinue = false;
                    continue;
                }
                BtGrids.BackupColors.Add(item.Value);
            }
        }

        void InitControls()
        {
            // 添加颜色
            LightColorItems.Add(new ColorBoxItem(Colors.White, "白色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Gray, "灰色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Orange, "橙色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Yellow, "黄色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Blue, "蓝色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Green, "绿色"));

            DarkColorItems.Add(new ColorBoxItem(Colors.Red, "红色"));
            DarkColorItems.Add(new ColorBoxItem(Colors.Black, "黑色"));
            DarkColorItems.Add(new ColorBoxItem(Colors.Purple, "紫色"));
            DarkColorItems.Add(new ColorBoxItem(Colors.Navy, "海军蓝色"));

            foreach (ColorBoxItem item in LightColorItems)
            {
                ColorBoxItems.Add(item);
            }
            foreach (ColorBoxItem item in DarkColorItems)
            {
                ColorBoxItems.Add(item);
            }
            ColorBoxSelectedItem = ColorBoxItems.FirstOrDefault(f => f.Text == "红色");
            BtGrids.PenColor = ColorBoxSelectedItem.Value;
            UpdateBackupColors();

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
            if (CurrentBtImage == null)
            {
                return;
            }

            BtGrids.ImageFile = CurrentBtImage.file;
            BtGrids.BtImageParent = CurrentBtImage;

            BtGrids.PenColor = ColorBoxSelectedItem.Value;
            BtGrids.PenWidth = float.Parse(PenWidthBox.Text);

            ColumnNumber = GetColumnCount();
            RowNumber = GetRowCount();
            InitPageMargin();


            CurrentPage.Height = CurrentBtImage.resolutionY;
            CurrentPage.Width = CurrentBtImage.resolutionX;

            BtGrids.Columns = ColumnNumber;
            BtGrids.Rows = RowNumber;
            BtGrids.DrawHeight = CurrentPage.Height - BtGrids.PageMargin.Top - BtGrids.PageMargin.Bottom;
            BtGrids.DrawWidth = CurrentPage.Width - BtGrids.PageMargin.Left - BtGrids.PageMargin.Right;

            BtGrids.OriginPoint = new Point(BtGrids.PageMargin.Left, BtGrids.PageMargin.Top);
            
            float GridHeight = (float)(BtGrids.DrawHeight / RowNumber);
            float GridWidth = (float)(BtGrids.DrawWidth / ColumnNumber);
            
            BtGrids.ElementRects.Clear();
            Point leftTop = new Point();
            for (int i = 0; i < RowNumber; i++)
            {
                leftTop = BtGrids.OriginPoint;
                leftTop.Y += i * GridHeight;
                for (int j = 0; j < ColumnNumber; j++)
                {
                    BtGrids.ElementRects.Add(new BeitieGridRect(new Rect(leftTop.X, leftTop.Y, GridWidth, GridHeight)));
                    leftTop.X += GridWidth;
                }
            }
            Debug.WriteLine("Image Parameter:\n col/row: ({0},{1}), resolution: ({2:0},{3:0})\n " +
                "PageMargin:({4},{5},{6},{7})", ColumnNumber, RowNumber, CurrentBtImage.resolutionX,
                CurrentBtImage.resolutionY, BtGrids.PageMargin.Left, BtGrids.PageMargin.Top, 
                BtGrids.PageMargin.Right, BtGrids.PageMargin.Bottom);
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
        
        private string GetFileTitle(StorageFile file)
        {
            string name = file.Name;
            int index = name.IndexOf('.');
            if (index == -1)
            {
                return name;
            }
            return name.Substring(0, index);
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
                if (TieAlbum.Text == "")
                    TieAlbum.Text = GetFileTitle(file);
                CurrentBtImage = new BeitieImage(CurrentPage, file);
                
                InitDrawParameters();
                ParsePageText();
                BtnMore.IsEnabled = true;
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
            draw.DrawLine((float)p1.X, (float)p1.Y, (float)p2.X, (float)p2.Y, BtGrids.PenColor, BtGrids.PenWidth);
        }

        private void PageDrawLines(CanvasDrawingSession draw)
        {
            int GridNumber = ColumnNumber * RowNumber;
            int index = 0;
            for (int i = 0; i < RowNumber; i++)
            {
                for (int j = 0; j < ColumnNumber; j++)
                {
                    index = i * ColumnNumber + j;
                    draw.DrawRectangle(BtGrids.ElementRects[index].rc, BtGrids.PenColor, BtGrids.PenWidth);
                }
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
            if (BtGrids.PenWidth <= 0)
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
            BtGrids.PenColor = ColorBoxSelectedItem.Value;
            UpdateBackupColors();
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
                BtGrids.PageMargin = new Thickness(oneForAllMargin, oneForAllMargin, oneForAllMargin, oneForAllMargin);
            }
            else
            {
                double[] margins = { 0.0, 0.0, 0, 0 };
                for (int i = 0; i < mc.Count; i++)
                {
                    margins[i] = double.Parse(mc.ElementAt(i).Value);
                }
                BtGrids.PageMargin = new Thickness(margins[0], margins[1], margins[2], margins[3]);
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
                BtGrids.PenWidth = 0;
                textbox.Text = "";
                Common.ShowMessageDlg("Invalid margin: " + textbox.Text, null);
            }
        }

        private void PenWidthBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var textbox = (TextBox)sender;
            if (Regex.IsMatch(textbox.Text, "^[\\d]+\\.?[\\d]?$") && textbox.Text != "")
            {
                BtGrids.PenWidth = float.Parse(PenWidthBox.Text);
            }
            else
            {
                BtGrids.PenWidth = 0;
                int pos = textbox.SelectionStart - 1;
                textbox.Text = textbox.Text.Remove(pos, 1);
                textbox.SelectionStart = pos;
                Common.ShowMessageDlg("Invalid pen width: " + textbox.Text, null);
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
        private async Task<StorageFolder> GetSaveFolder(string dir)
        {
            //StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder picFolder = KnownFolders.PicturesLibrary;
            StorageFolder folder = await picFolder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists);

            return folder;
        }

        private async Task<StorageFile> SaveSoftwareBitmapToFile(SoftwareBitmap softwareBitmap, string dir, string filename)
        {
            StorageFolder folder = await GetSaveFolder(dir);
            StorageFile outputFile = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            
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
            return outputFile;
        }
        async void SaveSingleCropImage(SoftwareBitmap input, Rect roi, string album, string filename)
        {
            SoftwareBitmap croppedBmp = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
                       (int)roi.Width, (int)roi.Height, BitmapAlphaMode.Premultiplied);

            _helper.Crop(input, croppedBmp, (int)roi.Left, (int)roi.Top, (int)roi.Width, (int)roi.Height);


            Debug.WriteLine("Save Single Crop Image: ({0:0},{3:0},{4:0},{5:0}), -> {1}\\{2}", roi.X, album, filename,
                roi.Y, roi.Width, roi.Height);
            await SaveSoftwareBitmapToFile(croppedBmp, album, filename);
        }

        public void HandlerSaveSplittedImages(object para)
        {
            this.SaveSplitted.Invoke(para, null);
        }

        public async void OnSaveSplitImagesDelegate(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SaveSplitImages(sender);
            });
        }

        public async Task<bool> ShowNotifyPageTextDlg()
        {
            ContentDialog locationPromptDialog = new ContentDialog
            {
                Title = "输入释文",
                Content = "你还没有输入释文, 是否生成图片?",
                CloseButtonText = "继续生成",
                PrimaryButtonText = "输入释文"
            };

            ContentDialogResult result = await locationPromptDialog.ShowAsync();
            return (result == ContentDialogResult.Primary);
        }

        public async void SaveSplitImages(object para)
        {
            string album = TieAlbum.Text;
            SoftwareBitmap inputBitmap = null;
            HashSet<Point> ElementIndexes = (HashSet<Point>)para;
            int StartNo = int.Parse(StartNoBox.Text);

            if ((PageText.Text == "") && !CurrentBtImage.PageTextConfirmed)
            {
                if (await ShowNotifyPageTextDlg())
                {
                    return;
                }
                CurrentBtImage.PageTextConfirmed = true;
            }

            NotifyUser("开始保存分割单字图片...", NotifyType.StatusMessage);

            if (BtGrids.IsImageRotated())
            {
                inputBitmap = await GetSoftwareBitmap(BtGrids.RotateFile);
            }
            else
            {
                inputBitmap = await GetSoftwareBitmap(CurrentBtImage.file);
            }
            
            // 从左到右，自上而下
            if (album == "")
            {
                System.DateTime currentTime = System.DateTime.Now;
                album = currentTime.ToString("yyyyMMdd_HHmmss");
            }

            if (ElementIndexes == null)
            {
                ElementIndexes = new HashSet<Point>();
                for (int i = BtGrids.Columns; i >= 1; i--)
                {
                    for (int j = 1; j <= BtGrids.Rows; j++)
                    {
                        ElementIndexes.Add(new Point(j, i));
                    }
                }
            }
            foreach (Point pnt in ElementIndexes)
            {
                Rect roi = BtGrids.GetRectangle((int)pnt.X, (int)pnt.Y);
                int index = BtGrids.GetIndex((int)pnt.X, (int)pnt.Y, true);
                BeitieElement element = BtGrids.Elements[index];

                if (element.type == BeitieElement.BeitieElementType.Kongbai)
                {
                    continue;
                }
                try
                {
                    string filename = "";
                    if (!element.NeedAddNo())
                    {
                        filename = string.Format("{0}.jpg", element.content);
                    }
                    else
                    {
                        filename = string.Format("{0}-{1}.jpg", element.no + StartNo, element.content);
                    }
                    SaveSingleCropImage(inputBitmap, roi, album, filename);
                }
                catch (Exception err)
                {
                    Debug.WriteLine(err.ToString());
                }
            }

            StorageFolder folder = await GetSaveFolder(album);
            NotifyUser("单字分割图片已保存到文件夹"+ folder.Path, NotifyType.StatusMessage);
        }
        private void OnSaveSplitImages(object sender, RoutedEventArgs e)
        {
            if (CurrentBtImage == null)
            {
                Common.ShowMessageDlg("请选择书法碑帖图片!", null);
                return;
            }
            SaveSplitImages(null);
        }
        private List<char> IGNORED_CHARS = new List<char>();
        
        private void InitMaps()
        {
            IGNORED_CHARS.Add(',');
            IGNORED_CHARS.Add('.');
            IGNORED_CHARS.Add(';');
            IGNORED_CHARS.Add('(');
            IGNORED_CHARS.Add(')');

            IGNORED_CHARS.Add('，');
            IGNORED_CHARS.Add('。');
            IGNORED_CHARS.Add('；');
            IGNORED_CHARS.Add('、');
            IGNORED_CHARS.Add('（');
            IGNORED_CHARS.Add('）');
        }

        private void UpdateParseStatus()
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

            string info = "";
            if (CurrentBtImage != null)
            {
                info += string.Format("图片尺寸: {0:0}*{1:0}, ", CurrentBtImage.resolutionX, CurrentBtImage.resolutionY);
            }
            info += string.Format("当前碑帖: {0}, 起始单字编号: {1}({2}), ", TieAlbum.Text, StartNoBox.Text,
                NoNameSwitch.IsOn ? NoNameSwitch.OnContent : NoNameSwitch.OffContent);
            info += string.Format("字: {0}, 阙字({5}): {1}, 印章({6}): {2}, 空白({7}): {3}, 其他({8}): {4}",
                CharCount, LostCharCount, SealCount, SpaceCount, OtherCount,
                "{缺}/□", "{印:}", "{}", "{XX}");

            NotifyUser(info, NotifyType.StatusMessage);
        }

        private void ParsePageText()
        {
            string txt = PageText.Text;
            int length = txt.Length;
            char single;
            bool specialTypeDetected = false;
            int OthersNo = 1;
            int ZiNo = 0;
            StringBuilder sb = new StringBuilder();

            BtGrids.Elements.Clear();
            if (length == 0)
            {
                length = BtGrids.Rows * BtGrids.Columns;
                for (int i = 0; i < length; i++)
                {
                    BtGrids.Elements.Add(new BeitieElement(BeitieElement.BeitieElementType.Zi,
                           "", ZiNo++));
                }
            }
            else
            {
                for (int i = 0; i < length; i++)
                {
                    single = txt[i];
                    if (IGNORED_CHARS.Contains(single))
                    {
                        continue;
                    }
                    else if (single == '□')
                    {
                        string name = new string(single, 1);
                        name += OthersNo++;
                        BtGrids.Elements.Add(new BeitieElement(BeitieElement.BeitieElementType.Quezi,
                            name, ZiNo++));
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
                            name += OthersNo++;
                            type = BeitieElement.BeitieElementType.Yinzhang;
                        }
                        else if (name.Length == 0)
                        {
                            name += OthersNo++;
                            type = BeitieElement.BeitieElementType.Kongbai;
                        }
                        else if (name.Contains("缺"))
                        {
                            name += OthersNo++;
                            type = BeitieElement.BeitieElementType.Quezi;
                        }
                        else
                        {
                            name += OthersNo++;
                            type = BeitieElement.BeitieElementType.Other;
                        }
                        BtGrids.Elements.Add(new BeitieElement(type, name, OnlyZiNo ? -1 : ZiNo++));
                        sb.Clear();
                    }
                    else if (specialTypeDetected)
                    {
                        sb.Append(single);
                    }
                    else
                    {
                        BtGrids.Elements.Add(new BeitieElement(BeitieElement.BeitieElementType.Zi, new string(single, 1), ZiNo++));
                    }
                }
            }
            
            UpdateParseStatus();
        }

        private void PageText_LostFocus(object sender, RoutedEventArgs e)
        {
            ParsePageText();
        }

        public async Task<CanvasBitmap> RotateImage(float angle)
        {
            // The XAML Image control can only display images in BRGA8 format with premultiplied or no alpha
            // The frame reader as configured in this sample gives BGRA8 with straight alpha, so need to convert it
         
            SoftwareBitmap inputBitmap = await GetSoftwareBitmap(CurrentBtImage.file);

            SoftwareBitmap originalBitmap = SoftwareBitmap.Convert(inputBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
            SoftwareBitmap outputBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
                originalBitmap.PixelWidth, originalBitmap.PixelHeight, BitmapAlphaMode.Premultiplied);

            _helper.Rotate(originalBitmap, outputBitmap, angle);

            BtGrids.RotateFile = await SaveSoftwareBitmapToFile(outputBitmap, "Tmp", "Rotate.jpg");
            return CanvasBitmap.CreateFromSoftwareBitmap(CurrentBtImage.creator, outputBitmap);
        }

        #region OpenCV Test Code
        enum OpenCVOperationType
        {
            Crop,
            Rotate,
            Blur,
            HoughLines,
            Contours,
            Histogram,
            MotionDetector
        }
        OpenCVOperationType currentOperation = OpenCVOperationType.Rotate;
        CanvasBitmap cvsBmpBak = null;
        bool pageRedrawed = false;
        float angleRotate = 30;

        private async void TestOpenCV()
        {
            Debug.WriteLine("TestOpenCV: Operation:{0}", currentOperation);
            BitmapAlphaMode mode = BitmapAlphaMode.Premultiplied;
            
            if (CurrentBtImage == null)
            {
                return;
            }
            SoftwareBitmap originalBitmap = null;
            if (cvsBmpBak == null)
            {
                SaveWholePage("canvas.jpg");
            }
            else if (!pageRedrawed)
            {
                UpdateCanvasBmp(cvsBmpBak);
                RefreshPage(50);
                pageRedrawed = true;
                return;
            }
            else
            {
                pageRedrawed = false;
            }


            RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap();
            await renderTargetBitmap.RenderAsync(CurrentPage);
            var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

            var inputBitmap = SoftwareBitmap.CreateCopyFromBuffer(pixelBuffer,
                                                     BitmapPixelFormat.Bgra8,
                                                     renderTargetBitmap.PixelWidth,
                                                     renderTargetBitmap.PixelHeight,
                                                     BitmapAlphaMode.Premultiplied);

            //var inputBitmap = await GetSoftwareBitmap("canvas.jpg");
            if (cvsBmpBak == null)
            {
                var iBmp = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
                    (int)CurrentBtImage.cvsBmp.SizeInPixels.Width, (int)CurrentBtImage.cvsBmp.SizeInPixels.Height, mode);
                cvsBmpBak = CanvasBitmap.CreateFromSoftwareBitmap(CurrentBtImage.creator, iBmp);
                cvsBmpBak.CopyPixelsFromBitmap(CurrentBtImage.cvsBmp);
                //return;
            }
            if (inputBitmap != null)
            {
                // The XAML Image control can only display images in BRGA8 format with premultiplied or no alpha
                // The frame reader as configured in this sample gives BGRA8 with straight alpha, so need to convert it
                originalBitmap = /*inputBitmap;// */SoftwareBitmap.Convert(inputBitmap, BitmapPixelFormat.Bgra8, mode);

                SoftwareBitmap outputBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
                    originalBitmap.PixelWidth, originalBitmap.PixelHeight, mode);

                // Operate on the image in the manner chosen by the user.
                if (currentOperation == OpenCVOperationType.Blur)
                {
                    _helper.Blur(originalBitmap, outputBitmap);
                }
                else if (currentOperation == OpenCVOperationType.HoughLines)
                {
                    _helper.HoughLines(originalBitmap, outputBitmap);
                }
                else if (currentOperation == OpenCVOperationType.Contours)
                {
                    _helper.Contours(originalBitmap, outputBitmap);
                }
                else if (currentOperation == OpenCVOperationType.Histogram)
                {
                    _helper.Histogram(originalBitmap, outputBitmap);
                }
                else if (currentOperation == OpenCVOperationType.MotionDetector)
                {
                    _helper.MotionDetector(originalBitmap, outputBitmap);
                }
                else if (currentOperation == OpenCVOperationType.Crop)
                {
                    outputBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8,
                        (int)BtGrids.PageMargin.Right, (int)BtGrids.PageMargin.Bottom, mode);
                    _helper.Crop(originalBitmap, outputBitmap, (int)BtGrids.PageMargin.Left, (int)BtGrids.PageMargin.Top,
                        (int)BtGrids.PageMargin.Right, (int)BtGrids.PageMargin.Bottom);
                }
                else if (currentOperation == OpenCVOperationType.Rotate)
                {
                    _helper.Rotate(originalBitmap, outputBitmap, angleRotate);
                    angleRotate -= 2;

                }
                //currentOperation++;
                if (currentOperation > OpenCVOperationType.MotionDetector)
                {
                    currentOperation = OpenCVOperationType.Crop;
                }
                await SaveSoftwareBitmapToFile(outputBitmap, "Test", "show.jpg");
                //UpdateCanvasBmp(CanvasBitmap.CreateFromSoftwareBitmap(CurrentBtImage.creator, outputBitmap));
                UpdateCanvasBmp(await RotateImage(angleRotate));
                RefreshPage(100);
            }
        }
        #endregion

        private async void BtnMore_Clicked(object sender, RoutedEventArgs e)
        {
            //if (true)
            //{
            //    TestOpenCV();
            //    return;
            //}

            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(GridsConfig), this);
                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();

                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            bool viewShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId, ViewSizePreference.UseMore);
        }
        bool HaveGotFocus = true;
        protected override void OnGotFocus(RoutedEventArgs e)
        {

            Debug.WriteLine("OnGotFocus(): {0}", HaveGotFocus);
            if (this.IsLoaded && !HaveGotFocus)
            {
                HaveGotFocus = true;
                RefreshPage();
            }
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            Debug.WriteLine("OnLostFocus(): {0}", HaveGotFocus);
            HaveGotFocus = false;
            base.OnLostFocus(e);
        }

        bool OnlyZiNo = true;
        private void NoNameSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            OnlyZiNo = !NoNameSwitch.IsOn;
            ParsePageText();
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };


        /// <summary>
        /// Display a message to the user.
        /// This method may be called from any thread.
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public void NotifyUser(string strMessage, NotifyType type)
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

            // Collapse the StatusBlock if it has no text to conserve real estate.
            StatusBorder.Visibility = (StatusBlock.Text != String.Empty) ? Visibility.Visible : Visibility.Collapsed;
            if (StatusBlock.Text != String.Empty)
            {
                StatusBorder.Visibility = Visibility.Visible;
                StatusPanel.Visibility = Visibility.Visible;
            }
            else
            {
                StatusBorder.Visibility = Visibility.Collapsed;
                StatusPanel.Visibility = Visibility.Collapsed;
            }

            // Raise an event if necessary to enable a screen reader to announce the status update.
            var peer = FrameworkElementAutomationPeer.FromElement(StatusBlock);
            if (peer != null)
            {
                peer.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateParseStatus();
        }
    }
}
