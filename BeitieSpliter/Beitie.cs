﻿using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.Resources.Core;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using static BeitieSpliter.LanguageHelper;

namespace BeitieSpliter
{
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

    public sealed class GlobalSettings
    {
        public static readonly string SETTING_MULTI_WINDOW = "MultiWindowMode";
        public static readonly string SETTING_TRANDITIONAL_HAN = "TranditionalHan";
        public static readonly string SETTING_SELECTED_ROW = "LastSelectedRow";
        public static readonly string SETTING_SELECTED_COLUMN = "LastSelectedColumn";
        public static readonly string SETTING_SELECTED_XINGCAO = "LastSelectedXingcao";
        private static readonly int DEFAULT_ROW = 9;
        private static readonly int DEFAULT_COLUMN = 6;
        public static bool MultiWindowMode = true;
        public static bool TranditionalChineseMode = false;
        public static int LastSelectedRow = DEFAULT_ROW - 1;
        public static int LastSelectedColumn = DEFAULT_COLUMN - 1;
        public static bool LastSelectedXingcao = false;
    }

    public sealed class LanguageHelper
    {
        public enum StringItemType
        {
            EnterText,          // 输入释文
            PromptNoText,       // 你还没有输入释文, 是否生成图片?
            ContinueGenerate,   // 继续生成 
            CloseDialog,        // 关闭当前窗口
            CloseDialogToMain,  // 输入释文需要关闭本窗口，转到主界面输入，是否转到主界面输入？
            DirectlyGenerate,   // 直接生成
            OK,                 // 好的 

            Green,      // 绿色
            White,      // 白色
            Orange,     // 橙色
            Gray,       // 灰色
            Yellow,     // 黄色 
            Blue,       // 蓝色
            Red,        // 红色
            Black,      // 黑色
            Purple,		// 紫色


            Close,      // 关闭
            Quezi,      // 阙字
            StartNoErrorFmt,       // 编号({0})超出范围, 请重新输入!
            ImageSizeFmt,       // 图片尺寸: {0:0}*{1:0}, 
            CurrentBeitieStartNoFmt,  // 当前碑帖: {0}, 起始单字编号: {1}({2}),   
            PleaseChooseBeitie, // 请选择书法碑帖图片
            NotfAfterSaveFmt,      // 单字分割图片({0}张)已保存到文件夹{1}
            SaveErrorFmt,       // 保存图片{0}出现错误!
            NoElemSelected,     // 未选择元素进行保存!
            BeginSaving,        // 开始保存分割单字图片...
            InvalidWidth,       // 无效宽度: 
            InvalidColumn,       // 无效列数: 
            InvalidRow,         // 无效行数: 
            InvalidMargin,       // 无效裁边: 
            InvalidZiCount,       // 无效字数: 
            InvalidParam,       // 参数错误，请更改参数后重试!
            ImageLoading,       // 图片正在加载中...
            SoftwareVersion,      // 软件版本：{0}.{1}.{2}.{3}
            ConfigNotifyInfoFmt,         // 当前图片: {0:0}*{1:0}, 元素个数: {2}, 行列数：{3}*{4}, 修改步进: {5:F1}
            AdjustElementFmt,       // 当前调整{0}
            AdjustColumnFmt,        // 当前调整第{0}列，选中{1}
            AdjustRowFmt,           // 当前调整第{0}行，选中{1}
            AdjustWholeFmt,         // 当前调整整页，选中{0}
            NotfToCaptureByMouseFmt,    // 请用鼠标截取{0}所在的区域
            NotfAdjustCurrentElementFmt,      // 当前选择调整{0}
            SplashInfoFirstPart,    // \r\n\r\nCtrl+鼠标滚轮进行放大缩小\r\n\r\n作者：卢立雪\r\n微信：13612977027\r\n邮箱：jackreagan@163.com\r\n\r\n
            SplashInfoSecondPart,   // \r\n感谢使用，欢迎反馈软件问题！
            AdjustBase,             // 选取蓝本
            AdjustObject,           // 操作对象
            DefineElemRectFirst,    // 请先定义当前元素矩形!
            AdjustElement,          // 调整元素
            SetAsKongbai,           // 设为空白元素
            SetAsQuezi,             // 设为阙字元素
            SetAsYin,               // 设为印章元素
            SetAsZi,                // 设为字元素
            CurrentReviseElemFmt,   // 当前可修改元素: {0}个, 
            CurrentSelectElemFmt,   // 当前选中元素：{0}, 
            CurrentElemRectSizeFmt, // 当前元素区域尺寸： {0:0}*{1:0}, 
            CurrentElemChangedFmt,  // 当前元素区域改变量: {0:0},{1:0},{2:0},{3:0}, 
            ParamError,         // 参数错误!
            DashedLine,         // 虚线
            DottedLine,         // 点线
            SolidLine,          // 实线
            Picture,            // 图片: 
            Folder,             //  文件夹: 
            PictureNotFound,    // 所选文件夹下找不到碑帖图片！
            FileBroken,         // 文件损坏或文件不支持!
            SingleChar,         // 单字
 
