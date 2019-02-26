using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;
using MathNet.Numerics.LinearAlgebra.Single;
using OpenGL;

namespace CubismFramework
{
    public class CubismOpenGlNetRenderer : ICubismRenderer, IDisposable
    {
        /// <summary>
        /// クリッピングマスクの幅と高さ
        /// </summary>
        private const int ClippingMaskSize = 256;

        private static float[] DefaultModelColor = new float[4] { 1.0f, 1.0f, 1.0f, 1.0f };

        public CubismOpenGlNetRenderer() { }

        ~CubismOpenGlNetRenderer()
        {
            Dispose(false);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                foreach (var clipping_mask in ClippingMasks)
                {
                    clipping_mask.Dispose();
                }
                foreach (var texture in Textures)
                {
                    texture.Dispose();
                }
                ShaderManager.Dispose();
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
        
        public override ICubismClippingMask CreateClippingMask()
        {
            var clipping_mask = new CubismOpenGlNetClippingMask(ClippingMaskSize, ClippingMaskSize);
            ClippingMasks.Add(clipping_mask);
            return clipping_mask;
        }

        public override ICubismTexture CreateTexture(byte[] texture_bytes)
        {
            var bitmap = new Bitmap(new MemoryStream(texture_bytes));
            var texture = new CubismOpenGlNetTexture(bitmap);
            Textures.Add(texture);
            return texture;
        }

        public override void DisposeClippingMask(ICubismClippingMask iclipping_mask)
        {
            var clipping_mask = (CubismOpenGlNetClippingMask)iclipping_mask;
            clipping_mask.Dispose();
            ClippingMasks.Remove(clipping_mask);
        }

        public override void DisposeTexture(ICubismTexture itexture)
        {
            var texture = (CubismOpenGlNetTexture)itexture;
            texture.Dispose();
            Textures.Remove(texture);
        }
        
        public override void StartDrawingModel(float[] model_color, Matrix mvp_matrix)
        {
            // コンテキストの状態を保存する
            State.SaveState();

            // 描画設定をする
            Gl.FrontFace(FrontFaceDirection.Ccw);
            
            Gl.Disable(EnableCap.ScissorTest);
            Gl.Disable(EnableCap.StencilTest);
            Gl.Disable(EnableCap.DepthTest);
            Gl.Enable(EnableCap.Blend);
            Gl.ColorMask(true, true, true, true);

            // 不要なバッファがバインドされていたら解除する
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            Gl.BindBuffer(BufferTarget.ArrayBuffer, 0);

            // モデルのパラメータをコピーする
            if ((model_color != null) && (model_color.Length == 4))
            {
                model_color.CopyTo(ModelColor, 0);
            }
            else
            {
                DefaultModelColor.CopyTo(ModelColor, 0);
            }
            MvpMatrix = (Matrix)mvp_matrix.Clone();
        }

        public override void StartDrawingMask(ICubismClippingMask iclipping_mask)
        {
            // クリッピングマスクをレンダリング先に設定し、ビューポートを全体にする
            var clipping_mask = (CubismOpenGlNetClippingMask)iclipping_mask;
            Gl.Viewport(0, 0, clipping_mask.Width, clipping_mask.Height);
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, clipping_mask.FrameBufferId);

            // フレームバッファをクリアする
            Gl.ClearColor(1.0f, 1.0f, 1.0f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);

            // マスクの描画用のシェーダーを設定する
            var shader = ShaderManager.ShaderForDrawMask();
            Gl.UseProgram(shader.ProgramId);

            // ブレンドモードを設定する
            Gl.BlendFuncSeparate(BlendingFactor.Zero, BlendingFactor.OneMinusSrcColor, BlendingFactor.Zero, BlendingFactor.OneMinusSrcAlpha);
        }

        public override void DrawMask(ICubismTexture itexture, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, ICubismClippingMask iclipping_mask, Matrix clipping_matrix, bool use_culling)
        {
            var texture = (CubismOpenGlNetTexture)itexture;
            var clipping_mask = (CubismOpenGlNetClippingMask)iclipping_mask;

            UseCulling = use_culling;
            
            var shader = ShaderManager.ShaderForDrawMask();
            
            // Drawableのテクスチャを設定する
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, texture.TextureId);
            Gl.Uniform1(shader.SamplerTexture0Location, 0);

            // 頂点バッファを設定する
            Gl.EnableVertexAttribArray((uint)shader.AttributePositionLocation);
            GCHandle pinned_vertex_buffer = GCHandle.Alloc(vertex_buffer, GCHandleType.Pinned);
            Gl.VertexAttribPointer((uint)shader.AttributePositionLocation, 2, VertexAttribType.Float, false, sizeof(float) * 2, pinned_vertex_buffer.AddrOfPinnedObject());

            // UVバッファを設定する
            Gl.EnableVertexAttribArray((uint)shader.AttributeTexCoordLocation);
            GCHandle pinned_uv_buffer = GCHandle.Alloc(uv_buffer, GCHandleType.Pinned);
            Gl.VertexAttribPointer((uint)shader.AttributeTexCoordLocation, 2, VertexAttribType.Float, false, sizeof(float) * 2, pinned_uv_buffer.AddrOfPinnedObject());

            // その他のパラメータを設定する
            Gl.Uniform4(shader.UnifromChannelFlagLocation, 1.0f, 0.0f, 0.0f, 0.0f);
            Gl.UniformMatrix4(shader.UniformClipMatrixLocation, false, clipping_matrix.AsColumnMajorArray());
            Gl.Uniform4(shader.UniformBaseColorLocation, -1.0f, -1.0f, 1.0f, 1.0f);
            
