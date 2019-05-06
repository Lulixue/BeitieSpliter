using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

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


    public sealed class Common
    {
        public static readonly float ZOOM_FACTOR_SCALE = 0.05F;
        public static readonly float KONGBAI_OPACITY = 0.5F;
        public static readonly float KONGBAI_X_LINE_WIDTH = 2F;
        public static readonly int MIN_ROW_COL = 1;
        public static readonly int MIN_INDEX = 0;
        public static readonly int DEFAULT_MAX_PEN_WIDTH = 10;
        public static readonly int DEFAULT_MAX_ROW_COLUMN = 20;
        public static readonly int MIN_ELEMENT_TEXT_HEIGHT = 20;
        public static readonly int MAX_ELEMENT_TEXT_HEIGHT = 50;
        public static readonly int EXTRA_MAX_ELEM_TXT_HEIGHT = 100;


        public static List<ColorBoxItem> LightColorItems = new List<ColorBoxItem>();
        public static List<ColorBoxItem> DarkColorItems = new List<ColorBoxItem>();
        
        public static void Init()
        {
            // 添加颜色
            LightColorItems.Clear();
            LightColorItems.Add(new ColorBoxItem(Colors.Green, "绿色"));
            LightColorItems.Add(new ColorBoxItem(Colors.White, "白色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Orange, "橙色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Gray, "灰色"));
            LightColorItems.Add(new ColorBoxItem(Colors.Yellow, "黄色"));

            DarkColorItems.Clear();
            DarkColorItems.Add(new ColorBoxItem(Colors.Blue, "蓝色"));
            DarkColorItems.Add(new ColorBoxItem(Colors.Red, "红色"));
            DarkColorItems.Add(new ColorBoxItem(Colors.Black, "黑色"));
            DarkColorItems.Add(new ColorBoxItem(Colors.Purple, "紫色"));
            DarkColorItems.Add(new ColorBoxItem(Colors.Navy, "海军蓝色"));
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
        public static async Task<bool> ShowNotifyPageTextDlg()
        {
            ContentDialog locationPromptDialog = new ContentDialog
            {
                Title = "输入释文",
                Content = "你还没有输入释文, 是否生成图片?",
                CloseButtonText = "继续生成",
                PrimaryButtonText = "输入释文",
            };

            ContentDialogResult result = await locationPromptDialog.ShowAsync();
            return (result == ContentDialogResult.Primary);
        }
        public static async Task<bool> ShowCloseSwitchToMainDlg()
        {
            ContentDialog locationPromptDialog = new ContentDialog
            {
                Title = "关闭当前窗口",
                Content = "输入释文需要关闭本窗口，转到主界面输入，是否转到主界面输入？",
                CloseButtonText = "直接生成",
                PrimaryButtonText = "好的",
            };

            ContentDialogResult result = await locationPromptDialog.ShowAsync();
            return (result == ContentDialogResult.Primary);
        }
        public static async void ShowMessageDlg(string msg, UICommandInvokedHandler handler)
        {
            // Create the message dialog and set its content
            var messageDialog = new MessageDialog(msg);

            // Add commands and set their callbacks; both buttons use the same callback function instead of inline event handlers
            messageDialog.Commands.Add(new UICommand(
                    "关闭", handler));

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
                    return "阙字";
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
            NumberedCount = n;
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

        public Rect GetMaxRectangle(int minRow, int minCol, int maxRow, int maxCol, bool actualSize = false)
        {
            Point pntLt = new Point();
            Point pntRb = new Point();

            GetMinLeftTop(minRow, maxRow, minCol, maxCol, ref pntLt);
            GetMaxRightBottom(minRow, maxRow, minCol, maxCol, ref pntRb);


            if (!actualSize)
            {
                //pntLt.X -= ExtraSize;
                //pntLt.Y -= ExtraSize;
                //pntRb.X += ExtraSize;
                //pntRb.Y += ExtraSize;

                //if (pntLt.X < 0)
                //{
                //    pntLt.X = 0;
                //}

                //if (pntLt.Y < 0)
                //{
                //    pntLt.Y = 0;
                //}

                //if (pntRb.X > BtImageParent.resolutionX)
                //{
                //    pntRb.X = BtImageParent.resolutionX;
                //}

                //if (pntRb.Y > BtImageParent.resolutionY)
                //{
                //    pntRb.Y = BtImageParent.resolutionY;
                //}
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


            return new Rect(pntLt, pntRb);
        }

        public void InitXingcaoRects()
        {
            Point pntLt = new Point();
            ElementRects.Clear();
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
                    if ((newBe.type == dstType) && (dstType == BeitieElement.BeitieElementType.Kongbai))
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