            AuxNone,            // 无辅助线
            AuxCross,           // 十字
            AuxCircle,          // 圆加十字
            AuxMi,              // 圆加米字
            Reload,             // 重载

            TopMost = 0x0100,    // 已经到了最上边了!
            LeftMost = 0x0200,   // 已经到了最左边了!
            RightMost = 0x0400,  // 已经到了最右边了!
            BottomMost = 0x0800, // 已经到了最下边了!
        }

        public enum ResourceType{
            Config,
            PlainStrings,
            Resources,
        }

        private static readonly int RESOURCE_COUNT = Enum.GetValues(typeof(ResourceType)).Length;
        private static readonly ResourceLoader[] ResLoaderHans = new ResourceLoader[RESOURCE_COUNT];
        private static readonly ResourceLoader[] ResLoaderHant = new ResourceLoader[RESOURCE_COUNT];

        static LanguageHelper()
        {
            foreach (var type in Enum.GetValues(typeof(ResourceType)))
            {
                ResLoaderHans[(int)type] = ResourceLoader.GetForViewIndependentUse(type.ToString());
                ResLoaderHant[(int)type] = ResourceLoader.GetForViewIndependentUse("T" + type.ToString());
            }

        }

        public static string GetPlainString(StringItemType type)
        {
            return GetPlainString(type, GlobalSettings.TranditionalChineseMode);
        }

        public static string GetPlainString(StringItemType type, bool hant, string defValue = "")
        { 
            string ret;
            int index = (int)ResourceType.PlainStrings;
            ResourceLoader loader = hant ? ResLoaderHant[index] : ResLoaderHans[index];

            ret = loader.GetString(type.ToString());

            return ret?.Replace("\\r\\n", "\r\n") ?? null;
        }

        public static string GetConfigString(string name, bool hant, string defValue = null)
        {
            string ret; 

            int index = (int)ResourceType.Config;
            ResourceLoader loader = hant ? ResLoaderHant[index] : ResLoaderHans[index];
            ret = loader.GetString(name);

            
            return ret?.Replace("\\r\\n", "\r\n") ?? null;
        }

        public static string GetString(string name, bool hant, string defValue = null)
        {
            string ret;
            int index = (int)ResourceType.Resources;
            ResourceLoader loader = hant ? ResLoaderHant[index] : ResLoaderHans[index];
            ret = loader.GetString(name);

            return ret?.Replace("\\r\\n", "\r\n") ?? null;
        }
    }


