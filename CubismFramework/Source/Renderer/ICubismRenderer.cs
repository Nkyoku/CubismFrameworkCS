using System;
using MathNet.Numerics.LinearAlgebra.Single;

namespace CubismFramework
{
    /// <summary>
    /// レンダリングに必要なメソッドを実装したインターフェースクラス
    /// </summary>
    public abstract class ICubismRenderer
    {
        /// <summary>
        /// ブレンドモードの種類。
        /// </summary>
        public enum BlendModeType
        {
            /// <summary>
            /// 通常
            /// </summary>
            Normal,

            /// <summary>
            /// 加算
            /// </summary>
            Add,

            /// <summary>
            /// 乗算
            /// </summary>
            Multiply
        }
        
        /// <summary>
        /// 乗算済みアルファの使用の有無の設定。
        /// trueならテクスチャのアルファ値を乗算済みアルファとして扱う。
        /// </summary>
        public virtual bool UsePremultipliedAlpha { get; set; } = false;

        /// <summary>
        /// テクスチャを作成する。
        /// 作成したテクスチャはDisposeTexture()で破棄する。
        /// </summary>
        /// <param name="texture_bytes">テクスチャの元となるテクスチャファイルのバイト配列</param>
        /// <returns>作成されたテクスチャ</returns>
        public abstract ICubismTexture CreateTexture(byte[] texture_bytes);

        /// <summary>
        /// テクスチャを破棄する。
        /// </summary>
        /// <param name="texture">破棄するテクスチャ</param>
        public abstract void DisposeTexture(ICubismTexture texture);

        /// <summary>
        /// クリッピングマスクを作成する。
        /// 作成したクリッピングマスクはDisposeClippingMask()で破棄する。
        /// </summary>
        /// <returns>作成されたクリッピングマスク</returns>
        public abstract ICubismClippingMask CreateClippingMask();

        /// <summary>
        /// クリッピングマスクを破棄する。
        /// </summary>
        /// <param name="clipping_mask">破棄するクリッピングマスク</param>
        public abstract void DisposeClippingMask(ICubismClippingMask clipping_mask);

        /// <summary>
        /// モデルの描画を開始する際に呼ばれる。
        /// </summary>
        /// <param name="model_color">モデルの色。4要素のfloat配列として与える。</param>
        /// <param name="mvp_matrix">MVP行列</param>
        public abstract void StartDrawingModel(float[] model_color, Matrix mvp_matrix);
        
        /// <summary>
        /// クリッピングマスクの描画を開始する際に呼ばれる。
        /// </summary>
        public virtual void StartDrawingMask(ICubismClippingMask clipping_mask) { }

        /// <summary>
        /// クリッピングマスクを描画する。
        /// 描画パラメータはフィールド値で事前に設定しておく。
        /// </summary>
        /// <param name="texture">テクスチャ</param>
        /// <param name="vertex_buffer">頂点バッファ</param>
        /// <param name="uv_buffer">UVバッファ</param>
        /// <param name="index_buffer">インデックスバッファ</param>
        /// <param name="clipping_mask">描画先のクリッピングマスク</param>
        /// <param name="clipping_matrix">クリッピングマスクの座標系へ座標変換するための行列</param>
        public abstract void DrawMask(ICubismTexture texture, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, ICubismClippingMask clipping_mask, Matrix clipping_matrix, bool use_culling);

        /// <summary>
        /// クリッピングマスクの描画が終了した際に呼ばれる。
        /// </summary>
        public virtual void EndDrawingMask(ICubismClippingMask clipping_mask) { }

        /// <summary>
        /// メッシュを描画する。
        /// 描画パラメータはフィールド値で事前に設定しておく。
        /// </summary>
        /// <param name="vertex_buffer">頂点バッファ</param>
        /// <param name="uv_buffer">UVバッファ</param>
        /// <param name="index_buffer">インデックスバッファ</param>
        /// <param name="texture">テクスチャ</param>
        /// <param name="clipping_mask">描画に使用するクリッピングマスク。マスクが使用されないときはnullが渡される。</param>
        /// <param name="clipping_matrix">クリッピングマスクの座標系へ座標変換するための行列</param>
        public abstract void DrawMesh(ICubismTexture texture, float[] vertex_buffer, float[] uv_buffer, short[] index_buffer, ICubismClippingMask clipping_mask, Matrix clipping_matrix, BlendModeType blend_mode, bool use_culling, double opacity);
        
        /// <summary>
        /// モデルの描画が終了した際に呼ばれる。
        /// </summary>
        public virtual void EndDrawingModel() { }
    }
}
