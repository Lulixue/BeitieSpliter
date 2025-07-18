﻿using System;
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
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.ApplicationModel.Resources;
using static BeitieSpliter.LanguageHelper;
using Newtonsoft.Json;
using Windows.ApplicationModel.DataTransfer;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BeitieSpliter
{
    public sealed class BeitieLocation
    { 
        public int l { set; get; }
        public int r { set; get; }
        public int t { set; get; }
        public int b { set; get; }
        public int w { set; get; }
        public int h { set; get; }
    }
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
        public float ratioY = 1f;
        public float ratioX = 1f;
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
            if (GlobalSettings.MultiWindowMode)
            {
                Common.SetWindowSize();
            }
            this.InitializeComponent();
            InitMaps();
            
            Common.Init();
        }

        private void ColumnIllegalHandler(IUICommand command)
        {

            ColumnCount.SelectedIndex = GlobalSettings.LastSelectedColumn;
        }
        private void RowIllegalHandler(IUICommand command)
        {
            RowCount.SelectedIndex = GlobalSettings.LastSelectedRow;
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
                Common.ShowMessageDlg(/*"列数非法: "*/GetPlainString(StringItemType.InvalidColumn) + ColumnCount.Text, new UICommandInvokedHandler(ColumnIllegalHandler));
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
               Common.ShowMessageDlg(/*"行数非法: "*/GetPlainString(StringItemType.InvalidRow) + RowCount.Text, new UICommandInvokedHandler(RowIllegalHandler));
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
            foreach (string grade in Common.TEXT_SIZE_GRADES)
            {
                TextSizeGrade.Items.Add(grade);
            }
            TextSizeGrade.SelectedIndex = 2;
           
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
            if (GlobalSettings.LastSelectedRow < RowCount.Items.Count)
            { 
                RowCount.SelectedIndex = GlobalSettings.LastSelectedRow;
            } 
            else
            {
                RowCount.Text = GlobalSettings.LastSelectedRow.ToString();
            }

            if (GlobalSettings.LastSelectedColumn < ColumnCount.Items.Count)
            {
                ColumnCount.SelectedIndex = GlobalSettings.LastSelectedColumn;
            } 
            else
            {
                ColumnCount.Text = GlobalSettings.LastSelectedColumn.ToString();
            }
            PenWidthCombo.SelectedIndex = 1;

            PenColorCombo.MinWidth = PenColorCombo.ActualWidth;
            CurrentPage.Height = ImageScrollViewer.ViewportHeight;
            CurrentPage.Width = ImageScrollViewer.ViewportWidth;

            StartNoBox.MaxWidth = StartNoBox.ActualWidth;
            TieAlbum.Width = TieAlbum.ActualWidth;
            ColumnCount.MinWidth = ColumnCount.ActualWidth;
            RowCount.MinWidth = RowCount.ActualWidth;
            PageText.MaxWidth = PageText.ActualWidth;
            
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
            var ratioXy = CurrentBtImage.resolutionX / CurrentBtImage.resolutionY;
            var maxSize = 16384;
            if (CurrentBtImage.resolutionX > maxSize || CurrentBtImage.resolutionY > maxSize)
            {
                if (ratioXy > 1f)
                {
                    CurrentPage.Width = maxSize;
                    CurrentPage.Height = maxSize / ratioXy;
                } 
                else
                {
                    CurrentPage.Height = maxSize;
                    CurrentPage.Width = maxSize * ratioXy;
                } 
                CurrentBtImage.ratioX = (float)(CurrentBtImage.resolutionX * 1.0 / CurrentPage.Width);
                CurrentBtImage.ratioY = (float)(CurrentBtImage.resolutionY * 1.0 / CurrentPage.Height);
            } 
            else
            {
                CurrentPage.Height = CurrentBtImage.resolutionY;
                CurrentPage.Width = CurrentBtImage.resolutionX;
            }

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

        private async void loadFolder(StorageFolder folder)
        {
            BtFolder = folder;
            // Application now has read/write access to all contents in the picked folder
            // (including other sub-folder contents)
            Windows.Storage.AccessCache.StorageApplicationPermissions.
            FutureAccessList.AddOrReplace("PickedFolderToken", BtFolder);

            IReadOnlyList<StorageFile> BtFolderFileList = await BtFolder.GetFilesAsync();

            if (BtFolderFileList.Count < 1)
            {
                Common.ShowMessageDlg(/*"所选文件夹下找不到碑帖图片！"*/GetPlainString(StringItemType.PictureNotFound), null);
                return;
            }

            ImageSlidePanel.Visibility = Visibility;
            FolderFileCombo.Items.Clear();
            DictBtFiles.Clear();
            int i = 0;
            int baseNo = 1;
            int pageSize = (ColumnNumber * RowNumber);
            foreach (StorageFile file in BtFolderFileList)
            {
                if (!FileIsInFilterTypes(file))
                {
                    continue;
                }

                FolderFileCombo.Items.Add(string.Format("[{0}/{1}]{2}", i + 1, BtFolderFileList.Count, file.Name));
                DictBtFiles.Add(i++, new BeitieAlbumItem(file, baseNo));
                baseNo += pageSize;
            }
            StartFolderFiles();
            SetDirFilePath(/*"文件夹: "*/GetPlainString(StringItemType.Folder) + BtFolder.Path);
            TieAlbum.Text = BtFolder.Name;
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
                loadFolder(BtFolder);
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

        public static void ImgAutoFitScrollView(BeitieImage img, ScrollViewer scroll)
        {
            // 将图片适应屏幕大小
            float factor = (float)(scroll.ActualWidth / img.resolutionX);
            if (factor > 1F)
            {
                factor = 1F;
            }

            scroll.ChangeView(0, 0, factor);

        }


        public void InitAfterBack()
        {
            CurrentPage.Height = CurrentBtImage.resolutionY;
            CurrentPage.Width = CurrentBtImage.resolutionX;
        }
        public void InitAfterImageLoaded()
        {
            BtGrids = new BeitieGrids();

            ImgAutoFitScrollView(CurrentBtImage, ImageScrollViewer);
            InitDrawParameters();
            ParsePageText();
            BtnMore.IsEnabled = true;

            int larger = (int)Common.GetLargerOne(CurrentBtImage.resolutionX, CurrentBtImage.resolutionY);
            int size = (larger / Common.PEN_WIDTH_DIVIDER);
            if (size < Common.DEFAULT_PEN_WIDTH)
            {
                size = Common.DEFAULT_PEN_WIDTH;
            }
            else
            {
                size += 1;
                size = Common.GetLessOne(size, PenWidthCombo.Items.Count); 
            } 
            PenWidthCombo.SelectedIndex = size - 1;
            RefreshPage(1);

        }
        public void BeitieImageInvalid()
        {
            Common.ShowMessageDlg(/*"文件损坏或不支持!"*/GetPlainString(StringItemType.FileBroken), null);
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
                else // first
                {
                    FolderFileCombo.SelectedIndex = (DictBtFiles.Count - 1);
                }
            }
            else if (sender == BtnNextImg)
            {
                if (FolderFileCombo.SelectedIndex < (DictBtFiles.Count - 1))
                {
                    FolderFileCombo.SelectedIndex++;
                }
                else // last
                {
                    FolderFileCombo.SelectedIndex = 0;
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
                    PageText.Text = "";
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
                Common.ShowMessageDlg(/*"文件损坏或不支持!"*/GetPlainString(StringItemType.FileBroken), null);
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

        Regex regex =  new Regex("(\\d+)-(\\S).jpg.json");

        private async void LoadDropFile(StorageFile file)
        {
            var result = regex.Match(file.Name);
            if (!result.Success)
            {
                LoadFile(file);
                return;
            }

            var number = result.Groups[1].ToString();

            this.PageText.Text = result.Groups[2].ToString();
            this.StartNoBox.Text = number;
            XingcaoModeCheck.IsChecked = true;
            var lines = await FileIO.ReadTextAsync(file);
            var location = JsonConvert.DeserializeObject<BeitieLocation>(lines);
            var offset = 0;
            var x = location.l - offset;
            BtGrids.XingcaoElements.Clear();
            BtGrids.XingcaoElements.Add(0, new BeitieGridRect(new Rect(x, location.t,
                location.r - location.l, location.b - location.t))
            {
                col = 0,
                revised = true
            });
            PageText_LostFocus(null, null);
        }

        private void LoadFile(StorageFile file)
        {
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

                SetDirFilePath(/*"图片: " */GetPlainString(StringItemType.Picture) + file.Path);
                TieAlbum.Text = GetFileTitle(file);
            }
            else
            {
                SetDirFilePath(null);
            }

        }

        private int FilePickerID = 1;
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
            StorageFile file = await openPicker.PickSingleFileAsync((++FilePickerID).ToString());
            LoadFile(file);
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

            string appVersion = string.Format(/*"软件版本：{0}.{1}.{2}.{3}"*/GetPlainString(StringItemType.SoftwareVersion),
                                Package.Current.Id.Version.Major,
                                Package.Current.Id.Version.Minor,
                                Package.Current.Id.Version.Build,
                                Package.Current.Id.Version.Revision);
            string help =  GetPlainString(StringItemType.SplashInfoFirstPart) + 
                appVersion +  GetPlainString(StringItemType.SplashInfoSecondPart);

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

        private string GetPlainString(StringItemType type)
        {
            return LanguageHelper.GetPlainString(type);
        }

        private void CurrentPage_OnDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            Debug.WriteLine(String.Format("CurrentPage_OnDraw called"));

            var draw = args.DrawingSession;

            draw.Clear(Colors.Gray);
            if (CurrentBtImage == null)
            {
                //draw.DrawText(, new Vector2(100, 100), Colors.Black);
                CanvasDrawText(draw, /*"请选择书法碑帖图片!"*/GetPlainString(StringItemType.PleaseChooseBeitie), Colors.Black);
                CanvasDrawHelpInfo(draw, Colors.White);
                return;
            }
            if (!IsParametersValid())
            {
                draw.Clear(Colors.Black);
                CanvasDrawText(draw, /*"参数错误，请更改参数后重试!"*/GetPlainString(StringItemType.InvalidParam), Colors.Red);
                return;
            }
            if (CurrentBtImage.cvsBmp == null)
            {
                draw.Clear(Colors.Black);
                CanvasDrawText(draw, /*"图片正在加载中..."*/GetPlainString(StringItemType.ImageLoading), Colors.Blue);
                RefreshPage();
                return;
            }

            draw.DrawImage(CurrentBtImage.cvsBmp);
            PageDrawLines(draw); 
        }

        private void Page_OnUnloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine(String.Format("Page_OnUnloaded called")); 
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
                GlobalSettings.LastSelectedColumn = columns-1;
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
                GlobalSettings.LastSelectedRow = rows-1;
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
            string TotalPattern = "^([0-9]+),?([0-9]+)?,?([0-9]+)?,?([0-9]+)?$";
            var textbox = (TextBox)sender;
            if (Regex.IsMatch(textbox.Text, TotalPattern) && textbox.Text != "")
            {
                InitDrawParameters();
                CurrentPage.Invalidate();
            }
            else
            {
                Common.ShowMessageDlg(/*"Invalid margin: " */GetPlainString(StringItemType.InvalidMargin) + textbox.Text, null);
                textbox.Text = Common.DEFAULT_MARGIN;
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

        private void PenWidth_LostFocus(object obj, RoutedEventArgs e)
        {
            try
            {
                BtGrids.PenWidth = float.Parse(PenWidthCombo.Text);
            }
            catch
            {
                Common.ShowMessageDlg(/*"无效宽度: "*/GetPlainString(StringItemType.InvalidWidth) + PenWidthCombo.Text, null);
                BtGrids.PenWidth = Common.DEFAULT_PEN_WIDTH;
                PenWidthCombo.Text = Common.DEFAULT_PEN_WIDTH.ToString();
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
        private static async Task<StorageFolder> GetSaveCopyFolder(string dir)
        {
            //StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder picFolder = KnownFolders.PicturesLibrary;
            StorageFolder folder = await picFolder.CreateFolderAsync(dir + " - 副本", CreationCollisionOption.OpenIfExists);

            return folder;
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
            StorageFolder copyFolder = await GetSaveCopyFolder(dir);
            StorageFile outputFile = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
            StorageFile outputCopyFile = await copyFolder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);

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
            using (IRandomAccessStream stream = await outputCopyFile.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.None))
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

            var location = new BeitieLocation
            {
                l = (int)roi.Left + imageXOffset,
                r = (int)roi.Right + imageXOffset,
                t = (int)roi.Top,
                b = (int)roi.Bottom,
                w = (int)input.PixelWidth,
                h = (int)input.PixelHeight
            };


            StorageFolder folder = await GetSaveFolder(album);
            StorageFile outputFile = await folder.CreateFileAsync(filename + ".json", CreationCollisionOption.ReplaceExisting);
            var data = await outputFile.OpenStreamForWriteAsync();

            using (StreamWriter r = new StreamWriter(data))
            {
                var serelizedfile = JsonConvert.SerializeObject(location);
                r.Write(serelizedfile);
            }
            data.Close();

            Debug.WriteLine("Save Single Crop Image: ({0:0},{3:0},{4:0},{5:0}), -> {1}\\{2}", roi.X, album, filename,
                roi.Y, roi.Width, roi.Height);
            await SaveWriteableBitmapToFile(croppedBmp, album, filename);
        }

        public void HandlerSaveSplittedImages(object para)
        {
            this.SaveSplitted.Invoke(para, null);
        }
        private bool saving = false;
        async void SaveSplitImagesProcWait(object para)
        {
            if (saving)
            {
                return;
            }
            saving = true;
            InitWait();
            await SaveSplitImagesProc(para);
            WaitForSaving();
            ConfigPage.HandlerShowSaveResultEvt(null);
            saving = false;
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
            NoChinese,
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
            NotifyUser(/*"开始保存分割单字图片..."*/GetPlainString(StringItemType.BeginSaving), NotifyType.StatusMessage);
            await SaveSplitImagesProc(para);
            WaitForSaving();
            SaveErrorType type = SaveErrType;
            
            if (type == SaveErrorType.NoSelectedItem)
            {
                string notfInfo = GetPlainString(StringItemType.NoElemSelected);
                NotifyUser(/*"未选择元素进行保存!"*/notfInfo, NotifyType.ErrorMessage);
                Common.ShowMessageDlg(/*"未选择元素进行保存!"*/notfInfo, null);
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
            else if (type == SaveErrorType.NoChinese)
            { 
                NotifyUser(SaveNotfInfo, NotifyType.ErrorMessage);
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
            string album = TieAlbum.Text + "_" + GetPlainString(StringItemType.SingleChar);
            WriteableBitmap inputBitmap;
            HashSet<int> ElementIndexes = (HashSet<int>)para;
            int StartNo = int.Parse(StartNoBox.Text);
            SaveErrType = SaveErrorType.Success;

            SaveNotfInfo = "";

            if (BtGrids.IsImageRotated() && BtGrids.RotateFile != null)
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
            // 生成全部
            if (ElementIndexes == null)
            {
                int noCount = BtGrids.GetNumberedCount();
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
                // 全部生成的时候才计算编号
                if (noCount > 0)
                {
                    if (FolderFileCombo.Items.Count > 1)
                    {  
                        UpdateBeitieAlbumNo(StartNo, noCount);
                    }
                }
            }
            if (ElementIndexes.Count == 0)
            {
                SetWait();
                SaveErrType = SaveErrorType.NoSelectedItem;
                return SaveErrType;
            }
            
            foreach (int index in ElementIndexes)
            {  
                BeitieElement element = BtGrids.Elements[index];
                if (element.type == BeitieElement.BeitieElementType.Kongbai)
                {
                    continue;
                }
                if (element.content.Length == 0)
                { 
                    SetWait(); 
                    SaveErrType = SaveErrorType.NoChinese;
                    return SaveErrType;
                }
            }

            HashSet<string> SaveFileNames = new HashSet<string>();

            string noFormatter = string.Format("{0}{1}{2}", "{0:D", TextSizeGrade.SelectedIndex + 2, "}");
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
                    filename = string.Format(noFormatter, element.no + StartNo);
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
                    SaveNotfInfo += string.Format(/*"保存图片{0}出现错误!"*/GetPlainString(StringItemType.SaveErrorFmt), filename);
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
                SaveNotfInfo = string.Format(/*"单字分割图片({0}张)已保存到文件夹{1}"*/GetPlainString(StringItemType.NotfAfterSaveFmt),
                    saveCount, folder.Path);
            }

            SetWait();
            return SaveErrType;
        }
        private async void OnSaveSplitImages(object sender, RoutedEventArgs e)
        {
            //if (CurrentBtImage == null)
            //{
            //    Common.ShowMessageDlg(/*"请选择书法碑帖图片!"*/GetPlainString(StringItemType.PleaseChooseBeitie), null);
            //    return;
            //}
            //SaveSplitImages(null);

            StorageFolder picFolder = KnownFolders.PicturesLibrary;
            StorageFolder folder = await picFolder.CreateFolderAsync("location", CreationCollisionOption.OpenIfExists);

            IReadOnlyList<StorageFile> BtFolderFileList = await folder.GetFilesAsync();
            var ordered = BtFolderFileList.OrderBy(f => f.Name);

            Dictionary<int, Rect> rects = new Dictionary<int, Rect>();
            var counter = 0;
            foreach (var file in ordered)
            {
                if (file.Name.Contains(".json"))
                {
                    var lines = await FileIO.ReadTextAsync(file);
                    var location = JsonConvert.DeserializeObject<BeitieLocation>(lines);
                    var offset = 0;
                    var x = location.l - offset; 
                    BtGrids.XingcaoElements.Add(counter, new BeitieGridRect(new Rect(x, location.t,
                        location.r - location.l, location.b - location.t))
                    {
                        col = counter,
                        revised = true 
                    });
                    counter++;
                }
            }
        }
        private List<char> IGNORED_CHARS = new List<char>();
        private List<string> FILETYPE_FILTERS = new List<string>();
        private void InitMaps()
        {
            IGNORED_CHARS.Add('\r');
            IGNORED_CHARS.Add('\n');
            IGNORED_CHARS.Add('\t');
            IGNORED_CHARS.Add(' ');
            IGNORED_CHARS.Add(',');
            IGNORED_CHARS.Add('.');
            IGNORED_CHARS.Add(':');
            IGNORED_CHARS.Add(';');
            IGNORED_CHARS.Add('(');
            IGNORED_CHARS.Add(')');
            IGNORED_CHARS.Add('>');
            IGNORED_CHARS.Add('<');
            IGNORED_CHARS.Add('[');
            IGNORED_CHARS.Add(']');
            IGNORED_CHARS.Add('\'');
            IGNORED_CHARS.Add('"');
            IGNORED_CHARS.Add('!');

            IGNORED_CHARS.Add('　');
            IGNORED_CHARS.Add('，');
            IGNORED_CHARS.Add('。');
            IGNORED_CHARS.Add('；');
            IGNORED_CHARS.Add('：');
            IGNORED_CHARS.Add('！');
            IGNORED_CHARS.Add('、');
            IGNORED_CHARS.Add('（');
            IGNORED_CHARS.Add('）');
            IGNORED_CHARS.Add('／');
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
            FILETYPE_FILTERS.Add(".tif");

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
                info += string.Format(/*"图片尺寸: {0:0}*{1:0}, "*/GetPlainString(StringItemType.ImageSizeFmt), CurrentBtImage.resolutionX, CurrentBtImage.resolutionY);
            }
            info += string.Format(/*"当前碑帖: {0}, 起始单字编号: {1}({2}), "*/GetPlainString(StringItemType.CurrentBeitieStartNoFmt), TieAlbum.Text, StartNoBox.Text,
                NoNameSwitch.IsOn ? NoNameSwitch.OnContent : NoNameSwitch.OffContent);
            info += string.Format("字: {0}, {9}({5}): {1}, 印章({6}): {2}, 空白({7}): {3}, 其他({8}): {4}",
                CharCount, LostCharCount, SealCount, SpaceCount, OtherCount,
                "{缺}/□", "{印:}", "{}", "{XX}", GetPlainString(StringItemType.Quezi));

            NotifyUser(info, NotifyType.StatusMessage);
        }

        private void LostFocus_StartNoBox(object sender, RoutedEventArgs e)
        {
            try
            {
                int.Parse(StartNoBox.Text);
            }
            catch (OverflowException)
            {
                Common.ShowMessageDlg(
                    string.Format(/*"编号({0})超出范围, 请重新输入!"*/GetPlainString(StringItemType.StartNoErrorFmt),
                                        StartNoBox.Text), 
                    null);
                StartNoBox.Text = "1";
            }
        }
        void UpdateBeitieAlbumNo(int pageZiNo, int pageZiCount)
        {
            int selectedIndex = FolderFileCombo.SelectedIndex;
            int columns = GetColumnCount();
            int rows = GetRowCount();
            int pageSize = rows * columns;

            var currentFile = DictBtFiles.ElementAt(selectedIndex);
            currentFile.Value.no = pageZiNo;
            currentFile.Value.NumberedCount = pageZiCount;

            int endIndex = DictBtFiles.Count-1; 

            for (int i = selectedIndex+1; i <= endIndex; i++)
            {
                var previous = DictBtFiles.ElementAt(i-1);
                var current = DictBtFiles.ElementAt(i);

                current.Value.no = previous.Value.no + previous.Value.NumberedCount;
                // 行草状态下只计算下一个的编号
                if (XingcaoMode)
                {
                    break;
                }
                else if (current.Value.NumberedCount == 0)
                {
                    current.Value.NumberedCount = pageSize;
                }
                else if (current.Value.NumberedCount > pageSize)
                {
                    current.Value.NumberedCount = pageSize;
                } 
                 
            }

        }

        private void ParsePageText()
        {
            string txt = PageText.Text;
            char single;
            bool specialTypeDetected = false;
            int OthersNo = 0;
            int YinzhangNo = 0;
            int QueziNo = 0;
            int ZiNo = 0;
            StringBuilder sb = new StringBuilder();

            if (FilterSwitch.IsOn)
            {
                var reg = new Regex("[(（](.*?)[)）]");
                txt = reg.Replace(txt, "");
            }
            int length = txt.Length;

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
                        BtGrids.AddElement(BtGrids.Elements.Count, 
                            new BeitieElement(BeitieElement.BeitieElementType.Quezi,
                            name, BtGrids.Elements.Count)
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
                        BeitieElement newBe = new BeitieElement(type, name, BtGrids.OnlyZiNo ? -1 : BtGrids.Elements.Count)
                        {
                            text = sb.ToString()
                        };
                        if (type != BeitieElement.BeitieElementType.Kongbai)
                        {
                            newBe.AddSuffix(no);
                        }
                        BtGrids.AddElement(BtGrids.Elements.Count, newBe);
                        sb.Clear();
                    }
                    else if (specialTypeDetected)
                    {
                        sb.Append(single);
                    }
                    else if (Common.CharIsChineseChar(single))
                    {
                        string content = new string(single, 1);
                        BtGrids.AddElement(BtGrids.Elements.Count, 
                            new BeitieElement(BeitieElement.BeitieElementType.Zi, content, BtGrids.Elements.Count)
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
                        for (int i = 0; i < sizeDelta; i++)
                        {
                            BtGrids.AddElement(BtGrids.Elements.Count, new BeitieElement(BeitieElement.BeitieElementType.Zi,
                                                "", BtGrids.Elements.Count));
                        }
                    }
                }
            }
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
                MoreOptionBtn.IsEnabled = bEnable;
                if (XingcaoMode)
                {
                    NoNameSwitch.IsEnabled = bEnable;
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

            if (!GlobalSettings.MultiWindowMode)
            {
                this.Frame.Background = new SolidColorBrush(Colors.Black);
                // 淡入淡出效果
                this.Frame.Navigate(typeof(GridsConfig), this, Common.GetNavTransInfo(Common.NavigationTransitionType.DrillIn));
                return;

            }
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

        private void UpdateChineseLanguage()
        {
            bool hant = GlobalSettings.TranditionalChineseMode;

            BtnMore.Content = LanguageHelper.GetString("BtnMore/Content", hant);
            ColumnCountTitle.Text = LanguageHelper.GetString("ColumnCountTitle/Text", hant);
            GridColorTitle.Text = LanguageHelper.GetString("GridColorTitle/Text", hant);
            GridWidthTitle.Text = LanguageHelper.GetString("GridWidthTitle/Text", hant);
            ImportBtDir.Content = LanguageHelper.GetString("ImportBtDir/Content", hant);
            ImportBtFile.Content = LanguageHelper.GetString("ImportBtFile/Content", hant);
            MarginTitle.Text = LanguageHelper.GetString("MarginTitle/Text", hant);
            MarginToolTip.Content = LanguageHelper.GetString("MarginToolTip/Content", hant);
            NoNameSwitch.OffContent = LanguageHelper.GetString("NoNameSwitch/OffContent", hant);
            NoNameSwitch.OnContent = LanguageHelper.GetString("NoNameSwitch/OnContent", hant);
            PageText.PlaceholderText = LanguageHelper.GetString("PageText/PlaceholderText", hant);
            RowCountTitle.Text = LanguageHelper.GetString("RowCountTitle/Text", hant);
            SaveSplitImgs.Content = LanguageHelper.GetString("SaveSplitImgs/Content", hant);
            TextSizeGrade.Header = LanguageHelper.GetString("TextSizeGrade/Header", hant);
            ZiCountTitle.Text = LanguageHelper.GetString("ZiCountTitle/Text", hant);
            TieAlbum.PlaceholderText = LanguageHelper.GetString("TieAlbum/PlaceholderText", hant);
        }

        public int PageViewId = 0;
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // 返回
            if (CurrentBtImage != null)
            {
                InitAfterBack();
                return;
            }

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
            UpdateChineseLanguage();
            if (GlobalSettings.LastSelectedXingcao)
            {
                XingcaoModeCheck.IsChecked = true;
                XincaoModeCheck_Clicked(null, null);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("OnNavigatedFrom()");
            if (e.Parameter.ToString().Equals("reload"))
            {
                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame == null)
                    return;
                if (rootFrame.CanGoBack)
                {
                    rootFrame.GoBack();
                }
                rootFrame.BackStack.Clear();
                return;
            }
            base.OnNavigatedFrom(e);
        }
        //like this
        private void Reload(object param = null)
        {
            //var type = Frame.CurrentSourcePageType;
            //Frame.Navigate(type, param);
            //Frame.BackStack.Remove(Frame.BackStack.Last());

            var frame = Window.Current.Content as Frame;

            frame.Navigate(Frame.CurrentSourcePageType, param);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("OnNavigatedTo()");
            if (e.Parameter.ToString().Equals("reload"))
            {
                Frame rootFrame = Window.Current.Content as Frame;
                if (rootFrame == null)
                    return;
                if (rootFrame.CanGoBack)
                {
                    rootFrame.GoBack();
                }
                rootFrame.BackStack.Clear();
                return;
            }
            //if (e.NavigationMode == NavigationMode.Back)
            //{ 
            //    return;
            //}
            
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
            GlobalSettings.LastSelectedXingcao = XingcaoMode;
        }

        private void ZiCount_LostFocus(object sender, RoutedEventArgs e)
        {
            try
            {
                int.Parse(ZiCountBox.Text);
            }
            catch
            {
                Common.ShowMessageDlg(GetPlainString(StringItemType.InvalidZiCount) + ZiCountBox.Text);
                ZiCountBox.Text = Common.DEFAULT_XINGCAO_ZI_COUNT.ToString();
            }

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


        private void TextChangedTieAlbum(object sender, TextChangedEventArgs e)
        {
            ToolTip toolTip = new ToolTip
            {
                Content = TieAlbum.Text,
                Placement = Windows.UI.Xaml.Controls.Primitives.PlacementMode.Bottom,
                HorizontalOffset = 20
            };
            ToolTipService.SetToolTip(TieAlbum, toolTip);
        }

        private void ClickedMoreOptions(object sender, RoutedEventArgs e)
        {
            Style menuStyle = new Style()
            {
                TargetType = typeof(MenuFlyoutPresenter)
            };
            menuStyle.Setters.Add(new Setter(BackgroundProperty, new SolidColorBrush(Colors.Black)));

            menuStyle.Setters.Add(new Setter(BorderBrushProperty, new SolidColorBrush(Colors.LightGray)));
            menuStyle.Setters.Add(new Setter(BorderThicknessProperty, 1));
            MenuFlyout myFlyout = new MenuFlyout(); 

            
            ToggleMenuFlyoutItem chtElem = new ToggleMenuFlyoutItem { Text = "繁體中文" };
            ToggleMenuFlyoutItem multiElem = new ToggleMenuFlyoutItem { Text = "多窗口" };
            multiElem.IsChecked = GlobalSettings.MultiWindowMode;
            multiElem.Click += ClickedMultiWindow;

            MenuFlyoutItem refresh = new MenuFlyoutItem { Text = GetPlainString(StringItemType.Reload) };

            refresh.Click += ClickedRefresh;
            chtElem.IsChecked = GlobalSettings.TranditionalChineseMode;
            chtElem.Click += ClickedTrandChinese;
            myFlyout.MenuFlyoutPresenterStyle = menuStyle;
            myFlyout.Items.Add(refresh);
            myFlyout.Items.Add(chtElem);
            myFlyout.Items.Add(new MenuFlyoutSeparator());
            myFlyout.Items.Add(multiElem);

            myFlyout.ShowAt(MoreOptionBtn, new Point(0, MoreOptionBtn.ActualHeight)); 
        }

        private void ClickedRefresh(object sender, RoutedEventArgs e)
        {
            CurrentPage.Height = ImageScrollViewer.ViewportHeight;
            CurrentPage.Width = ImageScrollViewer.ViewportWidth;
            FolderFileCombo_SelectionChanged(null, null);
        }

        private void ClickedTrandChinese(object sender, RoutedEventArgs e)
        {
            GlobalSettings.TranditionalChineseMode = !GlobalSettings.TranditionalChineseMode;
            UpdateChineseLanguage();
        }


        private void ClickedMultiWindow(object sender, RoutedEventArgs e)
        {
            GlobalSettings.MultiWindowMode = !GlobalSettings.MultiWindowMode;
        }

        private void PageText_TextChanged(object sender, TextChangedEventArgs e)
        {

        }
        private int imageXOffset = 0;
        private void PageTextWritten_TextChanged(object sender, TextChangedEventArgs e)
        {
            try {

                imageXOffset = int.Parse(PageTextWritten.Text);
            }
            catch (Exception exc)
            {
                imageXOffset = 0;
                Debug.WriteLine("Exception: " + exc.ToString());
                return;
            }
        }

        private void FilterSwitch_Toggled(object sender, RoutedEventArgs e)
        {

        }

        private async void ImageDrope(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                var item = items[0];
                if (item is StorageFolder)
                {
                    loadFolder(item as StorageFolder);
                } 
                else
                {
                    LoadDropFile(item as StorageFile);
                }
            }
        }

        private void ImageDragEnter(object sender, DragEventArgs e)
        {
            Debug.WriteLine("ImageDragEnter");
        }

        private void ImageDragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy; 
            Debug.WriteLine("ImageDragOver");
        }

        private void ImageDragStarting(UIElement sender, DragStartingEventArgs args)
        {

            Debug.WriteLine("ImageDragStarting");
        }

        private void ImageDropCompleted(UIElement sender, DropCompletedEventArgs args)
        {

            Debug.WriteLine("ImageDropCompleted");
        }
    }
}