using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BitMiracle.LibTiff.Classic;

namespace ImageProcessor.Common.Tiff
{
    public class TiffInfo
    {
        public string FilePath { get; set; }

        public byte[] Buffer { get; set; }

        public int Width { get; set; }
        public int Height { get; set; }

        /// <summary>
        /// 每个像素取样
        /// This will be 1 for black and white, or grayscale, but will normally be 3 for color images.
        /// </summary>
        public int SamplesPerPixel { get; set; }

        /// <summary>
        /// 单个像素是多少位
        /// </summary>
        public int BitsPerSample { get; set; }

        /// <summary>
        /// Member name	Value	Description
        /// TOPLEFT	    1	Row 0 top, Column 0 lhs.
        /// TOPRIGHT	2	Row 0 top, Column 0 rhs.
        /// BOTRIGHT	3	Row 0 bottom, Column 0 rhs.
        /// BOTLEFT	    4	Row 0 bottom, Column 0 lhs.
        /// LEFTTOP	    5	Row 0 lhs, Column 0 top.
        /// RIGHTTOP	6	Row 0 rhs, Column 0 top.
        /// RIGHTBOT	7	Row 0 rhs, Column 0 bottom.
        /// LEFTBOT	    8	Row 0 lhs, Column 0 bottom.
        /// </summary>
        public Orientation Orientation { get; set; }

        /// <summary>
        /// 应该是实际的行数，一般设置为height，
        /// </summary>
        public int RowsPerStrip { get; set; }

        public float XResolution { get; set; }

        public float YResolution { get; set; }

        /// <summary>
        /// Member name	Value Description(单位)
        /// NONE	    1	No meaningful units.
        /// INCH	    2	English.
        /// CENTIMETER	3	Metric.
        /// </summary>
        public ResUnit ResolutionUnit { get; set; }

        /// <summary>
        /// StorageConfiguration，以rgb图像为例，如果参数为Config，
        /// 则只有一个图像平面；如果为separate，这R数据存到一起，G数据一起，B数据一起
        /// Member name	Value	Description
        /// UNKNOWN	    0	Unknown (uninitialized).
        /// CONTIG	    1	Single image plane.
        /// SEPARATE	2	Separate planes of data.
        /// </summary>
        public PlanarConfig PlanarConfig { get; set; }

        /// <summary>
        /// 图像模式，	Member name	Value	Description
        /// MINISWHITE	0	Min value is white.
        /// MINISBLACK	1	Min value is black.
        /// RGB	        2	RGB color model.
        /// PALETTE	    3	Color map indexed.
        /// MASK	    4	[obsoleted by TIFF rev. 6.0] Holdout mask.
        /// SEPARATED	5	Color separations.
        /// YCBCR	    6	CCIR 601.
        /// CIELAB	    8	1976 CIE L*a*b*.
        /// ICCLAB	    9	ICC L*a*b*. Introduced post TIFF rev 6.0 by Adobe TIFF Technote 4.
        /// ITULAB	    10	ITU L*a*b*.
        /// LOGL	    32844	CIE Log2(L).
        /// LOGLUV	    32845	CIE Log2(L) (u',v').
        /// </summary>
        public Photometric Photometric { get; set; }

        public Compression Compression { get; set; }

        /// <summary>
        /// Member name	Value	Description
        /// MSB2LSB	        1	Most significant -> least.
        /// LSB2MSB	        2	Least significant -> most.
        /// </summary>
        public FillOrder FillOrder { get; set; }

        public TiffInfo(int width, int height, int bitPerPixel)
        {
            this.Width = width;
            this.Height = height;
            this.BitsPerSample = bitPerPixel;
            this.Buffer = new byte[width*height*bitPerPixel];
        }

        public TiffInfo()
        {

        }
    }

    public class DoubleByteTiffInfo : TiffInfo
    {
        public List<ushort[]> UshortBuffer { get; set; }

        public DoubleByteTiffInfo()
        {
        }

        public DoubleByteTiffInfo(int width, int height)
        {
            this.Width = width;
            this.Height = height;
            UshortBuffer = new List<ushort[]>(height);
        }
    }

