using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Common.Tracers;
using ImageProcessor.Common.Tiff;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace ImageTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public Tracer Tracer = new Tracer();

        public MainWindow()
        {
            InitializeComponent();

            try
            {
                var tiffs = TiffHelper.ReadDoubleByteTiff(@"D:\gaoqi\原始图像\tif\hengzhe.tif");

                var bmps = TiffHelper.SliceVetically(tiffs[0], 2);

                bmps = TiffHelper.SliceVetically(@"D:\gaoqi\原始图像\tif\hengzhe.tif", 2);

                var bmp1 = TiffHelper.ConvertTiffInfoToBitmap(bmps[0]);
                var bmp2 = TiffHelper.ConvertTiffInfoToBitmap(bmps[1]);

                TiffImage1.Source = BitmapToBitmapImage(bmp1);
                TiffImage2.Source = BitmapToBitmapImage(bmp2);

                var tiff1 = bmps[0];
                var tiff2 = bmps[1];

                TiffHelper.Create16BitGrayScaleTiff(tiff1.UshortBuffer, tiff1.Width, tiff1.Height, @"\tif\NewTiff1.tiff");
                TiffHelper.Create16BitGrayScaleTiff(tiff2.UshortBuffer, tiff2.Width, tiff2.Height, @"\tif\NewTiff2.tiff");
            }
            catch (Exception e)
            {
                Tracer.Exception(e);
            }
        }

        private BitmapSource BitmapToBitmapImage(System.Drawing.Bitmap bmp)
        {
            BitmapSource returnSource;
            try
            {
                returnSource = Imaging.CreateBitmapSourceFromHBitmap(bmp.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());
            }
            catch
            {
                returnSource = null;
            }

            return returnSource;
        }

        private void OpenFileDiag(object sender, RoutedEventArgs e)
        {
            // Configure open file dialog box
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.DefaultExt = ".tiff"; // Default file extension
            dlg.Filter = " Tiff (.tiff)|*.tiff|(.tif)|*.tif|All Files|*.*"; // Filter files by extension

            // Show open file dialog box
            bool? result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = dlg.FileName;

                var bmps = TiffHelper.SliceVetically(filename, 2);

                var bmp1 = TiffHelper.ConvertTiffInfoToBitmap(bmps[0]);
                var bmp2 = TiffHelper.ConvertTiffInfoToBitmap(bmps[1]);

                TiffImage1.Source = BitmapToBitmapImage(bmp1);
                TiffImage2.Source = BitmapToBitmapImage(bmp2);

                var tiff1 = bmps[0];
                var tiff2 = bmps[1];

                // Configure save file dialog box
                SaveFileDialog saveDlg = new SaveFileDialog();
                saveDlg.FileName = "Tiff"; // Default file name
                saveDlg.DefaultExt = ".tiff"; // Default file extension
                saveDlg.Filter = "Tiff (.tiff)|*.tiff"; // Filter files by extension

                // Show save file dialog box
                result = saveDlg.ShowDialog();

                // Process save file dialog box results
                if (result == true)
                {
                    // Save document
                    filename = saveDlg.FileName;

                    //TiffHelper.Create16BitGrayScaleTiff(tiff1.UshortBuffer, tiff1.Width, tiff1.Height, filename);
                    //TiffHelper.Create16BitGrayScaleTiff(tiff2.UshortBuffer, tiff2.Width, tiff2.Height, filename);

                    TiffHelper.CreateMultipage16BitTiff(
                        new List<List<ushort[]>>() {tiff1.UshortBuffer, tiff2.UshortBuffer}, tiff1.Width, tiff1.Height,
                        filename);
                }
            }
        }
    }
}
