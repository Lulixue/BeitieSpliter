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

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class GridsConfig : Page
    {
        enum CtrlMessageType
        {
            RowChange,
            ColumnChange,
            OperationChange,
            AdjustChange,
            RotateChange,
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
        OperationType OpType = OperationType.WholePage;
        BeitieGrids BtGrids = null;
        BeitieGrids LastBtGrids = new BeitieGrids();
        BeitieImage BtImage = null;
        MainPage ParentPage = null;
        Rect BtImageShowRect = new Rect();
        Rect BtImageAdjustRect = new Rect();
        HashSet<Point> DrawLineElements = new HashSet<Point>();
        Rect ToAdjustRect = new Rect();
        bool AvgCol = true;
        bool AvgRow = true;
        bool FixedHeight = false;
        bool FixedWidth = false;
        ChangeStruct ChangeRect = new ChangeStruct();
        ChangeStruct LastChangeRect = new ChangeStruct();
        ChangeStruct LastValidChange = new ChangeStruct();
        static int MIN_ROW_COLUMN = 1;

        public GridsConfig()
        {
            this.InitializeComponent();

            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            // Set XAML element as a draggable region.
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
                int maxIndex = 1;
                foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
                {
                    if (pair.Key > maxIndex)
                    {
                        maxIndex = pair.Key;
                    }
                }
                if (maxIndex == BtGrids.ElementCount)
                {
                    maxIndex = 1;
                }
                else
                {
                    maxIndex++;
                }
                CurrentElements.SelectedIndex = maxIndex - 1;
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

            if (!CheckOutOfImage(pntLt, pntRb))
            {
                PrintRect("Invalid", new Rect(pntLt, pntRb));
                return false;
            }
            ToAdjustRect = new Rect(pntLt, pntRb);

            int colNumber = ColumnNumber.SelectedIndex + 1;
            int elemIndex = CurrentElements.SelectedIndex + 1;


            if (!BtGrids.XingcaoElements.ContainsKey(elemIndex))
            {
                if ((ToAdjustRect.Width < 5) ||
                    (ToAdjustRect.Height < 5))
                {
                    return false;
                }
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

                BeitieGridRect bgr = new BeitieGridRect(new Rect(lt, rb))
                {
                    col = colNumber
                };
                BtGrids.XingcaoElements.Add(elemIndex, bgr);
            }
            else
            {

                BtGrids.XingcaoElements[elemIndex].col = colNumber;
                BtGrids.XingcaoElements[elemIndex].rc = new Rect(pntLt, pntRb);
               
            }

            return true;
        }

        bool CheckOutOfImage(Point pntLt, Point pntRb)
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

        bool UpdateElementsRects()
        {
            if ((BtGrids.XingcaoMode) && (!AdjustGridsSwitch.IsOn))
            {
                return UpdateXingcaoElementsRects();
            }


            ChangeStruct offset = new ChangeStruct();
            offset.Copy(ChangeRect);
            offset.left -= LastChangeRect.left;
            offset.right -= LastChangeRect.right;
            offset.top -= LastChangeRect.top;
            offset.bottom -= LastChangeRect.bottom;
            RectChangeType chgType = GetLastChangeType(offset);

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
 
            foreach (Point pnt in DrawLineElements)
            {
                int index = BtGrids.ToIndex((int)pnt.X, (int)pnt.Y);
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
                    if (pnt.X == MIN_ROW_COLUMN)
                    {
                        pntLt.Y += offset.top;
                    }
                    // 每列最后一个元素
                    else if (pnt.X == BtGrids.Rows)
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
                    if (pnt.Y == MIN_ROW_COLUMN)
                    {
                        pntLt.X += offset.left;
                    }
                    // 每行最后一个元素
                    else if (pnt.Y == BtGrids.Columns)
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
                    if (pnt.X == MIN_ROW_COLUMN)
                    {
                        pntLt.Y += offset.top;
                        revised = false;
                    }
                    if (pnt.X == BtGrids.Rows)
                    {
                        pntRb.Y += offset.bottom;
                        revised = false;
                    }
                    if (pnt.Y == MIN_ROW_COLUMN)
                    {
                        pntLt.X += offset.left;
                        revised = false;
                    }
                    if (pnt.Y == BtGrids.Columns)
                    {
                        pntRb.X += offset.right;
                        revised = false;
                    }
                }
                else
                {

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
                    if (!CheckOutOfImage(pntLt, pntRb))
                    { 
                        PrintRect("[" + (int)pnt.X + "," + (int)pnt.Y + "] Invalid", new Rect(pntLt, pntRb));
                        return false;
                    }


                    PrintRect("[" + (int)pnt.X + "," + (int)pnt.Y + "] Before", dstRc);
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
            double minDeltaY = 10000;
            int retIndex = 1;
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
                    
                    if (deltaY < minDeltaY)
                    {
                        retIndex = pair.Key;
                        minDeltaY = deltaY;
                    }
                }
            }

            return retIndex;
        }

        private void CalculateXingcaoDrawRect(CtrlMessageType type)
        {
            Debug.WriteLine("CalculateXingcaoDrawRect() called!");

            Point pntLt = new Point();
            Point pntRb = new Point();
            int SelectedCol = ColumnNumber.SelectedIndex + 1;
            int SelectedElementNo = CurrentElements.SelectedIndex + 1;
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
                        DrawLineElements.Add(new Point(row, col));
                    }
                }
            }
            
            CurrentItem.Height = (BtImageAdjustRect.Height + 2 * AdjustExtendSize);
            CurrentItem.Width = BtImageAdjustRect.Width + 2 * AdjustExtendSize;

            pntLt.X = AdjustExtendSize; // 0
            pntLt.Y = AdjustExtendSize; // 0
            pntRb.X = BtImageAdjustRect.Width + AdjustExtendSize;
            pntRb.Y = BtImageAdjustRect.Height + AdjustExtendSize;


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
                int elemIndex = CurrentElements.SelectedIndex + 1;

                if ((CtrlMessageType.ColumnChange == type) &&
                    (CtrlMessageType.OperationChange == type))
                {
                    int firstIndex = GetFirstXcElementInRect(colNumber);
                    if (firstIndex != elemIndex)
                    {
                        CurrentElements.SelectedIndex = firstIndex - 1;
                    }
                    Debug.WriteLine("Index first: {0}, selected: {1}", firstIndex, elemIndex);
                }

                PntrTargetType = PointerTargetType.ElementRect;
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

            ChangeRect = new ChangeStruct();
        
            Debug.WriteLine("Show Area Rect: ({1:0},{2:0},{3:0},{4:0})", 0,
               BtImageAdjustRect.X, BtImageAdjustRect.Y, BtImageAdjustRect.Width, BtImageAdjustRect.Height);
            Debug.WriteLine("Image Size: ({0}*{1}), Show Rect:({4:0},{5:0},{6:0},{7:0})",
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

                minCol = maxCol = SelectedCol;
                minRow = maxRow = SelectedRow;
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
            
            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = minCol; col <= maxCol; col++)
                {
                    DrawLineElements.Add(new Point(row, col));
                }
            }

            Rect rcStart = BtGrids.GetRectangle(minRow, minCol);
            Rect rcEnd = BtGrids.GetRectangle(maxRow, maxCol);

            ToAdjustRect.X = rcStart.X - BtImageAdjustRect.X;
            ToAdjustRect.Y = rcStart.Y - BtImageAdjustRect.Y;
            ToAdjustRect.Width = rcEnd.X - rcStart.X + rcEnd.Width;
            ToAdjustRect.Height = rcEnd.Y - rcStart.Y + rcEnd.Height;
            ChangeRect = new ChangeStruct();


            Debug.WriteLine("Operation: {0}, Previous: {1},{2}; Current: {3},{4}; Next: {5},{6}",
               OpType, PreRow, PreCol, SelectedRow, SelectedCol, NextRow, NextCol);
            Debug.WriteLine("Show Area Rect: ({1:0},{2:0},{3:0},{4:0})", 0,
                BtImageAdjustRect.X, BtImageAdjustRect.Y, BtImageAdjustRect.Width, BtImageAdjustRect.Height);
            Debug.WriteLine("Image Size: ({0}*{1}), Show Rect:({4:0},{5:0},{6:0},{7:0})",
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
            int row = 4;
            int col = 4;
            int index = BtGrids.GetIndex(4, 4, true);
            Debug.Assert(BtGrids.GetIndex(1, 1, true) == 45);
            Debug.Assert(BtGrids.GetIndex(1, 6, true) == 0);
            BtGrids.IndexToRowCol(index, ref row,  ref col, true);
            Debug.Assert(row == 4);
            Debug.Assert(col == 4);
            Debug.WriteLine("float: {0},{0:F1}", 1.24F, 1.24F);
        }
        void InitControls()
        {
            int index = 0;
            foreach (BeitieElement elem in BtGrids.Elements)
            {
                CurrentElements.Items.Add(String.Format("{0}[{1}]", elem.content, ++index));
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
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            Debug.WriteLine("OnNavigatedTo() called");
            ParentPage = (MainPage)e.Parameter;
            BtGrids = ParentPage.BtGrids;
            // 备份上一次的数据
            LastBtGrids.Columns = BtGrids.Columns;
            LastBtGrids.Rows = BtGrids.Rows;
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

            //BtImage = new BeitieImage(CurrentItem, BtGrids.ImageFile);
            BtImage = BtGrids.BtImageParent;
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
                OpObjectTitle.Text = "选取蓝本：";

                AdjustGridsSwitch.Visibility = Visibility.Visible;
                ChkHideGrid.Visibility = Visibility.Visible;

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
        private void DrawRectangle(CanvasDrawingSession draw, Rect rect, Color color, float strokeWidth)
        {
            //Debug.WriteLine("Draw Rectangle: ({0:0},{1:0},{2:0},{3:0}), Color: {4}, Width: {5}", rect.X, rect.Y, rect.Width, rect.Height,
            //    color, strokeWidth);
            
            draw.DrawRectangle((float)(rect.Left + AdjustExtendSize), (float)(rect.Top + AdjustExtendSize), 
                (float)rect.Width, (float)rect.Height, color, strokeWidth);
        }

        private void DrawLines(CanvasDrawingSession draw)
        {
            Point pntLt, pntRb;
            GetRectPoints(ToAdjustRect, ref pntLt, ref pntRb);
            Color BaseColor = (BtGrids.BackupColors.Count > 0) ? BtGrids.BackupColors.ElementAt(0) : Colors.Green;
            float penWidth = BtGrids.PenWidth;
            Color drawColor = BtGrids.PenColor;
            pntLt.X += ChangeRect.left;
            pntLt.Y += ChangeRect.top;
            pntRb.X += ChangeRect.right;
            pntRb.Y += ChangeRect.bottom;
            
            Rect drawRect = new Rect(pntLt, pntRb);
            
            //switch (CurrentLocation)
            //{
            //    case PointerLocation.InsideImageButNotElement:
            //    case PointerLocation.OutsideImage:
            //        break;
            //    default:
            //        penWidth = BtGrids.PenWidth + 3;
            //        if (BtGrids.GridType == BeitieGrids.ColorType.Dark)
            //        {
            //            BaseColor = Colors.Red;
            //        }
            //        else
            //        {
            //            BaseColor = Colors.SeaShell;
            //        }
            //        break;
            //}
            DrawRectangle(draw, drawRect, BaseColor, penWidth);

            lock (DrawLineElements)
            {
                foreach (Point elem in DrawLineElements)
                {
                    Rect rc = BtGrids.GetRectangle((int)elem.X, (int)elem.Y);
                    rc.X -= BtImageAdjustRect.X;
                    rc.Y -= BtImageAdjustRect.Y;
                    drawColor = BtGrids.PenColor;
                    if (BtGrids.GetRevised((int)elem.X, (int)elem.Y))
                    {
                        if (DrawLineElements.Count == 1)
                        {
                            //if (BtGrids.BackupColors.Count > 1)
                            //{
                            //    drawColor = BtGrids.BackupColors.ElementAt(1);
                            //}
                        }
                        else
                        {
                            if (BtGrids.BackupColors.Count > 0)
                            {
                                Random ran = new Random();
                                int n = ran.Next(1, BtGrids.BackupColors.Count);
                                drawColor = BtGrids.BackupColors.ElementAt(n);
                            }
                        }
                    }
                    DrawRectangle(draw, rc, drawColor, penWidth);
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

                        if (pair.Key == (CurrentElements.SelectedIndex + 1))
                        {
                            currentElemIndex = pair.Key;
                           
                        }
                        else
                        {
                            DrawRectangle(draw, rc, drawColor, penWidth);
                        }
                    }
                    // 最后绘制所选中元素，达到覆盖其他的目的
                    if (currentElemIndex > 0)
                    {
                        var result = BtGrids.XingcaoElements.Where(p => (p.Key == currentElemIndex));
                        foreach (KeyValuePair<int, BeitieGridRect> pair in result)
                        {
                            Rect rc = pair.Value.rc;
                            rc.X -= BtImageAdjustRect.X;
                            rc.Y -= BtImageAdjustRect.Y;

                            DrawRectangle(draw, rc, Colors.Red, penWidth + 2);

                            BeitieElement be = BtGrids.Elements[pair.Key-1];
                            string name = string.Format("元素[{0}]:{1}", pair.Key, be.content);
                            Rect txtRc = new Rect()
                            {
                                X = rc.Left
                            };
                        
                            if (rc.Bottom < (0.9 * BtImageAdjustRect.Height))
                            {
                                txtRc.Y = rc.Bottom + 1;
                            }
                            else
                            {
                                txtRc.Y = rc.Top - 21;
                            }
                            txtRc.Height = 20;
                            txtRc.Width = rc.Width;
                            // 在下方/上方显示当前元素名称
                            CanvasTextFormat fmt = new CanvasTextFormat();
                            fmt.FontSize = 10;
                            fmt.HorizontalAlignment = CanvasHorizontalAlignment.Center;
                            fmt.VerticalAlignment = CanvasVerticalAlignment.Center;
                            draw.FillRectangle(txtRc, Colors.Red);
                            draw.DrawText(name, txtRc, Colors.White, fmt);
                        }
                    }
                    

                }
            }
        }
        private void CurrentItem_OnDraw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
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
                // image 1000 * 1568
                // 
                //draw.DrawImage(BtImage.cvsBmp, (float)BtImageShowRect.X, (float)BtImageShowRect.Y);
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
                Refresh(CtrlMessageType.AdjustChange);
            }
            else
            {
                UpdateAdjustStatus();
            }
        }

        void UpdateAdjustStatus()
        {
            string info = "";
            info += string.Format("当前修改元素: {0}个, ", DrawLineElements.Count);
            info += string.Format("当前矩形尺寸： {0:0}*{1:0}, ", ToAdjustRect.Width, ToAdjustRect.Height);
            info += string.Format("当前矩形改变量: {0:0},{1:0},{2:0},{3:0}, ", ChangeRect.left, ChangeRect.top, ChangeRect.right, ChangeRect.bottom);
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
                BtGrids.angle += 0.3F;
            }
            else if (sender == BtnRotateRight)
            {
                BtGrids.angle -= 0.3F;
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

        private void BtnSave_Clicked(object sender, RoutedEventArgs e)
        {
            if (sender == BtnSaveAll)
            {
                ParentPage.HandlerSaveSplittedImages(null);
            }
            else
            {
                HashSet<int> elementIndexes = new HashSet<int>();
                if (!BtGrids.XingcaoMode)
                {
                    foreach (Point pnt in DrawLineElements)
                    {
                        elementIndexes.Add(BtGrids.GetIndex((int)pnt.X, (int)pnt.Y, true));
                    }
                }
                else
                {
                    if (OperationType.SingleElement == OpType)
                    {
                        elementIndexes.Add(CurrentElements.SelectedIndex + 1);
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
               
                ParentPage.HandlerSaveSplittedImages(elementIndexes);
            }
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

            }
            else if (sender == BtnResetElement)
            {
                int row = 1, col = 1;
                GetCurrentRowCol(ref row, ref col);
                BtGrids.ElementRects[BtGrids.ToIndex(row, col)] = new BeitieGridRect(LastBtGrids.GetElement(row, col).rc);

                int selectedElemIndex = CurrentElements.SelectedIndex + 1;
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
                Refresh(CtrlMessageType.AdjustChange);
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
            OnLeftBoarder,
            OnRightBoarder,
            OnTopBoarder,
            OnBottomBoarder,
            OnLeftTopBoarder,
            OnLeftBottomBoarder,
            OnRightTopBoarder,
            OnRightBottomBoarder,
            OnElementBody,
            InsideImage,
        }
        enum PointerStatus
        {
            Entered,
            Moved,
            Pressed,
            Released,
            Exited,
            PressedToDrag,
            ReleasedToExit,
            MoveOnBoarder,
            MoveToScalingBoarder,
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

        bool IsOnBoarder(double current, double baseline, double outMaxOffset, double inMaxOffset, RectLocation rl)
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
        private PointerLocation GetPointerLocation(Point pp)
        {
            double outMaxOffset = BtGrids.XingcaoMode ? 1 : AdjustDelta;
            double inMaxOffsetX = ToAdjustRect.Width * IN_OFFSET_PROP;
            double inMaxOffsetY = ToAdjustRect.Height * IN_OFFSET_PROP;

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

            if (IsOnBoarder(pp.X, ToAdjustRect.Left, outMaxOffset, inMaxOffsetX, RectLocation.Left))
            {
                if (IsOnBoarder(pp.Y, ToAdjustRect.Top, outMaxOffset, inMaxOffsetY, RectLocation.Top))
                {
                    return PointerLocation.OnLeftTopBoarder;
                }
                else if (IsOnBoarder(pp.Y, ToAdjustRect.Bottom, outMaxOffset, inMaxOffsetY, RectLocation.Bottom))
                {
                    return PointerLocation.OnLeftBottomBoarder;
                }
                else
                {
                    return PointerLocation.OnLeftBoarder;
                }
            }
            else if (IsOnBoarder(pp.X, ToAdjustRect.Right, outMaxOffset, inMaxOffsetX, RectLocation.Right))
            {
                if (IsOnBoarder(pp.Y, ToAdjustRect.Top, outMaxOffset, inMaxOffsetY, RectLocation.Top))
                {
                    return PointerLocation.OnRightTopBoarder;
                }
                else if (IsOnBoarder(pp.Y, ToAdjustRect.Bottom, outMaxOffset, inMaxOffsetY, RectLocation.Bottom))
                {
                    return PointerLocation.OnRightBottomBoarder;
                }
                else
                {
                    return PointerLocation.OnRightBoarder;
                }
            }
            else
            {
                if (IsOnBoarder(pp.Y, ToAdjustRect.Top, outMaxOffset, inMaxOffsetY, RectLocation.Top))
                {
                    return PointerLocation.OnTopBoarder;
                }
                else if (IsOnBoarder(pp.Y, ToAdjustRect.Bottom, outMaxOffset, inMaxOffsetY, RectLocation.Bottom))
                {
                    return PointerLocation.OnBottomBoarder;
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
                case PointerLocation.OnTopBoarder:
                    ChangeRect.top = deltaY;
                    break;
                case PointerLocation.OnBottomBoarder:
                    ChangeRect.bottom = deltaY;
                    break;
                case PointerLocation.OnLeftBoarder:
                    ChangeRect.left = deltaX;
                    break;
                case PointerLocation.OnRightBoarder:
                    ChangeRect.right = deltaX;
                    break;
                case PointerLocation.OnLeftTopBoarder:
                    ChangeRect.left = deltaX;
                    ChangeRect.top = deltaY;
                    break;
                case PointerLocation.OnRightBottomBoarder:
                    ChangeRect.right = deltaX;
                    ChangeRect.bottom = deltaY;
                    break;
                case PointerLocation.OnLeftBottomBoarder:
                    ChangeRect.left = deltaX;
                    ChangeRect.bottom = deltaY;
                    break;
                case PointerLocation.OnRightTopBoarder:
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
            Refresh(CtrlMessageType.AdjustChange, false);
        }


        private void UpdatePointerCursor(PointerLocation pl)
        {
            switch (pl)
            {
                case PointerLocation.InsideImage:
                case PointerLocation.InsideImageButNotElement:
                    ChangePointerCursor(CoreCursorType.Arrow);
                    break;
                case PointerLocation.OnTopBoarder:
                case PointerLocation.OnBottomBoarder:
                    ChangePointerCursor(CoreCursorType.SizeNorthSouth);
                    break;
                case PointerLocation.OnLeftBoarder:
                case PointerLocation.OnRightBoarder:
                    ChangePointerCursor(CoreCursorType.SizeWestEast);
                    break;
                case PointerLocation.OnLeftTopBoarder:
                case PointerLocation.OnRightBottomBoarder:
                    ChangePointerCursor(CoreCursorType.SizeNorthwestSoutheast);
                    break;
                case PointerLocation.OnLeftBottomBoarder:
                case PointerLocation.OnRightTopBoarder:
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
                foreach (KeyValuePair<int, BeitieGridRect> pair in BtGrids.XingcaoElements)
                {
                    if (IsPntInRect(pp, pair.Value.rc, 1))
                    {
                        if (CurrentElements.SelectedIndex != (pair.Key - 1))
                            CurrentElements.SelectedIndex = pair.Key - 1;
                        break;
                    }
                }
            }
        }

        private void UpdatePointer(Point pp, PointerStatus status)
        {
            LastPntrStatus = CurrentPntrStatus;
            LastPointerPnt = CurrentPointerPnt;
            LastLocation = CurrentLocation;

            PointerLocation loc = GetPointerLocation(pp);
            TracePointerLocation(pp, status);
            //NotifyUser(string.Format("Previous Status: {0}, Next: {1}", LastPntrStatus, status), NotifyType.StatusMessage);

            switch (status)
            {
                case PointerStatus.Entered:
                    {
                        //Random ran = new Random();
                        //int n = ran.Next(1, (int)CoreCursorType.Person);
                        //ChangePointerCursor((CoreCursorType)n);
                    }
                    break;
                case PointerStatus.Pressed:
                    if (loc == PointerLocation.OnElementBody)
                    {
                        LastChangeRect.Reset();
                        UpdateFixedChecks(false, false);
                        status = PointerStatus.PressedToDrag;
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
                        status = PointerStatus.Entered;
                    }
                    else if ((loc != PointerLocation.OutsideImage) &&
                        (loc != PointerLocation.InsideImageButNotElement))
                    {
                        LastChangeRect.Reset();
                        UpdateFixedChecks(false, false);
                        status = PointerStatus.MoveToScalingBoarder;
                    }
                    break;
                case PointerStatus.Released:
                    if ((LastPntrStatus == PointerStatus.PressedToDrag) ||
                        (LastPntrStatus == PointerStatus.MoveToScalingBoarder) ||
                        (LastPntrStatus == PointerStatus.MoveToCapture))
                    {
                        status = PointerStatus.ReleasedToExit;
                        UpdateElementsRects();
                        Refresh();
                    }
                    break;
                case PointerStatus.Moved:
                    if ((LastPntrStatus == PointerStatus.PressedToDrag) ||
                        (LastPntrStatus == PointerStatus.MoveToScalingBoarder) ||
                        (LastPntrStatus == PointerStatus.MoveToCapture))
                    {
                        CursorAdjustRect(pp);
                        return;
                    }
                    else if (BtGrids.XingcaoMode)
                    {
                        if (OpType != OperationType.SingleElement)
                        {
                            GetCursorOnXcElement(pp);
                        }
                    }
                    break;
                case PointerStatus.Exited:
                    if ((LastPntrStatus == PointerStatus.PressedToDrag) ||
                        (LastPntrStatus == PointerStatus.MoveToScalingBoarder) ||
                        (LastPntrStatus == PointerStatus.MoveToCapture))
                    {
                        Refresh();
                    }
                    loc = PointerLocation.OutsideImage;
                    break;
                default:
                    break;
            }
            if (loc != LastLocation)
            {
                UpdatePointerCursor(loc);
            }
            CurrentPntrStatus = status;
            CurrentLocation = loc;
            CurrentPointerPnt = pp;
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

            UpdatePointer(pp, PointerStatus.Pressed);
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
                OpObjectTitle.Text = "操作对象：";
            }
            else
            {
                ChkAvgCol.Visibility = Visibility.Collapsed;
                OpObjectTitle.Text = "选取蓝本：";
            }
            Refresh();
        }
    }
}