    /// <summary>
    /// 一个简单的Tiff读写程序,初步实现16位灰度数据的读写
    /// </summary>
    public static class TiffHelper
    {
        public const string ReadMode = "r";
        public const string WriteMode = "w";

        #region Write 16Bit Tiff

        public static void CreateMultipage16BitTiff(List<List<ushort[]>> buffers, int width, int height, string filePath)
        {
            if (buffers == null || buffers.Count <= 0)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            var dir = Path.GetDirectoryName(filePath);
            if (dir != null)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            else
            {
                throw new NullReferenceException("dir");
            }

            int numOfPages = buffers.Count;

            using (var output = BitMiracle.LibTiff.Classic.Tiff.Open(filePath, WriteMode))
            {
                for (int page = 0; page < numOfPages; page++)
                {
                    var info = CreateDoubleByteTiffInfo(null, buffers[page], width, height, filePath, height, width,
                        height);
                    SetTiffField(info, output);
                    output.SetField(TiffTag.SUBFILETYPE, FileType.PAGE);
                    output.SetField(TiffTag.PAGENUMBER, page, numOfPages);

                    int bytesPerRow = info.Width*sizeof (ushort);
                    byte[] tempBuffer = new byte[bytesPerRow];

                    for (int i = 0; i < height; i++)
                    {
                        Buffer.BlockCopy(info.UshortBuffer[i], 0, tempBuffer, 0, bytesPerRow);

                        output.WriteScanline(tempBuffer, i);
                    }

                    output.WriteDirectory();
                }
            }
        }

