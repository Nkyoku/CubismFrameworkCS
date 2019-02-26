using System;
using System.Drawing;
using System.Drawing.Imaging;
using OpenGL;

namespace CubismFramework
{
    internal class CubismOpenGlNetTexture : ICubismTexture, IDisposable
    {
        /// <summary>
        /// サイズを指定してテクスチャを作成する。
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        public CubismOpenGlNetTexture(int width, int height)
        {
            Width = width;
            Height = height;
            TextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, TextureId);
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, width, height, 0, OpenGL.PixelFormat.Rgba, OpenGL.PixelType.UnsignedByte, IntPtr.Zero);
            SetupParameters(Gl.LINEAR, Gl.CLAMP_TO_EDGE);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
        }

        ~CubismOpenGlNetTexture()
        {
            Dispose(false);
        }

        /// <summary>
        /// ビットマップからテクスチャを作成する。
        /// ピクセルフォーマットは内部でRGBAに変換される。
        /// </summary>
        /// <param name="source_bitmap">ビットマップ</param>
        public CubismOpenGlNetTexture(Bitmap source_bitmap)
        {
            // ビットマップのフォーマットをOpenGLのピクセルフォーマットに換算する
            OpenGL.PixelFormat source_format;
            int alignment;
            switch (source_bitmap.PixelFormat)
            {
            case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                source_format = OpenGL.PixelFormat.Bgr;
                alignment = 1;
                break;
            case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                source_format = OpenGL.PixelFormat.Bgr;
                alignment = 4;
                break;
            case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                source_format = OpenGL.PixelFormat.Bgra;
                alignment = 4;
                break;
            case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                source_format = OpenGL.PixelFormat.Bgra;
                alignment = 4;
                break;
            default:
                throw new ArgumentException();
            }
            
            // テクスチャを作成し、ビットマップデータを転送する
            TextureId = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2d, TextureId);
            BitmapData data = source_bitmap.LockBits(new Rectangle(0, 0, source_bitmap.Width, source_bitmap.Height), ImageLockMode.ReadOnly, source_bitmap.PixelFormat);
            Gl.PixelStore(PixelStoreParameter.UnpackAlignment, alignment);
            //Gl.PixelStore(PixelStoreParameter.UnpackRowLength, Math.Abs(data.Stride));
            Gl.TexImage2D(TextureTarget.Texture2d, 0, InternalFormat.Rgba, data.Width, data.Height, 0, source_format, OpenGL.PixelType.UnsignedByte, data.Scan0);
            Gl.PixelStore(PixelStoreParameter.UnpackAlignment, 4);
            Gl.PixelStore(PixelStoreParameter.UnpackRowLength, 0);
            source_bitmap.UnlockBits(data);
            SetupParameters(Gl.LINEAR, Gl.CLAMP_TO_EDGE);
            Gl.BindTexture(TextureTarget.Texture2d, 0);
        }
        
        /// <summary>
        /// テクスチャのパラメータを設定する。
        /// </summary>
        /// <param name="min_mag_filter">拡大縮小に使用されるアルゴリズム</param>
        /// <param name="wrap_mode">境界処理</param>
        private void SetupParameters(int min_mag_filter, int wrap_mode)
        {
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMinFilter, ref min_mag_filter);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureMagFilter, ref min_mag_filter);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapS, ref wrap_mode);
            Gl.TexParameteri(TextureTarget.Texture2d, TextureParameterName.TextureWrapT, ref wrap_mode);
        }
        
        public uint TextureId { get; private set; } = 0;

        public int Width { get; private set; } = 0;

        public int Height { get; private set; } = 0;

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Gl.DeleteTextures(new uint[] { TextureId });
                TextureId = 0;
                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