    public sealed class Common
    {
        public static readonly float ZOOM_FACTOR_SCALE = 0.05F;
        public static readonly float KONGBAI_OPACITY = 0.5F;
        public static readonly float KONGBAI_X_LINE_WIDTH = 2F;
        public static readonly int MIN_ROW_COL = 1;
        public static readonly int MIN_INDEX = 0;
        public static readonly int DEFAULT_MAX_PEN_WIDTH = 10;
        public static readonly float DEFAULT_AUX_WIDTH = 2.5F;
        public static readonly float DEFAULT_MAX_AUX_WIDTH = 6F;
        public static readonly int DEFAULT_MAX_ROW_COLUMN = 20;
        public static readonly int MIN_ELEMENT_TEXT_HEIGHT = 20;
        public static readonly int MAX_ELEMENT_TEXT_HEIGHT = 50;
        public static readonly int EXTRA_MAX_ELEM_TXT_HEIGHT = 100; 
        public static readonly int AUTOSLIDE_OFFSET = 10;
        public static readonly int DEFAULT_PEN_WIDTH = 2;
        public static readonly int PEN_WIDTH_DIVIDER = 1000;
        public static readonly int DEFAULT_SINGLE_PREVIEW_EXTSIZE = 10;
        public static readonly int DEFAULT_XINGCAO_ZI_COUNT = 100;
        public static readonly string DEFAULT_MARGIN = "1,1,1,1";
        public static readonly double WHOLEPAGE_DRAG_PROP = 2.0;


        public enum NavigationTransitionType
        {
            Default,
            Entrance,     
            DrillIn,
            Suppress,
            Common,
            Continuum,
            Slide,
        }

        public static List<ColorBoxItem> LightColorItems = new List<ColorBoxItem>();
        public static List<ColorBoxItem> DarkColorItems = new List<ColorBoxItem>();

        public static readonly string[] TEXT_SIZE_GRADES = { "10+", "100+", "1000+", "10000+" };
        public static readonly string[] HANT_LANGUAGE_CODES = { "zh-hk", "zh-mo", "zh-tw", "zh-hant"};

