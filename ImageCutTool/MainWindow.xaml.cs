using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Microsoft.Win32;
using System.IO;
#if DEBUG
using System.Linq;
#endif

namespace ImageCutTool
{

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        /* メンバ変数宣言 */
        private List<UIElement> canvasStock = new List<UIElement>(); //再描画する際に既存のパスを消す用の格納リスト
        private List<System.Windows.Point> linePointList = new List<System.Windows.Point>();
        private List<Line> drawLine = new List<Line>();
        private String loadImgStr = String.Empty;
        private bool lineComplateFlg = false;
        /* メンバ変数宣言 */
        /* デリゲート */
        //public delegate void ButtonClickEventHandler(object sender, RoutedEventArgs e);
        //public event ButtonClickEventHandler ButtonClick;

        public MainWindow()
        {
            InitializeComponent();
            //this.FileOpenButton.IsEnabled = false;
        }

        private void FileOpenButton_Click(object sender, RoutedEventArgs e)
        {

            // ダイアログのインスタンスを生成
            var dialog = new Microsoft.Win32.OpenFileDialog();
            loadImgStr = String.Empty;

            // ファイルの種類を設定
            // dialog.Filter = "テキストファイル (*.txt)|*.txt|全てのファイル (*.*)|*.*";

            // ダイアログを表示する
            if (dialog.ShowDialog() == true)
            {
                // 日本語対策のため、一度ファイルをバッファに読み込む
                loadImgStr = dialog.FileName;
                var array = File.ReadAllBytes(loadImgStr);
                Mat srcImg = Cv2.ImDecode(array, ImreadModes.Color);
                DrawImage(in srcImg);
            }
        }

        private void DrawImage(in Mat srcImg)
        {
            imgEditCanvas.Children.Clear();
            LineReset();
            imgEditCanvas.Width = srcImg.Cols;
            imgEditCanvas.Height = srcImg.Rows;
            Image img = new Image();
            imgEditCanvas.Children.Add(img);
            img.Source = srcImg.ToWriteableBitmap();

        }