            // 描画する
            GCHandle pinned_index_buffer = GCHandle.Alloc(index_buffer, GCHandleType.Pinned);
            Gl.DrawElements(PrimitiveType.Triangles, index_buffer.Length, DrawElementsType.UnsignedShort, pinned_index_buffer.AddrOfPinnedObject());

            // バッファのアドレスの固定を解除する
            pinned_vertex_buffer.Free();
            pinned_uv_buffer.Free();
            pinned_index_buffer.Free();
        }

        public override void EndDrawingMask(ICubismClippingMask iclipping_mask)
        {
            // レンダリング先とビューポートをオリジナルのものに戻す
            State.RestoreFrameBuffer();
            State.RestoreViewport();
        }

        public override void DrawMesh(ICubismTexture itexture, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, ICubismClippingMask iclipping_mask, Matrix clipping_matrix, BlendModeType blend_mode, bool use_culling, double opacity)
        {
            var texture = (CubismOpenGlNetTexture)itexture;
            var clipping_mask = iclipping_mask as CubismOpenGlNetClippingMask;
            bool use_clipping_mask = (clipping_mask != null);

            UseCulling = use_culling;
            BlendMode = blend_mode;

            var shader = ShaderManager.ShaderForDrawMesh(use_clipping_mask, UsePremultipliedAlpha);
            Gl.UseProgram(shader.ProgramId);

            // 頂点バッファを設定する
            Gl.EnableVertexAttribArray((uint)shader.AttributePositionLocation);
            GCHandle pinned_vertex_buffer = GCHandle.Alloc(vertex_buffer, GCHandleType.Pinned);
            Gl.VertexAttribPointer((uint)shader.AttributePositionLocation, 2, VertexAttribType.Float, false, sizeof(float) * 2, pinned_vertex_buffer.AddrOfPinnedObject());

            // UVバッファを設定する
            Gl.EnableVertexAttribArray((uint)shader.AttributeTexCoordLocation);
            GCHandle pinned_uv_buffer = GCHandle.Alloc(uv_buffer, GCHandleType.Pinned);
            Gl.VertexAttribPointer((uint)shader.AttributeTexCoordLocation, 2, VertexAttribType.Float, false, sizeof(float) * 2, pinned_uv_buffer.AddrOfPinnedObject());

            if (use_clipping_mask == true)
            {
                Gl.ActiveTexture(TextureUnit.Texture1);
                Gl.BindTexture(TextureTarget.Texture2d, clipping_mask.TextureId);
                Gl.Uniform1(shader.SamplerTexture1Location, 1);

                // View座標をClippingContextの座標に変換するための行列を設定
                Gl.UniformMatrix4(shader.UniformClipMatrixLocation, false, clipping_matrix.AsColumnMajorArray());

                // 使用するカラーチャンネルを設定
                Gl.Uniform4(shader.UnifromChannelFlagLocation, 1.0f, 0.0f, 0.0f, 0.0f);
            }
            else
            {
                Gl.ActiveTexture(TextureUnit.Texture1);
                Gl.BindTexture(TextureTarget.Texture2d, 0);
            }

            //テクスチャ設定
            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, texture.TextureId);
            Gl.Uniform1(shader.SamplerTexture0Location, 0);

            // 座標変換
            Gl.UniformMatrix4(shader.UniformMatrixLocation, false, MvpMatrix.AsColumnMajorArray());

            // モデルの色を設定する
            float[] color = new float[4];
            ModelColor.CopyTo(color, 0);
            color[3] *= (float)opacity;
            if (UsePremultipliedAlpha == true)
            {
                color[0] *= color[3];
                color[1] *= color[3];
                color[2] *= color[3];
            }
            Gl.Uniform4(shader.UniformBaseColorLocation, color[0], color[1], color[2], color[3]);

            // 描画する
            GCHandle pinned_index_buffer = GCHandle.Alloc(index_buffer, GCHandleType.Pinned);
            Gl.DrawElements(PrimitiveType.Triangles, index_buffer.Length, DrawElementsType.UnsignedShort, pinned_index_buffer.AddrOfPinnedObject());

            // バッファのアドレスの固定を解除する
            pinned_vertex_buffer.Free();
            pinned_uv_buffer.Free();
            pinned_index_buffer.Free();
        }

        public override void EndDrawingModel()
        {
            State.RestoreState();
        }

        private BlendModeType BlendMode
        {
            set
            {
                switch (value)
                {
                case BlendModeType.Normal:
                    Gl.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.One, BlendingFactor.OneMinusSrcAlpha);
                    break;

                case BlendModeType.Add:
                    Gl.BlendFuncSeparate(BlendingFactor.One, BlendingFactor.One, BlendingFactor.Zero, BlendingFactor.One);
                    break;

                case BlendModeType.Multiply:
                    Gl.BlendFuncSeparate(BlendingFactor.DstColor, BlendingFactor.OneMinusSrcAlpha, BlendingFactor.Zero, BlendingFactor.One);
                    break;
                }
            }
        }

        private bool UseCulling
        {
            set
            {
                if (value == true)
                {
                    Gl.Enable(EnableCap.CullFace);
                }
                else
                {
                    Gl.Disable(EnableCap.CullFace);
                }
            }
        }
        
        private CubismOpenGlNetShaderManager ShaderManager = new CubismOpenGlNetShaderManager();

        private List<CubismOpenGlNetClippingMask> ClippingMasks = new List<CubismOpenGlNetClippingMask>();

        private List<CubismOpenGlNetTexture> Textures = new List<CubismOpenGlNetTexture>();

        private float[] ModelColor = new float[4];

        private Matrix MvpMatrix = null;

        /// <summary>
        /// 描画を始める前のOpenGLコンテキストの状態を保存するオブジェクト
        /// </summary>
        private CubismOpenGlNetState State = new CubismOpenGlNetState();
    }
}