        public static Color GetColorOtherwise(Color bgColor)
        {  
            foreach (var item in LightColorItems)
            {
                if (item.Value == bgColor)
                {
                    return Colors.Black;
                }
            }
            return Colors.White;

        }
        public static void Init()
        {
            // 添加颜色
            LightColorItems.Clear();
            LightColorItems.Add(new ColorBoxItem(Colors.Green, /*"绿色"*/GetPlainString(StringItemType.Green)));
            LightColorItems.Add(new ColorBoxItem(Colors.White, "白色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Orange, "橙色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Gray, "灰色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Yellow, "黄色"));

            DarkColorItems.Clear();
            DarkColorItems.Add(new ColorBoxItem(Colors.Blue, /*"蓝色"*/GetPlainString(StringItemType.Blue)));
            DarkColorItems.Add(new ColorBoxItem(Colors.Red, /*"红色"*/GetPlainString(StringItemType.Red)));
            DarkColorItems.Add(new ColorBoxItem(Colors.Black, "黑色"));
            DarkColorItems.Add(new ColorBoxItem(Colors.Purple, "紫色")); 
        }

        public static int GetLessOne(int first, int second)
        {
            return (first > second) ? second : first;
        }
        public static double GetLessOne(double first, double second)
        {
            return (first > second) ? second : first;
        }
        public static double GetLargerOne(double first, double second)
        {
            return (first < second) ? second : first;
        }
        public static float GetLargerOne(float first, float second)
        {
            return (first < second) ? second : first;
        }
        // 系统语言是繁体
        public static bool SystemLanguageIsHanT()
        {
            var topUserLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];

            topUserLanguage = topUserLanguage.ToLower();
            foreach (var item in HANT_LANGUAGE_CODES)
            {
                if (topUserLanguage.Contains(item, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        public static string GetSystemLanguage()
        {
            var topUserLanguage = Windows.System.UserProfile.GlobalizationPreferences.Languages[0];
            var language = new Windows.Globalization.Language(topUserLanguage);
            var displayName = language.DisplayName;

            return displayName;
        }

        public static NavigationTransitionInfo GetNavTransInfo(NavigationTransitionType type)
        {
            switch (type)
            {
                case NavigationTransitionType.DrillIn:
                    return new DrillInNavigationTransitionInfo();
                case NavigationTransitionType.Suppress:
                    return new SuppressNavigationTransitionInfo();
                case NavigationTransitionType.Entrance:
                    return new EntranceNavigationTransitionInfo();
                case NavigationTransitionType.Common:
                    return new CommonNavigationTransitionInfo();
                case NavigationTransitionType.Continuum:
                    return new ContinuumNavigationTransitionInfo();
                case NavigationTransitionType.Slide:
                    return new SlideNavigationTransitionInfo();
                case NavigationTransitionType.Default:
                default:
                    return null;
            }
        }

        /* 
         * Block                                   Range       Comment
            CJK Unified Ideographs                  4E00-9FFF   Common
            CJK Unified Ideographs Extension A      3400-4DBF   Rare
            CJK Unified Ideographs Extension B      20000-2A6DF Rare, historic
            CJK Unified Ideographs Extension C      2A700–2B73F Rare, historic
            CJK Unified Ideographs Extension D      2B740–2B81F Uncommon, some in current use
            CJK Unified Ideographs Extension E      2B820–2CEAF Rare, historic
            private use                             E815 - E864
            CJK Compatibility Ideographs            F900-FAFF   Duplicates, unifiable variants, corporate characters
            CJK Compatibility Ideographs Supplement 2F800-2FA1F Unifiable variants
         * 
         */
        /*
         * private use  E815 - E864
         * '','','','','','','','','','','','','','','',
         * '','','','','','','','','','','','','','','',
         * '','','','','','','','','','','','','','','',
         * '','','','','','','','','','','','','','','',
         * '','','','','','','','','','','','','','','',
         * '','','','','',
         */

        public static readonly char UNICODE_CHS_EXT_A_START = (char)0x3400;  // CJK扩展字符集A
        public static readonly char UNICODE_CHS_EXT_A_END = (char)0x4DB5;
        public static readonly char UNICODE_CHS_CJK_CI_START = (char)0xF900;  // CJK扩展字符集CI
        public static readonly char UNICODE_CHS_CJK_CI_END = (char)0xFAFF;
        public static readonly char UNICODE_CHS_CJK_PRIVATE_START = (char)0xE815;  // CJK扩展字符集private
        public static readonly char UNICODE_CHS_CJK_PRIVATE_END = (char)0xE864;
        public static readonly char UNICODE_CHS_START = (char)0x4E00;      // CJK字符集
        public static readonly char UNICODE_CHS_END = (char)0x9FBB;

        public static Dictionary<char, char> DICT_UNICODE_CHINESE_RANGES = new Dictionary<char, char>
        {
            { UNICODE_CHS_START, UNICODE_CHS_END },
            { UNICODE_CHS_EXT_A_START, UNICODE_CHS_EXT_A_END },
            { UNICODE_CHS_CJK_CI_START, UNICODE_CHS_CJK_CI_END },
            { UNICODE_CHS_CJK_PRIVATE_START, UNICODE_CHS_CJK_PRIVATE_END },
        };
         
        public static bool CharIsChineseChar(char ch)
        {
            foreach (var pair in DICT_UNICODE_CHINESE_RANGES)
            {
                if ((pair.Key <= ch) && (pair.Value >= ch))
                {
                    return true;
                }
            }
            return false;
        } 
        public static void SetWindowSize()
        {
            {
                ApplicationView.PreferredLaunchViewSize = Common.GetSystemResolution(true);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(1280, 720));
                ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            }
        }
        public static void DrawKongbaiElement(CanvasDrawingSession draw, Rect rc)
        {
            CanvasSolidColorBrush KongbaiBrush = new CanvasSolidColorBrush(draw, Colors.White)
            {
                Opacity = KONGBAI_OPACITY
            };
            draw.FillRectangle(rc, KongbaiBrush);
            //draw.DrawText("空白", rc, Colors.Gray, InfoCTF);
            draw.DrawLine(new Vector2((float)rc.Left, (float)rc.Top),
                new Vector2((float)rc.Right, (float)rc.Bottom), Colors.Gray, KONGBAI_X_LINE_WIDTH);
            draw.DrawLine(new Vector2((float)rc.Left, (float)rc.Bottom),
                new Vector2((float)rc.Right, (float)rc.Top), Colors.Gray, KONGBAI_X_LINE_WIDTH);

        }
        public static Size GetSystemResolution(bool second=false)
        {
            var view = DisplayInformation.GetForCurrentView();
            var resolution = new Size(view.ScreenWidthInRawPixels, view.ScreenHeightInRawPixels);
            var scale = view.ResolutionScale == ResolutionScale.Invalid ? 1 : view.RawPixelsPerViewPixel;
            var bounds = new Size(resolution.Width / scale, resolution.Height / scale);
            if (second)
            {
                bounds = new Size(1280, 720);
            }
            return bounds;
        }
        public static void Sleep(int msTime)
        {
            AutoResetEvent h = new AutoResetEvent(false);
            h.WaitOne(msTime);
        }

        private static string GetPlainString(StringItemType type)
        {
            return LanguageHelper.GetPlainString(type);
        }
        public static async Task<bool> ShowNotifyPageTextDlg()
        {
            ContentDialog locationPromptDialog = new ContentDialog
            {
                Title = /*"输入释文"*/GetPlainString(StringItemType.EnterText),
                Content = /*"你还没有输入释文, 是否生成图片?"*/GetPlainString(StringItemType.PromptNoText),
                CloseButtonText = /*"继续生成"*/GetPlainString(StringItemType.ContinueGenerate),
                PrimaryButtonText = /*"输入释文"*/GetPlainString(StringItemType.EnterText),
            };

            ContentDialogResult result = await locationPromptDialog.ShowAsync();
            return (result == ContentDialogResult.Primary);
        }
        public static async Task<bool> ShowCloseSwitchToMainDlg()
        {
            ContentDialog locationPromptDialog = new ContentDialog
            {
                Title = /*"关闭当前窗口"*/GetPlainString(StringItemType.CloseDialog),
                Content = /*"输入释文需要关闭本窗口，转到主界面输入，是否转到主界面输入？"*/GetPlainString(StringItemType.CloseDialogToMain),
                CloseButtonText = /*"直接生成"*/GetPlainString(StringItemType.DirectlyGenerate),
                PrimaryButtonText = /*"好的"*/GetPlainString(StringItemType.OK),
            };

            ContentDialogResult result = await locationPromptDialog.ShowAsync();
            return (result == ContentDialogResult.Primary);
        }
        private static void ShowToastNotification(string title, string stringContent)
        {
            ToastNotifier ToastNotifier = ToastNotificationManager.CreateToastNotifier();
            Windows.Data.Xml.Dom.XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
            Windows.Data.Xml.Dom.XmlNodeList toastNodeList = toastXml.GetElementsByTagName("text");
            toastNodeList.Item(0).AppendChild(toastXml.CreateTextNode(title));
            toastNodeList.Item(1).AppendChild(toastXml.CreateTextNode(stringContent));
            Windows.Data.Xml.Dom.IXmlNode toastNode = toastXml.SelectSingleNode("/toast");
            Windows.Data.Xml.Dom.XmlElement audio = toastXml.CreateElement("audio");
            audio.SetAttribute("src", "ms-winsoundevent:Notification.SMS");

            ToastNotification toast = new ToastNotification(toastXml);
            toast.ExpirationTime = DateTime.Now.AddSeconds(4);
            ToastNotifier.Show(toast);
        }
        public static async void ShowMessageDlg(string msg, UICommandInvokedHandler handler = null)
        {
            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(msg);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                    /*"关闭"*/GetPlainString(StringItemType.Close), handler));

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
        public void AddSuffix(int no)
        {
            if (content != "")
            {
                content += "-";
            }
            content += TypeToString(type);
            content += no;
        }
        public bool NeedAddNo()
        {
            return no != -1;
        }
        public string TypeToString()
        {
            return TypeToString(type);
        }
        public static string TypeToString(BeitieElementType t)
        {
            switch (t)
            {
                case BeitieElementType.Yinzhang:
                    return "印章";
                case BeitieElementType.Kongbai:
                    return "空白";
                case BeitieElementType.Other:
                    return "其他";
                case BeitieElementType.Zi:
                    return "字";
                case BeitieElementType.Quezi:
                    return /*"阙字"*/GetPlainString(StringItemType.Quezi);
            }
            return "";
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
        public string text = "";
    }
    public class BeitieGridRect
    {
        public BeitieGridRect(Rect rect)
        {
            rc.X = rect.X;
            rc.Y = rect.Y;
            rc.Width = rect.Width;
            rc.Height = rect.Height;
            revised = false;
        }
        public BeitieGridRect(Rect rect, bool rev)
        {
            rc.X = rect.X;
            rc.Y = rect.Y;
            rc.Width = rect.Width;
            rc.Height = rect.Height;
            revised = rev;
        }
        public int col = 1;
        public Rect rc = new Rect();
        public bool revised = false;
    }

    public class BeitieAlbumItem
    {
        public BeitieAlbumItem(StorageFile f, int n)
        {
            file = f;
            no = n;
        }

        public StorageFile file = null;
        public int no = 0;
        public int NumberedCount = 0;
    }
    // 所有的index从0开始
    // 行列号row/col从1开始
    public sealed class BeitieGrids : ICloneable
    {
        public enum ColorType
        {
            Light,
            Dark,
        }
        public double GridHeight = 0.0;
        public double GridWidth = 0;
        public bool XingcaoMode = false;
        public int ElementCount = 0;
        public bool BookOldType = true;
        public ColorType GridType = ColorType.Dark;
        public float angle = 0;
        public float PenWidth = 1;
        public List<Color> BackupColors = new List<Color>();
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
        public bool OnlyZiNo = true;
        //public List<BeitieElement> Elements = new List<BeitieElement>();
        public Dictionary<int, BeitieElement> Elements = new Dictionary<int, BeitieElement>();
        // Point -> X: ColumnNo, Y: Index
        public Dictionary<int, BeitieGridRect> XingcaoElements = new Dictionary<int, BeitieGridRect>();
        public bool IsImageRotated() { return angle != 0; }
        public double ExtraSize = 5;

        public Rect GetDrawRect()
        {
            Rect drawRc = new Rect()
            {
                X = PageMargin.Left,
                Y = PageMargin.Top,
                Width = DrawWidth,
                Height = DrawHeight
            };

            return drawRc;
        }
        public int GetZiCount()
        {
            int ziCount = 0;
            foreach (var elem in Elements)
            {
                if ((elem.Value.type == BeitieElement.BeitieElementType.Zi) ||
                    (elem.Value.type == BeitieElement.BeitieElementType.Quezi))
                {
                    ziCount++;
                }
            }
            return ziCount;
        }
        public int GetNumberedCount()
        {
            int count = 0;
            if (XingcaoMode)
            {
                foreach (var elem in XingcaoElements)
                {
                    if (Elements[elem.Key].NeedAddNo())
                    {
                        count++;
                    }
                }
            }
            else
            {
                foreach (var elem in Elements)
                {
                    if (elem.Value.NeedAddNo())
                    {
                        count++;
                    }
                }
            }
            return count;

        }
        public string GetElementChar(int index)
        {
            BeitieElement be;
            Elements.TryGetValue(index, out be);
            if (be == null)
            {
                be = new BeitieElement(BeitieElement.BeitieElementType.Kongbai, "", -1);
            } 
            return be.content.ToString();
        }

        public string GetElementString(int index)
        {
            BeitieElement be;
            Elements.TryGetValue(index, out be);
            if (be == null)
            {
                be = new BeitieElement(BeitieElement.BeitieElementType.Kongbai, "", -1);
            }
            string name = "";
            if (!XingcaoMode)
            {
                name = be.TypeToString();
            }
            name += string.Format("元素{0}({1})", index + 1, be.content);
            if (be.content == "")
            {
                name = name.Replace("()", "");
            }
            return name;
        }

        public void AddElement(int i, BeitieElement be)
        {
            if (XingcaoMode || (i < ElementCount))
            {
                Elements.Add(i, be);
            }
        }
        public void UpdateElementCount(int count)
        {
            ElementCount = count;
            if (XingcaoElements.Count > count)
            {
                for (int i = count; i < (XingcaoElements.Count+50); i++)
                {
                    if (XingcaoElements.ContainsKey(i))
                    {
                        XingcaoElements.Remove(i);
                    }
                }
            }
        }

        public Rect GetMaxRectangle(int minRow, int minCol, int maxRow, int maxCol, bool actualSize = false, int extsize = 0)
        {
            Point pntLt = new Point();
            Point pntRb = new Point();

            GetMinLeftTop(minRow, maxRow, minCol, maxCol, ref pntLt);
            GetMaxRightBottom(minRow, maxRow, minCol, maxCol, ref pntRb);

            Point imgLt = new Point(0, 0);
            Point imgRb = new Point(BtImageParent.resolutionX, BtImageParent.resolutionY);
            if (!actualSize)
            {
                imgLt.X += PageMargin.Left;
                imgLt.Y += PageMargin.Top;
                imgRb.X -= PageMargin.Right;
                imgRb.Y -= PageMargin.Bottom;


                // 显示全图
                if (minRow == Common.MIN_ROW_COL)
                {
                    pntLt.Y = 0 + PageMargin.Top;
                }
                if (maxRow == Rows)
                {
                    double tailored = BtImageParent.resolutionY - PageMargin.Bottom;
                    pntRb.Y = (tailored < pntRb.Y) ? pntRb.Y : tailored;
                }
                if (minCol == Common.MIN_ROW_COL)
                {
                    pntLt.X = 0 + PageMargin.Left;
                }
                if (maxCol == Columns)
                {
                    double tailored = BtImageParent.resolutionX - PageMargin.Right;
                    pntRb.X = (tailored < pntRb.X) ? pntRb.X : tailored;
                }
            }
            if (extsize > 0)
            {
                double left = pntLt.X - extsize;
                double top  = pntLt.Y - extsize;
                double right = pntRb.X + extsize;
                double bottom = pntRb.Y + extsize;

                pntLt.X = Common.GetLargerOne(left, imgLt.X);
                pntLt.Y = Common.GetLargerOne(top, imgLt.Y);
                pntRb.X = Common.GetLessOne(right, imgRb.X);
                pntRb.Y = Common.GetLessOne(bottom, imgRb.Y);
            }


            return new Rect(pntLt, pntRb);
        }

        public void InitXingcaoRects()
        {
            Point pntLt = new Point();
            ElementRects.Clear();
            XingcaoElements.Clear();
            for (int i = 0; i < Columns; i++)
            {
                pntLt.X = i * GridWidth + OriginPoint.X;
                pntLt.Y = OriginPoint.Y;
                ElementRects.Add(new BeitieGridRect(new Rect(pntLt.X, pntLt.Y,
                    GridWidth, DrawHeight)));
            }
        }
        public bool ElementIsKongbai(int index)
        {
            BeitieElement be;
            Elements.TryGetValue(index, out be);
            if (be == null)
            {
                return false;
            }
            return be.type == BeitieElement.BeitieElementType.Kongbai;
        }
        public void UpdateElement(int index, BeitieElement.BeitieElementType dstType)
        {
            int ElementNo = 0;
            int IndexNo = 0;
            int OthersNo = 0;
            int YinzhangNo = 0;
            int QueziNo = 0;
            Dictionary<int, BeitieElement> newElements = new Dictionary<int, BeitieElement>();
            foreach (KeyValuePair<int, BeitieElement> pair in Elements)
            {
                BeitieElement newBe = new BeitieElement(pair.Value.type,
                    pair.Value.content, pair.Value.no)
                {
                    col = pair.Value.col,
                    text = pair.Value.text
                };

                if (IndexNo == index)
                {
                    if ((newBe.type != dstType) && (dstType == BeitieElement.BeitieElementType.Kongbai))
                    {
                        newElements.Add(IndexNo++, new BeitieElement(BeitieElement.BeitieElementType.Kongbai, "", -1));
                        if (IndexNo == ElementCount)
                        {
                            break;
                        }
                    }
                    else
                    {
                        newBe.type = dstType;
                    }
                }
                BeitieElement.BeitieElementType type = newBe.type;
                switch (type)
                {
                    case BeitieElement.BeitieElementType.Kongbai:
                        newBe.no = -1;
                        break;
                    case BeitieElement.BeitieElementType.Yinzhang:
                    case BeitieElement.BeitieElementType.Other:
                    case BeitieElement.BeitieElementType.Quezi:
                        if (!OnlyZiNo)
                        {
                            newBe.no = ElementNo++;
                        }
                        else
                        {
                            newBe.no = -1;
                        }
                        break;
                    case BeitieElement.BeitieElementType.Zi:
                    default:
                        newBe.no = ElementNo++;
                        break;
                }
                newBe.content = newBe.text;
                if (newBe.content == "")
                {
                    switch (type)
                    {
                        case BeitieElement.BeitieElementType.Yinzhang:
                            newBe.AddSuffix(++YinzhangNo);
                            break;
                        case BeitieElement.BeitieElementType.Other:
                            newBe.AddSuffix(++OthersNo);
                            break;
                        case BeitieElement.BeitieElementType.Quezi:
                            newBe.AddSuffix(++QueziNo);
                            break;
                    }
                }
                newElements.Add(IndexNo++, newBe);
                if (IndexNo == ElementCount)
                {
                    break;
                }
            }
            Elements = newElements;
        }

        public bool GetElementRoi(int index, ref Rect rc)
        {
            if (!XingcaoMode)
            {
                if (index > (ElementRects.Count-1))
                {
                    return false;
                }
                int row = 0, col = 0;
                IndexToRowCol(index , ref row, ref col, BookOldType);
                rc = GetRectangle(row, col);
            }
            else
            {
                BeitieGridRect bgr = null;
                XingcaoElements.TryGetValue(index, out bgr);
                if (bgr == null)
                {
                    return false;
                }
                rc = bgr.rc;
            }
            return true;
        }

        public void GetMinLeftTop(int MinRow, int MaxRow, int MinCol, int MaxCol, ref Point pntLt)
        {
            double minLeft = 100000.0;
            double minTop = 100000.0;

            for (int i = MinRow; i <= MaxRow; i++)
            {
                for (int j = MinCol; j <= MaxCol; j++)
                {
                    Rect rc = GetRectangle(i, j);
                    double left = rc.Left;
                    double top = rc.Top;

                    if (left < minLeft)
                    {
                        minLeft = left;
                    }
                    if (top < minTop)
                    {
                        minTop = top;
                    }
                }
            }
            pntLt.X = minLeft;
            pntLt.Y = minTop;
        }
        public void GetMaxRightBottom(int MinRow, int MaxRow, int MinCol, int MaxCol, ref Point pntRb)
        {
            double maxBottom = 0.0;
            double maxRight = 0;

            for (int i = MinRow; i <= MaxRow; i++)
            {
                for (int j = MinCol; j <= MaxCol; j++)
                {
                    Rect rc = GetRectangle(i, j);
                    double bottom = rc.Bottom;
                    double right = rc.Right;

                    if (bottom > maxBottom)
                    {
                        maxBottom = bottom;
                    }
                    if (right > maxRight)
                    {
                        maxRight = right;
                    }
                }
            }
            pntRb.X = maxRight;
            pntRb.Y = maxBottom;
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
            int retIndex;
            if (!oldStyle)
            {
                retIndex = ToIndex(row, col);
            }
            else
            {
                col = Columns - col;
                retIndex = col * Rows + row - 1;
            }
            Debug.Assert(retIndex >= 0);
            return retIndex;
        }
        public int IndexToOldStyle(int index)
        {
            int row = 1, col = 1;
            IndexToRowCol(index, ref row, ref col, false);
            return GetIndex(row, col, true);
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
}
