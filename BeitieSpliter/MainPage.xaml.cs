using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Windows.UI;
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
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml.Automation.Peers;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Brushes;
using Windows.ApplicationModel;
using Windows.UI.Xaml.Navigation;
using System.IO;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BeitieSpliter
{
    public sealed class BeitieImage
    {
        public BeitieImage(MainPage parent, CanvasControl cvs, StorageFile f, AutoResetEvent evt)
        {
            ParentPage = parent;
            creator = cvs;
            file = f;
            Init(evt);
        }
        public MainPage ParentPage;
        public CanvasControl creator;
        public StorageFile file;
        public float resolutionX = 0;
        public float resolutionY = 0;
        public double DipX = 0;
        public double DipY = 0;
        public string contents;
        public IRandomAccessStream iras;
        public CanvasBitmap cvsBmp;
        public bool FileSupported = true;
        public bool PageTextConfirmed = false;
        
        public void Init(AutoResetEvent evt)
        {
            GetJpgSize(evt);
        }

        public async void GetJpgSize(AutoResetEvent evt)
        {
            try
            {
                iras = await file.OpenAsync(FileAccessMode.Read);
                cvsBmp = await CanvasBitmap.LoadAsync(creator, iras);
                var sbmp = await MainPage.GetSoftwareBitmap(file);
                Common.Sleep(50);
                resolutionX = (float)cvsBmp.SizeInPixels.Width;//sbmp.PixelWidth;
                resolutionY = (float)cvsBmp.SizeInPixels.Height;//sbmp.PixelHeight;
                DipX = sbmp.DpiX;
                DipY = sbmp.DpiY;
                FileSupported = true;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.ToString());
                FileSupported = false;
            }
            evt.Set();
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
        bool XingcaoMode = false;
        GridsConfig ConfigPage = null;

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
            Common.SetWindowSize();
            this.InitializeComponent();
            InitMaps();
            Common.Init();
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
        
        void UpdateBackupColors()
        {
            BtGrids.BackupColors.Clear();
            bool NeedContinue = true;
            foreach (ColorBoxItem item in Common.LightColorItems)
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
                BtGrids.GridType = BeitieGrids.ColorType.Light;
                return;
            }

            BtGrids.BackupColors.Clear();
            BtGrids.GridType = BeitieGrids.ColorType.Dark;
            foreach (ColorBoxItem item in Common.DarkColorItems)
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
           
            foreach (ColorBoxItem item in Common.LightColorItems)
            {
                ColorBoxItems.Add(item);
            }
            foreach (ColorBoxItem item in Common.DarkColorItems)
            {
                ColorBoxItems.Add(item);
            }
            ColorBoxSelectedItem = ColorBoxItems.FirstOrDefault(f => f.Text == "黄色");
            BtGrids.PenColor = ColorBoxSelectedItem.Value;
            UpdateBackupColors();

            for (int i = 1; i < Common.DEFAULT_MAX_ROW_COLUMN; i++)
            {
                RowCount.Items.Add(i);
                ColumnCount.Items.Add(i);
            }
            for (int i = 1; i < Common.DEFAULT_MAX_PEN_WIDTH; i++)
            {
                PenWidthCombo.Items.Add(i);
            }
            RowCount.SelectedIndex = 8;
            ColumnCount.SelectedIndex = 5;
            PenWidthCombo.SelectedIndex = 1;
            
            CurrentPage.Height = ImageScrollViewer.ViewportHeight;
            CurrentPage.Width = ImageScrollViewer.ViewportWidth;
        }

        public bool InitDrawParameters()
        {
            if ((CurrentBtImage == null) ||
                (CurrentBtImage.resolutionX == 0))
            {
                return false;
            }
            BtGrids.XingcaoMode = XingcaoMode;
            BtGrids.ImageFile = CurrentBtImage.file;
            BtGrids.BtImageParent = CurrentBtImage;

            BtGrids.PenColor = ColorBoxSelectedItem.Value;
            BtGrids.PenWidth = float.Parse(PenWidthCombo.Text);

            ColumnNumber = GetColumnCount();
            RowNumber = GetRowCount();
            InitPageMargin();

            CurrentPage.Height = CurrentBtImage.resolutionY;
            CurrentPage.Width = CurrentBtImage.resolutionX;

            BtGrids.Columns = ColumnNumber;
            if (XingcaoMode)
            {
                BtGrids.Rows = 1;
                BtGrids.ElementCount = int.Parse(ZiCountBox.Text);
            }
            else
            {
                BtGrids.Rows = RowNumber;
                BtGrids.ElementCount = RowNumber * ColumnNumber;
            }
            BtGrids.DrawHeight = CurrentPage.Height - BtGrids.PageMargin.Top - BtGrids.PageMargin.Bottom;
            BtGrids.DrawWidth = CurrentPage.Width - BtGrids.PageMargin.Left - BtGrids.PageMargin.Right;

            BtGrids.OriginPoint = new Point(BtGrids.PageMargin.Left, BtGrids.PageMargin.Top);

            BtGrids.GridHeight = (float)(BtGrids.DrawHeight / RowNumber);
            BtGrids.GridWidth = (float)(BtGrids.DrawWidth / ColumnNumber);

            BtGrids.ElementRects.Clear();
            if (XingcaoMode)
            {
                BtGrids.InitXingcaoRects();
            }
            else
            {
                Point leftTop = new Point();
                for (int i = 0; i < RowNumber; i++)
                {
                    leftTop = BtGrids.OriginPoint;
                    leftTop.Y += i * BtGrids.GridHeight;
                    for (int j = 0; j < ColumnNumber; j++)
                    {
                        BtGrids.ElementRects.Add(new BeitieGridRect(new Rect(leftTop.X, leftTop.Y, 
                            BtGrids.GridWidth, BtGrids.GridHeight)));
                        leftTop.X += BtGrids.GridWidth;
                    }
                }
            }
            
            Debug.WriteLine("Image Parameter:\n col/row: ({0},{1}), element: {8}, resolution: ({2:0},{3:0})\n " +
                "PageMargin:({4},{5},{6},{7})", ColumnNumber, RowNumber, CurrentBtImage.resolutionX,
                CurrentBtImage.resolutionY, BtGrids.PageMargin.Left, BtGrids.PageMargin.Top, 
                BtGrids.PageMargin.Right, BtGrids.PageMargin.Bottom, BtGrids.ElementCount);
            return true;
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
                FileDirPath.Text = path;
                FileDirPath.Select(path.Length, 0);
                FileDirPath.SelectAll();
            }
        }

        StorageFolder BtFolder = null;
        Dictionary<int, BeitieAlbumItem> DictBtFiles = new Dictionary<int, BeitieAlbumItem>();
        private void StartFolderFiles()
        {
            if (FolderFileCombo.Items == null)
            {
                return;
            }
            if (FolderFileCombo.Items.Count <= 1)
            {
                BtnNextImg.IsEnabled = false;
                BtnPreviousImg.IsEnabled = false;
                BtnNextImg.Visibility = Visibility.Collapsed;
                BtnPreviousImg.Visibility = Visibility.Collapsed;
                FolderFileCombo.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnNextImg.IsEnabled = true;
                BtnPreviousImg.IsEnabled = true;
                BtnNextImg.Visibility = Visibility.Visible;
                BtnPreviousImg.Visibility = Visibility.Visible;
                FolderFileCombo.Visibility = Visibility.Visible;
            }
            FolderFileCombo.SelectedIndex = 0;
        }

        bool FileIsInFilterTypes(StorageFile file)
        {
            string name = file.Name.ToLower();
            foreach (string type in FILETYPE_FILTERS)
            {
                if (name.Contains(type))
                {
                    return true;
                }
            }
            return false;
        }

        private async void OnImportBeitieDir(object sender, RoutedEventArgs e)
        {
            var folderPicker = new Windows.Storage.Pickers.FolderPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop
            };
            folderPicker.FileTypeFilter.Add("*");

            BtFolder = await folderPicker.PickSingleFolderAsync();
            if (BtFolder != null)
            {
                // Application now has read/write access to all contents in the picked folder
                // (including other sub-folder contents)
                Windows.Storage.AccessCache.StorageApplicationPermissions.
                FutureAccessList.AddOrReplace("PickedFolderToken", BtFolder);

                IReadOnlyList<StorageFile> BtFolderFileList = await BtFolder.GetFilesAsync();

                if (BtFolderFileList.Count < 1)
                {
                    Common.ShowMessageDlg("所选文件夹下找不到碑帖图片！", null);
                    return;
                }

                ImageSlidePanel.Visibility = Visibility;
                FolderFileCombo.Items.Clear();
                DictBtFiles.Clear();
                int i = 0;
                int baseNo = 1;
                foreach (StorageFile file in BtFolderFileList)
                {
                    if (!FileIsInFilterTypes(file))
                    {
                        continue;
                    }
                    
                    FolderFileCombo.Items.Add(string.Format("[{0}/{1}]{2}", i+1, BtFolderFileList.Count, file.Name));
                    DictBtFiles.Add(i++, new BeitieAlbumItem(file, baseNo));
                    baseNo += (ColumnNumber * RowNumber);
                }
                StartFolderFiles();
                SetDirFilePath("文件夹: " + BtFolder.Path);
                TieAlbum.Text = BtFolder.Name;
            }
            else
            {
                SetDirFilePath(null);
            }
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
        public void InitAfterImageLoaded()
        {
            BtGrids = new BeitieGrids();
           
            // 将图片适应屏幕大小
            float factor = (float)(ImageScrollViewer.ActualWidth / CurrentBtImage.resolutionX);
            if (factor > 1F)
            {
                factor = 1F;
            }
#pragma warning disable CS0618 // Type or member is obsolete
            ImageScrollViewer.ZoomToFactor(factor);
#pragma warning restore CS0618 // Type or member is obsolete

            InitDrawParameters();
            ParsePageText();
            BtnMore.IsEnabled = true;

            RefreshPage(1);

        }
        public void BeitieImageInvalid()
        {
            Common.ShowMessageDlg("文件损坏或文件不支持!", null);
            CurrentBtImage = null;
            BtGrids = null;
        }

        private void OnImageSlide(object sender, RoutedEventArgs e)
        {
            if (sender == BtnPreviousImg)
            {
                if (FolderFileCombo.SelectedIndex >= 1)
                {
                    FolderFileCombo.SelectedIndex--;
                }
            }
            else if (sender == BtnNextImg)
            {
                if (FolderFileCombo.SelectedIndex < (DictBtFiles.Count - 1))
                {
                    FolderFileCombo.SelectedIndex++;
                }
            }
        }

        private void FolderFileCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DictBtFiles.Count < 1)
            {
                return;
            }
            foreach (KeyValuePair<int, BeitieAlbumItem> kv in DictBtFiles)
            {
                if (kv.Key == FolderFileCombo.SelectedIndex)
                {
                    OpenFile(kv.Value.file, kv.Value.no);
                    break;
                }
            }
        }

        private void RemoveFileItem(StorageFile file)
        {
            string match = "]" + file.Name;
            foreach (object item in FolderFileCombo.Items)
            {
                string strItem = item.ToString();
                int i = strItem.IndexOf(']');
                strItem = strItem.Substring(i);
                if (strItem.Equals(match))
                {
                    FolderFileCombo.Items.Remove(item);
                    break;
                }
            }
        }

        private async void OpenFile(StorageFile file, int no)
        {
            Debug.WriteLine(String.Format("Open FIle: {0}, No: {1}", file.Name, no));
            CurrentBtImage = null;
            AutoResetEvent evt = new AutoResetEvent(false);
            BeitieImage btimg = new BeitieImage(this, CurrentPage, file, evt);
            await Task.Run(() => {  evt.WaitOne(); });
            CurrentBtImage = btimg;
            if (!CurrentBtImage.FileSupported)
            {
                Common.ShowMessageDlg("文件损坏或不支持!", null);
                CurrentBtImage = null;
                BtGrids = null;
                RemoveFileItem(file);
                if (FolderFileCombo.Items.Count == 0)
                {
                    ImageSlidePanel.Visibility = Visibility.Collapsed;
                    TieAlbum.Text = "";
                }
                return;
            }
            InitAfterImageLoaded();

            StartNoBox.Text = string.Format("{0}", no);


            
        }
        private async void OnImportBeitieFile(object sender, RoutedEventArgs e)
        {
            FileOpenPicker openPicker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };

            foreach (string type in FILETYPE_FILTERS)
            {
                openPicker.FileTypeFilter.Add(type);
            }
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                ImageSlidePanel.Visibility = Visibility;
                // Application now has read/write access to the picked file
                BtFolder = null;

                DictBtFiles.Clear();
                FolderFileCombo.Items.Clear();
                FolderFileCombo.Items.Add("[1/1]" + file.Name);
                DictBtFiles.Add(0, new BeitieAlbumItem(file, 1));

                StartFolderFiles();
                
                SetDirFilePath("图片: " + file.Path);
                TieAlbum.Text = GetFileTitle(file);
            }
            else
            {
                SetDirFilePath(null);
            }
        }

        CanvasStrokeStyle StrokeStyle = new CanvasStrokeStyle()
        {
            TransformBehavior = CanvasStrokeTransformBehavior.Hairline,
            DashCap = CanvasCapStyle.Round,
            DashStyle = CanvasDashStyle.Dot,
        };
        readonly CanvasDashStyle StyleKaiElem = CanvasDashStyle.Solid;
        readonly CanvasDashStyle StyleXcElement = CanvasDashStyle.Dash;
        readonly CanvasDashStyle StyleAssistant = CanvasDashStyle.Dot;
        private void PageDrawLines(CanvasDrawingSession draw)
        {
            CanvasSolidColorBrush brush = new CanvasSolidColorBrush(draw, BtGrids.PenColor);
            float penW = BtGrids.PenWidth;

            // 将行草模式下的辅助线和元素区域使用dash和dot区分开
            if (XingcaoMode)
            {
                StrokeStyle.TransformBehavior = CanvasStrokeTransformBehavior.Hairline;
                StrokeStyle.DashStyle = StyleAssistant;
            }
            else
            {
                StrokeStyle.TransformBehavior = CanvasStrokeTransformBehavior.Normal;
                StrokeStyle.DashStyle = StyleKaiElem;
            }
            for (int i = 0; i < BtGrids.ElementRects.Count; i++)
            {
                Rect rc = BtGrids.ElementRects[i].rc;
                draw.DrawRectangle(rc, brush, penW, StrokeStyle);
                if (!BtGrids.XingcaoMode)
                {
                    int oldIndex = BtGrids.IndexToOldStyle(i);
                    if (BtGrids.ElementIsKongbai(oldIndex))
                    {
                        Common.DrawKongbaiElement(draw, rc);
                        draw.DrawRectangle(rc, Colors.Gray, BtGrids.PenWidth+1);
                    }
                }
            }
            
            if (XingcaoMode)
            {
                StrokeStyle.DashStyle = StyleXcElement;
                foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
                {
                    draw.DrawRectangle(pair.Value.rc, brush, BtGrids.PenWidth, StrokeStyle);
                }
            }
            Rect bodyRect = new Rect()
            {
                X = BtGrids.PageMargin.Left,
                Y = BtGrids.PageMargin.Top,
                Width = BtGrids.DrawWidth,
                Height = BtGrids.DrawHeight
            };
            draw.DrawRectangle(bodyRect, BtGrids.PenColor, BtGrids.PenWidth);
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

        private void CanvasDrawText(CanvasDrawingSession draw, string text, Color color)
        {
            Rect rc = new Rect()
            {
                X = 0,
                Y = 0,
                Width = CurrentPage.Width,
                Height = CurrentPage.Height / 4,
            };

            CanvasTextFormat fmt = new CanvasTextFormat()
            {
                FontSize = (int)(rc.Height * 0.2),
                HorizontalAlignment = CanvasHorizontalAlignment.Center,
                VerticalAlignment = CanvasVerticalAlignment.Center
            };

            draw.DrawText(text, rc, color, fmt);
        }

        private void CanvasDrawHelpInfo(CanvasDrawingSession draw, Color color)
        {

            string appVersion = string.Format("软件版本：{0}.{1}.{2}.{3}",
                                Package.Current.Id.Version.Major,
                                Package.Current.Id.Version.Minor,
                                Package.Current.Id.Version.Build,
                                Package.Current.Id.Version.Revision);
            string help = "\r\n\r\nCtrl+鼠标滚轮进行放大缩小\r\n\r\n" +
                "作者：卢立雪\r\n" +
                "微信：13612977027\r\n" +
                "邮箱：jackreagan@163.com\r\n\r\n" +
                appVersion + 
                "\r\n感谢使用，欢迎反馈软件问题！";

            Rect rc = new Rect()
            {
                X = 0,
                Y = CurrentPage.Height / 2,
                Width = CurrentPage.Width,
                Height = CurrentPage.Height / 4,
            };

            CanvasTextFormat fmt = new CanvasTextFormat()
            {
                FontSize = (int)(rc.Height * 0.1),
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                HorizontalAlignment = CanvasHorizontalAlignment.Center,
                VerticalAlignment = CanvasVerticalAlignment.Center
            };

            draw.DrawText(help, rc, color, fmt);
        }

        private void CurrentPage_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            Debug.WriteLine(String.Format("CurrentPage_OnDraw called"));

            var draw = args.DrawingSession;

            draw.Clear(Colors.Gray);
            if (CurrentBtImage == null)
            {
                //draw.DrawText(, new Vector2(100, 100), Colors.Black);
                CanvasDrawText(draw, "请选择书法碑帖图片!", Colors.Black);
                CanvasDrawHelpInfo(draw, Colors.White);
                return;
            }
            if (!IsParametersValid())
            {
                draw.Clear(Colors.Black);
                CanvasDrawText(draw, "参数错误，请更改参数后重试!", Colors.Red);
                return;
            }
            if (CurrentBtImage.cvsBmp == null)
            {
                draw.Clear(Colors.Black);
                CanvasDrawText(draw, "图片正在加载中...", Colors.Blue);
                RefreshPage();
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

        void UpdateColumnCount()
        {
            int columns = GetColumnCount();
            if (columns == ColumnNumber)
            {
                return;
            }
            ColumnNumber = columns;
            if (BtGrids.XingcaoMode ||
                IsColumnRowValid())
            {
                InitDrawParameters();
            }
            ParsePageText();
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
            ParsePageText();
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

        public async void SetPenColor(string clr)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ColorBoxSelectedItem = ColorBoxItems.FirstOrDefault(f => f.Text == clr);
                UpdatePenColor();
            });
        }
        public void UpdatePenColor()
        {
            BtGrids.PenColor = ColorBoxSelectedItem.Value;
            UpdateBackupColors();
            CurrentPage.Invalidate();
        }

        private void PenColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePenColor();
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
            string backupText = textbox.Text;
            if (Regex.IsMatch(textbox.Text, TotalPattern) && textbox.Text != "")
            {
                InitDrawParameters();
                CurrentPage.Invalidate();
            }
            else
            {
                Common.ShowMessageDlg("Invalid margin: " + textbox.Text, null);
                textbox.Text = backupText;
            }
        }

        public async Task<bool> SetPenWidth(string txt)
        {
            bool bRet = true;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if (Regex.IsMatch(txt, "^[\\d]+\\.?[\\d]?$") && (txt != ""))
                {
                    PenWidthCombo.Text = txt;
                    BtGrids.PenWidth = float.Parse(PenWidthCombo.Text);
                }
                else
                {
                    bRet = false;
                }
            });

            return bRet;
        }

        private void PenWidth_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            BtGrids.PenWidth = (PenWidthCombo?.SelectedIndex + 1) ?? 2;
            CurrentPage.Invalidate();
        }

        private void PenWidth_LostFocus(object sender, RoutedEventArgs e)
        {
            var combo = (ComboBox)sender;

            if (combo?.Text == "")
            {
                return;
            }

            if (Regex.IsMatch(combo.Text, "^[\\d]+\\.?[\\d]?$") && combo.Text != "")
            {
                BtGrids.PenWidth = float.Parse(PenWidthCombo.Text);
            }
            else
            {
                BtGrids.PenWidth = 2;
                Common.ShowMessageDlg("无效宽度: " + combo.Text, null);
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
        


        public static async Task<SoftwareBitmap> GetSoftwareBitmap(StorageFile inputFile)
        {
            SoftwareBitmap softwareBitmap;
            using (IRandomAccessStream stream = await inputFile.OpenAsync(FileAccessMode.Read))
            {
                // Create the decoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

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


        void UpdateCanvasBmp(CanvasBitmap bmp)
        {
            CurrentBtImage.cvsBmp = bmp;
            CurrentBtImage.resolutionY = (float)bmp.Bounds.Height;
            CurrentBtImage.resolutionX = (float)bmp.Bounds.Width;
            Debug.WriteLine("New Bitmap: {0:0},{1:0}", CurrentBtImage.resolutionX, CurrentBtImage.resolutionY);
            InitDrawParameters();
        }
        private static async Task<StorageFolder> GetSaveFolder(string dir)
        {
            //StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder picFolder = KnownFolders.PicturesLibrary;
            StorageFolder folder = await picFolder.CreateFolderAsync(dir, CreationCollisionOption.OpenIfExists);

            return folder;
        }
        private static async Task<StorageFile> SaveWriteableBitmapToFile(WriteableBitmap image, string dir, string filename)
        {
            //BitmapEncoder 存放格式
            Guid bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            if (filename.EndsWith("jpg"))
            {
                bitmapEncoderGuid = BitmapEncoder.JpegEncoderId;
            }
            else if (filename.EndsWith("png"))
            {
                bitmapEncoderGuid = BitmapEncoder.PngEncoderId;
            }
            else if (filename.EndsWith("bmp"))
            {
                bitmapEncoderGuid = BitmapEncoder.BmpEncoderId;
            }
            else if (filename.EndsWith("tiff"))
            {
                bitmapEncoderGuid = BitmapEncoder.TiffEncoderId;
            }
            else if (filename.EndsWith("gif"))
            {
                bitmapEncoderGuid = BitmapEncoder.GifEncoderId;
            }

            StorageFolder folder = await GetSaveFolder(dir);
            StorageFile outputFile = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

            using (IRandomAccessStream stream = await outputFile.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.None))
            {
                BitmapEncoder encoder = await BitmapEncoder.CreateAsync(bitmapEncoderGuid, stream);
                Stream pixelStream = image.PixelBuffer.AsStream();
                byte[] pixels = new byte[pixelStream.Length];
                await pixelStream.ReadAsync(pixels, 0, pixels.Length);

                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                          (uint)image.PixelWidth,
                          (uint)image.PixelHeight,
                          96.0,
                          96.0,
                          pixels);

                await encoder.FlushAsync();
            }
            return outputFile;
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
        async void SaveSingleCropImage(WriteableBitmap input, Rect roi, string album, string filename)
        {
            WriteableBitmap croppedBmp = input.Crop(roi);
             
            Debug.WriteLine("Save Single Crop Image: ({0:0},{3:0},{4:0},{5:0}), -> {1}\\{2}", roi.X, album, filename,
                roi.Y, roi.Width, roi.Height);
            await SaveWriteableBitmapToFile(croppedBmp, album, filename);
        }

        public void HandlerSaveSplittedImages(object para)
        {
            this.SaveSplitted.Invoke(para, null);
        }

        async void SaveSplitImagesProcWait(object para)
        {
            InitWait();
            await SaveSplitImagesProc(para);
            WaitForSaving();
            ConfigPage.HandlerShowSaveResultEvt(null);
        }

        public async void OnSaveSplitImagesDelegate(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SaveSplitImagesProcWait(sender);
            });
        }

        public enum SaveErrorType
        {
            NoPageText,
            NoSelectedItem,
            ParaError,
            Success,
        }

        public async Task<bool> NeedNotifyPageText()
        {
            bool bRet = false;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                bRet = (PageText.Text == "") && !CurrentBtImage.PageTextConfirmed;
            });
            return bRet;
        }
        public SaveErrorType SaveErrType = SaveErrorType.Success;
        public string SaveNotfInfo = "";
        public AutoResetEvent SaveEvent = new AutoResetEvent(false);
        
        public void SetWait()
        {
            SaveEvent.Set();
            Debug.WriteLine("SetWait()");
        }

        public void InitWait()
        {
            Debug.WriteLine("InitWait()");
            SaveEvent = new AutoResetEvent(false);
            //evt.Reset();
        }

        public void WaitForSaving()
        {
            Task.Run(() => {
                SaveEvent.WaitOne();
            });

            Debug.WriteLine("WaitOne() ok");
        }

        public async void SaveSplitImages(object para)
        {
            if (await NeedNotifyPageText())
            {
                if (await Common.ShowNotifyPageTextDlg())
                {
                    return;
                }
                CurrentBtImage.PageTextConfirmed = true;
            }
            InitWait();
            NotifyUser("开始保存分割单字图片...", NotifyType.StatusMessage);
            await SaveSplitImagesProc(para);
            WaitForSaving();
            SaveErrorType type = SaveErrType;
            
            if (type == SaveErrorType.NoSelectedItem)
            {
                NotifyUser("未选择元素进行保存!", NotifyType.ErrorMessage);
                Common.ShowMessageDlg("未选择元素进行保存!", null);
            }
            else if (type == SaveErrorType.ParaError)
            {
                NotifyUser(SaveNotfInfo, NotifyType.ErrorMessage);
                Common.ShowMessageDlg(SaveNotfInfo, null);
            }
            else if (type == SaveErrorType.Success)
            {
                NotifyUser(SaveNotfInfo, NotifyType.StatusMessage);
                Common.ShowMessageDlg(SaveNotfInfo, null);
            }
        }
        public bool HashsetHasValue(HashSet<string> hs, string val)
        {
            string actual;
            if (hs.TryGetValue(val, out actual))
            {
                if (actual != null)
                {
                    return true;
                }
            }
            return false;
        }
        public string GetTimeStamp()
        {
            return System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        }

        public async Task<SaveErrorType> SaveSplitImagesProc(object para)
        {
            string album = TieAlbum.Text;
            WriteableBitmap inputBitmap;
            HashSet<int> ElementIndexes = (HashSet<int>)para;
            int StartNo = int.Parse(StartNoBox.Text);
            SaveErrType = SaveErrorType.Success;

            SaveNotfInfo = "";

            if (BtGrids.IsImageRotated())
            {
                inputBitmap = await OpenWriteableBitmapFile(BtGrids.RotateFile);
            }
            else
            {
                inputBitmap = await OpenWriteableBitmapFile(CurrentBtImage.file);
            }
            
            // 从左到右，自上而下
            if (album == "")
            {
                album = GetTimeStamp();
            }

            if (ElementIndexes == null)
            {
                ElementIndexes = new HashSet<int>();
                if (BtGrids.XingcaoMode)
                {
                    foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
                    {
                        ElementIndexes.Add(pair.Key);
                    }
                }
                else
                {
                    for (int i = 0; i < BtGrids.ElementCount; i++)
                    {
                        ElementIndexes.Add(i);
                    }
                }
               
            }
            if (ElementIndexes.Count == 0)
            {
                SetWait();
                SaveErrType = SaveErrorType.NoSelectedItem;
                return SaveErrType;
            }
            HashSet<string> SaveFileNames = new HashSet<string>();


            int saveCount = 0;
            foreach (int index in ElementIndexes)
            {
                Rect roi = new Rect();
                BeitieElement element = BtGrids.Elements[index];
                if (element.type == BeitieElement.BeitieElementType.Kongbai)
                {
                    continue;
                }

                string filename = "";
                if (!element.NeedAddNo())
                {
                    filename = string.Format("{0}.jpg", element.content);
                }
                else
                {
                    filename = string.Format("{0}", element.no + StartNo);
                    if (element.content != "")
                    {
                        filename += "-" + element.content;
                    }
                    if (HashsetHasValue(SaveFileNames, filename))
                    {
                        filename += "-" + GetTimeStamp();
                    }

                    SaveFileNames.Add(filename);
                    filename += ".jpg";
                }


                if (!BtGrids.GetElementRoi(index, ref roi))
                {
                    SaveNotfInfo += "保存图片" + filename + "出现错误!";
                    SaveErrType = SaveErrorType.ParaError;
                    continue;
                }

                try
                {
                    SaveSingleCropImage(inputBitmap, roi, album, filename);
                    saveCount++;

                }
                catch (Exception err)
                {
                    Debug.WriteLine(err.ToString());
                }
            }
            if (SaveErrType == SaveErrorType.Success)
            {
                StorageFolder folder = await GetSaveFolder(album);
                SaveNotfInfo = string.Format("单字分割图片({0}张)已保存到文件夹{1}",
                    saveCount, folder.Path);
            }

            SetWait();
            return SaveErrType;
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
        private List<string> FILETYPE_FILTERS = new List<string>();
        private void InitMaps()
        {
            IGNORED_CHARS.Add(',');
            IGNORED_CHARS.Add('.');
            IGNORED_CHARS.Add(';');
            IGNORED_CHARS.Add('(');
            IGNORED_CHARS.Add(')');
            IGNORED_CHARS.Add('>');
            IGNORED_CHARS.Add('<');
            IGNORED_CHARS.Add('[');
            IGNORED_CHARS.Add(']');
            IGNORED_CHARS.Add('\'');
            IGNORED_CHARS.Add('"');

            IGNORED_CHARS.Add('，');
            IGNORED_CHARS.Add('。');
            IGNORED_CHARS.Add('；');
            IGNORED_CHARS.Add('、');
            IGNORED_CHARS.Add('（');
            IGNORED_CHARS.Add('）');
            IGNORED_CHARS.Add('“');
            IGNORED_CHARS.Add('”');
            IGNORED_CHARS.Add('《');
            IGNORED_CHARS.Add('》');
            IGNORED_CHARS.Add('〈');
            IGNORED_CHARS.Add('〉');

            FILETYPE_FILTERS.Add(".jpg");
            FILETYPE_FILTERS.Add(".jpeg");
            FILETYPE_FILTERS.Add(".png");
            FILETYPE_FILTERS.Add(".bmp");
            FILETYPE_FILTERS.Add(".gif");

        }

        private void UpdateParseStatus()
        {
            int CharCount = 0;
            int LostCharCount = 0;
            int SpaceCount = 0;
            int SealCount = 0;
            int OtherCount = 0;
            foreach (KeyValuePair<int, BeitieElement> pair in BtGrids.Elements)
            {
                switch (pair.Value.type)
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

        private void LostFocus_StartNoBox(object sender, RoutedEventArgs e)
        {
            int curZiNo = int.Parse(StartNoBox.Text);
            UpdateBeitieAlbumNo(curZiNo);
            RefreshPage();
        }
        void UpdateBeitieAlbumNo(int curZiNO)
        {
            KeyValuePair<int, BeitieAlbumItem> prev;
            int selectedIndex = FolderFileCombo.SelectedIndex;
            foreach (KeyValuePair<int, BeitieAlbumItem> kv in DictBtFiles)
            {
                if (kv.Key < selectedIndex)
                {
                    //
                }
                else if (kv.Key == selectedIndex)
                {
                    kv.Value.no = curZiNO;
                }
                else if (prev.Value != null)
                {
                    kv.Value.no = prev.Value.NumberedCount + prev.Value.no;
                }
                prev = kv;
            }
        }
        void UpdateBeitieAlbumItemNo(int curMaxZiNo)
        {
            KeyValuePair<int, BeitieAlbumItem> prev;
            foreach (KeyValuePair<int, BeitieAlbumItem> kv in DictBtFiles)
            {
                if (kv.Key == FolderFileCombo.SelectedIndex)
                {
                    kv.Value.NumberedCount = curMaxZiNo;
                }
                if (prev.Value != null)
                {
                    kv.Value.no = prev.Value.NumberedCount + prev.Value.no;
                }
                prev = kv;
            }
        }

        private void ParsePageText()
        {
            string txt = PageText.Text;
            int length = txt.Length;
            char single;
            bool specialTypeDetected = false;
            int OthersNo = 0;
            int YinzhangNo = 0;
            int QueziNo = 0;
            int ZiNo = 0;
            StringBuilder sb = new StringBuilder();

            BtGrids.Elements.Clear();
            if (length == 0)
            {
                length = BtGrids.ElementCount;
                for (int i = 0; i < length; i++)
                {
                    BtGrids.Elements.Add(i, new BeitieElement(BeitieElement.BeitieElementType.Zi,
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
                        BtGrids.AddElement(i, new BeitieElement(BeitieElement.BeitieElementType.Quezi,
                            name, ZiNo++)
                        {
                            text = name
                        });
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
                        int no = 0;

                        if (name.Contains("印"))
                        {
                            type = BeitieElement.BeitieElementType.Yinzhang;
                            no = ++YinzhangNo;
                        }
                        else if (name.Length == 0)
                        {
                            type = BeitieElement.BeitieElementType.Kongbai;
                        }
                        else if (name.Contains("缺"))
                        {
                            type = BeitieElement.BeitieElementType.Quezi;
                            no = ++QueziNo;
                        }
                        else
                        {
                            type = BeitieElement.BeitieElementType.Other;
                            no = ++OthersNo;
                        }
                        BeitieElement newBe = new BeitieElement(type, name, BtGrids.OnlyZiNo ? -1 : ZiNo++)
                        {
                            text = sb.ToString()
                        };
                        if (type != BeitieElement.BeitieElementType.Kongbai)
                        {
                            newBe.AddSuffix(no);
                        }
                        BtGrids.AddElement(ZiNo, newBe);
                        sb.Clear();
                    }
                    else if (specialTypeDetected)
                    {
                        sb.Append(single);
                    }
                    else
                    {
                        string content = new string(single, 1);
                        BtGrids.AddElement(ZiNo, new BeitieElement(BeitieElement.BeitieElementType.Zi, content, ZiNo++)
                            { text = content});
                    }
                }
                if (BtGrids.XingcaoMode)
                {
                    BtGrids.UpdateElementCount(BtGrids.Elements.Count);
                    ZiCountBox.Text = string.Format("{0}", BtGrids.ElementCount);
                }
                else
                {
                    int sizeDelta = BtGrids.ElementCount - BtGrids.Elements.Count;
                    if (sizeDelta > 0)
                    {
                        int count = BtGrids.Elements.Count;
                        for (int i = 1; i < sizeDelta; i++)
                        {
                            BtGrids.AddElement(count + i, new BeitieElement(BeitieElement.BeitieElementType.Zi,
                                                "", ZiNo++));
                        }
                    }
                }
            }
            UpdateBeitieAlbumItemNo(ZiNo);
            UpdateParseStatus();
        }

        private void PageText_LostFocus(object sender, RoutedEventArgs e)
        {
            ParsePageText();
            RefreshPage();
        }

        private static async Task<WriteableBitmap> OpenWriteableBitmapFile(StorageFile file)
        {
            using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
                WriteableBitmap image = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                image.SetSource(stream);

                return image;
            }
        }

        public async Task<CanvasBitmap> RotateImage(float angle)
        {
            WriteableBitmap wbmp = await OpenWriteableBitmapFile(CurrentBtImage.file);
            WriteableBitmap rotatedWbmp = wbmp.RotateFree(angle);
            BtGrids.RotateFile = await SaveWriteableBitmapToFile(rotatedWbmp, "Tmp", "Rotate.jpg");

            var iras = await BtGrids.RotateFile.OpenAsync(FileAccessMode.Read);
            return await CanvasBitmap.LoadAsync(CurrentPage, iras);
        }


        int SettingPageViewID = 0;
        bool SettingPageShown = false;
        bool SettingPageClosed = false;
        public void SetConfigPage(GridsConfig page)
        {
            ConfigPage = page;
        }

        private async void EnableRowColumn(bool bEnable)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                ColumnCount.IsEnabled = bEnable;
                RowCount.IsEnabled = bEnable;
                XingcaoModeCheck.IsEnabled = bEnable;
                ImportBtFile.IsEnabled = bEnable;
                ImportBtDir.IsEnabled = bEnable;
                if (XingcaoMode)
                {
                    ZiCountBox.IsEnabled = bEnable;
                    PageText.IsEnabled = bEnable;
                }
            });
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

            WriteableBitmap inputBitmap = await OpenWriteableBitmapFile(CurrentBtImage.file);


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

                WriteableBitmap outputBitmap = inputBitmap;

                // Operate on the image in the manner chosen by the user.
                if (currentOperation == OpenCVOperationType.Blur)
                {

                }
                else if (currentOperation == OpenCVOperationType.HoughLines)
                {

                }
                else if (currentOperation == OpenCVOperationType.Contours)
                {

                }
                else if (currentOperation == OpenCVOperationType.Histogram)
                {

                }
                else if (currentOperation == OpenCVOperationType.MotionDetector)
                {

                }
                else if (currentOperation == OpenCVOperationType.Crop)
                {
                    outputBitmap = inputBitmap.Crop((int)BtGrids.PageMargin.Left, (int)BtGrids.PageMargin.Top,
                        (int)BtGrids.PageMargin.Right, (int)BtGrids.PageMargin.Bottom);
                }
                else if (currentOperation == OpenCVOperationType.Rotate)
                {
                    outputBitmap = inputBitmap.RotateFree(angleRotate);
                    angleRotate -= 2;

                }
                //currentOperation++;
                if (currentOperation > OpenCVOperationType.MotionDetector)
                {
                    currentOperation = OpenCVOperationType.Crop;
                }
                await SaveWriteableBitmapToFile(outputBitmap, "Test", "show.jpg");
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
            Debug.WriteLine("ID: {0}, shown: {1}, closed: {2}", SettingPageViewID,
                SettingPageShown, SettingPageClosed);
            var views = CoreApplication.Views;
            if (views.Count > 1)
            {
                if (!SettingPageClosed)
                {
                    await ApplicationViewSwitcher.SwitchAsync(SettingPageViewID);
                    return;
                }
            }
            SettingPageClosed = false;
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(GridsConfig), this);

                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();
                var newAppView = ApplicationView.GetForCurrentView();
                newAppView.Consolidated += NewAppView_Consolidated;
                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            SettingPageViewID = newViewId;
            SettingPageShown = await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId, ViewSizePreference.Custom);
            if (SettingPageShown)
            {
                EnableRowColumn(false);
            }
        }
        private void NewAppView_Consolidated(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            Debug.WriteLine("NewAppView_Consolidated()");
            SettingPageClosed = true;
            EnableRowColumn(true);
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

        private void NoNameSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            BtGrids.OnlyZiNo = !NoNameSwitch.IsOn;
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
        public int PageViewId = 0;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            InitControls();
            UpdateParseStatus();
            
            UpdateColumnCount();
            UpdateRowCount();
            this.SaveSplitted += new EventHandler(this.OnSaveSplitImagesDelegate);
            //ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Maximized;
            Debug.WriteLine("Actual W/H: {0},{1}, W/H: {2}，{3},",
               this.ActualWidth, this.ActualHeight, this.Height, this.Width);
            PageViewId = ApplicationView.GetForCurrentView().Id;

            var CurrentView = ApplicationView.GetForCurrentView();
            ApplicationView.TerminateAppOnFinalViewClose = false;
            CurrentView.Consolidated += ConsolidatedMainView;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("OnNavigatedFrom()");
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("OnNavigatedFrom()");
            base.OnNavigatedTo(e);
        }
        private void ConsolidatedMainView(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            //if (!SettingPageClosed)
            //{
            //   await ApplicationViewSwitcher.SwitchAsync(PageViewId,
            //                         SettingPageViewID,
            //                        ApplicationViewSwitchingOptions.ConsolidateViews);
            //}
            CoreApplication.Exit();
        }

        private void XincaoModeCheck_Clicked(object sender, RoutedEventArgs e)
        {
            if (XingcaoModeCheck == null)
            {
                return;
            }
            if (!XingcaoModeCheck?.IsChecked ?? false)
            {
                XingcaoMode = false;

                RowCount.Visibility = Visibility.Visible;
                RowCountTitle.Visibility = Visibility.Visible;
                ZiCountBox.Visibility = Visibility.Collapsed;
                ZiCountTitle.Visibility = Visibility.Collapsed;
            }
            else
            {
                XingcaoMode = true;
                RowCount.Visibility = Visibility.Collapsed;
                RowCountTitle.Visibility = Visibility.Collapsed;
                ZiCountBox.Visibility = Visibility.Visible;
                ZiCountTitle.Visibility = Visibility.Visible;
            }

            InitDrawParameters();
            ParsePageText();
            RefreshPage();
        }

        private void ZiCount_LostFocus(object sender, RoutedEventArgs e)
        {
            InitDrawParameters();
            RefreshPage();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (CurrentBtImage == null)
            { 
                CurrentPage.Height = ImageScrollViewer.ViewportHeight;
                CurrentPage.Width = ImageScrollViewer.ViewportWidth;
                RefreshPage();  
            }
            
        }

    }
}