        public static void Create16BitGrayScaleTiff(List<ushort[]> ushortBuffer, int width, int height, string filePath)
        {
            var info = CreateDoubleByteTiffInfo(null, ushortBuffer, width, height, filePath, height, width, height);
            Create16BitGrayScaleTiff(info);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="ushortBuffer">字节数组</param>
        /// <param name="width">图像的宽</param>
        /// <param name="height">图像的高</param>
        /// <param name="filePath"></param>
        /// <param name="rowsPerStrip">图像的实际高度</param>
        /// <param name="xResolution">x分辨率</param>
        /// <param name="yResolution">y分辨率</param>
        /// <param name="resUnit">分辨率单位，默认选inch</param>
        /// <param name="planarConfig">数据平面存储方式</param>
        /// <param name="bytePerSample">单个像素是多少位</param>
        /// <param name="photometric">图像模式</param>
        /// <param name="compression">压缩方式</param>
        /// <param name="samplePerPixel">一个像素几个采样</param>
        /// <param name="orientation">方向</param>
        /// <param name="fillOrder">大小端</param>
        public static void Create16BitGrayScaleTiff(List<ushort[]> ushortBuffer, int width, int height, string filePath,
            int rowsPerStrip,
            int xResolution, int yResolution, ResUnit resUnit = ResUnit.INCH,
            PlanarConfig planarConfig = PlanarConfig.CONTIG,
            int bytePerSample = 16, Photometric photometric = Photometric.MINISBLACK,
            Compression compression = Compression.NONE,
            int samplePerPixel = 1, Orientation orientation = Orientation.TOPLEFT,
            FillOrder fillOrder = FillOrder.MSB2LSB)
        {
            var info = CreateDoubleByteTiffInfo(null, ushortBuffer, width, height, filePath, height, width, height);
            Create16BitGrayScaleTiff(info);
        }

        public static void Create16BitGrayScaleTiff(DoubleByteTiffInfo info)
        {
            VerifyDoubleBitTiffInfo(info);
            
            using (var image = BitMiracle.LibTiff.Classic.Tiff.Open(info.FilePath, WriteMode))
            {
                SetTiffField(info, image);

                int bytesPerRow = info.Width*sizeof (ushort);
                byte[] tempBuffer = new byte[bytesPerRow];

                for (int i = 0; i < info.Height; i++)
                {
                    Buffer.BlockCopy(info.UshortBuffer[i], 0, tempBuffer, 0, bytesPerRow);
                    image.WriteScanline(tempBuffer, i);
                }
            }
        }

        private static void VerifyDoubleBitTiffInfo(DoubleByteTiffInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            if (info.UshortBuffer == null || info.UshortBuffer.Count <= 0)
            {
                throw new ArgumentNullException("info.UshortBuffer");
            }
            if (string.IsNullOrWhiteSpace(info.FilePath))
            {
                throw new ArgumentNullException("info.FilePath");
            }

            //如果不创建目录，图像不能生成
            var dir = Path.GetDirectoryName(info.FilePath);
            if (string.IsNullOrWhiteSpace(dir))
            {
                throw new NullReferenceException("dir");
            }
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buffer">字节数组</param>
        /// <param name="width">图像的宽</param>
        /// <param name="height">图像的高</param>
        /// <param name="filePath"></param>
        /// <param name="rowsPerStrip">图像的实际高度</param>
        /// <param name="xResolution">x分辨率</param>
        /// <param name="yResolution">y分辨率</param>
        /// <param name="resUnit">分辨率单位，默认选inch</param>
        /// <param name="planarConfig">数据平面存储方式</param>
        /// <param name="bytePerSample">单个像素是多少位</param>
        /// <param name="photometric">图像模式</param>
        /// <param name="compression">压缩方式</param>
        /// <param name="samplePerPixel">一个像素几个采样</param>
        /// <param name="orientation">方向</param>
        /// <param name="fillOrder">大小端</param>
        public static void CreateGrayScaleTiff(byte[] buffer, int width, int height, string filePath, int rowsPerStrip,
            int xResolution, int yResolution, ResUnit resUnit = ResUnit.INCH,
            PlanarConfig planarConfig = PlanarConfig.CONTIG,
            int bytePerSample = 16, Photometric photometric = Photometric.MINISBLACK,
            Compression compression = Compression.NONE,
            int samplePerPixel = 1, Orientation orientation = Orientation.TOPLEFT,
            FillOrder fillOrder = FillOrder.MSB2LSB)
        {
            var info = CreateDoubleByteTiffInfo(buffer, null, width, height, filePath, height, width, height);
            Create16BitGrayScaleTiff(info);
        }

        public static void CreateGrayScaleTiff(TiffInfo info)
        {
            if (info == null)
            {
                throw new ArgumentNullException("info");
            }
            if (info.Buffer == null || info.Buffer.Length <= 0)
            {
                throw new ArgumentNullException("info.Buffer");
            }
            if (string.IsNullOrWhiteSpace(info.FilePath))
            {
                throw new ArgumentNullException("info.FilePath");
            }
            using (var image = BitMiracle.LibTiff.Classic.Tiff.Open(info.FilePath, WriteMode))
            {
                SetTiffField(info, image);

                int bytesPerRow = info.Width*sizeof (ushort);
                byte[] tempBuffer = new byte[bytesPerRow];

                for (int i = 0; i < info.Height; i++)
                {
                    Buffer.BlockCopy(info.Buffer, i*bytesPerRow, tempBuffer, 0, bytesPerRow);
                    image.WriteScanline(tempBuffer, i);
                }
            }
        }

        private static void SetTiffField(TiffInfo info, BitMiracle.LibTiff.Classic.Tiff image)
        {
            if (image == null)
            {
                throw new NullReferenceException("image");
            }

            // We need to set some values for basic tags before we can add any data
            image.SetField(TiffTag.IMAGEWIDTH, info.Width);
            image.SetField(TiffTag.IMAGELENGTH, info.Height);
            image.SetField(TiffTag.BITSPERSAMPLE, info.BitsPerSample);
            image.SetField(TiffTag.SAMPLESPERPIXEL, info.SamplesPerPixel);
            image.SetField(TiffTag.ORIENTATION, info.Orientation);
            image.SetField(TiffTag.ROWSPERSTRIP, info.Height);
            image.SetField(TiffTag.XRESOLUTION, info.XResolution);
            image.SetField(TiffTag.YRESOLUTION, info.YResolution);
            image.SetField(TiffTag.RESOLUTIONUNIT, info.ResolutionUnit);
            image.SetField(TiffTag.PLANARCONFIG, info.PlanarConfig);
            image.SetField(TiffTag.PHOTOMETRIC, info.Photometric);
            image.SetField(TiffTag.COMPRESSION, info.Compression);
            image.SetField(TiffTag.FILLORDER, info.FillOrder);
        }

        private static DoubleByteTiffInfo CreateDoubleByteTiffInfo(byte[] buffer, List<ushort[]> ushortBuffer, int width,
            int height, string filePath,
            int rowsPerStrip,
            int xResolution, int yResolution, ResUnit resUnit = ResUnit.INCH,
            PlanarConfig planarConfig = PlanarConfig.CONTIG,
            int bytePerSample = 16, Photometric photometric = Photometric.MINISBLACK,
            Compression compression = Compression.NONE,
            int samplePerPixel = 1, Orientation orientation = Orientation.TOPLEFT,
            FillOrder fillOrder = FillOrder.MSB2LSB)
        {
            return new DoubleByteTiffInfo()
            {
                FilePath = filePath,
                Buffer = buffer,
                UshortBuffer = ushortBuffer,
                Width = width,
                Height = height,
                RowsPerStrip = rowsPerStrip,
                XResolution = xResolution,
                YResolution = yResolution,
                ResolutionUnit = resUnit,
                PlanarConfig = planarConfig,
                BitsPerSample = bytePerSample,
                Photometric = photometric,
                Compression = compression,
                SamplesPerPixel = samplePerPixel,
                Orientation = orientation,
                FillOrder = fillOrder
            };
        }

        #endregion

        #region Read 16Bit Tiff

        /// <summary>
        /// 读取一个16bit Tiff图像
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static List<DoubleByteTiffInfo> ReadDoubleByteTiff(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            List<DoubleByteTiffInfo> tiffs = new List<DoubleByteTiffInfo>();

            using (var tiff = BitMiracle.LibTiff.Classic.Tiff.Open(path, ReadMode))
            {
                int width = tiff.GetField(TiffTag.IMAGEWIDTH)[0].ToInt();
                int height = tiff.GetField(TiffTag.IMAGELENGTH)[0].ToInt();

                byte[] scanline = new byte[tiff.ScanlineSize()];

                for (int i = 0; i < tiff.NumberOfDirectories(); i++)
                {
                    var doubleByteTiffInfo = new DoubleByteTiffInfo(width, height);

                    for (int j = 0; j < height; j++)
                    {
                        tiff.ReadScanline(scanline, j);
                        var scanline16Bit = new ushort[tiff.ScanlineSize() >> 1];
                        Buffer.BlockCopy(scanline, 0, scanline16Bit, 0, scanline.Length);
                        doubleByteTiffInfo.UshortBuffer.Add(scanline16Bit);
                    }
                    tiffs.Add(doubleByteTiffInfo);
                }
            }
            return tiffs;
        }

        /// <summary>
        /// 读取16bittiff并转换为灰度图。bitmap的16位灰度图显示有问题，因此转换为24rgb格式，用于显示
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Bitmap ReadDoubleByteTiffAsBmp(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException("path");
            }

            Bitmap result;

            using (var tif = BitMiracle.LibTiff.Classic.Tiff.Open(path, ReadMode))
            {
                FieldValue[] res = tif.GetField(TiffTag.IMAGELENGTH);
                int height = res[0].ToInt();

                res = tif.GetField(TiffTag.IMAGEWIDTH);
                int width = res[0].ToInt();

                res = tif.GetField(TiffTag.BITSPERSAMPLE);
                short bpp = res[0].ToShort();
                if (bpp != 16)
                    return null;

                res = tif.GetField(TiffTag.SAMPLESPERPIXEL);
                short spp = res[0].ToShort();
                if (spp != 1)
                    return null;

                res = tif.GetField(TiffTag.PHOTOMETRIC);
                Photometric photo = (Photometric) res[0].ToInt();
                if (photo != Photometric.MINISBLACK && photo != Photometric.MINISWHITE)
                    return null;

                int stride = tif.ScanlineSize();
                byte[] buffer = new byte[stride];

                result = new Bitmap(width, height, PixelFormat.Format24bppRgb);
                byte[] buffer8Bit = null;

                for (int i = 0; i < height; i++)
                {
                    Rectangle imgRect = new Rectangle(0, i, width, 1);
                    BitmapData imgData = result.LockBits(imgRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                    if (buffer8Bit == null)
                        buffer8Bit = new byte[imgData.Stride];
                    else
                        Array.Clear(buffer8Bit, 0, buffer8Bit.Length);

                    tif.ReadScanline(buffer, i);
                    ConvertBuffer(buffer, buffer8Bit);

                    Marshal.Copy(buffer8Bit, 0, imgData.Scan0, buffer8Bit.Length);
                    result.UnlockBits(imgData);
                }
            }

            return result;
        }

        private static void ConvertBuffer(byte[] buffer, byte[] buffer8Bit)
        {
            for (int src = 0, dst = 0; src < buffer.Length; dst++)
            {
                int value16 = buffer[src++];
                value16 = value16 + (buffer[src++] << 8);
                buffer8Bit[dst++] = (byte) (value16/257.0 + 0.5);
                buffer8Bit[dst++] = (byte) (value16/257.0 + 0.5);
                buffer8Bit[dst] = (byte) (value16/257.0 + 0.5);
            }
        }

        #endregion

        #region ProcessTiff

        /// <summary>
        /// 纵向均匀切割Tiff图像，返回数据链表
        /// </summary>
        /// <param name="tiff"></param>
        /// <param name="count">切割的分块数</param>
        /// <returns></returns>
        public static List<DoubleByteTiffInfo> SliceVetically(DoubleByteTiffInfo tiff , int count)
        {
            var originalWidth = tiff.Width;
            var newWidth = originalWidth / count;

            var newBytesWidth = newWidth*sizeof (ushort);

            var tiffLists = new List<DoubleByteTiffInfo>();


            for (int i = 0; i < count; i++)
            {
                DoubleByteTiffInfo info = new DoubleByteTiffInfo(newWidth, tiff.Height);

                for (int j = 0; j < tiff.Height; j++)
                {
                    ushort[] temp = new ushort[newWidth];
                    Buffer.BlockCopy(tiff.UshortBuffer[j], newBytesWidth*i, temp, 0, newBytesWidth);
                    info.UshortBuffer.Add(temp);
                }
                tiffLists.Add(info);
            }
            return tiffLists;
        }

        /// <summary>
        /// 纵向均匀切割Tiff图像，返回数据链表
        /// </summary>
        /// <param name="path"></param>
        /// <param name="count">切割的分块数</param>
        /// <returns></returns>
        public static List<DoubleByteTiffInfo> SliceVetically(string path, int count)
        {
            var tiffs = ReadDoubleByteTiff(path);

            if (tiffs == null || tiffs.Count <= 0)
            {
                return null;
            }
            //只处理第一目录数据
            var tiff = tiffs[0];
            return SliceVetically(tiff, count);
        }

        public static Bitmap ConvertTiffInfoToBitmap(DoubleByteTiffInfo info)
        {
            var result = new Bitmap(info.Width, info.Height, PixelFormat.Format24bppRgb);
            byte[] buffer8Bit = null;

            for (int i = 0; i < info.Height; i++)
            {
                Rectangle imgRect = new Rectangle(0, i, info.Width, 1);
                BitmapData imgData = result.LockBits(imgRect, ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

                if (buffer8Bit == null)
                    buffer8Bit = new byte[imgData.Stride];
                else
                    Array.Clear(buffer8Bit, 0, buffer8Bit.Length);

                for (int dst = 0, j = 0; j < info.Width; j++)
                {
                    ushort value16 = info.UshortBuffer[i][j];
                    buffer8Bit[dst++] = (byte)(value16 / 257.0 + 0.5);
                    buffer8Bit[dst++] = (byte)(value16 / 257.0 + 0.5);
                    buffer8Bit[dst++] = (byte)(value16 / 257.0 + 0.5);
                }

                Marshal.Copy(buffer8Bit, 0, imgData.Scan0, buffer8Bit.Length);
                result.UnlockBits(imgData);
            }
            return result;
        }

        #endregion
    }
}
