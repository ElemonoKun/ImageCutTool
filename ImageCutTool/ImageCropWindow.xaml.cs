using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using OpenCvSharp;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;

namespace ImageCutTool
{
    /// <summary>
    /// ImageCropWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ImageCropWindow : System.Windows.Window
    {

        //private Point _startPoint;
        /* メンバ変数宣言 始まり*/
        //! 再描画する際に既存のCanvasを消す用の格納リスト
        public List<UIElement> _canvasStock = new List<UIElement>();
        //! 切り取り対象画像上で左クリックされた際の座標値格納リスト
        public List<System.Windows.Point> _linePointList = new List<System.Windows.Point>();
        //! 切り取り/描画用ラインの格納リスト
        public List<Line> _drawLine = new List<Line>();
        //! ライン完成ボタン押下時に始点とつなげるためのフラグ
        public bool _lineComplateFlg = false;
        //! 現在読み込まれている切り取り元画像のファイルパス
        public String _loadImgStr = String.Empty;
        //! 画像をドラッグして動かすときの開始座標
        private System.Windows.Point _startPos;
        //! 倍率調整記録用変数
        private double _magnification = 1.0;
        //! 座標値保存用
        private double originX = 0.0;
        private double originY = 0.0;

        public ImageCropWindow()
        {
            InitializeComponent();
            const double scale = 1.02;
            var matrix = ImgCutViewer.RenderTransform.Value;
            // 拡大処理
            matrix.ScaleAt(scale, scale, 0, 0);
            ImgCutViewer.RenderTransform = new MatrixTransform(matrix);
        }

        public void ImgCutViewer_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DrawLine(e.GetPosition(this.ImgCutViewer));
        }

        public void DrawLine()
        {
            int linePointNow = _linePointList.Count - 1;
            _drawLine[linePointNow].X2 = _linePointList[0].X;
            _drawLine[linePointNow].Y2 = _linePointList[0].Y;
            _drawLine[linePointNow].Stroke = new SolidColorBrush(Colors.LimeGreen);
            _drawLine[linePointNow].StrokeThickness = 1;
            this.ImgCutViewer.Children.Add(_drawLine[linePointNow]);
            _canvasStock.Add(_drawLine[linePointNow]);
        }

        public void DrawLine(System.Windows.Point p)
        {
            if (_lineComplateFlg)
            {   //既存のCanvasを削除
                foreach (UIElement ui in _canvasStock)
                {
                    this.ImgCutViewer.Children.Remove(ui);
                }
                _canvasStock.Clear();
                _linePointList.Clear();
                _drawLine.Clear();
                _lineComplateFlg = false;
            }

            // Listに座標値を追加
            _linePointList.Add(p);
            _drawLine.Add(new Line());

            int linePointNow = _linePointList.Count - 1;
            int linePointBefore = _linePointList.Count - 2;
            switch (_linePointList.Count)
            {
                case 1:
                    _drawLine[linePointNow].X1 = _linePointList[linePointNow].X;
                    _drawLine[linePointNow].Y1 = _linePointList[linePointNow].Y;
                    break;
                default:
                    _drawLine[linePointBefore].X2 = _linePointList[linePointNow].X;
                    _drawLine[linePointBefore].Y2 = _linePointList[linePointNow].Y;
                    _drawLine[linePointNow].X1 = _linePointList[linePointNow].X;
                    _drawLine[linePointNow].Y1 = _linePointList[linePointNow].Y;
                    break;
            }


            if (_linePointList.Count > 1)
            {
                _drawLine[linePointBefore].Stroke = new SolidColorBrush(Colors.LimeGreen);
                _drawLine[linePointBefore].StrokeThickness = 1;
                this.ImgCutViewer.Children.Add(_drawLine[linePointBefore]);
            }
            _canvasStock.Add(_drawLine[linePointNow]);

        }

