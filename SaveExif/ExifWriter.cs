using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Reflection;
using System.Text;

namespace SaveExif
{
    // Exifのtag idの参考: https://exiftool.org/TagNames/EXIF.html
    public class ExifWriter
    {
        private Image _image;

        public ExifWriter(Image image)
        {
            _image = image;
        }

        // PropertyItemのコンストラクタが公開されてないので無理やりアクセス
        private static readonly ConstructorInfo propertyItemCtor = typeof(PropertyItem).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null);
        internal PropertyItem NewPropertyItem()
        {
            return (PropertyItem)propertyItemCtor.Invoke(null);
        }

        internal PropertyItem MakeProperty(int id, short type, byte[] value)
        {
            var p = NewPropertyItem();
            p.Id = id;
            p.Type = type;
            p.Value = value;
            p.Len = value.Length;
            return p;
        }

        internal PropertyItem MakeAsciiProperty(int id, string text)
        {
            return MakeProperty(id, 2, Encoding.ASCII.GetBytes($"{text}\0"));
        }

        internal PropertyItem MakeUnicodeProperty(int id, string text)
        {
            var header = new byte[] { 0x55, 0x4E, 0x49, 0x43, 0x4F, 0x44, 0x45, 0x0 };
            var textBytes = Encoding.Unicode.GetBytes(text);
            var b = new byte[header.Length + textBytes.Length];
            Buffer.BlockCopy(header, 0, b, 0, header.Length);
            Buffer.BlockCopy(textBytes, 0, b, header.Length, textBytes.Length);
            return MakeProperty(id, 7, b);
        }

        /// <summary>
        /// カメラのモデル
        /// </summary>
        /// <param name="value"></param>
        public void SetModel(string value)
        {
            _image.SetPropertyItem(MakeAsciiProperty(0x0110, value));
        }

        /// <summary>
        /// カメラのメーカー
        /// </summary>
        /// <param name="value"></param>
        public void SetMake(string value)
        {
            _image.SetPropertyItem(MakeAsciiProperty(0x010f, value));
        }

        /// <summary>
        /// 撮影日時
        /// </summary>
        /// <param name="value"></param>
        public void SetDateTimeOriginal(DateTime value)
        {
            _image.SetPropertyItem(MakeAsciiProperty(0x9003, value.ToLocalTime().ToString("yyyy:MM:dd HH:mm:ss")));
        }

        /// <summary>
        /// 写真の説明・タイトル
        /// </summary>
        /// <param name="value"></param>
        public void SetDescription(string value)
        {
            _image.SetPropertyItem(MakeAsciiProperty(0x010E, value));
        }

        /// <summary>
        /// 撮影者
        /// </summary>
        /// <param name="value"></param>
        public void SetArtist(string value)
        {
            _image.SetPropertyItem(MakeAsciiProperty(0x013B, value));
        }

        /// <summary>
        /// 写真を処理したソフト？
        /// </summary>
        /// <param name="value"></param>
        public void SetSoftware(string value)
        {
            _image.SetPropertyItem(MakeAsciiProperty(0x0131, value));
        }

        /// <summary>
        /// 自由記入欄。ここは日本語おｋ。
        /// </summary>
        /// <param name="unicodeValue"></param>
        public void SetUserComment(string unicodeValue)
        {
            _image.SetPropertyItem(MakeUnicodeProperty(0x9286, unicodeValue));
        }
    }
}
