using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenGL;

namespace CubismFramework
{
    internal class CubismOpenGlNetShaderManager : IDisposable
    {
        /// <summary>
        /// エラー文字列の最大長
        /// </summary>
        private const int MaxErrorLength = 1024;

        /// <summary>
        /// シェーダープログラムを作成する。
        /// </summary>
        public CubismOpenGlNetShaderManager()
        {
            MaskDrawingShader = new SetupMaskShaderProgram();
            UnmaskedMeshDrawingShader = new UnmaskedShaderProgram();
            MaskedMeshDrawingShader = new MaskedShaderProgram();
            UnmaskedPremultipliedAlphaMeshDrawingShader = new UnmaskedPremultipliedAlphaShaderProgram();
            MaskedPremultipliedAlphaMeshDrawingShader = new MaskedPremultipliedAlphaShaderProgram();
        }

        ~CubismOpenGlNetShaderManager()
        {
            Dispose(false);
        }
        
        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                MaskDrawingShader.Dispose();
                UnmaskedMeshDrawingShader.Dispose();
                MaskedMeshDrawingShader.Dispose();
                UnmaskedPremultipliedAlphaMeshDrawingShader.Dispose();
                MaskedPremultipliedAlphaMeshDrawingShader.Dispose();
                disposedValue = true;
            }
        }
        
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        /// <summary>
        /// マスクの描画に使用するシェーダープログラムを取得する。
        /// </summary>
        /// <returns>シェーダープログラム</returns>
        public GlShaderProgram ShaderForDrawMask()
        {
            return MaskDrawingShader;
        }

        /// <summary>
        /// メッシュの描画に使用するシェーダープログラムを取得する。
        /// </summary>
        /// <param name="use_clipping_mask">trueならクリッピングマスクを使用して描画する</param>
        /// <param name="use_premultipled_alpha">trueならテクスチャを乗算済みアルファ形式として扱う</param>
        /// <returns>シェーダープログラム</returns>
        public GlShaderProgram ShaderForDrawMesh(bool use_clipping_mask, bool use_premultipled_alpha)
        {
            if (use_clipping_mask == false)
            {
                if (use_premultipled_alpha == false)
                {
                    return UnmaskedMeshDrawingShader;
                }
                else
                {
                    return UnmaskedPremultipliedAlphaMeshDrawingShader;
                }
            }
            else
            {
                if (use_premultipled_alpha == false)
                {
                    return MaskedMeshDrawingShader;
                }
                else
                {
                    return MaskedPremultipliedAlphaMeshDrawingShader;
                }
            }
        }
        
        private GlShaderProgram MaskDrawingShader = null;
        private GlShaderProgram UnmaskedMeshDrawingShader = null;
        private GlShaderProgram MaskedMeshDrawingShader = null;
        private GlShaderProgram UnmaskedPremultipliedAlphaMeshDrawingShader = null;
        private GlShaderProgram MaskedPremultipliedAlphaMeshDrawingShader = null;
        
        /// <summary>
        /// シェーダーを管理するクラス
        /// </summary>
        private class GlShader : IDisposable
        {
            public GlShader(ShaderType shaderType, string[] source)
            {
                if (source == null)
                {
                    throw new ArgumentNullException(nameof(source));
                }
                ShaderId = Gl.CreateShader(shaderType);
                Gl.ShaderSource(ShaderId, source);
                Gl.CompileShader(ShaderId);
                Gl.GetShader(ShaderId, ShaderParameterName.CompileStatus, out int compile_succeeded);
                if (compile_succeeded == 0)
                {
                    StringBuilder log = new StringBuilder(MaxErrorLength);
                    Gl.GetShaderInfoLog(ShaderId, MaxErrorLength, out int log_length, log);
                    throw new InvalidOperationException($"Failed to compile shader : {log}");
                }
            }

            ~GlShader()
            {
                Dispose(false);
            }
            
            public readonly uint ShaderId;

            #region IDisposable Support
            private bool disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    Gl.DeleteShader(ShaderId);
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
            #endregion
        }

        /// <summary>
        /// シェーダープログラムを管理するクラス
        /// </summary>
        internal class GlShaderProgram : IDisposable
        {
            public GlShaderProgram(string vertex_shader_source, string fragment_shader_source)
                : this(new string[1] { vertex_shader_source }, new string[1] { fragment_shader_source }) { }

            public GlShaderProgram(string[] vertex_shader_source, string[] fragment_shader_source)
            {
                using (GlShader vertex_shader = new GlShader(ShaderType.VertexShader, vertex_shader_source))
                using (GlShader fragment_shader = new GlShader(ShaderType.FragmentShader, fragment_shader_source))
                {
                    ProgramId = Gl.CreateProgram();
                    Gl.AttachShader(ProgramId, vertex_shader.ShaderId);
                    Gl.AttachShader(ProgramId, fragment_shader.ShaderId);
                    Gl.LinkProgram(ProgramId);
                    Gl.GetProgram(ProgramId, ProgramProperty.LinkStatus, out int link_succeeded);
                    if (link_succeeded == 0)
                    {
                        StringBuilder log = new StringBuilder(MaxErrorLength);
                        Gl.GetProgramInfoLog(ProgramId, MaxErrorLength, out int log_length, log);
                        throw new InvalidOperationException($"Failed to link program : {log}");
                    }
                }
            }

            ~GlShaderProgram()
            {
                Dispose(false);
            }

            public int AttributeLocation(string attribute_name)
            {
                int location = Gl.GetAttribLocation(ProgramId, attribute_name);
                if (location < 0)
                {
                    throw new InvalidOperationException($"No attribute {attribute_name}");
                }
                return location;
            }

            public int UniformLocation(string uniform_name)
            {
                int location = Gl.GetUniformLocation(ProgramId, uniform_name);
                if (location < 0)
                {
                    throw new InvalidOperationException($"No uniform {uniform_name}");
                }
                return location;
            }

            public uint ProgramId { get; private set; }
            
            public int AttributePositionLocation { get; protected set; } = -1;
            public int AttributeTexCoordLocation { get; protected set; } = -1;
            public int SamplerTexture0Location { get; protected set; } = -1;
            public int SamplerTexture1Location { get; protected set; } = -1;
            public int UniformMatrixLocation { get; protected set; } = -1;
            public int UniformClipMatrixLocation { get; protected set; } = -1;
            public int UnifromChannelFlagLocation { get; protected set; } = -1;
            public int UniformBaseColorLocation { get; protected set; } = -1;

            #region IDisposable Support
            private bool disposedValue = false;

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    Gl.DeleteProgram(ProgramId);
                    ProgramId = 0;
                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                Dispose(true);
            }
            #endregion
        }
        
        internal class SetupMaskShaderProgram : GlShaderProgram
        {
            public SetupMaskShaderProgram() : base(VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                UniformClipMatrixLocation = UniformLocation("u_clipMatrix");
                UnifromChannelFlagLocation = UniformLocation("u_channelFlag");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
            
            private static string[] VertexShaderSource = {
                "attribute vec4 a_position;",
                "attribute vec2 a_texCoord;",
                "varying vec2 v_texCoord;",
                "varying vec4 v_myPos;",
                "uniform mat4 u_clipMatrix;",
                "void main()",
                "{",
                "gl_Position = u_clipMatrix * a_position;",
                "v_myPos = u_clipMatrix * a_position;",
                "v_texCoord = a_texCoord;",
                "v_texCoord.y = 1.0 - v_texCoord.y;",
                "}"
            };

            private static string[] FragmentShaderSource =
            {
                "precision mediump float;",
                "varying vec2 v_texCoord;",
                "varying vec4 v_myPos;",
                "uniform sampler2D s_texture0;",
                "uniform vec4 u_channelFlag;",
                "uniform vec4 u_baseColor;",
                "void main()",
                "{",
                "float isInside = ",
                "  step(u_baseColor.x, v_myPos.x/v_myPos.w)",
                "* step(u_baseColor.y, v_myPos.y/v_myPos.w)",
                "* step(v_myPos.x/v_myPos.w, u_baseColor.z)",
                "* step(v_myPos.y/v_myPos.w, u_baseColor.w);",
                "gl_FragColor = u_channelFlag * texture2D(s_texture0 , v_texCoord).a * isInside;",
                "}"
            };
        }

        internal class UnmaskedShaderProgram : GlShaderProgram
        {
            public UnmaskedShaderProgram() : base(VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                UniformMatrixLocation = UniformLocation("u_matrix");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
            
            private static string[] VertexShaderSource = {
                "attribute vec4 a_position;",
                "attribute vec2 a_texCoord;",
                "varying vec2 v_texCoord;",
                "uniform mat4 u_matrix;",
                "void main()",
                "{",
                "gl_Position = u_matrix * a_position;",
                "v_texCoord = a_texCoord;",
                "v_texCoord.y = 1.0 - v_texCoord.y;",
                "}"
            };

            private static string[] FragmentShaderSource =
            {
                "precision mediump float;",
                "varying vec2 v_texCoord;",
                "uniform sampler2D s_texture0;",
                "uniform vec4 u_baseColor;",
                "void main()",
                "{",
                "vec4 color = texture2D(s_texture0 , v_texCoord) * u_baseColor;",
                "gl_FragColor = vec4(color.rgb * color.a,  color.a);",
                "}"
            };
        }

        internal class MaskedShaderProgram : GlShaderProgram
        {
            public MaskedShaderProgram() : base(VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                SamplerTexture1Location = UniformLocation("s_texture1");
                UniformMatrixLocation = UniformLocation("u_matrix");
                UniformClipMatrixLocation = UniformLocation("u_clipMatrix");
                UnifromChannelFlagLocation = UniformLocation("u_channelFlag");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
            
            private static string[] VertexShaderSource = {
                "attribute vec4 a_position;",
                "attribute vec2 a_texCoord;",
                "varying vec2 v_texCoord;",
                "varying vec4 v_clipPos;",
                "uniform mat4 u_matrix;",
                "uniform mat4 u_clipMatrix;",
                "void main()",
                "{",
                "gl_Position = u_matrix * a_position;",
                "v_clipPos = u_clipMatrix * a_position;",
                "v_texCoord = a_texCoord;",
                "v_texCoord.y = 1.0 - v_texCoord.y;",
                "}"
            };

            private static string[] FragmentShaderSource =
            {
                "varying vec2 v_texCoord;",
                "varying vec4 v_clipPos;",
                "uniform sampler2D s_texture0;",
                "uniform sampler2D s_texture1;",
                "uniform vec4 u_channelFlag;",
                "uniform vec4 u_baseColor;",
                "void main()",
                "{",
                "vec4 col_formask = texture2D(s_texture0 , v_texCoord) * u_baseColor;",
                "col_formask.rgb = col_formask.rgb  * col_formask.a ;",
                "vec4 clipMask = (1.0 - texture2D(s_texture1, v_clipPos.xy / v_clipPos.w)) * u_channelFlag;",
                "float maskVal = clipMask.r + clipMask.g + clipMask.b + clipMask.a;",
                "col_formask = col_formask * maskVal;",
                "gl_FragColor = col_formask;",
                "}"
            };
        }

        internal class UnmaskedPremultipliedAlphaShaderProgram : GlShaderProgram
        {
            public UnmaskedPremultipliedAlphaShaderProgram() : base(VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                UniformMatrixLocation = UniformLocation("u_matrix");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
            
            private static string[] VertexShaderSource = {
                "attribute vec4 a_position;",
                "attribute vec2 a_texCoord;",
                "varying vec2 v_texCoord;",
                "uniform mat4 u_matrix;",
                "void main()",
                "{",
                "gl_Position = u_matrix * a_position;",
                "v_texCoord = a_texCoord;",
                "v_texCoord.y = 1.0 - v_texCoord.y;",
                "}"
            };

            private static string[] FragmentShaderSource =
            {
                "precision mediump float;",
                "varying vec2 v_texCoord;",
                "uniform sampler2D s_texture0;",
                "uniform vec4 u_baseColor;",
                "void main()",
                "{",
                "gl_FragColor = texture2D(s_texture0 , v_texCoord) * u_baseColor;",
                "}"
            };
        }

        internal class MaskedPremultipliedAlphaShaderProgram : GlShaderProgram
        {
            public MaskedPremultipliedAlphaShaderProgram() : base(VertexShaderSource, FragmentShaderSource)
            {
                AttributePositionLocation = AttributeLocation("a_position");
                AttributeTexCoordLocation = AttributeLocation("a_texCoord");
                SamplerTexture0Location = UniformLocation("s_texture0");
                SamplerTexture1Location = UniformLocation("s_texture1");
                UniformMatrixLocation = UniformLocation("u_matrix");
                UniformClipMatrixLocation = UniformLocation("u_clipMatrix");
                UnifromChannelFlagLocation = UniformLocation("u_channelFlag");
                UniformBaseColorLocation = UniformLocation("u_baseColor");
            }
            
            private static string[] VertexShaderSource = {
                "attribute vec4 a_position;",
                "attribute vec2 a_texCoord;",
                "varying vec2 v_texCoord;",
                "varying vec4 v_clipPos;",
                "uniform mat4 u_matrix;",
                "uniform mat4 u_clipMatrix;",
                "void main()",
                "{",
                "gl_Position = u_matrix * a_position;",
                "v_clipPos = u_clipMatrix * a_position;",
                "v_texCoord = a_texCoord;",
                "v_texCoord.y = 1.0 - v_texCoord.y;",
                "}"
            };

            private static string[] FragmentShaderSource =
            {
                "precision mediump float;",
                "varying vec2 v_texCoord;",
                "varying vec4 v_clipPos;",
                "uniform sampler2D s_texture0;",
                "uniform sampler2D s_texture1;",
                "uniform vec4 u_channelFlag;",
                "uniform vec4 u_baseColor;",
                "void main()",
                "{",
                "vec4 col_formask = texture2D(s_texture0 , v_texCoord) * u_baseColor;",
                "vec4 clipMask = (1.0 - texture2D(s_texture1, v_clipPos.xy / v_clipPos.w)) * u_channelFlag;",
                "float maskVal = clipMask.r + clipMask.g + clipMask.b + clipMask.a;",
                "col_formask = col_formask * maskVal;",
                "gl_FragColor = col_formask;",
                "}"
            };
        }
    }
}
