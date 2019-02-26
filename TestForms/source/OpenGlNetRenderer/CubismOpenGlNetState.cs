using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;

namespace CubismFramework
{
    internal class CubismOpenGlNetState
    {
        /// <summary>
        /// OpenGLの状態を保存する。
        /// </summary>
        public void SaveState()
        {
            Gl.Get(Gl.ARRAY_BUFFER_BINDING, out LastArrayBufferBinding);
            Gl.Get(Gl.ELEMENT_ARRAY_BUFFER_BINDING, out LastElementArrayBufferBinding);
            Gl.Get(Gl.CURRENT_PROGRAM, out LastProgram);

            Gl.Get(Gl.ACTIVE_TEXTURE, out LastActiveTexture);
            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.Get(Gl.TEXTURE_BINDING_2D, out LastTexture1Binding2D);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.Get(Gl.TEXTURE_BINDING_2D, out LastTexture0Binding2D);
            
            Gl.GetVertexAttrib(0, Gl.VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[0]);
            Gl.GetVertexAttrib(1, Gl.VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[1]);
            Gl.GetVertexAttrib(2, Gl.VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[2]);
            Gl.GetVertexAttrib(3, Gl.VERTEX_ATTRIB_ARRAY_ENABLED, out LastVertexAttribArrayEnabled[3]);

            LastScissorTest = Gl.IsEnabled(EnableCap.ScissorTest);
            LastStencilTest = Gl.IsEnabled(EnableCap.StencilTest);
            LastDepthTest = Gl.IsEnabled(EnableCap.DepthTest);
            LastCullFace = Gl.IsEnabled(EnableCap.CullFace);
            LastBlend = Gl.IsEnabled(EnableCap.Blend);

            Gl.Get(Gl.FRONT_FACE, out LastFrontFace);

            Gl.Get(Gl.COLOR_WRITEMASK, LastColorMask);
            
            Gl.Get(Gl.BLEND_SRC_RGB, out LastBlending[0]);
            Gl.Get(Gl.BLEND_DST_RGB, out LastBlending[1]);
            Gl.Get(Gl.BLEND_SRC_ALPHA, out LastBlending[2]);
            Gl.Get(Gl.BLEND_DST_ALPHA, out LastBlending[3]);

            Gl.Get(Gl.FRAMEBUFFER_BINDING, out LastFrameBuffer);
            Gl.Get(Gl.VIEWPORT, LastViewport);
        }
        
        /// <summary>
        /// 保存した状態を復帰する。
        /// </summary>
        public void RestoreState()
        {
            Gl.UseProgram((uint)LastProgram);

            SetEnabledVertexAttribArray(0, LastVertexAttribArrayEnabled[0] != 0);
            SetEnabledVertexAttribArray(1, LastVertexAttribArrayEnabled[1] != 0);
            SetEnabledVertexAttribArray(2, LastVertexAttribArrayEnabled[2] != 0);
            SetEnabledVertexAttribArray(3, LastVertexAttribArrayEnabled[3] != 0);

            SetEnabled(EnableCap.ScissorTest, LastScissorTest);
            SetEnabled(EnableCap.StencilTest, LastStencilTest);
            SetEnabled(EnableCap.DepthTest, LastDepthTest);
            SetEnabled(EnableCap.CullFace, LastCullFace);
            SetEnabled(EnableCap.Blend, LastBlend);
            
            Gl.FrontFace((FrontFaceDirection)LastFrontFace);

            Gl.ColorMask(LastColorMask[0] != 0, LastColorMask[1] != 0, LastColorMask[2] != 0, LastColorMask[3] != 0);

            Gl.BindBuffer(BufferTarget.ArrayBuffer, (uint)LastArrayBufferBinding);
            Gl.BindBuffer(BufferTarget.ElementArrayBuffer, (uint)LastElementArrayBufferBinding);

            Gl.ActiveTexture(TextureUnit.Texture1);
            Gl.BindTexture(TextureTarget.Texture2d, (uint)LastTexture1Binding2D);

            Gl.ActiveTexture(TextureUnit.Texture0);
            Gl.BindTexture(TextureTarget.Texture2d, (uint)LastTexture0Binding2D);

            Gl.ActiveTexture((TextureUnit)LastActiveTexture);
            
            Gl.BlendFuncSeparate((BlendingFactor)LastBlending[0], (BlendingFactor)LastBlending[1], (BlendingFactor)LastBlending[2], (BlendingFactor)LastBlending[3]);

            RestoreViewport();
            RestoreFrameBuffer();
        }

        /// <summary>
        /// ビューポートの状態を復帰する。
        /// </summary>
        public void RestoreViewport()
        {
            Gl.Viewport(LastViewport[0], LastViewport[1], LastViewport[2], LastViewport[3]);
        }

        /// <summary>
        /// フレームバッファの状態を復帰する。
        /// </summary>
        public void RestoreFrameBuffer()
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)LastFrameBuffer);
        }

        private static void SetEnabled(EnableCap cap, bool enabled)
        {
            if (enabled == true)
            {
                Gl.Enable(cap);
            }
            else
            {
                Gl.Disable(cap);
            }
        }

        private static void SetEnabledVertexAttribArray(int index, bool enabled)
        {
            if (enabled == true)
            {
                Gl.EnableVertexAttribArray((uint)index);
            }
            else
            {
                Gl.DisableVertexAttribArray((uint)index);
            }
        }
        
        private int LastArrayBufferBinding;
        private int LastElementArrayBufferBinding;
        private int LastProgram;
        private int LastActiveTexture;
        private int LastTexture0Binding2D;
        private int LastTexture1Binding2D;
        private int[] LastVertexAttribArrayEnabled = new int[4];
        private bool LastScissorTest;
        private bool LastBlend;
        private bool LastStencilTest;
        private bool LastDepthTest;
        private bool LastCullFace;
        private int LastFrontFace;
        private int[] LastColorMask = new int[4];
        private int[] LastBlending = new int[4];
        private int LastFrameBuffer;
        private int[] LastViewport = new int[4];
    }
}
