using System;
using System.Drawing;

namespace CubismFramework
{
    /// <summary>
    /// Drawableへのアクセスオブジェクト
    /// </summary>
    public class CubismDrawable
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        internal CubismDrawable(int drawable_index, string name, int texture_number, CubismCore.ConstantDrawableFlags constant_flags, int[] clipping_masks)
        {
            Index = drawable_index;
            Name = name;
            TextureIndex = texture_number;
            UseCulling = ((constant_flags & CubismCore.ConstantDrawableFlags.IsDoubleSided) == 0);
            BlendMode = ConvertBlendMode(constant_flags);
            ClippingMaskIndexes = clipping_masks;
        }
        
        /// <summary>
        /// Drawableの内部情報を更新する。
        /// </summary>
        internal void Update(double opacity, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, CubismCore.DynamicDrawableFlags dynamic_flags)
        {
            // 不透明度を更新する
            Opacity = opacity;

            // バッファを更新する
            VertexBuffer = vertex_buffer;
            UvBuffer = uv_buffer;
            IndexBuffer = index_buffer;
            if (VertexBuffer.Length != UvBuffer.Length)
            {
                throw new ArgumentException();
            }

            // 動的フラグを更新する
            Visible = ((dynamic_flags & CubismCore.DynamicDrawableFlags.IsVisible) != 0);
            VisibilityChanged = ((dynamic_flags & CubismCore.DynamicDrawableFlags.VisibilityDidChange) != 0);
            OpacityChanged = ((dynamic_flags & CubismCore.DynamicDrawableFlags.OpacityDidChange) != 0);
            DrawOrderChanged = ((dynamic_flags & CubismCore.DynamicDrawableFlags.DrawOrderDidChange) != 0);
            RenderOrderChanged = ((dynamic_flags & CubismCore.DynamicDrawableFlags.RenderOrderDidChange) != 0);
            VertexPositionsChanged = ((dynamic_flags & CubismCore.DynamicDrawableFlags.VertexPositionsDidChange) != 0);

            // バウンディングボックスをクリアする
            if (VertexPositionsChanged == true)
            {
                BoundingBoxInternal.X = 0.0f;
                BoundingBoxInternal.Y = 0.0f;
                BoundingBoxInternal.Width = 0.0f;
                BoundingBoxInternal.Height = 0.0f;
            }
        }
        
        /// <summary>
        /// ブレンドモードを内部形式に変換する。
        /// </summary>
        private static ICubismRenderer.BlendModeType ConvertBlendMode(CubismCore.ConstantDrawableFlags flags)
        {
            if ((flags & CubismCore.ConstantDrawableFlags.BlendAdditive) != 0)
            {
                return ICubismRenderer.BlendModeType.Add;
            }
            else if ((flags & CubismCore.ConstantDrawableFlags.BlendMultiplicative) != 0)
            {
                return ICubismRenderer.BlendModeType.Multiply;
            }
            return ICubismRenderer.BlendModeType.Normal;
        }
        
        /// <summary>
        /// DrawableIndex
        /// </summary>
        public int Index { get; private set; } = -1;

        /// <summary>
        /// Drawable名
        /// </summary>
        public string Name { get; private set; } = null;

        /// <summary>
        /// テクスチャ番号
        /// </summary>
        public int TextureIndex { get; private set; } = -1;

        /// <summary>
        /// クリッピングマスクとなるDrawableのインデックスのリスト
        /// </summary>
        public int[] ClippingMaskIndexes { get; private set; } = null;

        /// <summary>
        /// 頂点バッファ
        /// </summary>
        public float[] VertexBuffer { get; private set; } = null;

        /// <summary>
        /// 頂点のUVバッファ
        /// </summary>
        public float[] UvBuffer { get; private set; } = null;

        /// <summary>
        /// インデックスバッファ
        /// </summary>
        public short[] IndexBuffer { get; private set; } = null;

        /// <summary>
        /// 不透明度
        /// </summary>
        public double Opacity { get; private set; } = 1.0;

        /// <summary>
        /// 背面カリングの使用
        /// </summary>
        public bool UseCulling { get; private set; } = false;

        /// <summary>
        /// ブレンドモード
        /// </summary>
        public ICubismRenderer.BlendModeType BlendMode { get; private set; } = ICubismRenderer.BlendModeType.Normal;

        /// <summary>
        /// 可視性
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// 可視性が変化した。
        /// </summary>
        public bool VisibilityChanged { get; private set; } = false;

        /// <summary>
        /// 不透明度が変化した。
        /// </summary>
        public bool OpacityChanged { get; private set; } = false;

        /// <summary>
        /// 描画順が変化した。
        /// </summary>
        public bool DrawOrderChanged { get; private set; } = false;

        /// <summary>
        /// 描画順序が変化した。
        /// </summary>
        public bool RenderOrderChanged { get; private set; } = false;

        /// <summary>
        /// 頂点座標が変化した。
        /// </summary>
        public bool VertexPositionsChanged { get; private set; } = false;

        /// <summary>
        /// Drawableを囲む矩形。
        /// 頂点座標の更新後に初めてアクセスされたときに計算される。
        /// </summary>
        public RectangleF BoundingBox
        {
            get
            {
                if (BoundingBoxInternal.IsEmpty == true)
                {
                    // バウンディングボックスを計算する
                    float[] vertex_buffer = VertexBuffer;
                    int vertex_count = vertex_buffer.Length / 2;
                    float min_x = float.MaxValue, min_y = float.MaxValue;
                    float max_x = float.MinValue, max_y = float.MinValue;
                    for (int vertex_index = 0; vertex_index < vertex_count; vertex_index++)
                    {
                        float x = vertex_buffer[2 * vertex_index];
                        float y = vertex_buffer[2 * vertex_index + 1];
                        min_x = Math.Min(min_x, x);
                        max_x = Math.Max(max_x, x);
                        min_y = Math.Min(min_y, y);
                        max_y = Math.Max(max_y, y);
                    }
                    if ((min_x < max_x) && (min_y < max_y))
                    {
                        BoundingBoxInternal.X = min_x;
                        BoundingBoxInternal.Y = min_y;
                        BoundingBoxInternal.Width = max_x - min_x;
                        BoundingBoxInternal.Height = max_y - min_y;
                    }
                }
                return BoundingBoxInternal;
            }
            set { BoundingBoxInternal = value; }
        }
        private RectangleF BoundingBoxInternal = RectangleF.Empty;
    }
}
