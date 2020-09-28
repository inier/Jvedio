using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Jvedio
{
    public static class ImageProcess
    {

        public static System.Drawing.Bitmap byteArrayToImage(byte[] byteArrayIn)
        {
            MemoryStream ms = new MemoryStream(byteArrayIn);
            System.Drawing.Image returnImage = System.Drawing.Image.FromStream(ms);
            System.Drawing.Bitmap bitmap = (System.Drawing.Bitmap)returnImage;
            return bitmap;
        }

        public static byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }

        public static string ImageToBase64(Bitmap bitmap, string fileFullName = "")
        {
            try
            {
                if (fileFullName != "")
                {
                    Bitmap bmp = new Bitmap(fileFullName);
                    MemoryStream ms = new MemoryStream();
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] arr = new byte[ms.Length]; ms.Position = 0;
                    ms.Read(arr, 0, (int)ms.Length); ms.Close();
                    return Convert.ToBase64String(arr);
                }
                else
                {
                    MemoryStream ms = new MemoryStream();
                    bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                    byte[] arr = new byte[ms.Length]; ms.Position = 0;
                    ms.Read(arr, 0, (int)ms.Length); ms.Close();
                    return Convert.ToBase64String(arr);
                }

            }
            catch
            {

                return null;
            }
        }

        public static Bitmap Base64ToBitmap(string base64)
        {
            base64 = base64.Replace("data:image/png;base64,", "").Replace("data:image/jgp;base64,", "").Replace("data:image/jpg;base64,", "").Replace("data:image/jpeg;base64,", "");//将base64头部信息替换
            byte[] bytes = Convert.FromBase64String(base64);
            MemoryStream memStream = new MemoryStream(bytes);
            Image mImage = Image.FromStream(memStream);
            Bitmap bp = new Bitmap(mImage);
            return bp;
            //bp.Save("C:/Users/Administrator/Desktop/" + DateTime.Now.ToString("yyyyMMddHHss") + ".jpg", System.Drawing.Imaging.ImageFormat.Jpeg);//注意保存路径
        }

        public static Int32Rect GetActressRect(BitmapSource bitmapSource, Int32Rect int32Rect)
        {
            if (bitmapSource.PixelWidth > 125 && bitmapSource.PixelHeight > 125)
            {
                int width = 250;
                int y = int32Rect.Y + (int32Rect.Height / 2) - width / 2; ;
                int x = int32Rect.X + (int32Rect.Width / 2) - width / 2;
                if (x < 0) x = 0;
                if (y < 0) y = 0;
                if (x + width > bitmapSource.PixelWidth) x = bitmapSource.PixelWidth - width;
                if (y + width > bitmapSource.PixelHeight) y = bitmapSource.PixelHeight - width;
                return new Int32Rect(x, y, width, width);
            }
            else
                return Int32Rect.Empty;

        }

        public static Int32Rect GetRect(BitmapSource bitmapSource, Int32Rect int32Rect)
        {
            // 150*200
            if (bitmapSource.PixelWidth >= bitmapSource.PixelHeight)
            {
                int y = 0;
                int width = (int)(0.75 * bitmapSource.PixelHeight);
                int x = int32Rect.X + (int32Rect.Width / 2) - width / 2;
                int height = bitmapSource.PixelHeight;
                if (x < 0) x = 0;
                if (x + width > bitmapSource.PixelWidth) x = bitmapSource.PixelWidth - width;
                return new Int32Rect(x, y, width, height);
            }
            else
            {
                int x = 0;
                int height = (int)(0.75 * bitmapSource.PixelWidth);
                int y = int32Rect.Y + (int32Rect.Height / 2) - height / 2;
                int width = bitmapSource.PixelWidth;
                if (y < 0) y = 0;
                if (y + height > bitmapSource.PixelHeight) x = bitmapSource.PixelHeight - height;
                return new Int32Rect(x, y, width, height);
            }

        }

        public static BitmapSource CutImage(BitmapSource bitmapSource, Int32Rect cut)
        {
            //计算Stride
            var stride = bitmapSource.Format.BitsPerPixel * cut.Width / 8;
            byte[] data = new byte[cut.Height * stride];
            bitmapSource.CopyPixels(cut, data, stride, 0);
            return BitmapSource.Create(cut.Width, cut.Height, 0, 0, PixelFormats.Bgr32, null, data, stride);
        }

        public static Bitmap ImageSourceToBitmap(ImageSource imageSource)
        {
            BitmapSource m = (BitmapSource)imageSource;
            Bitmap bmp = new System.Drawing.Bitmap(m.PixelWidth, m.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp.LockBits(
            new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp.Size), ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            m.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride); bmp.UnlockBits(data);
            return bmp;
        }

        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                bitmap.Save(stream, ImageFormat.Jpeg);
                stream.Position = 0;
                BitmapImage result = new BitmapImage();
                result.BeginInit();
                result.CacheOption = BitmapCacheOption.OnLoad;
                result.StreamSource = stream;
                result.EndInit();
                result.Freeze();

                return result;
            }
        }

    }
}
