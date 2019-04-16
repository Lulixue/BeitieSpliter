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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BeitieSpliter
{
    
    public class ChangeStruct
    {
        public double left = 0;
        public double right = 0;
        public double top = 0;
        public double bottom = 0;

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
        enum OperationType
        {
            SingleElement,
            SingleRow,
            SingleColumn,
            WholePage,
        }
        OperationType OpType = OperationType.SingleElement;
        BeitieGrids BtGrids = null;
        BeitieGrids LastBtGrids = null;
        BeitieImage BtImage = null;
        MainPage ParentPage = null;
        Rect BtImageShowRect = new Rect();
        Rect BtImageAdjustRect = new Rect();
        HashSet<Point> DrawLineElements = new HashSet<Point>();
        Rect ToAdjustRect = new Rect();
        bool AvgCol = true;
        bool AvgRow = true;
        ChangeStruct ChangeRect = new ChangeStruct();
        ChangeStruct LastChangeRect = new ChangeStruct();
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
                rc.X, rc.Y, rc.Width, rc.Height);
        }
        void GetRectPoints(Rect rc, ref Point pntLt, ref Point pntRb)
        {
            pntLt = new Point(rc.X, rc.Y);
            pntRb = new Point(rc.X + rc.Width, rc.Y + rc.Height);
        }

        void UpdateElementsRects()
        {
            ChangeStruct offset = new ChangeStruct();
            offset.Copy(ChangeRect);
            offset.left -= LastChangeRect.left;
            offset.right -= LastChangeRect.right;
            offset.top -= LastChangeRect.top;
            offset.bottom -= LastChangeRect.bottom;

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
                    if (pnt.Y == MIN_ROW_COLUMN)
                    {
                        pntLt.Y += offset.top;
                    }
                    // 每列最后一个元素
                    else if (pnt.Y == BtGrids.Rows)
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
                        pntLt.Y += offset.left;
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
                    PrintRect("Before", dstRc);
                    BtGrids.ElementRects[index] = new BeitieGridRect(new Rect(pntLt, pntRb), dstRevised);
                    PrintRect("After", BtGrids.ElementRects[index].rc);
                }
            }
            if (OperationType.SingleElement == OpType)
            {
                return;
            }
            // 平均分布行和列
            if (AvgCol || AvgRow)
            {
                Point pntLt2, pntRb2;
                GetRectPoints(ToAdjustRect, ref pntLt2, ref pntRb2);
                pntLt2.X += ChangeRect.left;
                pntLt2.Y += ChangeRect.top;
                pntRb2.X += ChangeRect.right;
                pntRb2.Y += ChangeRect.bottom;

                double height = pntRb2.Y - pntLt2.Y;
                double width = pntRb2.X - pntLt2.X;


                Point PntLeftTop = new Point();
                int minCol = MIN_ROW_COLUMN;
                int minRow = MIN_ROW_COLUMN;
                int maxCol = BtGrids.Columns;
                int maxRow = BtGrids.Rows;
                if (AvgRow && ((OperationType.SingleColumn == OpType) || (OperationType.WholePage == OpType)))
                {
                    double AvgHeight = height / BtGrids.Rows;

                    if (OpType == OperationType.SingleColumn)
                    {
                        minCol = maxCol = ColumnNumber.SelectedIndex + 1;
                    }

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
                                PntLeftTop.X = dstRc.X;
                                PntLeftTop.Y = dstRc.Y;
                            }
                            else
                            {
                                PntLeftTop.Y += AvgHeight;
                                if (row == BtGrids.Rows)
                                {
                                    PntLeftTop.Y = (PntLeftTop.Y > dstRc.Y) ? dstRc.Y : PntLeftTop.Y;
                                }
                            }
                            // 已经调整过了就不再弄了
                            if (!revised)
                            {
                                PrintRect("(Avg Column)Before[" + row + "," + col + "]", dstRc);
                                BtGrids.ElementRects[index] = new BeitieGridRect(new Rect(PntLeftTop.X, PntLeftTop.Y, dstRc.Width, AvgHeight), false);
                                PrintRect("After", BtGrids.ElementRects[index].rc);
                                Debug.WriteLine("\r\n");
                            }
                        }
                       
                    }
                }
                if (AvgCol && ((OperationType.SingleRow == OpType) || (OperationType.WholePage == OpType)))
                {
                    double AvgWidth = width / BtGrids.Columns;

                    if (OpType == OperationType.SingleRow)
                    {
                        minRow = maxRow = RowNumber.SelectedIndex + 1;
                    }

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
                                PntLeftTop.Y = dstRc.Y;
                            }
                            else
                            {
                                PntLeftTop.X += AvgWidth;
                                if (col == BtGrids.Columns)
                                {
                                    PntLeftTop.X = (PntLeftTop.X > dstRc.X) ? dstRc.X : PntLeftTop.X;
                                }
                            }
                            // 已经调整过了就不再弄了
                            if (!revised)
                            {
                                PrintRect("(Avg Row) Before[" + row + "," + col + "]", dstRc);
                                BtGrids.ElementRects[index] = new BeitieGridRect(new Rect(PntLeftTop.X, PntLeftTop.Y, AvgWidth, dstRc.Height), false);
                                PrintRect("After", BtGrids.ElementRects[index].rc);
                                Debug.WriteLine("\r\n");
                            }
                        }
                    }
                }

            }
        }

        private Point GetDrawLineLtCoord(int row, int col)
        {
            int index = BtGrids.ToIndex(row, col);
            Rect dstRc = BtGrids.ElementRects[index].rc;

            return new Point(dstRc.X, dstRc.Y);
        }

        private void CalculateDrawRect()
        {
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

            Rect rcLt = BtGrids.GetRectangle(PreRow, PreCol);
            Rect rcRb = BtGrids.GetRectangle(NextRow, NextCol);

            pntLt.X = rcLt.Left;
            pntLt.Y = rcLt.Top;
            pntRb.X = rcRb.Right;
            pntRb.Y = rcRb.Bottom;

            BtImageAdjustRect = new Rect(pntLt, pntRb);

            pntLt.X = 0/* - BtImage.resolutionX*/;
            pntLt.Y = 0/* - BtImage.resolutionY*/;
            pntRb.X = BtImageAdjustRect.Width;
            pntRb.Y = BtImageAdjustRect.Height;

            BtImageShowRect = new Rect(pntLt, pntRb);
            Debug.WriteLine("Operation: {0}, Previous: {1},{2}; Current: {3},{4}; Next: {5},{6}",
                OpType, PreRow, PreCol, SelectedRow, SelectedCol, NextRow, NextCol);
            Debug.WriteLine("Show Area Rect: ({1:0},{2:0},{3:0},{4:0})",0,
                BtImageAdjustRect.X, BtImageAdjustRect.Y, BtImageAdjustRect.Width, BtImageAdjustRect.Height);
            Debug.WriteLine("Image Size: ({0}*{1}), Show Rect:({4:0},{5:0},{6:0},{7:0})",
                BtImage.resolutionX, BtImage.resolutionY, 0, 0,
                BtImageShowRect.X, BtImageShowRect.Y, BtImageShowRect.Width, BtImageShowRect.Height);
            
            CurrentItem.Height = BtImageAdjustRect.Height;
            CurrentItem.Width = BtImageAdjustRect.Width;
            
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

            Debug.WriteLine("To Adjust Rect: ({1:0},{2:0},{3:0},{4:0})", 0,
               ToAdjustRect.X, ToAdjustRect.Y, ToAdjustRect.Width, ToAdjustRect.Height);
            ChangeRect = new ChangeStruct();

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
        private void Refresh(bool AdjustImage = false)
        {
            if (IsParaInvalid())
            {
                return;
            }
            if (!AdjustImage)
            {
                CalculateDrawRect();
            }
            CurrentItem.Invalidate();
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
            TestCase();
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
            LastBtGrids = (BeitieGrids)BtGrids.Clone();
            //BtImage = new BeitieImage(CurrentItem, BtGrids.ImageFile);
            BtImage = BtGrids.BtImageParent;
        }

        private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("SettingsPage_Loaded() called");

            IRandomAccessStream ir = await BtGrids.ImageFile.OpenAsync(FileAccessMode.Read);
            BitmapImage bi = new BitmapImage();
            await bi.SetSourceAsync(ir);

            ImageBrush imageBrush = new ImageBrush();
            imageBrush.ImageSource = bi;
            imageBrush.Opacity = 0.75;
            InitControls();
            CalculateDrawRect();
            UpdateAngle();
            Operation_Checked(null, null);
            //OperationGrid.MaxWidth = BtGrids.DrawWidth;
            //OperationGrid.MaxHeight = BtGrids.DrawHeight; 
            //OperationGrid.Background = imageBrush;
        }
        private Object SyncObj = new Object();
        private void UpdateElementRowCol(bool baseOnRowCol)
        {
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
                int col = ColumnNumber.SelectedIndex + 1;
                int row = RowNumber.SelectedIndex + 1;
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
            Debug.WriteLine("Draw Rectangle: ({0:0},{1:0},{2:0},{3:0}), Color: {4}, Width: {5}", rect.X, rect.Y, rect.Width, rect.Height,
                color, strokeWidth);
            draw.DrawRectangle(rect, color, strokeWidth);
        }
        private void DrawLines(CanvasDrawingSession draw)
        {
            Point pntLt, pntRb;
            GetRectPoints(ToAdjustRect, ref pntLt, ref pntRb);
            pntLt.X += ChangeRect.left;
            pntLt.Y += ChangeRect.top;
            pntRb.X += ChangeRect.right;
            pntRb.Y += ChangeRect.bottom;
            
            Rect drawRect = new Rect(pntLt, pntRb);
            DrawRectangle(draw, ToAdjustRect, Colors.Green , BtGrids.PenWidth);
            DrawRectangle(draw, drawRect, BtGrids.PenColor, BtGrids.PenWidth+1);
            
            foreach (Point elem in DrawLineElements)
            {
                Rect rc = BtGrids.GetRectangle((int)elem.X, (int)elem.Y);
                rc.X -= BtImageAdjustRect.X;
                rc.Y -= BtImageAdjustRect.Y;

                DrawRectangle(draw, rc, BtGrids.PenColor, BtGrids.PenWidth);
            }
        }

        private void CurrentItem_OnDraw(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
        {
            var draw = args.DrawingSession;

            draw.Clear(Colors.Gray);
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

        private void ColumnNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateElementRowCol(true);
            Refresh();
        }

        private void RowNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateElementRowCol(true);
            Refresh();
        }

        private void Operation_Checked(object sender, RoutedEventArgs e)
        {
            if (OpSingleElement?.IsChecked ?? false)
            {
                OpType = OperationType.SingleElement;
                if (BtnLeftElement != null)
                {
                    BtnLeftElement.Content = "左一字";
                    BtnRightElement.Content = "右一字";
                    BtnTopElement.Content = "上一字";
                    BtnBottomElement.Content = "下一字";
                    BtnLeftElement.IsEnabled = true;
                    BtnTopElement.IsEnabled = true;
                    BtnRightElement.IsEnabled = true;
                    BtnBottomElement.IsEnabled = true;
                }
            }
            else if (OpSingleRow?.IsChecked ?? false)
            {
                OpType = OperationType.SingleRow;
                
                if (BtnLeftElement != null)
                {
                    BtnTopElement.Content = "上一行";
                    BtnBottomElement.Content = "下一行";
                    BtnLeftElement.IsEnabled = false;
                    BtnRightElement.IsEnabled = false;
                    BtnTopElement.IsEnabled = true;
                    BtnBottomElement.IsEnabled = true;
                }
            }
            else if (OpSingleColumn?.IsChecked ?? false)
            {
                OpType = OperationType.SingleColumn;
                if (BtnLeftElement != null)
                {
                    BtnLeftElement.Content = "左一列";
                    BtnRightElement.Content = "右一列";
                    BtnLeftElement.IsEnabled = false;
                    BtnBottomElement.IsEnabled = false;
                    BtnTopElement.IsEnabled = true;
                    BtnRightElement.IsEnabled = true;
                }
            }
            else if (OpWholePage?.IsChecked ?? false)
            {
                OpType = OperationType.WholePage;
                if (BtnLeftElement != null)
                {
                    BtnLeftElement.IsEnabled = false;
                    BtnBottomElement.IsEnabled = false;
                    BtnTopElement.IsEnabled = false;
                    BtnRightElement.IsEnabled = false;
                }
            }
            Refresh();
        }


        private void Adjust_Clicked(object sender, RoutedEventArgs e)
        {
            LastChangeRect.Copy(ChangeRect);
            if (sender == BtnBottomAdd)
            {
                ChangeRect.bottom++;
            }
            else if (sender == BtnBottomMinus)
            {
                ChangeRect.bottom--;
            }
            else if (sender == BtnLeftAdd)
            {
                ChangeRect.left++;
            }
            else if (sender == BtnLeftMinus)
            {
                ChangeRect.left--;
            }
            else if (sender == BtnRightAdd)
            {
                ChangeRect.right++;
            }
            else if (sender == BtnRightMinus)
            {
                ChangeRect.right--;
            }
            else if (sender == BtnTopAdd)
            {
                ChangeRect.top++;
            }
            else if (sender == BtnTopMinus)
            {
                ChangeRect.top--;
            }
            UpdateElementsRects();
            Debug.WriteLine("Change: {0:0},{1:0},{2:0},{3:0}", ChangeRect.left, ChangeRect.top, ChangeRect.right, ChangeRect.bottom);
            Refresh(true);
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
                ColumnNumber.SelectedIndex = GetPreviousColRow(ColumnNumber.SelectedIndex, 0);
            }
            else if (sender == BtnRightElement)
            {
                ColumnNumber.SelectedIndex = GetNextColRow(ColumnNumber.SelectedIndex, BtGrids.Columns - 1);
            }
            else if (sender == BtnTopElement)
            {
                if (RowNumber.SelectedIndex == 0)
                {
                    if (ColumnNumber.SelectedIndex == (BtGrids.Columns - 1))
                    {
                        return;
                    }
                    ColumnNumber.SelectedIndex = GetNextColRow(ColumnNumber.SelectedIndex, BtGrids.Columns - 1);
                    ColumnNumber.SelectedIndex = GetPreviousColRow(ColumnNumber.SelectedIndex, 0);
                    RowNumber.SelectedIndex = BtGrids.Rows - 1;
                }
                else
                {
                    RowNumber.SelectedIndex = GetPreviousColRow(RowNumber.SelectedIndex, 0);
                }
            }
            else if (sender == BtnBottomElement)
            {
                if (RowNumber.SelectedIndex == (BtGrids.Rows-1))
                {
                    if (ColumnNumber.SelectedIndex == 0)
                    {
                        return;
                    }
                    ColumnNumber.SelectedIndex = GetPreviousColRow(ColumnNumber.SelectedIndex, 0);
                    RowNumber.SelectedIndex = 0;
                }
                else
                {
                    RowNumber.SelectedIndex = GetNextColRow(RowNumber.SelectedIndex, BtGrids.Rows - 1);
                }
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

            Refresh(true);
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

            Refresh(true);
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
            Refresh(true);
        }

        private void BtnSave_Clicked(object sender, RoutedEventArgs e)
        {
            ParentPage.SaveSplitImages();
        }
    }
}
