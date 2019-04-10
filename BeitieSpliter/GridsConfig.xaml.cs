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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace BeitieSpliter
{
    
    public class ChangeStruct
    {
        public int left = 0;
        public int right = 0;
        public int top = 0;
        public int bottom = 0;
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
        }
        OperationType OpType = OperationType.SingleElement;
        BeitieGrids BtGrids = null;
        BeitieImage BtImage = null;
        Rect BtImageShowRect = new Rect();
        Rect BtImageAdjustRect = new Rect();
        List<Point> DrawLineElements = new List<Point>();
        Rect ToAdjustRect = new Rect();
        ChangeStruct ChangeRect = new ChangeStruct();
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

        private void CalculateDrawRect()
        {
            Point pntLt = new Point();
            Point pntRb = new Point();
            int SelectedCol = ColumnNumber.SelectedIndex + 1;
            int SelectedRow = RowNumber.SelectedIndex + 1;
            int minCol = 1;
            int maxCol = BtGrids.Columns;
            int minRow = 1;
            int maxRow = BtGrids.Rows;

            int PreCol = GetPreviousColRow(SelectedCol, 1);
            int PreRow = GetPreviousColRow(SelectedRow, 1);
            int NextCol = GetNextColRow(SelectedCol, BtGrids.Columns);
            int NextRow = GetNextColRow(SelectedRow, BtGrids.Rows);

            DrawLineElements.Clear();
            if (OperationType.SingleColumn == OpType)
            {
                PreRow = 1;
                NextRow = BtGrids.Rows;
                minCol = maxCol = SelectedCol;

            }
            else if (OperationType.SingleRow == OpType)
            {
                PreCol = 1;
                NextCol = BtGrids.Columns;
                minRow = maxRow = SelectedRow;
            }
            else
            {
                minCol = maxCol = SelectedCol;
                minRow = maxRow = SelectedRow;
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
        private void Refresh(bool reloadImage = false)
        {
            if (IsParaInvalid())
            {
                return;
            }
            CalculateDrawRect();
            CurrentItem.Invalidate();
        }
        void InitControls()
        {
            // 添加颜色

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
            BtGrids = (BeitieGrids)e.Parameter;

            //BtImage = new BeitieImage(CurrentItem, BtGrids.ImageFile);
            BtImage = BtGrids.BtImageParent;
            InitControls();
            CalculateDrawRect();
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

            //OperationGrid.MaxWidth = BtGrids.DrawWidth;
            //OperationGrid.MaxHeight = BtGrids.DrawHeight; 
            //OperationGrid.Background = imageBrush;
        }
        
        private void DrawLines(CanvasDrawingSession draw)
        {
            Rect drawRect = ToAdjustRect;
            drawRect.X += ChangeRect.left;
            drawRect.Y += ChangeRect.top;
            drawRect.Width += ChangeRect.right;
            drawRect.Height += ChangeRect.bottom;

            draw.DrawRectangle(ToAdjustRect, Colors.Azure , BtGrids.PenWidth);
            draw.DrawRectangle(drawRect, BtGrids.PenColor, BtGrids.PenWidth);
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
            Refresh();
        }

        private void RowNumber_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Refresh();
        }

        private void Operation_Checked(object sender, RoutedEventArgs e)
        {
            if (OpSingleElement.IsChecked ?? false)
            {
                OpType = OperationType.SingleElement;
            }
            else if (OpSingleRow.IsChecked ?? false)
            {
                OpType = OperationType.SingleRow;
            }
            else if (OpSingleColumn.IsChecked ?? false)
            {
                OpType = OperationType.SingleColumn;
            }
            Refresh(true);
        }

        private void Adjust_Clicked(object sender, RoutedEventArgs e)
        {
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
            Debug.WriteLine("Change: {0:0},{1:0},{2:0},{3:0}", ChangeRect.left, ChangeRect.top, ChangeRect.right, ChangeRect.bottom);
            Refresh();
        }
    }
}
