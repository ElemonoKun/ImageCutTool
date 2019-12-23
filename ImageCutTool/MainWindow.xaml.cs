/**
 * @file MainWindow.xaml
 * @brief ImageCutToolのメインウィンドウ
 * @author ElemonoKun|[^v^ ]]
 * @date 2019/10/14
 */
using System;
using System.Windows;
using System.Windows.Controls;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System.IO;
#if DEBUG
#endif

namespace ImageCutTool
{

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        //! 画像表示用の別ウィンドウ
        public ImageCropWindow _imgCropWin;

        /* デリゲート 始まり*/
        //public delegate void ButtonClickEventHandler(object sender, RoutedEventArgs e);
        //public event ButtonClickEventHandler ButtonClick;
        /* デリゲート 終わり*/

        public MainWindow()
        {
            InitializeComponent();
        }

        /* イベントハンドラ 始まり */
        /**
         * @fn
         * 元画像読み込み用ボタンイベント
         * @brief
         * -ファイル選択ダイアログから
         *  切り取りたい画像ファイルを選択する。
         * @return 無し
         * @detail 
         * -選択された画像がメインウィンドウ上に
         *  切り取り対象として表示される。
         */
        private void FileOpenButton_Click(object sender, RoutedEventArgs e)
        {

            // ダイアログのインスタンス生成と設定
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "全てのファイル (*.*)|*.*";

            if (dialog.ShowDialog() == true)
            {
                _imgCropWin = new ImageCropWindow();
                _imgCropWin._loadImgStr = String.Empty;
                if (IsImgExt(dialog.FileName) != 0) // 拡張子チェック
                {
                    MessageBox.Show("ファイルが画像っぽくない気がします…違ってたらすみません[>A<];]");
                    return;
                }
                // 日本語対策のため、一度ファイルをバッファに読み込む
                _imgCropWin._loadImgStr = dialog.FileName;
                var array = File.ReadAllBytes(_imgCropWin._loadImgStr);
                Mat srcImg = Cv2.ImDecode(array, ImreadModes.Color);

                // 画像表示用サブウィンドウ生成
                // + 100 は余白を持たせるため
                _imgCropWin.Width = srcImg.Cols+100;
                _imgCropWin.Height = srcImg.Rows+100;
                _imgCropWin.Show();
                DrawImage(in srcImg);
            }
        }
        private void DrawImage(in Mat srcImg)
        {
            //↓ 新しく画像を読み込んだので、今までの保存してたエレメント削除
            _imgCropWin.ImgCutViewer.Children.Clear();
            LineReset();
            //↑ 新しく画像を読み込んだので、今までの保存してたエレメント削除
            //ImgEditCanvas.Width = srcImg.Cols;
            //ImgEditCanvas.Height = srcImg.Rows;
            Image editImg = new Image();
            _imgCropWin.ImgCutViewer.Children.Add(editImg);
            editImg.Source = srcImg.ToWriteableBitmap();

        }

        private void CaptionClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
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
            _imgCropWin.ImgCutProc(false);
        }

        private void SaveImgButton_Click(object sender, RoutedEventArgs e)
        {
            _imgCropWin.ImgCutProc(true);
        }

        private void LineCompleteButton_Click(object sender, RoutedEventArgs e)
        {
            _imgCropWin._lineComplateFlg = true;
            _imgCropWin.DrawLine();
        }
        private void LineResetButton_Click(object sender, RoutedEventArgs e)
        {
            LineReset();
        }

        private void LineUndoButton_Click(object sender, RoutedEventArgs e)
        {
            int linePointNow = _imgCropWin._linePointList.Count - 1;
            int linePointBefore = _imgCropWin._linePointList.Count - 2;
            if (_imgCropWin._linePointList.Count > 0)
            {
                _imgCropWin._linePointList.Remove(_imgCropWin._linePointList[linePointNow]);
            }
            if(_imgCropWin._drawLine.Count > 0)
            {
                if(_imgCropWin._drawLine.Count > 1)
                {
                    if(_imgCropWin._lineComplateFlg)
                    {
                        _imgCropWin.ImgCutViewer.Children.Remove(_imgCropWin._drawLine[linePointNow]);
                        _imgCropWin.ImgCutViewer.Children.Remove(_imgCropWin._drawLine[linePointBefore]);
                        _imgCropWin._lineComplateFlg = false;
                    }
                    else
                    {
                        _imgCropWin.ImgCutViewer.Children.Remove(_imgCropWin._drawLine[linePointBefore]);
                    }
                }
                _imgCropWin._drawLine.Remove(_imgCropWin._drawLine[linePointNow]);
            }

        }
        /* イベントハンドラ 終わり*/

        /* メソッド始まり */

        private void LineReset()
        {
            foreach (UIElement ui in _imgCropWin._canvasStock)
            {
                _imgCropWin.ImgCutViewer.Children.Remove(ui);
            }
            _imgCropWin._canvasStock.Clear();
            _imgCropWin._linePointList.Clear();
            _imgCropWin._drawLine.Clear();
            _imgCropWin._lineComplateFlg = false;
        }

        private int IsImgExt(String checkStr)
        {
            switch(System.IO.Path.GetExtension(checkStr)) // 拡張子チェック
            {
                case ".JPG":
                case ".jpg":
                case ".jpeg":
                case ".png":
                case ".bmp":
                case ".gif":
                    return 0;
                default:
                    return -1;
            }
        }

        /* メソッド終わり */
    }
}
