using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.Storage.Streams;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.UI;
using System.Numerics;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.ApplicationModel.Core;
using Windows.System.Threading;
using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Input;
using Windows.UI.ViewManagement;
using Microsoft.Graphics.Canvas.Text;
using Windows.System;
using static BeitieSpliter.MainPage;
using Microsoft.Graphics.Canvas.Brushes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Windows.UI.Xaml.Media.Animation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BeitieSpliter
{
    
    public class ChangeStruct
    {
        public double left = 0;
        public double right = 0;
        public double top = 0;
        public double bottom = 0;

        public void Reset()
        {
            left = 0;
            right = 0;
            top = 0;
            bottom = 0;
        }

        public void Copy(ChangeStruct cs)
        {
            left = cs.left;
            right = cs.right;
            top = cs.top;
            bottom = cs.bottom;
        }
    }

    public class IntIndex
    {
        public IntIndex(int r, int c)
        {
            row = r;
            col = c;
        }
        public int row = -1;
        public int col = -1;
    }
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GridsConfig : Page, INotifyPropertyChanged
    {
        enum CtrlMessageType
        {
            RowChange,
            ColumnChange,
            OperationChange,
            AdjustChange,
            RotateChange,
            RedrawRequest,
            NotSpecified,
        }

        enum RectChangeType
        {
            None = 0,
            TopAdd = 0x01,
            TopMinus = 0x02,
            BottomAdd = 0x04,
            BottomMinus = 0x10,
            LeftAdd = 0x20,
            LeftMinus = 0x40,
            RightAdd = 0x100,
            RightMinus = 0x200,
        }
        enum OperationType
        {
            SingleElement,
            SingleRow,
            SingleColumn,
            WholePage,
        }

        enum PenLineType
        {
            Dash = 0,
            Dot = 1,
            Line = 2
        }
        
        public event EventHandler ShowSaveResultEvtHdlr;
        static bool HideGridChecked = false;
        static bool SingleFocusMode = false;
        static bool ShowSizeMode = false;
        static bool NoOpacityMode = false;
        static bool HideScrollBar = false;
        static PenLineType LineType = PenLineType.Dash;
        OperationType OpType = OperationType.WholePage;
        BeitieGrids BtGrids = null;
        BeitieGrids LastBtGrids = new BeitieGrids();
        BeitieImage BtImage = null;
        MainPage ParentPage = null;
        Rect BtImageShowRect = new Rect();
        Rect BtImageAdjustRect = new Rect();
        HashSet<IntIndex> DrawLineElements = new HashSet<IntIndex>();
        Rect ToAdjustRect = new Rect();
        bool AvgCol = true;
        bool AvgRow = true;
        bool FixedHeight = false;
        bool FixedWidth = false;
        ChangeStruct ChangeRect = new ChangeStruct();
        ChangeStruct LastChangeRect = new ChangeStruct();
        ChangeStruct LastValidChange = new ChangeStruct();
        static readonly int MIN_ROW_COLUMN = 1;

        private ObservableCollection<ColorBoxItem> _ColorBoxItems = new ObservableCollection<ColorBoxItem>();
        public ObservableCollection<ColorBoxItem> ColorBoxItems
        {
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


        string GetLineTypeString(PenLineType type)
        {
            switch (type)
            {
                case PenLineType.Dash:
                    return "虚线";
                case PenLineType.Dot:
                    return "点线";
                case PenLineType.Line:
                    return "实线";
            }
            return "未知";
        }
        PenLineType GetLineType(string t)
        {
            if (t == "虚线")
            {
                return PenLineType.Dash;
            }
            else if (t == "点线")
            {
                return PenLineType.Dot;
            }
            else
            {
                return PenLineType.Line;
            }
        }
        
        public GridsConfig()
        {
            this.InitializeComponent();
            
        }

        private int GetPreviousColRow(int current, int MIN)
        {
            return (current == MIN) ? MIN : (current - 1);
        }
        private int GetNextColRow(int current, int MAX)
        {
            return (current == MAX) ? MAX : (current + 1);
        }
        void PrintRect(string title, Rect rc)
        {
            Debug.WriteLine("{0}: {1:0},{2:0},{3:0},{4:0}", title,
                rc.X, rc.Y, rc.X + rc.Width, rc.Y + rc.Height);
        }
        void GetRectPoints(Rect rc, ref Point pntLt, ref Point pntRb)
        {
            pntLt = new Point(rc.X, rc.Y);
            pntRb = new Point(rc.X + rc.Width, rc.Y + rc.Height);
        }

        RectChangeType GetLastChangeType(ChangeStruct offset)
        {
            if (offset.left != 0)
            {
                if (offset.left < 0)
                    return RectChangeType.LeftMinus;
                else
                    return RectChangeType.LeftAdd;
            }
            else if (offset.right != 0)
            {
                if (offset.right < 0)
                    return RectChangeType.RightMinus;
                else
                    return RectChangeType.RightAdd;
            }
            else if (offset.bottom != 0)
            {
                if (offset.bottom < 0)
                    return RectChangeType.BottomMinus;
                else
                    return RectChangeType.BottomAdd;
            }
            else if (offset.top != 0)
            {
                if (offset.top < 0)
                    return RectChangeType.TopMinus;
                else
                    return RectChangeType.TopAdd;
            }
            return RectChangeType.None;
        }
        void FixedChangedRect(RectChangeType type, ref Point pntLt, ref Point pntRb, ChangeStruct offset)
        {
            switch (type)
            {
                case RectChangeType.LeftAdd:
                case RectChangeType.LeftMinus:
                    pntRb.X += offset.left;
                    break;
                case RectChangeType.RightMinus:
                case RectChangeType.RightAdd:
                    pntLt.X += offset.right;
                    break;
                case RectChangeType.TopAdd:
                case RectChangeType.TopMinus:
                    pntRb.Y += offset.top;
                    break;
                case RectChangeType.BottomAdd:
                case RectChangeType.BottomMinus:
                    pntLt.Y += offset.bottom;
                    break;
                case RectChangeType.None:
                default:
                    break;
            }
        }

        bool IsChangeNoteOverflow()
        {
            Point pntLt, pntRb;
            GetRectPoints(ToAdjustRect, ref pntLt, ref pntRb); 

            pntLt.X += ChangeRect.left;
            pntLt.Y += ChangeRect.top;
            pntRb.X += ChangeRect.right;
            pntRb.Y += ChangeRect.bottom;

            if (pntLt.X < 0)
            {
                Common.ShowMessageDlg("已经到了最右边了!", null);
                NotifyUser("已经到了最右边了!", NotifyType.ErrorMessage);
                PrintRect("[Change] Invalid", new Rect(pntLt, pntRb));
                return false;
            }
            else if ((int)pntRb.X > (int)BtGrids.BtImageParent.resolutionX)
            {
                Common.ShowMessageDlg("已经到了最左边了!", null);
                NotifyUser("已经到了最左边了!", NotifyType.ErrorMessage);
                PrintRect("[Change] Invalid", new Rect(pntLt, pntRb));
                return false;
            }
            else if (pntLt.Y < 0)
            {
                Common.ShowMessageDlg("已经到了最上边了!", null);
                NotifyUser("已经到了最上边了!", NotifyType.ErrorMessage);
                PrintRect("[Change] Invalid", new Rect(pntLt, pntRb));
                return false;
            }
            else if ((int)pntRb.Y > (int)BtGrids.BtImageParent.resolutionY)
            {
                Common.ShowMessageDlg("已经到了最下边了!", null);
                NotifyUser("已经到了最下边了!", NotifyType.ErrorMessage);
                PrintRect("[Change] Invalid", new Rect(pntLt, pntRb));
                return false;
            }
            return true;
        }
        void XingcaoMoveToNextElement()
        {
            lock (BtGrids.XingcaoElements)
            {
                int maxIndex = Common.MIN_INDEX;
                foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
                {
                    if (pair.Key > maxIndex)
                    {
                        maxIndex = pair.Key;
                    }
                }
                if (maxIndex == (BtGrids.ElementCount-1))
                {
                    maxIndex = Common.MIN_INDEX;
                }
                else
                {
                    maxIndex++;
                }
                if (CurrentElements.SelectedIndex != maxIndex)
                {
                    CurrentElements.SelectedIndex = maxIndex;
                }
            }
        }

        bool UpdateXingcaoElementsRects()
        {
            ChangeStruct offset = new ChangeStruct();
            offset.Copy(ChangeRect);
            offset.left -= LastChangeRect.left;
            offset.right -= LastChangeRect.right;
            offset.top -= LastChangeRect.top;
            offset.bottom -= LastChangeRect.bottom;

            Rect dstRc = ToAdjustRect;
            Point pntLt = new Point(dstRc.X, dstRc.Y);
            Point pntRb = new Point(dstRc.X + dstRc.Width, dstRc.Y + dstRc.Height);

            pntLt.X += offset.left;
            pntRb.X += offset.right;
            pntLt.Y += offset.top;
            pntRb.Y += offset.bottom;

            if (!CheckIfOutOfimage(pntLt, pntRb))
            {
                PrintRect("Invalid", new Rect(pntLt, pntRb));
                return false;
            }
            ToAdjustRect = new Rect(pntLt, pntRb);

            int colNumber = ColumnNumber.SelectedIndex+1;
            int elemIndex = CurrentElements.SelectedIndex;

            Point lt = new Point
            {
                X = ToAdjustRect.X + BtImageAdjustRect.X,
                Y = ToAdjustRect.Y + BtImageAdjustRect.Y,
            };
            Point rb = new Point
            {
                X = lt.X + ToAdjustRect.Width,
                Y = lt.Y + ToAdjustRect.Height
            };
            if (!BtGrids.XingcaoElements.ContainsKey(elemIndex))
            {
                if ((ToAdjustRect.Width < 5) ||
                    (ToAdjustRect.Height < 5))
                {
                    return false;
                }
                

                BeitieGridRect bgr = new BeitieGridRect(new Rect(lt, rb))
                {
                    col = colNumber
                };
                BtGrids.XingcaoElements.Add(elemIndex, bgr);
                FirstShowBanner = false;
            }
            else
            {
                
                BtGrids.XingcaoElements[elemIndex].col = colNumber;
                BtGrids.XingcaoElements[elemIndex].rc = new Rect(lt, rb);
                
            }

            return true;
        }

        bool CheckIfOutOfimage(Point pntLt, Point pntRb)
        {
            if (pntLt.X < 0)
            {
                Common.ShowMessageDlg("已经到了最右边了!", null);
                NotifyUser("已经到了最右边了!", NotifyType.ErrorMessage);
                return false;
            }
            else if ((int)pntRb.X > (int)BtGrids.BtImageParent.resolutionX)
            {
                Common.ShowMessageDlg("已经到了最左边了!", null);
                NotifyUser("已经到了最左边了!", NotifyType.ErrorMessage);
                return false;
            }
            else if (pntLt.Y < 0)
            {
                Common.ShowMessageDlg("已经到了最上边了!", null);
                NotifyUser("已经到了最上边了!", NotifyType.ErrorMessage);
                return false;
            }
            else if ((int)pntRb.Y > (int)BtGrids.BtImageParent.resolutionY)
            {
                Common.ShowMessageDlg("已经到了最下边了!", null);
                NotifyUser("已经到了最下边了!", NotifyType.ErrorMessage);
                return false;
            }
            return true;
        }
        bool IntEqual(double d1, double d2)
        {
            double delta = d1 - d2;
            return Math.Abs(delta) < 1.0;
        }

        private readonly double MAX_STABLIZATION_OFFSET = 30;

        bool UpdateElementsRects()
        {
            if ((BtGrids.XingcaoMode) && (!AdjustGridsSwitch.IsOn))
            {
                return UpdateXingcaoElementsRects();
            }
            Dictionary<int, BeitieGridRect> backupElements = new Dictionary<int, BeitieGridRect>();

            ChangeStruct offset = new ChangeStruct();
            offset.Copy(ChangeRect);
            offset.left -= LastChangeRect.left;
            offset.right -= LastChangeRect.right;
            offset.top -= LastChangeRect.top;
            offset.bottom -= LastChangeRect.bottom;
            RectChangeType chgType = GetLastChangeType(offset);
            int SelectedCol = ColumnNumber.SelectedIndex + 1;
            int SelectedRow = RowNumber.SelectedIndex + 1;

            // 在调节整列或整行时忽略边框的反向的位移
            if (OperationType.SingleRow == OpType)
            {
                // 整个横向部分已经撑满了
                if (IntEqual(ToAdjustRect.Width, BtGrids.DrawWidth))
                {
                    if (offset.left == offset.right)
                    {
                        if (Math.Abs(offset.left) < MAX_STABLIZATION_OFFSET)
                        {
                            offset.left = 0;
                            Debug.WriteLine("Stablization Horizon: {0:0}->{1:0}", offset.right, offset.left);
                            offset.right = offset.left;
                        }
                    }
                }
            }
            else if (OperationType.SingleColumn == OpType)
            {
                // 整个纵向部分已经撑满了
                if (IntEqual(ToAdjustRect.Height, BtGrids.DrawHeight))
                {
                    if (offset.top == offset.bottom)
                    {

                        if (Math.Abs(offset.top) < MAX_STABLIZATION_OFFSET)
                        {
                            offset.top = 0;
                            Debug.WriteLine("Stablization Horizon: {0:0}->{1:0}", offset.bottom, offset.top);
                            offset.bottom = offset.top;
                        }
                    }
                }
            }

            Debug.WriteLine("Offset: {0},{1},{2},{3}", offset.left, offset.top, offset.right, offset.bottom);
            if (chgType <= RectChangeType.BottomMinus)
            {
                if (!FixedHeight)
                    chgType = RectChangeType.None;
            }
            else if (!FixedWidth)
            {
                chgType = RectChangeType.None;
            }
 
            foreach (IntIndex pnt in DrawLineElements)
            {
                int index = BtGrids.ToIndex(pnt.row, pnt.col);
                Rect dstRc = BtGrids.ElementRects[index].rc;
                bool revised = BtGrids.ElementRects[index].revised;
                Point pntLt = new Point(dstRc.X, dstRc.Y);
                Point pntRb = new Point(dstRc.X + dstRc.Width, dstRc.Y + dstRc.Height);
                bool dstRevised = true;

                if (OperationType.SingleColumn == OpType)
                {
                    pntLt.X += offset.left;
                    pntRb.X += offset.right;

                    if (AvgRow)
                    {
                        dstRevised = false;
                    }
                    // 每列第一个元素
                    if (pnt.row == MIN_ROW_COLUMN)
                    {
                        pntLt.Y += offset.top;
                    }
                    // 每列最后一个元素
                    else if (pnt.row == BtGrids.Rows)
                    {
                        pntRb.Y += offset.bottom;
                    }
                }
                else if (OperationType.SingleRow == OpType)
                {


                    pntLt.Y += offset.top;
                    pntRb.Y += offset.bottom;

                    if (AvgCol)
                    {
                        dstRevised = false;
                    }
                    // 每行第一个元素
                    if (pnt.col == MIN_ROW_COLUMN)
                    {
                        pntLt.X += offset.left;
                    }
                    // 每行最后一个元素
                    else if (pnt.col == BtGrids.Columns)
                    {
                        pntRb.X += offset.right;
                    }
                }
                else if (OperationType.WholePage == OpType)
                {
                    revised = true;
                    if (AvgCol || AvgRow)
                    {
                        dstRevised = false;
                    }
                    if (pnt.row == MIN_ROW_COLUMN)
                    {
                        pntLt.Y += offset.top;
                        revised = false;
                    }
                    if (pnt.row == BtGrids.Rows)
                    {
                        pntRb.Y += offset.bottom;
                        revised = false;
                    }
                    if (pnt.col == MIN_ROW_COLUMN)
                    {
                        pntLt.X += offset.left;
                        revised = false;
                    }
                    if (pnt.col == BtGrids.Columns)
                    {
                        pntRb.X += offset.right;
                        revised = false;
                    }
                }
                else
                {
                    if ((pnt.row != SelectedRow) ||
                        (pnt.col != SelectedCol))
                    {
                        continue;

                    }

                    pntLt.X += offset.left;
                    pntRb.X += offset.right;
                    pntLt.Y += offset.top;
                    pntRb.Y += offset.bottom;
                    revised = false;
                }
                if (!revised)
                {
                    if (FixedWidth || FixedHeight)
                    {
                        FixedChangedRect(chgType, ref pntLt, ref pntRb, offset);
                    }
                    if (!CheckIfOutOfimage(pntLt, pntRb))
                    { 
                        PrintRect("[" + pnt.row + "," + pnt.col + "] Invalid", new Rect(pntLt, pntRb));
                        // 恢复修改的元素
                        foreach (KeyValuePair<int, BeitieGridRect> pair in backupElements)
                        {
                            BtGrids.ElementRects[pair.Key] =
                                new BeitieGridRect(pair.Value.rc)
                                {
                                    revised = pair.Value.revised
                                };
                        }
                        return false;
                    }


                    PrintRect("[" + pnt.row + "," + pnt.col + "] Before", dstRc);
                    backupElements.Add(index, new BeitieGridRect(BtGrids.ElementRects[index].rc)
                    {
                        revised = BtGrids.ElementRects[index].revised
                    });
                    BtGrids.ElementRects[index] = new BeitieGridRect(new Rect(pntLt, pntRb), dstRevised);
                    PrintRect("After", BtGrids.ElementRects[index].rc);
                }
            }
            if ((OperationType.SingleElement == OpType) ||
                (!AvgCol && !AvgRow))
            {
                return true;
            }
            // 平均分布行和列
            {
                Point PntLeftTop = new Point();
                Point pntRightBottom = new Point();
                int minCol = MIN_ROW_COLUMN;
                int minRow = MIN_ROW_COLUMN;
                int maxCol = BtGrids.Columns;
                int maxRow = BtGrids.Rows;
                if (AvgRow && ((OperationType.SingleColumn == OpType) || (OperationType.WholePage == OpType)))
                {
                    
                    if (OpType == OperationType.SingleColumn)
                    {
                        minCol = maxCol = ColumnNumber.SelectedIndex + 1;
                    }
                    double height = BtGrids.GetRectangle(maxRow, maxCol).Bottom - BtGrids.GetRectangle(minRow, minCol).Top;
                    double AvgHeight = height / BtGrids.Rows;

                    Debug.WriteLine("Height: {0:F1}, Avg Height: {1:F1}", height, AvgHeight);

                    // 每列以第一个元素为起点
                    for (int col = minCol; col <= maxCol; col++)
                    {
                        for (int row = MIN_ROW_COLUMN; row <= BtGrids.Rows; row++)
                        {
                            int index = BtGrids.ToIndex(row, col);
                            Rect dstRc = BtGrids.ElementRects[index].rc;
                            bool revised = BtGrids.ElementRects[index].revised;

                            if (row == MIN_ROW_COLUMN)
                            {
                                PntLeftTop.Y = dstRc.Y;
                            }
                            else
                            {
                                PntLeftTop.Y += AvgHeight;
                            }
                            PntLeftTop.X = dstRc.X;
                            pntRightBottom.X = PntLeftTop.X + dstRc.Width;
                            pntRightBottom.Y = PntLeftTop.Y + AvgHeight;
                            if (row == BtGrids.Rows)
                            {
                                pntRightBottom.Y = dstRc.Bottom;
                            }
                            // 已经调整过了就不再弄了
                            if (!revised)
                            {
                                BtGrids.ElementRects[index] = new BeitieGridRect(new Rect(PntLeftTop, pntRightBottom), false);

                                PrintRect("(Avg Column)Before[" + row + "," + col + "]", dstRc);
                                PrintRect("After", BtGrids.ElementRects[index].rc);
                                Debug.WriteLine("\n");
                            }
                        }
                       
                    }
                }
                if (AvgCol && ((OperationType.SingleRow == OpType) || (OperationType.WholePage == OpType)))
                {
                    
                    if (OpType == OperationType.SingleRow)
                    {
                        minRow = maxRow = RowNumber.SelectedIndex + 1;
                    }
                    double width = BtGrids.GetRectangle(maxRow, maxCol).Right - BtGrids.GetRectangle(minRow, minCol).Left;
                    double AvgWidth = width / BtGrids.Columns;

                    Debug.WriteLine("Width: {0:F1}, Avg Width: {1:F1}", width, AvgWidth);
                    // 每行以第一个元素为起点
                    for (int row = minRow; row <= maxRow; row++)
                    {
                        for (int col = MIN_ROW_COLUMN; col <= BtGrids.Columns; col++)
                        {
                            int index = BtGrids.ToIndex(row, col);
                            Rect dstRc = BtGrids.ElementRects[index].rc;
                            bool revised = BtGrids.ElementRects[index].revised;

                            if (col == MIN_ROW_COLUMN)
                            {
                                PntLeftTop.X = dstRc.X;
                            }
                            else
                            {
                                PntLeftTop.X += AvgWidth;
                            }
                            PntLeftTop.Y = dstRc.Y;
                            pntRightBottom.X = PntLeftTop.X + AvgWidth;
                            pntRightBottom.Y = PntLeftTop.Y + dstRc.Height;
                            if (col == BtGrids.Columns)
                            {
                                pntRightBottom.X = dstRc.Right;
                            }
                            // 已经调整过了就不再弄了
                            if (!revised)
                            {
                                BtGrids.ElementRects[index] = new BeitieGridRect(new Rect(PntLeftTop, pntRightBottom), false);
                                
                                PrintRect("(Avg Row) Before[" + row + "," + col + "]", dstRc);
                                PrintRect("After", BtGrids.ElementRects[index].rc);
                                Debug.WriteLine("\n"); 
                            }
                        }
                    }
                }
            }
            return true;
        }

        private Point GetDrawLineLtCoord(int row, int col)
        {
            int index = BtGrids.ToIndex(row, col);
            Rect dstRc = BtGrids.ElementRects[index].rc;

            return new Point(dstRc.X, dstRc.Y);
        }

        private double AdjustExtendSize = 0;

        private int GetFirstXcElementInRect(int rol)
        {
            Rect rc = BtGrids.GetMaxRectangle(MIN_ROW_COLUMN, rol, MIN_ROW_COLUMN, rol);
            double minDelta = 10000;
            int retIndex = Common.MIN_INDEX;
            foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
            {
                Rect itemRc = pair.Value.rc;
                Point pntC = new Point()
                {
                    X = itemRc.X + itemRc.Width / 2,
                    Y = itemRc.Y + itemRc.Height / 2,
                };
                
                if (IsPntInRect(pntC, rc, 0))
                {
                    double deltaY = Math.Abs(rc.Y - itemRc.Y);
                    double deltaX = Math.Abs(rc.X - itemRc.X);
                    double delta = deltaX + deltaY;
                    
                    if (delta < minDelta)
                    {
                        retIndex = pair.Key;
                        minDelta = delta;
                    }
                }
            }

            return retIndex;
        }
        private void ResetChangeRect()
        {
            ChangeRect = new ChangeStruct();
            UpdateOpInfoBox();
        }

        private void CalculateXingcaoDrawRect(CtrlMessageType type)
        {
            Debug.WriteLine("CalculateXingcaoDrawRect() called!");

            Point pntLt = new Point();
            Point pntRb = new Point();
            int SelectedCol = ColumnNumber.SelectedIndex + 1;
            int SelectedElementNo = CurrentElements.SelectedIndex;
            int PreCol = GetPreviousColRow(SelectedCol, MIN_ROW_COLUMN);
            int NextCol = GetNextColRow(SelectedCol, BtGrids.Columns);
            int minCol = MIN_ROW_COLUMN;
            int maxCol = BtGrids.Columns;
            int minRow = MIN_ROW_COLUMN;
            int maxRow = BtGrids.Rows;

            if (OperationType.SingleColumn == OpType)
            {
                minCol = maxCol = SelectedCol;
            }
            else
            {
                // whole page
                PreCol = minCol;
                NextCol = maxCol;
            }
            BtImageAdjustRect = BtGrids.GetMaxRectangle(minRow, PreCol, maxRow, NextCol);

            DrawLineElements.Clear();
            if (!ChkHideGrid?.IsChecked ?? true)
            {
                for (int row = minRow; row <= maxRow; row++)
                {
                    for (int col = minCol; col <= maxCol; col++)
                    {
                        DrawLineElements.Add(new IntIndex(row, col));
                    }
                }
            }
            
            CurrentItem.Height = (BtImageAdjustRect.Height + 2 * AdjustExtendSize);
            CurrentItem.Width = BtImageAdjustRect.Width + 2 * AdjustExtendSize;

            pntLt.X = AdjustExtendSize; // 0
            pntLt.Y = AdjustExtendSize; // 0
            pntRb.X = BtImageAdjustRect.Width + AdjustExtendSize;
            pntRb.Y = BtImageAdjustRect.Height + AdjustExtendSize;


            PntrTargetType = PointerTargetType.ElementRect;
            BtImageShowRect = new Rect(pntLt, pntRb);
            ToAdjustRect = new Rect();
            if (AdjustGridsSwitch?.IsOn ?? false)
            {
                Rect rcStart = BtGrids.GetRectangle(minRow, minCol);
                Rect rcEnd = BtGrids.GetRectangle(maxRow, maxCol);

                ToAdjustRect.X = rcStart.X - BtImageAdjustRect.X;
                ToAdjustRect.Y = rcStart.Y - BtImageAdjustRect.Y;
                ToAdjustRect.Width = rcEnd.X - rcStart.X + rcEnd.Width;
                ToAdjustRect.Height = rcEnd.Y - rcStart.Y + rcEnd.Height;
            }
            else
            { 
                int colNumber = ColumnNumber.SelectedIndex + 1;
                int elemIndex = CurrentElements.SelectedIndex;

                if ((CtrlMessageType.ColumnChange == type)||
                    (CtrlMessageType.OperationChange == type))
                {
                    int firstIndex = GetFirstXcElementInRect(colNumber);
                    if (firstIndex != elemIndex)
                    {
                        CurrentElements.SelectedIndex = firstIndex;
                    }
                    Debug.WriteLine("Index first: {0}, selected: {1}", firstIndex, elemIndex);
                    elemIndex = firstIndex;
                }

                if (!BtGrids.XingcaoElements.ContainsKey(elemIndex))
                {
                    PntrTargetType = PointerTargetType.XingcaoInitDraw;
                }
                else
                {
                    var result = BtGrids.XingcaoElements.Where(p => p.Key == elemIndex);
                    foreach (KeyValuePair<int, BeitieGridRect> pair in result)
                    {
                        Rect rc = pair.Value.rc;
                        
                        ToAdjustRect.X = rc.X - BtImageAdjustRect.X;
                        ToAdjustRect.Y = rc.Y - BtImageAdjustRect.Y;
                        ToAdjustRect.Width = rc.Width;
                        ToAdjustRect.Height = rc.Height;

                        if (pair.Value.col != colNumber)
                        {
                            BtGrids.XingcaoElements[pair.Key].col = colNumber;
                        }
                        break;
                    }
                }

            }

            ResetChangeRect();
            Debug.WriteLine("Current Element: {0}, Status: {1}", CurrentElements.SelectedIndex + 1,
                PntrTargetType);
            Debug.WriteLine("Show Area Rect: ({1:0},{2:0},{3:0},{4:0})", 0,
               BtImageAdjustRect.X, BtImageAdjustRect.Y, BtImageAdjustRect.Width, BtImageAdjustRect.Height);
            Debug.WriteLine("Image Size: ({0}*{1}), ShowRect Size:({4:0},{5:0},{6:0},{7:0})",
                BtImage.resolutionX, BtImage.resolutionY, 0, 0,
                BtImageShowRect.X, BtImageShowRect.Y, BtImageShowRect.Width, BtImageShowRect.Height);
            Debug.WriteLine("To Adjust Rect: ({1:0},{2:0},{3:0},{4:0}), Target Type: {5}", 0,
               ToAdjustRect.X, ToAdjustRect.Y, ToAdjustRect.Width, ToAdjustRect.Height, PntrTargetType);

        }
        private void CalculateDrawRect(CtrlMessageType type = CtrlMessageType.NotSpecified)
        {
            if (BtGrids.XingcaoMode)
            {
                CalculateXingcaoDrawRect(type);
                return;
            }
            Debug.WriteLine("CalculateDrawRect() called!");

            Point pntLt = new Point();
            Point pntRb = new Point();
            int SelectedCol = ColumnNumber.SelectedIndex + 1;
            int SelectedRow = RowNumber.SelectedIndex + 1;
            int minCol = MIN_ROW_COLUMN;
            int maxCol = BtGrids.Columns;
            int minRow = MIN_ROW_COLUMN;
            int maxRow = BtGrids.Rows;

            int PreCol = GetPreviousColRow(SelectedCol, MIN_ROW_COLUMN);
            int PreRow = GetPreviousColRow(SelectedRow, MIN_ROW_COLUMN);
            int NextCol = GetNextColRow(SelectedCol, BtGrids.Columns);
            int NextRow = GetNextColRow(SelectedRow, BtGrids.Rows);

            DrawLineElements.Clear();
            if (OperationType.SingleColumn == OpType)
            {
                PreRow = MIN_ROW_COLUMN;
                NextRow = BtGrids.Rows;
                minCol = maxCol = SelectedCol;
            }
            else if (OperationType.SingleRow == OpType)
            {
                PreCol = MIN_ROW_COLUMN;
                NextCol = BtGrids.Columns;
                minRow = maxRow = SelectedRow;
            }
            else if (OperationType.SingleElement == OpType)
            {
                // 单字专注，只显示周围区域
                if (SingleFocusMode)
                {
                    minCol = maxCol = SelectedCol;
                    minRow = maxRow = SelectedRow;
                }
                else
                {
                    // 单字显示全部区域
                    PreRow = minRow;
                    PreCol = minCol;
                    NextRow = maxRow;
                    NextCol = maxCol;
                }
            }
            else 
            {
                // whole page
                PreRow = minRow;
                PreCol = minCol;
                NextRow = maxRow;
                NextCol = maxCol;
            }
            
            BtImageAdjustRect = BtGrids.GetMaxRectangle(PreRow, PreCol, NextRow, NextCol);

            pntLt.X = AdjustExtendSize; // 0
            pntLt.Y = AdjustExtendSize; // 0
            pntRb.X = BtImageAdjustRect.Width + AdjustExtendSize;
            pntRb.Y = BtImageAdjustRect.Height + AdjustExtendSize;

            BtImageShowRect = new Rect(pntLt, pntRb);
           
            
            CurrentItem.Height = (BtImageAdjustRect.Height + 2 * AdjustExtendSize);
            CurrentItem.Width = BtImageAdjustRect.Width + 2 * AdjustExtendSize;

            if (OperationType.SingleElement == OpType)
            {
                for (int row = PreRow; row <= NextRow; row++)
                {
                    for (int col = PreCol; col <= NextCol; col++)
                    {
                        DrawLineElements.Add(new IntIndex(row, col));
                    }
                }
                minCol = maxCol = SelectedCol;
                minRow = maxRow = SelectedRow;
            }
            else
            {
                for (int row = minRow; row <= maxRow; row++)
                {
                    for (int col = minCol; col <= maxCol; col++)
                    {
                        DrawLineElements.Add(new IntIndex(row, col));
                    }
                }
            }
            
            Rect maxRect = BtGrids.GetMaxRectangle(minRow, minCol, maxRow, maxCol, true);

            ToAdjustRect.X = maxRect.X - BtImageAdjustRect.X;
            ToAdjustRect.Y = maxRect.Y - BtImageAdjustRect.Y;
            ToAdjustRect.Width = maxRect.Width;
            ToAdjustRect.Height = maxRect.Height;

            ResetChangeRect();
            Debug.WriteLine("Operation: {0}, Previous: {1},{2}; Current: {3},{4}; Next: {5},{6}",
               OpType, PreRow, PreCol, SelectedRow, SelectedCol, NextRow, NextCol);
            Debug.WriteLine("Show Area Rect: ({1:0},{2:0},{3:0},{4:0})", 0,
                BtImageAdjustRect.X, BtImageAdjustRect.Y, BtImageAdjustRect.Width, BtImageAdjustRect.Height);
            Debug.WriteLine("Image Size: ({0}*{1}), ShowRect Size:({4:0},{5:0},{6:0},{7:0})",
                BtImage.resolutionX, BtImage.resolutionY, 0, 0,
                BtImageShowRect.X, BtImageShowRect.Y, BtImageShowRect.Width, BtImageShowRect.Height);
            Debug.WriteLine("To Adjust Rect: ({1:0},{2:0},{3:0},{4:0})", 0,
               ToAdjustRect.X, ToAdjustRect.Y, ToAdjustRect.Width, ToAdjustRect.Height);
        }
        private bool IsParaInvalid()
        {
            if (BtGrids == null)
            {
                return true;
            }
            if (ColumnNumber.SelectedIndex == -1)
            {
                return true;
            }
            if (RowNumber.SelectedIndex == -1)
            {
                return true;
            }
            return false;
        }
        private void Refresh(CtrlMessageType type = CtrlMessageType.NotSpecified, bool updateStatus = true)
        {
            if (IsParaInvalid())
            {
                return;
            }
            lock (BtImage)
            { 

                switch (type)
                {
                    case CtrlMessageType.ColumnChange:
                    case CtrlMessageType.RowChange:
                    case CtrlMessageType.OperationChange:
                    case CtrlMessageType.NotSpecified:
                        CalculateDrawRect(type);
                        break;
                    default:
                        break;
                }
            }
            if (updateStatus)
                UpdateAdjustStatus();
            Task.Run(() =>
            {
                //Common.Sleep(30);
                CurrentItem.Invalidate();
            });
            
        }
        void TestCase()
        {
            //int row = 4;
            //int col = 4;
            //int index = BtGrids.GetIndex(4, 4, true);
            //Debug.Assert(BtGrids.GetIndex(1, 1, true) == 45);
            //Debug.Assert(BtGrids.GetIndex(1, 6, true) == 0);
            //BtGrids.IndexToRowCol(index, ref row,  ref col, true);
            //Debug.Assert(row == 4);
            //Debug.Assert(col == 4);
            Debug.WriteLine("float: {0},{0:F1},{0:F0}, {0:0}", 1.24F, 1.24F, 1.4F, 1.4F);
        }
        void InitControls()
        {
            TestCase();

            int index = 0;
            foreach (KeyValuePair<int, BeitieElement> pair in BtGrids.Elements)
            {
                CurrentElements.Items.Add(String.Format("{0}[{1}]", pair.Value.content, ++index));
            }
            CurrentElements.SelectedIndex = 0;

            for (int i = 1; i <= BtGrids.Rows; i++)
            {
                RowNumber.Items.Add(i);
            }
            RowNumber.SelectedIndex = 0;
            for (int i = 1; i <=BtGrids.Columns; i++)
            {
                ColumnNumber.Items.Add(i);
            }
            ColumnNumber.SelectedIndex = BtGrids.Columns - 1;
            CurrentItem.Height = BtImage.resolutionY;
            CurrentItem.Width = BtImage.resolutionX;

            foreach (ColorBoxItem item in Common.LightColorItems)
            {
                ColorBoxItems.Add(item);
            }
            foreach (ColorBoxItem item in Common.DarkColorItems)
            {
                ColorBoxItems.Add(item);
            }
            ColorBoxSelectedItem = ColorBoxItems.FirstOrDefault(f => f.Value == BtGrids.PenColor);

            //////网格
            for (int i = 1; i < Common.DEFAULT_MAX_PEN_WIDTH; i++)
            {
                PenWidthCombo.Items.Add(i);
            }
            bool IsFloat = (BtGrids.PenWidth - (int)BtGrids.PenWidth) > 0;
            string strWidth;
            if (IsFloat)
            {
                strWidth = string.Format("{0:F1}", BtGrids.PenWidth);
            }
            else
            {
                strWidth = string.Format("{0:0}", BtGrids.PenWidth);

            }
            PenWidthCombo.Text = strWidth;

            PenLineTypeCombo.Items.Clear();
            PenLineTypeCombo.Items.Add(GetLineTypeString(PenLineType.Dash));
            PenLineTypeCombo.Items.Add(GetLineTypeString(PenLineType.Dot));
            PenLineTypeCombo.Items.Add(GetLineTypeString(PenLineType.Line));

            if (BtGrids.PenWidth > 3)
            {
                LineType = PenLineType.Line;
            }

            PenLineTypeCombo.SelectedIndex = (int)LineType;

            // 设置存储变量
            ChkSingleFocus.IsChecked = SingleFocusMode;
            ChkHideGrid.IsChecked = HideGridChecked;
            ChkShowSize.IsChecked = ShowSizeMode;
            ChkNoOpacity.IsChecked = NoOpacityMode;
            ChkHideScrollBar.IsChecked = HideScrollBar;
            HideViewerScrollBar(HideScrollBar);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            Debug.WriteLine("OnNavigatedFrom() called"); 
            base.OnNavigatedFrom(e);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            Debug.WriteLine("OnNavigatedFrom() called"); 
            base.OnNavigatingFrom(e);
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("OnNavigatedTo() called");
            ParentPage = (MainPage)e.Parameter;
            BtGrids = ParentPage.BtGrids;
            // 备份上一次的数据
            LastBtGrids.Columns = BtGrids.Columns;
            LastBtGrids.Rows = BtGrids.Rows;
            LastBtGrids.ElementRects.Clear();
            foreach (BeitieGridRect bgr in BtGrids.ElementRects)
            {
                LastBtGrids.ElementRects.Add(new BeitieGridRect(bgr.rc));
            }
            LastBtGrids.XingcaoElements.Clear();
            foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
            {
                LastBtGrids.XingcaoElements.Add(pair.Key, new BeitieGridRect(pair.Value.rc)
                {
                    col = pair.Value.col
                });
            }
            LastBtGrids.Elements.Clear();
            foreach (KeyValuePair<int, BeitieElement> pair in BtGrids.Elements)
            {
                LastBtGrids.Elements.Add(pair.Key, new BeitieElement(pair.Value.type,
                    pair.Value.content, pair.Value.no)
                {
                    col = pair.Value.col,
                    text = pair.Value.text
                });
            }

            //BtImage = new BeitieImage(CurrentItem, BtGrids.ImageFile);
            BtImage = BtGrids.BtImageParent;
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Maximized;

            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame?.CanGoBack ?? false)
            {
                // If we have pages in our in-app backstack and have opted in to showing back, do so
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
            else
            {
                // Remove the UI from the title bar if there are no pages in our in-app back stack
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            }
        }

        void AdjustAddHandler(Button btn)
        {
            btn.AddHandler(PointerPressedEvent, new PointerEventHandler(Adjust_PointerPressed), true);
            btn.AddHandler(PointerReleasedEvent, new PointerEventHandler(Adjust_PointerReleased), true);
        }

        void UpdateChangeStep()
        {
            double gridHeight = BtGrids.BtImageParent.resolutionY / BtGrids.Rows;
            double gridWidth = BtGrids.BtImageParent.resolutionX / BtGrids.Columns;
            double greater = (gridHeight > gridWidth) ? gridHeight : gridWidth;

            ChangeStep = greater / 50;
            ChangeStep = (ChangeStep < 1) ? 1 : ChangeStep;
            ChangeStep = 1;     // 有了鼠标调节，按钮调节只是辅助性微调

            ChangeStepTxtBox.Text = string.Format("{0:0}", ChangeStep);
        }

        private void SettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            ParentPage.SetConfigPage(null);
        }
        private void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("SettingsPage_Loaded() called");

            InitControls();
            CalculateDrawRect();
            UpdateAngle();
            Operation_Checked(null, null);
            UpdateChangeStep();

            AdjustAddHandler(BtnLeftAdd);
            AdjustAddHandler(BtnLeftMinus);
            AdjustAddHandler(BtnTopAdd);
            AdjustAddHandler(BtnTopMinus);
            AdjustAddHandler(BtnBottomAdd);
            AdjustAddHandler(BtnBottomMinus);
            AdjustAddHandler(BtnRightMinus);
            AdjustAddHandler(BtnRightAdd);
            NotifyUser(string.Format("当前图片: {0:0}*{1:0}, 元素个数: {2}, 行列数：{3}*{4}, 修改步进: {5:F1}",
                BtImage.resolutionX, BtImage.resolutionY, BtGrids.ElementRects.Count, BtGrids.Rows, BtGrids.Columns, ChangeStep),
                NotifyType.StatusMessage);

            CoreApplication.GetCurrentView().TitleBar.ExtendViewIntoTitleBar = false;

            if (BtGrids.XingcaoMode)
            {
                HangNoTitle.Visibility = Visibility.Collapsed;
                RowNumber.Visibility = Visibility.Collapsed;
                OpSingleRow.Visibility = Visibility.Collapsed;
                ChkFixedHeight.Visibility = Visibility.Collapsed;
                ChkFixedWidth.Visibility = Visibility.Collapsed;
                ChkAvgCol.Visibility = Visibility.Collapsed;
                ChkAvgRow.Visibility = Visibility.Collapsed;
                ChkSingleFocus.Visibility = Visibility.Collapsed;
                OpObjectTitle.Text = "选取蓝本";

                AdjustGridsSwitch.Visibility = Visibility.Visible;
                ChkHideGrid.Visibility = Visibility.Visible;

            }
            else
            {
                MainPage.ImgAutoFitScrollView(BtImage, ItemScrollViewer);
            }
            Window.Current.CoreWindow.Dispatcher.AcceleratorKeyActivated += Dispatcher_AcceleratorKeyActivated;
            this.ShowSaveResultEvtHdlr += new EventHandler(this.OnShowSaveResultEvtHdlr);
            ParentPage.SetConfigPage(this);
            if (GlobalSettings.MultiWindowMode)
            {
                Common.SetWindowSize();
            }
        }


        private void GetCurrentRowCol(ref int row, ref int col)
        {
            col = ColumnNumber.SelectedIndex + 1;
            row = RowNumber.SelectedIndex + 1;
        }

        private Object SyncObj = new Object();
        private void UpdateElementRowCol(bool baseOnRowCol)
        {
            if (BtGrids.XingcaoMode)
            {
                return;
            }
            lock(SyncObj)
            {
                Debug.WriteLine("UpdateElementRowCol: {0}", baseOnRowCol);
                if (IsParaInvalid())
                {
                    return;
                }
                if (CurrentElements.SelectedIndex == -1)
                {
                    return;
                }
                int ElemIndex = CurrentElements.SelectedIndex;
                int row = 1, col = 1;
                GetCurrentRowCol(ref row, ref col);
                if (baseOnRowCol)
                {
                    int ToElemIndex = BtGrids.GetIndex(row, col, true);
                    Debug.WriteLine("RowCol: ({0},{1}) -> ElementIndex: {2}", row, col, ToElemIndex);
                    Debug.Assert(ElemIndex < CurrentElements.Items.Count);
                    if (ToElemIndex != ElemIndex)
                    {
                        CurrentElements.SelectedIndex = ToElemIndex;
                    }
                }
                else
                {
                    int ToRow = 0;
                    int ToCol = 0;
                    BtGrids.IndexToRowCol(ElemIndex, ref ToRow, ref ToCol, true);
                    Debug.WriteLine("RowCol: ({0},{1}) <- ElementIndex: {2}", ToRow, ToCol, ElemIndex);
                    Debug.Assert(ToRow <= RowNumber.Items.Count);
                    Debug.Assert(ToCol <= ColumnNumber.Items.Count);
                    if (row != ToRow)
                    {
                        RowNumber.SelectedIndex = ToRow - 1;
                    }
                        
                    if (col != ToCol)
                    {
                        ColumnNumber.SelectedIndex = ToCol - 1;
                    }
                }
            }
           
        }
        CanvasStrokeStyle StrokeStyle = new CanvasStrokeStyle()
        {
            TransformBehavior = CanvasStrokeTransformBehavior.Hairline,
            DashCap = CanvasCapStyle.Round,
            DashOffset = 1.0F
        };
        private void DrawRectangle(CanvasDrawingSession draw, Rect rect, Color color,
            float strokeWidth, PenLineType type)
        {
            if (type == PenLineType.Line)
            {
                DrawRectangle(draw, rect, color, strokeWidth, true);
            }
            else if (type == PenLineType.Dash)
            {
                DrawRectangle(draw, rect, color, strokeWidth, false, CanvasDashStyle.Dash);
            }
            else
            {
                DrawRectangle(draw, rect, color, strokeWidth, false, CanvasDashStyle.Dot);
            }
        }
        private void DrawRectangle(CanvasDrawingSession draw, Rect rect, Color color, 
            float strokeWidth, bool solid = false, CanvasDashStyle style = CanvasDashStyle.Dash)
        {
            //Debug.WriteLine("Draw Rectangle: ({0:0},{1:0},{2:0},{3:0}), Color: {4}, Width: {5}", rect.X, rect.Y, rect.Width, rect.Height,
            //    color, strokeWidth);
            CanvasSolidColorBrush brush = new CanvasSolidColorBrush(draw, color);
            Rect rc = new Rect()
            {
                X = rect.Left + AdjustExtendSize,
                Y = rect.Top + AdjustExtendSize,
                Width = rect.Width,
                Height = rect.Height
            };

            if (solid)
            {
                draw.DrawRectangle(rc, color, strokeWidth);
            }
            else
            {
                StrokeStyle.DashStyle = style;
                draw.DrawRectangle(rc, brush, strokeWidth, StrokeStyle);
            }
        }

        private void UpdateOpNotfText(int selRow, int selCol, int index)
        {
            string notfInfo;
            string elemstr = BtGrids.GetElementString(index);
            switch (OpType)
            {
                case OperationType.SingleColumn:
                    notfInfo = string.Format("当前调整第{0}列，选中{1}", selCol, elemstr);
                    break;
                case OperationType.SingleRow:
                    notfInfo = string.Format("当前调整第{0}行，选中{1}", selRow, elemstr);
                    break;
                case OperationType.WholePage:
                    notfInfo = string.Format("当前调整整页，选中{0}", elemstr);
                    break;
                case OperationType.SingleElement:
                default:
                    notfInfo = string.Format("当前调整{0}", elemstr);
                    break;

            }
            OpNotfText.Text = notfInfo;
        }

        private void UpdateOpNotfTextXc(int currentElemIndex)
        {
            string notfInfo;
            if (currentElemIndex < 0)
            {
                notfInfo = string.Format("请用鼠标截取{0}所在的区域",
                                            BtGrids.GetElementString(CurrentElements.SelectedIndex));
            }
            else
            {
                notfInfo = string.Format("当前选择调整{0}", BtGrids.GetElementString(currentElemIndex));
            }

            OpNotfText.Text = notfInfo;
        }

        private void DrawBanner(CanvasDrawingSession draw, int currentElemIndex)
        {
            Rect rc = new Rect()
            {
                X = 0,
                Y = GetItemScrollHeight()
            };
            rc.Height = (BtImageShowRect.Height - rc.Y) * 0.1;
            rc.Width = BtImageShowRect.Width;

            if (rc.Height > 30)
            {
                rc.Height = 30;
            }
            rcBanner = rc;
            // 在下方/上方显示当前元素名称
            CanvasTextFormat fmt = new CanvasTextFormat()
            {
                FontSize = (int)rc.Height / 2,
                HorizontalAlignment = CanvasHorizontalAlignment.Center,
                VerticalAlignment = CanvasVerticalAlignment.Center
            };

            if (fmt.FontSize < 10)
            {
                fmt.FontSize = 10;
            }
            Debug.WriteLine("Height: {0:0}, FontSize: {1}", rc.Height, fmt.FontSize);

            string notfInfo;
            if (currentElemIndex < 0)
            {
                notfInfo = string.Format("请用鼠标截取{0}所在的区域", 
                                            BtGrids.GetElementString(CurrentElements.SelectedIndex));
            }
            else
            {
                notfInfo = string.Format("当前选择调整{0}", BtGrids.GetElementString(currentElemIndex));
            }

            draw.FillRectangle(rc, Colors.Green);
            draw.DrawText(notfInfo, rc, Colors.White, fmt);
        }

        private Size MeasureTextSize(string text, CanvasTextFormat textFormat, float limitedToWidth = 0.0f, float limitedToHeight = 0.0f)
        {
            /*
            var device = CanvasDevice.GetSharedDevice();

            var layout = new CanvasTextLayout(device, text, textFormat, limitedToWidth, limitedToHeight);

            var width = layout.DrawBounds.Width;
            var height = layout.DrawBounds.Height;
            return new Size(width, height);*/


            var tb = new TextBlock { Text = text + "add", FontSize = textFormat.FontSize };
            tb.Measure(new Size(Double.PositiveInfinity, Double.PositiveInfinity));
            return tb.DesiredSize;

        }

        private void DrawElementText(CanvasDrawingSession draw, Rect rc, int index, bool moveToCapture = false)
        {
            string name = BtGrids.GetElementString(index);
            string sizeTxt = string.Format("{0:0}✕{1:0}", rc.Width, rc.Height);
            double selectedPenWidth = BtGrids.PenWidth + 1;
            double smallerLength = (rc.Height > rc.Width) ? rc.Width : rc.Height;
            
            double height = smallerLength * 0.2;
            double maxTxtHeight = Common.MAX_ELEMENT_TEXT_HEIGHT;

            if (LineType == PenLineType.Line)
            {
                maxTxtHeight = Common.EXTRA_MAX_ELEM_TXT_HEIGHT;
            }
            height = (height < Common.MIN_ELEMENT_TEXT_HEIGHT) ? Common.MIN_ELEMENT_TEXT_HEIGHT : height;
            height = (height > maxTxtHeight) ? maxTxtHeight : height;

            int szFontSize = (int)height / 2;
            string maxString = (name.Length > sizeTxt.Length) ? name : sizeTxt;
            
            Rect txtRc = new Rect()
            {
                X = rc.Left
            };

            Rect szRc = new Rect()
            {
                X = rc.Right + BtGrids.PenWidth,
                Y = rc.Top,
            };
            

            if (moveToCapture)
            {
                if ((rc.Width == 0) || (rc.Height == 0))
                {
                    return;
                }
                if (ShowSizeMode)
                {
                    sizeTxt = name + "\r\n" + sizeTxt;
                }
                else
                {
                    sizeTxt = name;
                }
            }
            else
            {
                txtRc.X = rc.Left - selectedPenWidth/2;
                if (rc.Bottom < (0.9 * BtImageAdjustRect.Height))
                {
                    txtRc.Y = rc.Bottom + 1;
                }
                else
                {
                    txtRc.Y = rc.Top - 1 - height;
                }
                txtRc.Height = height;
                txtRc.Width = rc.Width + selectedPenWidth;

            }

            // 绘制尺寸
            {

                CanvasTextFormat szFmt = new CanvasTextFormat()
                {
                    FontSize = szFontSize,
                    HorizontalAlignment = CanvasHorizontalAlignment.Center,
                    VerticalAlignment = CanvasVerticalAlignment.Center
                };
                Size sz = MeasureTextSize(maxString, szFmt);

                szRc.Width = sz.Width + 10;
                szRc.Height = sz.Height + 2;
                if (szRc.Right > BtImageShowRect.Width)
                {
                    szRc.X = rc.Left - szRc.Width - BtGrids.PenWidth;
                    szRc.Width = szRc.Width;
                }
                if (!moveToCapture)
                {
                    if (ShowSizeMode)
                    {
                        FillOpacity(draw, szRc, Colors.White, 0.7);
                        draw.DrawText(sizeTxt, szRc, Colors.Black, szFmt);
                    }
                }
                else
                {
                    szRc.Height *= 2;
                    DrawRectangle(draw, szRc, Colors.Black, 1);
                    draw.FillRectangle(szRc, Colors.White);
                    draw.DrawText(sizeTxt, szRc, Colors.Blue, szFmt);
                }

            }

            if (!moveToCapture)
            {

                // 在下方/上方显示当前元素名称
                CanvasTextFormat fmt = new CanvasTextFormat()
                {
                    FontSize = (int)txtRc.Height / 2,
                    HorizontalAlignment = CanvasHorizontalAlignment.Center,
                    VerticalAlignment = CanvasVerticalAlignment.Center
                };

                // 在调整区域时不覆盖
                RedrawImageRect(draw, rc);
                DrawRectangle(draw, rc, Colors.Red, (float)selectedPenWidth, true);
                draw.FillRectangle(txtRc, Colors.Red);
                draw.DrawText(name, txtRc, Colors.White, fmt);
            }
        }
        private void RedrawImageRect(CanvasDrawingSession draw, Rect rc)
        {
            Rect srcRc = new Rect()
            {
                X = rc.X + BtImageAdjustRect.X,
                Y = rc.Y + BtImageAdjustRect.Y,
                Width = rc.Width,
                Height = rc.Height,
            };
            // 确保图片不变形
            if (srcRc.X < BtGrids.PageMargin.Left)
            {
                srcRc.X = BtGrids.PageMargin.Left;
            }
            if (srcRc.Y < BtGrids.PageMargin.Top)
            {
                srcRc.Y = BtGrids.PageMargin.Top;
            }
            if (srcRc.Right > BtGrids.DrawWidth)
            {
                srcRc.Width = BtGrids.DrawWidth - srcRc.X;
            }
            if (srcRc.Bottom > BtGrids.DrawHeight)
            {
                srcRc.Height = BtGrids.DrawHeight - srcRc.Y;
            }
            rc.Width = srcRc.Width;
            rc.Height = srcRc.Height;

            draw.FillRectangle(rc, Colors.Red);
            draw.DrawImage(BtImage.cvsBmp, rc, srcRc);
        }
        private double GetItemScrollHeight()
        {
            var transform = CurrentItem.TransformToVisual(ItemScrollViewer);
            var point = transform.TransformPoint(new Point(0, -10));

            return point.Y;
        }
        CanvasTextFormat InfoCTF = new CanvasTextFormat()
        {
            VerticalAlignment = CanvasVerticalAlignment.Center,
            HorizontalAlignment = CanvasHorizontalAlignment.Center,
        };
        bool FirstShowBanner = true;
        Rect rcBanner = new Rect();
        
        private void FillOpacity(CanvasDrawingSession draw, Rect rc, Color clr, double opacity, bool noOpacity=false)
        {
            if (noOpacity)
            {
                return;
            }

            CanvasSolidColorBrush opBrush = new CanvasSolidColorBrush(draw, clr)
            {
                Opacity = (float)opacity
            };
            draw.FillRectangle(rc, opBrush);
        }

        private Color GetRandomColor()
        {
            Random ran = new Random();
            int n = ran.Next(1, BtGrids.BackupColors.Count);
            return BtGrids.BackupColors.ElementAt(n);
        }

        private void AutoSlideToElement(Rect elementRc)
        {
            Point screenLt = new Point
            {
                X = ItemScrollViewer.HorizontalOffset,
                Y = ItemScrollViewer.VerticalOffset
            };
            Point screenRb = new Point
            {
                X = screenLt.X + ItemScrollViewer.ActualWidth,
                Y = screenLt.Y + ItemScrollViewer.ActualHeight
            };

            float zoomFactor = ItemScrollViewer.ZoomFactor;
            bool needScroll = false;

            Rect rcZoomed = new Rect
            {
                X = (elementRc.X  - BtImageAdjustRect.Left) * zoomFactor,
                Y = (elementRc.Y - BtImageAdjustRect.Top) * zoomFactor,
                Width = elementRc.Width * zoomFactor,
                Height = elementRc.Height * zoomFactor,
            };


            Debug.WriteLine("Screen: {0:F0},{1:F0},{2:F0},{3:F0}", screenLt.X, screenLt.Y, screenRb.X, screenRb.Y);
            Debug.WriteLine("Point: {0:F0},{1:F0}", rcZoomed.X, rcZoomed.Y);

            int deltaX = 0;
            int deltaY = 0;
            if ((rcZoomed.Bottom > screenRb.Y) ||
                (rcZoomed.Y < screenLt.Y))
            {
                deltaY = (int)(elementRc.Height * 0.2);// Common.AUTOSLIDE_OFFSET;
                deltaY = (deltaY < Common.AUTOSLIDE_OFFSET) ? Common.AUTOSLIDE_OFFSET : deltaY;
                needScroll = true;
            }
            if ((rcZoomed.Right > screenRb.X) ||
                (rcZoomed.X < screenLt.X))
            {
                deltaX = (int)(elementRc.Width * 0.2);//Common.AUTOSLIDE_OFFSET;
                deltaX = (deltaX < Common.AUTOSLIDE_OFFSET) ? Common.AUTOSLIDE_OFFSET : deltaX;
                needScroll = true;
            }
            if (!needScroll)
            {
                return;
            }

            double offsetX = (rcZoomed.X - deltaX);
            double offsetY = (rcZoomed.Y - deltaY);
            if (offsetX < 0)
            {
                offsetX = 0;
            }
            if (offsetY < 0)
            {
                offsetY = 0;
            }
            offsetX = (deltaX == 0) ? screenLt.X : offsetX;
            offsetY = (deltaY == 0) ? screenLt.Y : offsetY;
            Debug.WriteLine("Offset: {0:F0},{1:F0}", offsetX, offsetY);
            ItemScrollViewer.ChangeView(offsetX, offsetY, ItemScrollViewer.ZoomFactor); 
        }

        bool XingCaoAutoSlided = false;
        private void DrawLines(CanvasDrawingSession draw)
        {
            Point pntLt, pntRb;
            GetRectPoints(ToAdjustRect, ref pntLt, ref pntRb);
            Color BaseColor = BtGrids.PenColor;
            //Color OpacityColor = (BtGrids.BackupColors.Count > 0) ? BtGrids.BackupColors.ElementAt(0) : Colors.Green;
            float penWidth = BtGrids.PenWidth;
            Color drawColor = BtGrids.PenColor;
            Color RevisedColor = BtGrids.PenColor;// GetRandomColor();

            pntLt.X += ChangeRect.left;
            pntLt.Y += ChangeRect.top;
            pntRb.X += ChangeRect.right;
            pntRb.Y += ChangeRect.bottom;
            Rect drawRect = new Rect(pntLt, pntRb);
            
            DrawRectangle(draw, drawRect, BaseColor, penWidth, true);

            lock (DrawLineElements)
            {
                foreach (IntIndex elem in DrawLineElements)
                {
                    int row = elem.row;
                    int col = elem.col;
                    Rect rc = BtGrids.GetRectangle(row, col);
                    rc.X -= BtImageAdjustRect.X;
                    rc.Y -= BtImageAdjustRect.Y;
                    drawColor = BtGrids.PenColor;
                    if (BtGrids.GetRevised(row, col))
                    {
                        if (DrawLineElements.Count > 1)
                        {
                            //  在改变过程中不变化颜色
                            if ((CurrentPntrStatus != PointerStatus.MoveToScalingBorder) &&
                                (CurrentPntrStatus != PointerStatus.PressedToDrag))
                            {
                                drawColor = RevisedColor;
                            }
                             
                        }
                    }
                    if (BtGrids.ElementIsKongbai(BtGrids.GetIndex(row, col, BtGrids.BookOldType)))
                    {
                        InfoCTF.FontSize = (float)rc.Height / 4;

                        Common.DrawKongbaiElement(draw, rc);
                        drawColor = Colors.Gray;
                        DrawRectangle(draw, rc, drawColor, penWidth+1, true);
                    }
                    else
                    {
                        //辅助线使用点
                        if (BtGrids.XingcaoMode)
                        {
                            DrawRectangle(draw, rc, drawColor, penWidth, false, CanvasDashStyle.Dot);
                        }
                        else 
                        {
                            DrawRectangle(draw, rc, drawColor, penWidth, LineType);
                        }
                    }
                }
                if (!BtGrids.XingcaoMode)
                {
                    int selrow = RowNumber.SelectedIndex + 1;
                    int selcol = ColumnNumber.SelectedIndex + 1;
                    int index = BtGrids.GetIndex(selrow, selcol, BtGrids.BookOldType);
                    Rect rc = BtGrids.GetRectangle(selrow, selcol);

                    //if (OpType == OperationType.SingleElement)
                    {
                        AutoSlideToElement(rc);
                    }
                    UpdateOpNotfText(selrow, selcol, index);

                    rc.X -= BtImageAdjustRect.X;
                    rc.Y -= BtImageAdjustRect.Y; 
                    if ((OperationType.SingleElement == OpType))
                    {
                        FillOpacity(draw, BtImageShowRect, Colors.White, 0.1);
                        DrawElementText(draw, rc, index);
                    }
                    else
                    {
                        rc.X += 1;
                        rc.Y += 1;
                        rc.Width -= 2;
                        rc.Height -= 2;
                        RedrawImageRect(draw, rc);
                        //draw.DrawRectangle(rc, Colors.Red);
                        FillOpacity(draw, rc, Colors.White, 0.2);
                    } 
                }
            }
            
            if (BtGrids.XingcaoMode)
            {
                lock (BtGrids.XingcaoElements)
                {
                    int currentElemIndex = -1;
                    foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
                    {
                        Rect rc = pair.Value.rc;
                        rc.X -= BtImageAdjustRect.X;
                        rc.Y -= BtImageAdjustRect.Y;

                        if (pair.Key == CurrentElements.SelectedIndex)
                        {
                            currentElemIndex = pair.Key;
                           
                        }
                        else
                        {
                            DrawRectangle(draw, rc, drawColor, penWidth, LineType);
                        }
                    }

                    // 最后绘制所选中元素，达到覆盖其他的目的
                    if (currentElemIndex >= Common.MIN_INDEX)
                    {
                        var result = BtGrids.XingcaoElements.Where(p => (p.Key == currentElemIndex));
                        foreach (KeyValuePair<int, BeitieGridRect> pair in result)
                        {
                            Rect rc = pair.Value.rc;
                            rc.X -= BtImageAdjustRect.X;
                            rc.Y -= BtImageAdjustRect.Y;

                            RedrawImageRect(draw, drawRect);
                            DrawElementText(draw, rc, pair.Key);

                            AutoSlideToElement(rc);
                        }
                    }
                    else
                    {
                        if (LastPntrStatus == PointerStatus.MoveToCapture)
                        {

                            FillOpacity(draw, BtImageShowRect, Colors.White, 0.15, NoOpacityMode);
                            int index = CurrentElements.SelectedIndex;
                            RedrawImageRect(draw, drawRect);
                            DrawElementText(draw, drawRect, index, true);
                        }
                    }

                    UpdateOpNotfTextXc(currentElemIndex);
                    if (!XingCaoAutoSlided && (BtGrids.XingcaoElements.Count == 0))
                    {
                        Rect rcDefault = new Rect
                        {
                            X = BtGrids.DrawWidth - 10,
                            Y = 0,
                            Width = 10,
                            Height = 10
                        };
                        AutoSlideToElement(rcDefault);
                        XingCaoAutoSlided = true;
                    }

                }
            }
            if ((CurrentPntrStatus == PointerStatus.MoveToScalingBorder) ||
               (CurrentPntrStatus == PointerStatus.PressedToDrag))
            {
                if ((OpType != OperationType.SingleElement))
                {
                    RedrawImageRect(draw, BtImageShowRect);
                }
                
                FillOpacity(draw, BtImageShowRect, Colors.White, 0.15, NoOpacityMode);

                RedrawImageRect(draw, drawRect);
                DrawRectangle(draw, drawRect, BaseColor, penWidth, true);
            }
        }
        private void CurrentItem_OnDraw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            Debug.WriteLine("CurrentItem_OnDraw() called");
            lock(BtImage.cvsBmp)
            {
                var draw = args.DrawingSession;
                
                draw.Clear(Colors.Black);
                draw.DrawRectangle(BtImageShowRect, Colors.White, 1);
                if ((BtGrids == null) || (BtImage == null))
                {
                    draw.DrawText("参数错误!", new Vector2(100, 100), Colors.Black);
                    return;
                }
                if (BtImage.cvsBmp == null)
                {
                    draw.Clear(Colors.Black);
                    draw.DrawText("图片正在加载中...", new Vector2(100, 100), Colors.Blue);
                    Refresh();
                    return;
                }
                draw.DrawImage(BtImage.cvsBmp, BtImageShowRect, BtImageAdjustRect);
                DrawLines(draw);
            }
            
        }

        private void ColumnNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateElementRowCol(true);
            Refresh(CtrlMessageType.ColumnChange);
        }

        private void RowNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateElementRowCol(true);
            Refresh(CtrlMessageType.RowChange);
        }

        private void UpdateFixedChecks()
        {
            FixedHeight = ChkFixedHeight?.IsChecked ?? false;
            FixedWidth = ChkFixedWidth?.IsChecked ?? false;
        }
        private void UpdateFixedChecks(bool bFixedW, bool bFixedH)
        {
            ChkFixedHeight.IsChecked = bFixedH;
            ChkFixedWidth.IsChecked = bFixedW;
            
            FixedHeight = ChkFixedHeight?.IsChecked ?? false;
            FixedWidth = ChkFixedWidth?.IsChecked ?? false;
        }
        private void SetButton(object uielem, bool enabled)
        {
            Button btn = (Button)uielem;
            btn.IsEnabled = enabled;

            btn.Visibility = enabled ? Visibility.Visible : Visibility.Collapsed;
        }

        private void Operation_Checked(object sender, RoutedEventArgs e)
        {
            if (BtGrids == null)
            {
                return;
            }
            if (OpSingleElement?.IsChecked ?? false)
            {
                OpType = OperationType.SingleElement;
                if (BtnLeftElement != null)
                {
                    BtnTopElement.Content = "上一字";
                    BtnBottomElement.Content = "下一字";
                    SetButton(BtnTopElement, true);
                    SetButton(BtnBottomElement, true);

                    if (!BtGrids.XingcaoMode)
                    {
                        SetButton(BtnLeftElement, true);
                        SetButton(BtnRightElement, true);
                        BtnLeftElement.Content = "左一字";
                        BtnRightElement.Content = "右一字";
                        UpdateFixedChecks(true, true);
                    }
                    else
                    {
                        SetButton(BtnLeftElement, false);
                        SetButton(BtnRightElement, false);
                    }
                }
            }
            else if (OpSingleRow?.IsChecked ?? false)
            {
                OpType = OperationType.SingleRow;
                
                if (BtnLeftElement != null)
                {
                    BtnTopElement.Content = "上一行";
                    BtnBottomElement.Content = "下一行";
                    SetButton(BtnLeftElement, false);
                    SetButton(BtnRightElement, false);
                    SetButton(BtnTopElement, true);
                    SetButton(BtnBottomElement, true);
                    UpdateFixedChecks(false, false);
                }
            }
            else if (OpSingleColumn?.IsChecked ?? false)
            {
                OpType = OperationType.SingleColumn;
                if (BtnLeftElement != null)
                {
                    BtnLeftElement.Content = "左一列";
                    BtnRightElement.Content = "右一列";
                    SetButton(BtnLeftElement, true);
                    SetButton(BtnRightElement, true);
                    SetButton(BtnBottomElement, false);
                    SetButton(BtnTopElement, false);
                    UpdateFixedChecks(false, false);
                }
            }
            else if (OpWholePage?.IsChecked ?? false)
            {
                OpType = OperationType.WholePage;
                if (BtnLeftElement != null)
                {
                    SetButton(BtnRightElement, false);
                        SetButton(BtnLeftElement, false);
                    if (BtGrids.XingcaoMode)
                    {
                        SetButton(BtnTopElement, true);
                        SetButton(BtnBottomElement, true);
                    }
                    else
                    {
                        SetButton(BtnTopElement, false);
                        SetButton(BtnBottomElement, false);
                    }
                    UpdateFixedChecks(false, false);
                }
            }

            Refresh(CtrlMessageType.OperationChange);
        }
        
        private void UpdateOpInfoBox()
        {
            string txt = "";
            if (ChangeRect.left != 0)
            {
                txt += string.Format("左移:{0:0} ", ChangeRect.left);
            }
            if (ChangeRect.right != 0)
            {
                txt += string.Format("右移:{0:0} ", ChangeRect.right);
            }
            if (ChangeRect.top != 0)
            {
                txt += string.Format("上移:{0:0} ", ChangeRect.top);
            }
            if (ChangeRect.bottom != 0)
            {
                txt += string.Format("下移:{0:0} ", ChangeRect.bottom);
            }
            AdjustOpInfoBox.Text = txt;
        }

        private void AdjustFunction(object sender, bool UpdateRect)
        {
            if (sender == BtnBottomAdd)
            {
                ChangeRect.bottom += ChangeStep;
            }
            else if (sender == BtnBottomMinus)
            {
                ChangeRect.bottom -= ChangeStep;
            }
            else if (sender == BtnLeftAdd)
            {
                ChangeRect.left += ChangeStep;
            }
            else if (sender == BtnLeftMinus)
            {
                ChangeRect.left -= ChangeStep;
            }
            else if (sender == BtnRightAdd)
            {
                ChangeRect.right += ChangeStep;
            }
            else if (sender == BtnRightMinus)
            {
                ChangeRect.right -= ChangeStep;
            }
            else if (sender == BtnTopAdd)
            {
                ChangeRect.top += ChangeStep;
            }
            else if (sender == BtnTopMinus)
            {
                ChangeRect.top -= ChangeStep;
            }
            UpdateOpInfoBox();
            Debug.WriteLine("Change: {0:0},{1:0},{2:0},{3:0}", ChangeRect.left, ChangeRect.top, ChangeRect.right, ChangeRect.bottom);
            if (!IsChangeNoteOverflow())
            {
                Debug.WriteLine("Change invalid, revert to last one");
                ChangeRect.Copy(LastChangeRect);
                return;
            }
            if (UpdateRect)
            {
                if (!UpdateElementsRects())
                {
                    Debug.WriteLine("Change invalid, revert to last one");
                    ChangeRect.Copy(LastChangeRect);
                    return;
                }
                Refresh();
            }
            else
            {
                UpdateAdjustStatus();
            }
        }

        int GetXingCaoElementCount()
        {
            if (OperationType.SingleElement == OpType)
            {
                return 1;
            }
            else if (OperationType.WholePage == OpType)
            {
                return BtGrids.XingcaoElements.Count;
            }
            else if (OperationType.SingleColumn == OpType)
            {
                int counter = 0;
                int selcol = ColumnNumber.SelectedIndex + 1;
                foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
                {
                    if (pair.Value.col == selcol)
                    {
                        counter++;
                    }
                }
                return counter;
            }
            else
            {
                return 0;
            }
        }

        void UpdateAdjustStatus()
        {
            string info = "";
            int selectedElemIndex = CurrentElements.SelectedIndex;
            info += string.Format("当前可修改元素: {0}个, ", BtGrids.XingcaoMode ?
                GetXingCaoElementCount() : DrawLineElements.Count );
            info += string.Format("当前选中元素：{0}, ", BtGrids.GetElementString(selectedElemIndex));
            info += string.Format("当前元素区域尺寸： {0:0}*{1:0}, ", ToAdjustRect.Width, ToAdjustRect.Height);
            if (!BtGrids.XingcaoMode)
            {
                info += string.Format("当前元素区域改变量: {0:0},{1:0},{2:0},{3:0}, ", ChangeRect.left, ChangeRect.top, ChangeRect.right, ChangeRect.bottom);
            }
            info += string.Format("修改角度: {0:F1}", BtGrids.angle);
            NotifyUser(info, NotifyType.StatusMessage);
        }

        
        private void CurrentElements_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateElementRowCol(false);
            Refresh();
        }

        private void ElementMove_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender == BtnLeftElement)
            {
                int selectedIndex = ColumnNumber.SelectedIndex;
                if (selectedIndex == 0)
                {
                    selectedIndex = ColumnNumber.Items.Count - 1;
                }
                else
                {
                    selectedIndex = GetPreviousColRow(ColumnNumber.SelectedIndex, 0);
                }
                ColumnNumber.SelectedIndex = selectedIndex;
            }
            else if (sender == BtnRightElement)
            {

                int selectedIndex = ColumnNumber.SelectedIndex;
                if (selectedIndex == (ColumnNumber.Items.Count - 1))
                {
                    selectedIndex = 0;
                }
                else
                {
                    selectedIndex = GetNextColRow(ColumnNumber.SelectedIndex, BtGrids.Columns - 1);
                }
                ColumnNumber.SelectedIndex = selectedIndex;
            }
            else if (sender == BtnTopElement)
            {
                int selectedIndex = CurrentElements.SelectedIndex;
                if (selectedIndex == 0)
                {
                    selectedIndex = CurrentElements.Items.Count - 1;
                }
                else
                {
                    selectedIndex = GetPreviousColRow(CurrentElements.SelectedIndex, 0);
                }
                CurrentElements.SelectedIndex = selectedIndex;
            }
            else if (sender == BtnBottomElement)
            {

                int selectedIndex = CurrentElements.SelectedIndex;
                if (selectedIndex == (CurrentElements.Items.Count - 1))
                {
                    selectedIndex = 0;
                }
                else
                {
                    selectedIndex = GetNextColRow(CurrentElements.SelectedIndex, (CurrentElements.Items.Count - 1));
                }
                CurrentElements.SelectedIndex = selectedIndex;
            }
        }
        private /*async*/ void UpdateAngle(bool fromTxtBox = false)
        {
            //await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            //   () =>
            //   {
            //    }
            //);
            if (AngleTxtBox.Text != null)
            {
                float angle = float.Parse(AngleTxtBox.Text);
                if (angle == BtGrids.angle)
                {
                    return;
                }
                if (fromTxtBox)
                {
                    BtGrids.angle = angle;
                }
                else
                {
                    AngleTxtBox.Text = string.Format("{0:F2}", BtGrids.angle);
                }
            }
        }

        private async void Rotate_Clicked(object sender, RoutedEventArgs args)
        {
            if (sender == BtnRotateLeft)
            {
                BtGrids.angle -= 0.1F;
            }
            else if (sender == BtnRotateRight)
            {
                BtGrids.angle += 0.1F;
            }
            UpdateAngle(false);

            try
            {
                BtImage.cvsBmp = await ParentPage.RotateImage(BtGrids.angle);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.ToString());
                return;
            }

            Refresh(CtrlMessageType.RotateChange);
        }

        private async void AngleTextBox_LostFocus(object sender, RoutedEventArgs args)
        {
            UpdateAngle(true);

            try
            {
                BtImage.cvsBmp = await ParentPage.RotateImage(BtGrids.angle);
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception: " + e.ToString());
                return;
            }

            Refresh(CtrlMessageType.RotateChange);
        }

        private void AverageCheck_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender == ChkAvgCol)
            {
                AvgCol = ChkAvgCol.IsChecked ?? true;
            }
            else if (sender == ChkAvgRow)
            {
                AvgRow = ChkAvgRow.IsChecked ?? true;
            }
            UpdateElementsRects();
            Refresh(CtrlMessageType.AdjustChange);
        }

        private Rect GetRowColRect()
        {
            int minRow = MIN_ROW_COLUMN;
            int minCol = MIN_ROW_COLUMN;
            int maxRow = BtGrids.Rows;
            int maxCol = BtGrids.Columns;

            int row = 0, col = 0;
            GetCurrentRowCol(ref row, ref col);

            switch (OpType)
            {
                case OperationType.SingleColumn:
                    minCol = maxCol = col;
                    break;
                case OperationType.SingleRow:
                    minRow = maxRow = row;
                    break;
                case OperationType.SingleElement:
                    minCol = maxCol = col;
                    minRow = maxRow = row;
                    break;
                case OperationType.WholePage:
                default:
                    break;
            }

            return new Rect(new Point(minRow, minCol), new Point(maxRow, maxCol));
        }
        private async void SaveSplittedImages(object para)
        {
            if (await ParentPage.NeedNotifyPageText())
            {
                if (await Common.ShowNotifyPageTextDlg())
                {
                    bool bContinue = true;
                    if (BtGrids.XingcaoMode)
                    {
                        bool bRet = await Common.ShowCloseSwitchToMainDlg();
                        if (!bRet)
                        {
                            bContinue = false;
                        }
                        else
                        {
                            if (Frame.CanGoBack)
                            {
                                Frame.GoBack();
                            }
                            else
                            {
                                await ApplicationViewSwitcher.SwitchAsync(ParentPage.PageViewId,
                                         ApplicationView.GetForCurrentView().Id,
                                        ApplicationViewSwitchingOptions.ConsolidateViews);
                            }
                        }
                    }
                    else
                    {
                        if (Frame.CanGoBack)
                        {
                            Frame.GoBack();
                        }
                        else
                        {
                            await ApplicationViewSwitcher.SwitchAsync(ParentPage.PageViewId);
                        }
                    }
                    if (bContinue)
                    {
                        return;
                    }
                }
                BtGrids.BtImageParent.PageTextConfirmed = true;
            }
            ParentPage.HandlerSaveSplittedImages(para);
        }
        public void HandlerShowSaveResultEvt(object para)
        {
            this.ShowSaveResultEvtHdlr.Invoke(para, null);
        }

        private async void OnShowSaveResultEvtHdlr(object sender, EventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SaveErrorType type = ParentPage.SaveErrType;

                if (type == SaveErrorType.NoSelectedItem)
                {
                    NotifyUser("未选择元素进行保存!", NotifyType.ErrorMessage);
                    Common.ShowMessageDlg("未选择元素进行保存!", null);
                }
                else if (type == SaveErrorType.ParaError)
                {
                    NotifyUser(ParentPage.SaveNotfInfo, NotifyType.ErrorMessage);
                    Common.ShowMessageDlg(ParentPage.SaveNotfInfo, null);
                }
                else if (type == SaveErrorType.Success)
                {
                    NotifyUser(ParentPage.SaveNotfInfo, NotifyType.StatusMessage);
                    Common.ShowMessageDlg(ParentPage.SaveNotfInfo, null);
                }

                ApplicationView.GetForCurrentView().ExitFullScreenMode();
            });
        }

        private void BtnSave_Clicked(object sender, RoutedEventArgs e)
        {
            HashSet<int> elementIndexes = new HashSet<int>();
            if (sender == BtnSaveAll)
            {
                elementIndexes = null;
            }
            else
            {
                if (!BtGrids.XingcaoMode)
                {
                    foreach (IntIndex pnt in DrawLineElements)
                    {
                        elementIndexes.Add(BtGrids.GetIndex(pnt.row, pnt.col, true));
                    }
                }
                else
                {
                    if (OperationType.SingleElement == OpType)
                    {
                        elementIndexes.Add(CurrentElements.SelectedIndex);
                    }
                    else if (OperationType.SingleColumn == OpType)
                    {
                        int selectedCol = ColumnNumber.SelectedIndex + 1;
                        foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
                        {
                            if (pair.Value.col == selectedCol)
                            {
                                elementIndexes.Add(pair.Key);
                            }
                        }
                    }
                    else
                    {
                        elementIndexes = null;
                    }
                }

                //ParentPage.HandlerSaveSplittedImages(elementIndexes);

            }
            SaveSplittedImages(elementIndexes);
        }
        

        private void FixedCheck_Clicked(object sender, RoutedEventArgs e)
        {
            UpdateFixedChecks();
        }

        private async void AdjustTimerFunction(ThreadPoolTimer timer)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.High, () =>
            {
                Debug.WriteLine("AdjustTimerFunction() called");
                AdjustFunction(CurrentAdjustSender, false);
            });
        }
        ThreadPoolTimer AdjustTimer = null;
        static int AdjustTimerPeriod = 100;
        TimeSpan delay = TimeSpan.FromMilliseconds(AdjustTimerPeriod);
        object CurrentAdjustSender = null;
        private void Adjust_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            //Debug.WriteLine("Adjust_PointerPressed() called");
            CurrentAdjustSender = sender;
            LastChangeRect.Copy(ChangeRect);
            AdjustTimer = ThreadPoolTimer.CreatePeriodicTimer(new TimerElapsedHandler(AdjustTimerFunction), delay);
        }

        private void Adjust_PointerReleased(object sender, PointerRoutedEventArgs e)
        { 
            //Debug.WriteLine("Adjust_PointerReleased() called");
            if (AdjustTimer != null)
            {
                AdjustTimer.Cancel();
            }
            AdjustFunction(sender, true);
        }

        private void ResetCurrentElement()
        {
            int row = 1, col = 1;
            GetCurrentRowCol(ref row, ref col);
            BtGrids.ElementRects[BtGrids.ToIndex(row, col)] = new BeitieGridRect(LastBtGrids.GetElement(row, col).rc);

            int selectedElemIndex = CurrentElements.SelectedIndex;
            BtGrids.XingcaoElements.Remove(selectedElemIndex);
            if (LastBtGrids.XingcaoElements.ContainsKey(selectedElemIndex))
            {
                BeitieGridRect bgr = new BeitieGridRect(new Rect());
                LastBtGrids.XingcaoElements.TryGetValue(selectedElemIndex, out bgr);
                BtGrids.XingcaoElements.Add(selectedElemIndex, new BeitieGridRect(bgr.rc)
                {
                    col = bgr.col,
                    revised = false
                });
            }
            
            BtGrids.Elements.Remove(selectedElemIndex);
            if (LastBtGrids.Elements.ContainsKey(selectedElemIndex))
            {
                LastBtGrids.Elements.TryGetValue(selectedElemIndex, out BeitieElement be);
                BtGrids.Elements.Add(selectedElemIndex, new BeitieElement(be.type,
                    be.content, be.no)
                {
                    col = be.col,
                    text = be.text
                });
            }
        }

        private void ResetElement_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender == BtnResetAllElements)
            {
                BtGrids.ElementRects.Clear();
                foreach (BeitieGridRect bgr in LastBtGrids.ElementRects)
                {
                    BtGrids.ElementRects.Add(new BeitieGridRect(bgr.rc));
                }
                BtGrids.XingcaoElements.Clear();
                foreach (KeyValuePair<int, BeitieGridRect> pair in LastBtGrids.XingcaoElements)
                {
                    BtGrids.XingcaoElements.Add(pair.Key, new BeitieGridRect(pair.Value.rc)
                    {
                        col = pair.Value.col,
                        revised = false
                    }
                    );
                }
                BtGrids.Elements.Clear();
                foreach (KeyValuePair<int, BeitieElement> pair in LastBtGrids.Elements)
                {
                    BtGrids.Elements.Add(pair.Key, new BeitieElement(pair.Value.type,
                        pair.Value.content, pair.Value.no)
                    {
                        col = pair.Value.col,
                        text = pair.Value.text
                    });
                }
            }
            else if (sender == BtnResetElement)
            {
                ResetCurrentElement();
            }
            Refresh();
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
        bool HaveGotFocus = true;
        protected override void OnGotFocus(RoutedEventArgs e)
        { 
            Debug.WriteLine("SettingPage OnGotFocus(): {0}", HaveGotFocus);
            if (this.IsLoaded && !HaveGotFocus)
            {
                HaveGotFocus = true;
                Refresh(CtrlMessageType.RedrawRequest);
            }
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(RoutedEventArgs e)
        {
            Debug.WriteLine("SettingPage OnLostFocus(): {0}", HaveGotFocus);
            HaveGotFocus = false;
            base.OnLostFocus(e);
        }
        
        double ChangeStep = 1.0;
        private void ChangeStepTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ChangeStep = double.Parse(ChangeStepTxtBox.Text);
        }
        enum PointerTargetType
        {
            ElementRect,
            XingcaoInitDraw,
        }
        PointerTargetType PntrTargetType = PointerTargetType.ElementRect;

        enum PointerLocation
        {
            OutsideImage,
            InsideImageButNotElement,
            OnLeftBorder,
            OnRightBorder,
            OnTopBorder,
            OnBottomBorder,
            OnLeftTopBorder,
            OnLeftBottomBorder,
            OnRightTopBorder,
            OnRightBottomBorder,
            OnElementBody,
            InsideImage,
        }
        enum PointerStatus
        {
            Entered,
            Moved,
            Pressed,
            RBtnPressed,
            Released,
            Exited,
            PressedToDrag,
            ReleasedToExit,
            MoveOnBorder,
            MoveToScalingBorder,
            MoveToCapture,
        }
        PointerStatus LastPntrStatus = PointerStatus.Exited;
        PointerStatus CurrentPntrStatus = PointerStatus.Exited;
        PointerLocation LastLocation = PointerLocation.OutsideImage;
        PointerLocation CurrentLocation = PointerLocation.OutsideImage;
        Point LastPointerPnt = new Point();
        Point CurrentPointerPnt = new Point();
        double AdjustDelta = 15;

        void ChangePointerCursor(CoreCursorType type)
        {
            //NotifyUser(string.Format("Cursor: {0}", type), NotifyType.StatusMessage);
            if (type == CoreCursorType.Custom)
            {
                return;
            }
            Window.Current.CoreWindow.PointerCursor = new Windows.UI.Core.CoreCursor(type, 1);
        }
        
        enum RectLocation
        {
            Left, 
            Top, 
            Bottom,
            Right,
        }

        bool IsOnBorder(double current, double baseline, double outMaxOffset, double inMaxOffset, RectLocation rl)
        {
            double delta = 0;
            
            switch (rl)
            {
                case RectLocation.Left:
                case RectLocation.Top:
                    delta = baseline - current;
                    break;
                case RectLocation.Bottom:
                case RectLocation.Right:
                    delta = current - baseline;
                    break;
            }

            //Debug.WriteLine("IsOnBorder: {0:F1} -> MaxOffset: {1:F1},{2:F1}", delta, outMaxOffset, inMaxOffset);
            if (delta > outMaxOffset)
            {
                return false;
            }
            if (delta < -inMaxOffset)
            {
                return false;
            }

            return true;
        }

        bool IsPntInRect(Point pp, Rect rc, double delta)
        {
            if ((pp.X < (rc.Left - delta)) ||
                (pp.Y < (rc.Top - delta)))
            {
                return false;
            }
            if ((pp.X > (rc.Right + delta)) ||
                (pp.Y > (rc.Bottom + delta)))
            {
                return false;
            }
            return true;
        }

        static readonly double IN_OFFSET_PROP = 0.15;
        static readonly double IN_OFFSET_MAX = 20;
        private PointerLocation GetPointerLocation(Point pp)
        {
            double outMaxOffset = BtGrids.XingcaoMode ? 1 : AdjustDelta;
            double inMaxOffsetX = ToAdjustRect.Width * IN_OFFSET_PROP;
            double inMaxOffsetY = ToAdjustRect.Height * IN_OFFSET_PROP;

            if (inMaxOffsetX > IN_OFFSET_MAX)
            {
                inMaxOffsetX = IN_OFFSET_MAX;
            }
            if (inMaxOffsetY > IN_OFFSET_MAX)
            {
                inMaxOffsetY = IN_OFFSET_MAX;
            }

            if (!IsPntInRect(pp, new Rect(0, 0, BtImageAdjustRect.Width, BtImageAdjustRect.Height), outMaxOffset))
            {
                return PointerLocation.OutsideImage;
            }

            if (PntrTargetType == PointerTargetType.XingcaoInitDraw)
            {
                return PointerLocation.InsideImage;
            }
            

            if (!IsPntInRect(pp, ToAdjustRect, outMaxOffset))
            {
                return PointerLocation.InsideImageButNotElement;
            }

            if (IsOnBorder(pp.X, ToAdjustRect.Left, outMaxOffset, inMaxOffsetX, RectLocation.Left))
            {
                if (IsOnBorder(pp.Y, ToAdjustRect.Top, outMaxOffset, inMaxOffsetY, RectLocation.Top))
                {
                    return PointerLocation.OnLeftTopBorder;
                }
                else if (IsOnBorder(pp.Y, ToAdjustRect.Bottom, outMaxOffset, inMaxOffsetY, RectLocation.Bottom))
                {
                    return PointerLocation.OnLeftBottomBorder;
                }
                else
                {
                    return PointerLocation.OnLeftBorder;
                }
            }
            else if (IsOnBorder(pp.X, ToAdjustRect.Right, outMaxOffset, inMaxOffsetX, RectLocation.Right))
            {
                if (IsOnBorder(pp.Y, ToAdjustRect.Top, outMaxOffset, inMaxOffsetY, RectLocation.Top))
                {
                    return PointerLocation.OnRightTopBorder;
                }
                else if (IsOnBorder(pp.Y, ToAdjustRect.Bottom, outMaxOffset, inMaxOffsetY, RectLocation.Bottom))
                {
                    return PointerLocation.OnRightBottomBorder;
                }
                else
                {
                    return PointerLocation.OnRightBorder;
                }
            }
            else
            {
                if (IsOnBorder(pp.Y, ToAdjustRect.Top, outMaxOffset, inMaxOffsetY, RectLocation.Top))
                {
                    return PointerLocation.OnTopBorder;
                }
                else if (IsOnBorder(pp.Y, ToAdjustRect.Bottom, outMaxOffset, inMaxOffsetY, RectLocation.Bottom))
                {
                    return PointerLocation.OnBottomBorder;
                }
                return PointerLocation.OnElementBody;
            }

        }

        void CursorAdjustRect(Point pp)
        {
            double deltaX = pp.X - LastPointerPnt.X;
            double deltaY = pp.Y - LastPointerPnt.Y;

            ChangeRect.Reset();
            switch (LastLocation)
            {
                case PointerLocation.InsideImageButNotElement:
                    break;
                case PointerLocation.OnTopBorder:
                    ChangeRect.top = deltaY;
                    break;
                case PointerLocation.OnBottomBorder:
                    ChangeRect.bottom = deltaY;
                    break;
                case PointerLocation.OnLeftBorder:
                    ChangeRect.left = deltaX;
                    break;
                case PointerLocation.OnRightBorder:
                    ChangeRect.right = deltaX;
                    break;
                case PointerLocation.OnLeftTopBorder:
                    ChangeRect.left = deltaX;
                    ChangeRect.top = deltaY;
                    break;
                case PointerLocation.OnRightBottomBorder:
                    ChangeRect.right = deltaX;
                    ChangeRect.bottom = deltaY;
                    break;
                case PointerLocation.OnLeftBottomBorder:
                    ChangeRect.left = deltaX;
                    ChangeRect.bottom = deltaY;
                    break;
                case PointerLocation.OnRightTopBorder:
                    ChangeRect.right = deltaX;
                    ChangeRect.top = deltaY;
                    break;
                case PointerLocation.OnElementBody:
                    ChangeRect.right = deltaX;
                    ChangeRect.top = deltaY;
                    ChangeRect.left = deltaX;
                    ChangeRect.bottom = deltaY;
                    break;
                case PointerLocation.InsideImage:
                    ToAdjustRect = new Rect(LastPointerPnt, pp);
                    break;
                case PointerLocation.OutsideImage:
                default:
                    return;
            }
            Refresh(CtrlMessageType.RedrawRequest);
        }


        private void UpdatePointerCursor(PointerLocation pl)
        {
            switch (pl)
            {
                case PointerLocation.InsideImage:
                case PointerLocation.InsideImageButNotElement:
                    ChangePointerCursor(CoreCursorType.Arrow);
                    break;
                case PointerLocation.OnTopBorder:
                case PointerLocation.OnBottomBorder:
                    ChangePointerCursor(CoreCursorType.SizeNorthSouth);
                    break;
                case PointerLocation.OnLeftBorder:
                case PointerLocation.OnRightBorder:
                    ChangePointerCursor(CoreCursorType.SizeWestEast);
                    break;
                case PointerLocation.OnLeftTopBorder:
                case PointerLocation.OnRightBottomBorder:
                    ChangePointerCursor(CoreCursorType.SizeNorthwestSoutheast);
                    break;
                case PointerLocation.OnLeftBottomBorder:
                case PointerLocation.OnRightTopBorder:
                    ChangePointerCursor(CoreCursorType.SizeNortheastSouthwest);
                    break;
                case PointerLocation.OnElementBody:
                    ChangePointerCursor(CoreCursorType.SizeAll);
                    break;
                case PointerLocation.OutsideImage:
                default:
                    ChangePointerCursor(CoreCursorType.Arrow);
                    break;
            }
        }
        private void GetCursorOnXcElement(Point pp)
        {
            lock (BtGrids.XingcaoElements)
            {
                pp.X += BtImageAdjustRect.X;
                pp.Y += BtImageAdjustRect.Y;
                foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
                {
                    if (IsPntInRect(pp, pair.Value.rc, -5))
                    {
                        if (CurrentElements.SelectedIndex != pair.Key)
                            CurrentElements.SelectedIndex = pair.Key;
                        break;
                    }
                }
            }
        }
        private void GetCursorOnElement(Point pp)
        {
            lock (BtGrids.ElementRects)
            {
                bool IsSingle = (OpType == OperationType.SingleElement);
                pp.X += BtImageAdjustRect.X;
                pp.Y += BtImageAdjustRect.Y;
                int count = BtGrids.ElementRects.Count;

                foreach (IntIndex ii in DrawLineElements)
                {
                    BeitieGridRect bgr = BtGrids.GetElement(ii.row, ii.col);
                    if (IsPntInRect(pp, bgr.rc, -5))
                    {
                        int index = BtGrids.GetIndex(ii.row, ii.col, BtGrids.BookOldType);

                        if (CurrentElements.SelectedIndex != index)
                            CurrentElements.SelectedIndex = index;
                        break;
                    }
                }
                

            }
        }
        
        private void ShowElementMenu(Point pp)
        {
            MenuFlyout myFlyout = new MenuFlyout();


            MenuFlyoutItem resetElem = new MenuFlyoutItem { Text = "重置元素" };
            MenuFlyoutItem saveElem = new MenuFlyoutItem { Text = "保存元素" };

            resetElem.Click += ElementMenuResetElement_Click;
            saveElem.Click += ElementMenuSaveElement_Click;

            if (!BtGrids.XingcaoMode)
            {
                int selectedElemIndex = CurrentElements.SelectedIndex + 1;
                MenuFlyoutItem setAsKb = new MenuFlyoutItem { Text = "设为空白元素" };
                MenuFlyoutItem setAsYz = new MenuFlyoutItem { Text = "设为印章元素" };
                MenuFlyoutItem setAsZi = new MenuFlyoutItem { Text = "设为字元素" };
                MenuFlyoutItem setAsQuezi = new MenuFlyoutItem { Text = "设为阙字元素" };
                MenuFlyoutItem AdjustElem = new MenuFlyoutItem { Text = "调整元素" + selectedElemIndex };
                AdjustElem.Click += ElementMenuAdjusttElement_Click;
                setAsKb.Click += ElementMenuSetAs_Click;
                setAsYz.Click += ElementMenuSetAs_Click;
                setAsZi.Click += ElementMenuSetAs_Click;
                setAsQuezi.Click += ElementMenuSetAs_Click;
                myFlyout.Items.Add(AdjustElem);
                myFlyout.Items.Add(setAsKb);
                myFlyout.Items.Add(setAsYz);
                myFlyout.Items.Add(setAsQuezi);
                myFlyout.Items.Add(setAsZi);

            }
            myFlyout.Items.Add(resetElem);
            myFlyout.Items.Add(saveElem);

            //if you only want to show in left or buttom 

            //myFlyout.Placement = FlyoutPlacementMode.Left;

            //the code can show the flyout in your mouse click 
            myFlyout.ShowAt(CurrentItem as UIElement, pp);
        }

        private bool IsOnElement(PointerLocation loc)
        {
            switch (loc)
            {
                case PointerLocation.OnElementBody:
                case PointerLocation.OnLeftBorder:
                case PointerLocation.OnLeftBottomBorder:
                case PointerLocation.OnRightBorder:
                case PointerLocation.OnRightBottomBorder:
                case PointerLocation.OnTopBorder:
                case PointerLocation.OnLeftTopBorder:
                case PointerLocation.OnRightTopBorder:
                case PointerLocation.OnBottomBorder:
                    return true;
            }
            return false;
        }

        private void UpdatePointer(Point pp, PointerStatus status)
        {
            LastPntrStatus = CurrentPntrStatus;
            LastPointerPnt = CurrentPointerPnt;
            LastLocation = CurrentLocation;

            PointerLocation loc = GetPointerLocation(pp);
            //TracePointerLocation(pp, status);
          
            bool NeedRedraw = false;
            switch (status)
            {
                case PointerStatus.Entered:
                    {
                        //Random ran = new Random();
                        //int n = ran.Next(1, (int)CoreCursorType.Person);
                        //ChangePointerCursor((CoreCursorType)n);
                    }
                    break;
                case PointerStatus.RBtnPressed:
                    if (IsOnElement(loc))
                    {
                        if (!BtGrids.XingcaoMode)
                        {
                            GetCursorOnElement(pp);
                        }
                        ShowElementMenu(pp);
                    }
                    break;
                case PointerStatus.Pressed:
                    if (loc == PointerLocation.OnElementBody)
                    {
                        // 禁用楷书整体的拖拽移动
                        if ((OpType == OperationType.WholePage) &&
                              !BtGrids.XingcaoMode)
                        {
                            GetCursorOnElement(pp);
                            ShowElementMenu(pp);
                            loc = PointerLocation.InsideImageButNotElement;
                            status = PointerStatus.Entered;
                        }
                        else
                        {
                            LastChangeRect.Reset();
                            UpdateFixedChecks(false, false);
                            status = PointerStatus.PressedToDrag;
                        }
                    }
                    else if (loc == PointerLocation.InsideImage)
                    {
                        LastChangeRect.Reset();
                        status = PointerStatus.MoveToCapture;
                    }
                    else if (loc == PointerLocation.InsideImageButNotElement)
                    {
                        if (BtGrids.XingcaoMode &&
                            (OpType != OperationType.SingleElement))
                        {
                            XingcaoMoveToNextElement();
                        }
                        else if (!BtGrids.XingcaoMode &&
                            (OpType == OperationType.SingleElement))
                        {
                            GetCursorOnElement(pp);
                        }
                        status = PointerStatus.Entered;
                    }
                    else if ((loc != PointerLocation.OutsideImage) &&
                        (loc != PointerLocation.InsideImageButNotElement))
                    {
                        LastChangeRect.Reset();
                        UpdateFixedChecks(false, false);
                        status = PointerStatus.MoveToScalingBorder;
                    }
                    break;
                case PointerStatus.Released:
                    if ((LastPntrStatus == PointerStatus.PressedToDrag) ||
                        (LastPntrStatus == PointerStatus.MoveToScalingBorder) ||
                        (LastPntrStatus == PointerStatus.MoveToCapture))
                    {
                        status = PointerStatus.ReleasedToExit;
                        UpdateElementsRects();
                        Refresh();
                    }
                    else
                    {
                        NeedRedraw = true;
                    }
                    break;
                case PointerStatus.Moved:
                    if ((LastPntrStatus == PointerStatus.PressedToDrag) ||
                        (LastPntrStatus == PointerStatus.MoveToScalingBorder) ||
                        (LastPntrStatus == PointerStatus.MoveToCapture))
                    {
                        CursorAdjustRect(pp);
                        return;
                    }
                    else if (BtGrids.XingcaoMode)
                    {
                        if ((OpType != OperationType.SingleElement) &&
                            (!AdjustGridsSwitch.IsOn))
                        {
                            GetCursorOnXcElement(pp);
                        }
                        if (FirstShowBanner && IsPntInRect(pp, rcBanner, 1))
                        {
                            FirstShowBanner = false;
                        }
                    }
                    else
                    {
                        if (OpType != OperationType.SingleElement)
                        {
                            GetCursorOnElement(pp);
                        }
                    }
                    break;
                case PointerStatus.Exited:
                    if ((LastPntrStatus == PointerStatus.PressedToDrag) ||
                        (LastPntrStatus == PointerStatus.MoveToScalingBorder) ||
                        (LastPntrStatus == PointerStatus.MoveToCapture))
                    {
                        Refresh();
                    }
                    else
                    {
                        NeedRedraw = true;
                    }
                    loc = PointerLocation.OutsideImage;
                    break;
                default:
                    break;
            }
            if (!BtGrids.XingcaoMode && (OpType == OperationType.WholePage))
            {
                if ((loc == PointerLocation.OnElementBody))
                {
                    loc = PointerLocation.InsideImageButNotElement;
                }
            }
            if (loc != LastLocation)
            {
                UpdatePointerCursor(loc);
            }
            CurrentPntrStatus = status;
            CurrentLocation = loc;
            CurrentPointerPnt = pp;
            if (NeedRedraw)
            {
                Refresh(CtrlMessageType.RedrawRequest);
            }
        }

        private void TracePointerLocation(Point pp, PointerStatus status)
        {
            string info = string.Format("ToAdjustRect: {0:0},{1:0},{2:0},{3:0}, ", ToAdjustRect.Left, ToAdjustRect.Top,
                ToAdjustRect.Right, ToAdjustRect.Bottom);
            info += string.Format("PointerLocation: {0:0},{1:0}, -> {2}: {3} ", pp.X, pp.Y, status,
                GetPointerLocation(pp));
            info += string.Format("ChangeRect: {0:0},{1:0},{2:0},{3:0}",
                ChangeRect.left, ChangeRect.top, ChangeRect.right, ChangeRect.bottom);

            NotifyUser(info, NotifyType.StatusMessage);
        }

        private void PointerMovedCurrentItem(object sender, PointerRoutedEventArgs e)
        {
            PointerPoint ptrpnt = e.GetCurrentPoint((UIElement)sender);
            Point pp = new Point(ptrpnt.Position.X, ptrpnt.Position.Y);
            pp.X -= AdjustExtendSize;
            pp.Y -= AdjustExtendSize;

            UpdatePointer(pp, PointerStatus.Moved);
        }

        private void PointerPressedCurrentItem(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("SettingPage PointerPressed ");
            PointerPoint ptrpnt = e.GetCurrentPoint((UIElement)sender);
            Point pp = new Point(ptrpnt.Position.X, ptrpnt.Position.Y);
            pp.X -= AdjustExtendSize;
            pp.Y -= AdjustExtendSize;

            PointerStatus pstatus = PointerStatus.Pressed;
            if (ptrpnt.Properties.IsRightButtonPressed)
            {
                pstatus = PointerStatus.RBtnPressed;
            }

            UpdatePointer(pp, pstatus);
        }

        private void PointerEnteredCurrentItem(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("SettingPage PointerEntered ");
            PointerPoint ptrpnt = e.GetCurrentPoint((UIElement)sender);
            Point pp = new Point(ptrpnt.Position.X, ptrpnt.Position.Y);
            pp.X -= AdjustExtendSize;
            pp.Y -= AdjustExtendSize;

            UpdatePointer(pp, PointerStatus.Entered);
        }

        private void PointerExitedCurrentItem(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("SettingPage PointerExited ");
            PointerPoint ptrpnt = e.GetCurrentPoint((UIElement)sender);
            Point pp = new Point(ptrpnt.Position.X, ptrpnt.Position.Y);
            pp.X -= AdjustExtendSize;
            pp.Y -= AdjustExtendSize;

            UpdatePointer(pp, PointerStatus.Exited);
        }

        private void PointerReleasedCurrentItem(object sender, PointerRoutedEventArgs e)
        {
            Debug.WriteLine("SettingPage PointerReleased");
            PointerPoint ptrpnt = e.GetCurrentPoint((UIElement)sender);
            Point pp = new Point(ptrpnt.Position.X, ptrpnt.Position.Y);
            pp.X -= AdjustExtendSize;
            pp.Y -= AdjustExtendSize;

            UpdatePointer(pp, PointerStatus.Released);
        }

        private void GridCheck_Clicked(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void AdjustGrids_Toggled(object sender, RoutedEventArgs e)
        {
            if (AdjustGridsSwitch.IsOn)
            {
                ChkAvgCol.Visibility = Visibility.Visible;
                ChkHideGrid.IsChecked = false;
                OpObjectTitle.Text = "操作对象";
            }
            else
            {
                ChkAvgCol.Visibility = Visibility.Collapsed;
                OpObjectTitle.Text = "选取蓝本";
            }
            Refresh();
        }

        private void UpdatePointer(VirtualKey key, CoreAcceleratorKeyEventType type)
        {
            switch (LastLocation)
            {
                case PointerLocation.OutsideImage:
                case PointerLocation.InsideImageButNotElement:
                case PointerLocation.InsideImage:
                    return;
            }
            if (type == CoreAcceleratorKeyEventType.KeyDown)
            {
                if (key == VirtualKey.Up)
                {
                    CurrentPointerPnt.Y -= ChangeStep;
                }
                else if (key == VirtualKey.Down)
                {
                    CurrentPointerPnt.Y += ChangeStep;
                }
                else if (key == VirtualKey.Left)
                {
                    CurrentPointerPnt.X -= ChangeStep;
                }
                else if (key == VirtualKey.Right)
                {
                    CurrentPointerPnt.X += ChangeStep;
                }
                CursorAdjustRect(CurrentPointerPnt);
            }
            else if (type == CoreAcceleratorKeyEventType.KeyUp)
            {
                LastPointerPnt = CurrentPointerPnt;
                LastChangeRect.Reset();
                UpdateElementsRects();
                Refresh();
            }
        }
        private void Dispatcher_AcceleratorKeyActivated(CoreDispatcher sender, AcceleratorKeyEventArgs args)
        {
            Debug.WriteLine("KeyEvent: {0} {1}", args.VirtualKey, args.EventType);
            // 键盘和按钮都是辅助调节，没必要做那么复杂，比如两个键同时按下的检测
            switch (args.VirtualKey)
            {
                case VirtualKey.Up:
                case VirtualKey.Down:
                case VirtualKey.Left:
                case VirtualKey.Right:
                    UpdatePointer(args.VirtualKey, args.EventType);
                    break;
            }
        }

        private void SettingPage_LostFocus(object sender, RoutedEventArgs e)
        {

        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Debug.WriteLine("OnSizeChanged() called");
            Debug.WriteLine("New Size: {0}", e.NewSize);
        }
        private void ElementMenuAdjusttElement_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ElementMenuAdjusttElement_Click();");
            OpSingleElement.IsChecked = true;
        }
        private void ElementMenuResetElement_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ElementMenuResetElement_Click();");
            ResetCurrentElement();
            Refresh();
        }
        private void ElementMenuSaveElement_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ElementMenuSaveElement_Click();");
            int index = CurrentElements.SelectedIndex;

            HashSet<int> elementIndexes = new HashSet<int>();
            elementIndexes.Add(index);
            if (BtGrids.XingcaoMode)
            {
                BeitieGridRect bgr;
                BtGrids.XingcaoElements.TryGetValue(index, out bgr);
                if (bgr == null)
                {
                    Common.ShowMessageDlg("请先定义当前元素矩形!", null);
                    return;
                }
            }
            SaveSplittedImages(elementIndexes);
        }
        private void UpdateCurrentElementCombo()
        {
            int selected = CurrentElements.SelectedIndex;
            CurrentElements.Items.Clear();
            int index = 0;
            foreach (KeyValuePair<int, BeitieElement> pair in BtGrids.Elements)
            {
                CurrentElements.Items.Add(String.Format("{0}[{1}]", pair.Value.content, ++index));
            }
            CurrentElements.SelectedIndex = selected;
        }
        private void ElementMenuSetAs_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ElementMenuSetAs_Click();");
            var item = (MenuFlyoutItem)sender;
            string txt = item.Text;
            int index = CurrentElements.SelectedIndex;
            BeitieElement.BeitieElementType type = BeitieElement.BeitieElementType.Zi;

            if (txt.Contains("空白"))
            {
                type = BeitieElement.BeitieElementType.Kongbai;
            }
            else if (txt.Contains("印章"))
            {
                type = BeitieElement.BeitieElementType.Yinzhang;
            }
            else if (txt.Contains("阙字"))
            {
                type = BeitieElement.BeitieElementType.Quezi;
            }
            else if (txt.Contains("字"))
            {
                type = BeitieElement.BeitieElementType.Zi;
            }

            BtGrids.UpdateElement(index, type);
            UpdateCurrentElementCombo();
            Refresh();
        }

        private void Clicked_Zoom(object sender, RoutedEventArgs e)
        {
            var btn = (Button)sender;
            float factor = ItemScrollViewer.ZoomFactor;
            if (btn == BtnZoomIn)
            {
                factor += Common.ZOOM_FACTOR_SCALE;
            }
            else
            {
                factor -= Common.ZOOM_FACTOR_SCALE;
            }
#pragma warning disable CS0618 // Type or member is obsolete
            ItemScrollViewer.ZoomToFactor(factor);
#pragma warning restore CS0618 // Type or member is obsolete
        }

        private void ClickedToggleMenu(object sender, RoutedEventArgs e)
        {

            Splitter.IsPaneOpen = !Splitter.IsPaneOpen;
        }

        private async void LostFocusPen(object sender, RoutedEventArgs e)
        {
            bool bRet = await ParentPage.SetPenWidth(PenWidthCombo.Text);
            if (!bRet)
            {
                Common.ShowMessageDlg("无效宽度: " + PenWidthCombo.Text, null);
                PenWidthCombo.Text = string.Format("{0:F0}", BtGrids.PenWidth);
                await ParentPage.SetPenWidth(PenWidthCombo.Text);
            }
            Refresh(CtrlMessageType.RedrawRequest);
        }

        private async void SelectionChangedPen(object sender, SelectionChangedEventArgs e)
        {
            if (sender == PenColorCombo)
            {
                ParentPage.SetPenColor(ColorBoxSelectedItem.Text);
            }
            else if (sender == PenWidthCombo)
            {
                await ParentPage.SetPenWidth(PenWidthCombo.Text);
            }
            else if (sender == PenLineTypeCombo)
            {
                int selected = PenLineTypeCombo.SelectedIndex;
                if (selected >= 0)
                {
                    LineType = (PenLineType)selected;
                }
            }
            Refresh(CtrlMessageType.RedrawRequest);
        }

        private void ClickedSingleFocus(object sender, RoutedEventArgs e)
        {
            SingleFocusMode = ChkSingleFocus?.IsChecked ?? true;
            Refresh();
        }

        private void ClickedShowSize(object sender, RoutedEventArgs e)
        {
            ShowSizeMode = ChkShowSize?.IsChecked ?? false;
            Refresh(CtrlMessageType.RedrawRequest);
        }

        private void ClickedNoOpacity(object sender, RoutedEventArgs e)
        {
            NoOpacityMode = ChkNoOpacity?.IsChecked ?? false;
            Refresh(CtrlMessageType.RedrawRequest);
        }

        int RewardPageViewID = 0;
        bool RewardPageClosed = true;
        private async void ClickedRewardMe(object sender, RoutedEventArgs e)
        {
            if (!GlobalSettings.MultiWindowMode)
            {
                this.Frame.Navigate(typeof(RewardMePage), null, Common.GetNavTransInfo(Common.NavigationTransitionType.DrillIn));
                return;
            }

            var views = CoreApplication.Views;
            if (views.Count > 1)
            {
                if (!RewardPageClosed)
                {
                    await ApplicationViewSwitcher.SwitchAsync(RewardPageViewID);
                    return;
                }
            }
            RewardPageClosed = false;
            CoreApplicationView newView = CoreApplication.CreateNewView();
            int newViewId = 0;
            await newView.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Frame frame = new Frame();
                frame.Navigate(typeof(RewardMePage), this);

                Window.Current.Content = frame;
                // You have to activate the window in order to show it later.
                Window.Current.Activate();
                var newAppView = ApplicationView.GetForCurrentView();
                newAppView.Consolidated += ConsolidatedRewardMePage;
                newViewId = ApplicationView.GetForCurrentView().Id;
            });
            RewardPageViewID = newViewId;
            await ApplicationViewSwitcher.TryShowAsStandaloneAsync(newViewId, ViewSizePreference.Custom);

        }
        private void ConsolidatedRewardMePage(ApplicationView sender, ApplicationViewConsolidatedEventArgs args)
        {
            Debug.WriteLine("ConsolidatedRewardMePage()");
            RewardPageClosed = true; 
        }

        private void ClickHorizontalScroll(object sender, RoutedEventArgs e)
        {
            double offsetX = ItemScrollViewer.HorizontalOffset;
            double offsetY = ItemScrollViewer.VerticalOffset;

            double offsetStep = CurrentItem.Width / 20.0;

            if (offsetStep < 10)
            {
                offsetStep = 10;
            }

            if (sender == LeftScrollBtn)
            {
                offsetX -= offsetStep;
            }
            else
            {
                offsetX += offsetStep;
            }

            ItemScrollViewer.ChangeView(offsetX, offsetY, null);

        }
        private void HideViewerScrollBar(bool bHide)
        {
            if (bHide)
            {
                ItemScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
                ItemScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
            }
            else
            {
                ItemScrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
                ItemScrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            }
        }

        private void ClickedhideScrollBar(object sender, RoutedEventArgs e)
        {
            HideScrollBar = ChkHideScrollBar.IsChecked ?? false;
            HideViewerScrollBar(HideScrollBar);
        }
    }
}
