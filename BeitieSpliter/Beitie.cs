using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    public sealed class Common
    {
        public static void SetWindowSize()
        {
            {
                ApplicationView.PreferredLaunchViewSize = Common.GetSystemResolution(true);
                ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(1280, 720));
                ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
            }
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

    public sealed class BeitieGrids : ICloneable
    {
        public enum ColorType
        {
            Light,
            Dark,
        }
        public static int MIN_ROW_COL = 1;
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
        public List<BeitieElement> Elements = new List<BeitieElement>();
        // Point -> X: ColumnNo, Y: Index
        public Dictionary<int, BeitieGridRect> XingcaoElements = new Dictionary<int, BeitieGridRect>();
        public bool IsImageRotated() { return angle != 0; }
        public double ExtraSize = 5;

        public string GetElementString(int index)
        {
            BeitieElement be = Elements[index];
            string name = string.Format("元素{0}({1})", index + 1, be.content);
            if (be.content == "")
            {
                name = name.Replace("()", "");
            }
            return name;
        }

        public Rect GetMaxRectangle(int minRow, int minCol, int maxRow, int maxCol)
        {
            Point pntLt = new Point();
            Point pntRb = new Point();

            GetMinLeftTop(minRow, maxRow, minCol, maxCol, ref pntLt);
            GetMaxRightBottom(minRow, maxRow, minCol, maxCol, ref pntRb);

            pntLt.X -= ExtraSize;
            pntLt.Y -= ExtraSize;
            pntRb.X += ExtraSize;
            pntRb.Y += ExtraSize;

            if (pntLt.X < 0)
            {
                pntLt.X = 0;
            }

            if (pntLt.Y < 0)
            {
                pntLt.Y = 0;
            }

            if (pntRb.X > BtImageParent.resolutionX)
            {
                pntRb.X = BtImageParent.resolutionX;
            }

            if (pntRb.Y > BtImageParent.resolutionY)
            {
                pntRb.Y = BtImageParent.resolutionY;
            }
            // 显示全图
            if (minRow == MIN_ROW_COL)
            {
                pntLt.Y = 0;
            }
            if (maxRow == Rows)
            {
                pntRb.Y = BtImageParent.resolutionY;
            }
            if (minCol == MIN_ROW_COL)
            {
                pntLt.X = 0;
            }
            if (maxCol == Columns)
            {
                pntRb.X = BtImageParent.resolutionX;
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

        public bool GetElementRoi(int index, ref Rect rc)
        {
            if (!XingcaoMode)
            {
                if (index > ElementRects.Count)
                {
                    return false;
                }
                int row = 0, col = 0;
                IndexToRowCol(index - 1, ref row, ref col, BookOldType);
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
}