        public void ImgCutProc(bool saveFlg)
        {
            var array = File.ReadAllBytes(this._loadImgStr);
            var mat = Cv2.ImDecode(array, ImreadModes.Color);
            Mat srcImg = Cv2.ImDecode(array, ImreadModes.Color);
            Mat maskImg = new Mat(srcImg.Rows, srcImg.Cols, MatType.CV_8UC3, new Scalar(0, 0, 0));
            for (int i = 0; i < _linePointList.Count; i++)
            {
                int list_i = i;
                if (i > _linePointList.Count - 1)
                {
                    list_i = 0;
                }
                Cv2.Line(maskImg, new OpenCvSharp.Point(_drawLine[list_i].X1, _drawLine[list_i].Y1),
                         new OpenCvSharp.Point(_drawLine[list_i].X2, _drawLine[list_i].Y2),
                         new Scalar(255, 255, 255));

            }
            Cv2.FloodFill(maskImg, new OpenCvSharp.Point(0, 0), new Scalar(255, 255, 255, 255));
            Cv2.BitwiseNot(maskImg, maskImg);
            Mat dstImg = new Mat();
            srcImg.CopyTo(dstImg, maskImg);
            Cv2.CvtColor(dstImg, dstImg, ColorConversionCodes.BGR2BGRA);
#if DEBUG
            var minIdxX = _drawLine
                .Select((val, idx) => new { inVal = val, inIdx = idx })
                .Aggregate((min, compareVal) => (min.inVal.X1 < compareVal.inVal.X1) ? min : compareVal)
                .inIdx;
            var maxIdxX = _drawLine
                .Select((val, idx) => new { inVal = val, inIdx = idx }).
                Aggregate((max, compareVal) => (max.inVal.X1 > compareVal.inVal.X1) ? max : compareVal)
                .inIdx;
            var minIdxY = _drawLine
                .Select((val, idx) => new { V = val, I = idx })
                .Aggregate((min, working) => (min.V.Y1 < working.V.Y1) ? min : working)
                .I;
            var maxIdxY = _drawLine
                .Select((val, idx) => new { V = val, I = idx }).
                Aggregate((max, working) => (max.V.Y1 > working.V.Y1) ? max : working).
                I;
            Console.WriteLine("min index in X" + minIdxX);
            Console.WriteLine("max index in X" + maxIdxX);
            Console.WriteLine("min index in Y" + minIdxY);
            Console.WriteLine("max index in Y" + maxIdxY);
            Console.WriteLine(_linePointList.Select(x => x.X).Min());
            Console.WriteLine(_linePointList.Select(x => x.Y).Max());
            Console.WriteLine(_linePointList.Select(y => y.Y).Min());
            Console.WriteLine(_linePointList.Select(y => y.Y).Max());
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
                dialog.Filter = "すべてのファイル|*.*|png ファイル|*.png|bmp ファイル|*.bmp|jpeg ファイル|*.jpeg";
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

        private void ImgCutViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            double prevMagnification = _magnification; // 変更前の倍率を保存

            if (e.Delta > 0)
            {
                // 拡大
                if (_magnification > 1)
                {
                    _magnification += 1;
                }
                else
                {
                    _magnification += 0.05;
                }
            }
            else
            {
                // 縮小
                if (_magnification > 1)
                {
                    _magnification -= 1;
                }
                else
                {
                    _magnification = Math.Max(0.05, _magnification - 0.05); // 0.05以下にしない
                }
            }

            double scale = prevMagnification / _magnification; // 今回拡大(縮小)した量(率)
            double width = ImgCutViewer.Width; // 画面の幅
            double hight = ImgCutViewer.Height; // 画面の高さ
            originX = (originX - width / 2) * scale + width / 2;
            originY = (originY - hight / 2) * scale + hight / 2;
            //var matrix = ImgCutViewer.RenderTransform.Value;
            //if (e.Delta > 0)
            //{
            //    // 拡大処理
            //    matrix.ScaleAt(scale, scale, e.GetPosition(ImgCutViewer).X, e.GetPosition(ImgCutViewer).Y);
            //    _magnification *= scale;
            //}
            //else
            //{
            //    // 縮小処理
            //    matrix.ScaleAt(1.0 / scale, 1.0 / scale, e.GetPosition(ImgCutViewer).X, e.GetPosition(ImgCutViewer).Y);
            //    _magnification *= 1.0 / scale;
            //}

            //ImgCutViewer.RenderTransform = new MatrixTransform(matrix);
        }

        private void ImgCutViewer_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            ImgCutViewer.CaptureMouse();
            _startPos = e.GetPosition(ImgCutViewer);
        }

        private void ImgCutViewer_MouseMove(object sender, MouseEventArgs e)
        {
            if (ImgCutViewer.IsMouseCaptured)
            {
                var matrix = ImgCutViewer.RenderTransform.Value;
                Vector v = _startPos - e.GetPosition(ImgCutViewer);
                matrix.Translate(-v.X, -v.Y);
                ImgCutViewer.RenderTransform = new MatrixTransform(matrix);
                _startPos = e.GetPosition(ImgCutViewer);
            }
        }

        private void ImgCutViewer_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ImgCutViewer.ReleaseMouseCapture();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                var matrix = ImgCutViewer.RenderTransform.Value;
                _magnification = 1.0;
                matrix.M11 = 1.0;
                matrix.M12 = 0.0;
                matrix.M21 = 0.0;
                matrix.M22 = 1.0;
                matrix.OffsetX = 0.0;
                matrix.OffsetY = 0.0;
                ImgCutViewer.RenderTransform = new MatrixTransform(matrix);
            }
        }
    }

}
