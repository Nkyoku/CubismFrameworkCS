using System;
using OpenGL;

namespace CubismFramework
{
    class CubismOpenGlNetClippingMask : ICubismClippingMask, IDisposable
    {
        /// <summary>
        /// クリッピングマスクの描画のためのフレームバッファを作成する。
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        public CubismOpenGlNetClippingMask(int width, int height)
        {
            // テクスチャを生成する
            Texture = new CubismOpenGlNetTexture(width, height);

            // フレームバッファを生成し、テクスチャを割り当てる
            uint[] fbos = new uint[1];
            Gl.GenFramebuffers(fbos);
            FrameBufferId = fbos[0];
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferId);
            Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureId, 0);
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        ~CubismOpenGlNetClippingMask()
        {
            Dispose(false);
        }

        /// <summary>
        /// フレームバッファのサイズを変更する。
        /// </summary>
        /// <param name="width">幅</param>
        /// <param name="height">高さ</param>
        public void Resize(int width, int height)
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, FrameBufferId);
            Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, 0, 0);
            Texture.Dispose();
            Texture = new CubismOpenGlNetTexture(width, height);
            Gl.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureId, 0);
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }
        
        public uint FrameBufferId { get; private set; } = 0;

        private CubismOpenGlNetTexture Texture;

        public uint TextureId
        {
            get { return Texture.TextureId; }
        }
        
        public int Width
        {
            get { return Texture.Width; }
        }

        public int Height
        {
            get { return Texture.Height; }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Gl.DeleteFramebuffers(new uint[1] { FrameBufferId });
                FrameBufferId = 0;
                Texture.Dispose();
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