        private void imgEditCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DrawLine(e.GetPosition(this.imgEditCanvas));
        }

        private void DrawLine()
        {
            int linePointNow = linePointList.Count - 1;
            drawLine[linePointNow].X2 = linePointList[0].X;
            drawLine[linePointNow].Y2 = linePointList[0].Y;
            drawLine[linePointNow].Stroke = new SolidColorBrush(Colors.LimeGreen);
            drawLine[linePointNow].StrokeThickness = 1;
            imgEditCanvas.Children.Add(drawLine[linePointNow]);
            canvasStock.Add(drawLine[linePointNow]);
        }

        private void DrawLine(System.Windows.Point p)
        {
            if (lineComplateFlg)
            {   //既存のパスを削除
                foreach (UIElement ui in canvasStock)
                {
                    imgEditCanvas.Children.Remove(ui);
                }
                canvasStock.Clear();
                linePointList.Clear();
                drawLine.Clear();
                lineComplateFlg = false;
            }

            // Listに座標値を追加
            linePointList.Add(p);
            drawLine.Add(new Line());

            int linePointNow = linePointList.Count - 1;
            int linePointBefore = linePointList.Count - 2;
            switch (linePointList.Count)
            {
                case 1:
                    X1_TBlk.Text = linePointList[linePointNow].X.ToString();
                    Y1_TBlk.Text = linePointList[linePointNow].Y.ToString();
                    drawLine[linePointNow].X1 = linePointList[linePointNow].X;
                    drawLine[linePointNow].Y1 = linePointList[linePointNow].Y;
                    break;
                default:
                    X1_TBlk.Text = linePointList[linePointNow].X.ToString();
                    Y1_TBlk.Text = linePointList[linePointNow].Y.ToString();
                    drawLine[linePointBefore].X2 = linePointList[linePointNow].X;
                    drawLine[linePointBefore].Y2 = linePointList[linePointNow].Y;
                    drawLine[linePointNow].X1 = linePointList[linePointNow].X;
                    drawLine[linePointNow].Y1 = linePointList[linePointNow].Y;
                    break;
            }


            if (linePointList.Count > 1)
            {
                drawLine[linePointBefore].Stroke = new SolidColorBrush(Colors.LimeGreen);
                drawLine[linePointBefore].StrokeThickness = 1;
                imgEditCanvas.Children.Add(drawLine[linePointBefore]);
            }
            canvasStock.Add(drawLine[linePointNow]);

        }

        private void CaptionClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CaptionNormal_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Normal;
        }

        private void CaptionMaximized_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
        }

        private void CaptionMinimized_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CutImgViewButton_Click(object sender, RoutedEventArgs e)
        {
            ImgCutProc(false);
        }

        private void SaveImgButton_Click(object sender, RoutedEventArgs e)
        {
            ImgCutProc(true);
        }

        private void ImgCutProc(bool saveFlg)
        {
            var array = File.ReadAllBytes(loadImgStr);
            var mat = Cv2.ImDecode(array, ImreadModes.Color);
            Mat srcImg = Cv2.ImDecode(array, ImreadModes.Color);
            Mat maskImg = new Mat(srcImg.Rows, srcImg.Cols, MatType.CV_8UC3, new Scalar(0, 0, 0));
            for (int i = 0; i < linePointList.Count; i++)
            {
                int list_i = i;
                if (i > linePointList.Count-1)
                {
                    list_i = 0;
                }
                Cv2.Line(maskImg, new OpenCvSharp.Point(drawLine[list_i].X1, drawLine[list_i].Y1),
                         new OpenCvSharp.Point(drawLine[list_i].X2, drawLine[list_i].Y2),
                         new Scalar(255, 255, 255));

            }
            Cv2.FloodFill(maskImg, new OpenCvSharp.Point(0, 0), new Scalar(255, 255, 255, 255));
            Cv2.BitwiseNot(maskImg, maskImg);
            Mat dstImg = new Mat();
            srcImg.CopyTo(dstImg, maskImg);
            Cv2.CvtColor(dstImg, dstImg, ColorConversionCodes.BGR2BGRA);
#if DEBUG
            var minIdxX = drawLine
                .Select((val, idx) => new { V = val, I = idx })
                .Aggregate((min, working) => (min.V.X1 < working.V.X1) ? min : working)
                .I;
            var maxIdxX = drawLine
                .Select((val, idx) => new { V = val, I = idx }).
                Aggregate((max, working) => (max.V.X1 > working.V.X1) ? max : working).
                I;
            var minIdxY = drawLine
                .Select((val, idx) => new { V = val, I = idx })
                .Aggregate((min, working) => (min.V.Y1 < working.V.Y1) ? min : working)
                .I;
            var maxIdxY = drawLine
                .Select((val, idx) => new { V = val, I = idx }).
                Aggregate((max, working) => (max.V.Y1 > working.V.Y1) ? max : working).
                I;
            Console.WriteLine("min index in X" + minIdxX);
            Console.WriteLine("max index in X" + maxIdxX);
            Console.WriteLine("min index in Y" + minIdxY);
            Console.WriteLine("max index in Y" + maxIdxY);
            Console.WriteLine(linePointList.Select(x => x.X).Min());
            Console.WriteLine(linePointList.Select(x => x.Y).Max());
            Console.WriteLine(linePointList.Select(y => y.Y).Min());
            Console.WriteLine(linePointList.Select(y => y.Y).Max());
#endif
            for (int y = 0; y < dstImg.Rows; ++y)
            {
                for (int x = 0; x < dstImg.Cols; ++x)
                {
                    Vec4b px = dstImg.At<Vec4b>(y, x);
                    if (px[0] + px[1] + px[2] == 0)
                    {
                        px[3] = 0;
                        dstImg.Set<Vec4b>(y, x, px);
                    }
                }
            }
            if (saveFlg)
            {

                var dialog = new SaveFileDialog();
                dialog.Title = "ファイルの保存先を選択してね！";
                dialog.InitialDirectory = @"C:\Users";
                dialog.FileName = "cutimg.png";
                dialog.Filter = "すべてのファイル|*.*|png ファイル|*.png";
                dialog.RestoreDirectory = true;
                dialog.FilterIndex = 2;
                if (true == dialog.ShowDialog())
                {
                    Cv2.ImWrite(dialog.FileName, dstImg);
                }
            }
            else
            {
                Cv2.ImShow("dstImg", dstImg);
            }
        }

        private void LineCompleteButton_Click(object sender, RoutedEventArgs e)
        {
            lineComplateFlg = true;
            DrawLine();
        }

        private void LineResetButton_Click(object sender, RoutedEventArgs e)
        {
            LineReset();
        }

        private void LineReset()
        {
            foreach (UIElement ui in canvasStock)
            {
                imgEditCanvas.Children.Remove(ui);
            }
            canvasStock.Clear();
            linePointList.Clear();
            drawLine.Clear();
            lineComplateFlg = false;
        }

        private void LineUndoButton_Click(object sender, RoutedEventArgs e)
        {
            if(linePointList.Count > 0)
            {
                linePointList.Remove(linePointList[linePointList.Count - 1]);
            }
            if(drawLine.Count > 0)
            {
                if(drawLine.Count > 1)
                {
                    if(lineComplateFlg)
                    {
                        imgEditCanvas.Children.Remove(drawLine[drawLine.Count - 1]);
                        imgEditCanvas.Children.Remove(drawLine[drawLine.Count - 2]);
                        lineComplateFlg = false;
                    }
                    else
                    {
                        imgEditCanvas.Children.Remove(drawLine[drawLine.Count - 2]);
                    }
                }
                drawLine.Remove(drawLine[drawLine.Count - 1]);
            }

        }
    }
}
