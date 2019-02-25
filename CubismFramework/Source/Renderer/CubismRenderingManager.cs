using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using MathNet.Numerics.LinearAlgebra.Single;

namespace CubismFramework
{
    /// <summary>
    /// モデルを描画するアルゴリズムを実装したクラス
    /// </summary>
    public class CubismRenderingManager : IDisposable
    {
        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="renderer">描画に使用するレンダラー</param>
        /// <param name="asset">描画するモデルのアセット</param>
        public CubismRenderingManager(ICubismRenderer renderer, CubismAsset asset)
        {
            Renderer = renderer;
            Asset = asset;
            Model = Asset.Model;

            // モデルのテクスチャをレンダラーにコピーする
            RendererTextures = new ICubismTexture[Asset.TextureByteArrays.Length];
            for (int index = 0; index < Asset.TextureByteArrays.Length; index++)
            {
                RendererTextures[index] = Renderer.CreateTexture(Asset.TextureByteArrays[index]);
            }
            
            // 各DrawableでどのDrawableをクリッピングマスクとして使用するか調べ、
            // もし同じDrawableの組み合わせをマスクとして用いるDrawableがあればマスクを共有するようにする
            int drawable_count = Model.DrawableCount;
            List<CubismClippingContext> all_clipping_contexts = new List<CubismClippingContext>();
            DrawableClippingContexts = new CubismClippingContext[drawable_count];
            for (int index = 0; index < drawable_count; index++)
            {
                var drawable = Model.GetDrawable(index);
                if (drawable.ClippingMaskIndexes.Length <= 0)
                {
                    DrawableClippingContexts[index] = null;
                    continue;
                }
                CubismClippingContext new_clippling_context = null;
                int[] mask_indexes = drawable.ClippingMaskIndexes.Distinct().OrderBy(x => x).ToArray();
                foreach (var target in all_clipping_contexts)
                {
                    if (mask_indexes.SequenceEqual(target.ClippingIdList) == true)
                    {
                        new_clippling_context = target;
                        break;
                    }
                }
                if (new_clippling_context == null)
                {
                    // クリッピングマスクをレンダラーに作成する
                    new_clippling_context = new CubismClippingContext(Renderer.CreateClippingMask(), mask_indexes);
                    all_clipping_contexts.Add(new_clippling_context);
                }
                new_clippling_context.ClippedDrawableIndexList.Add(index);
                DrawableClippingContexts[index] = new_clippling_context;
            }
            AllClippingContexts = all_clipping_contexts.ToArray();
        }

        /// <summary>
        /// デストラクタ。
        /// レンダラーに作成したリソースの解放を行う。
        /// </summary>
        ~CubismRenderingManager()
        {
            Dispose(false);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                foreach (var texture in RendererTextures)
                {
                    Renderer.DisposeTexture(texture);
                }
                foreach (var clipping_context in AllClippingContexts)
                {
                    Renderer.DisposeClippingMask(clipping_context.Target);
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        /// <summary>
        /// クリッピングマスクを描画する。
        /// </summary>
        private void SetupClippingMasks()
        {
            foreach (var clipping_context in AllClippingContexts)
            {
                // このクリッピングマスクを利用するDrawable群を囲む矩形を計算する
                var bounds = CalcClippedDrawTotalBounds(clipping_context);
                if (bounds.IsEmpty == true)
                {
                    // Drawableは描画されず、クリッピングマスクは不要であるのでスキップする
                    continue;
                }

                // マスクとなるDrawableをマスクの座標系に投影する行列MatrixForMaskと
                // マスクをModelの座標系に投影する行列MatrixForDrawを計算する

                // マスクの周囲に少しマージンを設ける
                // できるだけ小さいほうが精度が良い
                const float MARGIN = 0.05f;
                RectangleF inflated_bounds = RectangleF.Inflate(bounds, bounds.Width * MARGIN, bounds.Height * MARGIN);
                
                // シェーダ用の計算式を求める。回転を考慮しない場合は以下のとおり
                float scale_x = 1.0f / inflated_bounds.Width;
                float scale_y = 1.0f / inflated_bounds.Height;

                // マスク生成時に使う行列を求める
                // View to Layout(0～1) to Framebuffer(-1～1)
                clipping_context.MatrixForMask = new DenseMatrix(4, 4, new float[]{
                        2.0f * scale_x, 0.0f, 0.0f, 0.0f,
                        0.0f, 2.0f * scale_y, 0.0f, 0.0f,
                        0.0f, 0.0f, 1.0f, 0.0f,
                        -2.0f * scale_x * inflated_bounds.X - 1.0f, -2.0f * scale_y * inflated_bounds.Y - 1.0f, 0.0f, 1.0f
                    });

                // モデル描画時にモデルの座標からマスクの座標へ変換する行列を求める
                clipping_context.MatrixForDraw = new DenseMatrix(4, 4, new float[]{
                        scale_x, 0.0f, 0.0f, 0.0f,
                        0.0f, scale_y, 0.0f, 0.0f,
                        0.0f, 0.0f, 1.0f, 0.0f,
                        -scale_x * inflated_bounds.X, -scale_y * inflated_bounds.Y, 0.0f, 1.0f
                    });

                // マスクを描画する
                Renderer.StartDrawingMask(clipping_context.Target);
                foreach(int drawable_index in clipping_context.ClippingIdList)
                {
                    var drawable = Model.GetDrawable(drawable_index);
                    var texture = RendererTextures[drawable.TextureIndex];
                    float[] vertex_buffer = drawable.VertexBuffer;
                    float[] uv_buffer = drawable.UvBuffer;
                    short[] index_buffer = drawable.IndexBuffer;
                    Renderer.DrawMask(texture, vertex_buffer, uv_buffer, index_buffer, clipping_context.Target, clipping_context.MatrixForMask, drawable.UseCulling);
                }
                Renderer.EndDrawingMask(clipping_context.Target);
            }
        }

        /// <summary>
        /// レンダラーでモデルを描画する。
        /// </summary>
        public void Draw(Matrix mvp_matrix)
        {
            try
            {
                // 描画を開始する
                Renderer.StartDrawingModel(Asset.ModelColor, mvp_matrix);
                
                // クリッピングマスクを描画する
                SetupClippingMasks();
                
                // Drawableの描画順を取得し、描画する順番にインデックスを並べ替えたリストを作成する
                int drawable_count = Model.DrawableCount;
                var drawable_render_order = Model.GetDrawableRenderOrders();
                int[] reordered_drawable_indexes = new int[drawable_count];
                for (int index = 0; index < drawable_count; index++)
                {
                    reordered_drawable_indexes[drawable_render_order[index]] = index;
                }
                
                // 指定された順番通りにDrawableを描画していく
                foreach (int drawable_index in reordered_drawable_indexes)
                {
                    var drawable = Model.GetDrawable(drawable_index);
                    if (drawable.Visible == false)
                    {
                        // Drawableは非表示なので描画しない
                        continue;
                    }

                    // メッシュを描画する
                    var texture = RendererTextures[drawable.TextureIndex];
                    float[] vertex_buffer = drawable.VertexBuffer;
                    float[] uv_buffer = drawable.UvBuffer;
                    short[] index_buffer = drawable.IndexBuffer;
                    CubismClippingContext clipping_context = DrawableClippingContexts[drawable_index];
                    ICubismClippingMask clipping_mask = (clipping_context != null) ? clipping_context.Target : null;
                    Matrix clipping_matrix = (clipping_context != null) ? clipping_context.MatrixForDraw : DenseMatrix.CreateIdentity(4);
                    Renderer.DrawMesh(texture, vertex_buffer, uv_buffer, index_buffer, clipping_mask, clipping_matrix, drawable.BlendMode, drawable.UseCulling, drawable.Opacity);
                }
            }
            finally
            {
                Renderer.EndDrawingModel();
            }
        }
        
        /// <summary>
        /// 指定されたクリッピングマスクがマスクするDrawableの占める矩形を計算する。
        /// 現時点でそのクリッピングマスクを使用するDrawableが無い場合はEmptyを返す。
        /// </summary>
        /// <param name="clipping_context">計算対象のクリッピングマスク</param>
        private RectangleF CalcClippedDrawTotalBounds(CubismClippingContext clipping_context)
        {
            RectangleF result = RectangleF.Empty;
            foreach (var index in clipping_context.ClippedDrawableIndexList)
            {
                // Drawableのバウンディングボックスとの和をとる
                RectangleF bounding_box = Model.GetDrawable(index).BoundingBox;
                if (bounding_box.IsEmpty == false)
                {
                    result = RectangleF.Union(result, bounding_box);
                }
            }
            return result;
        }
        
        /// <summary>
        /// 描画に使用するレンダラー
        /// </summary>
        private ICubismRenderer Renderer;

        /// <summary>
        /// 描画するモデルのアセット
        /// </summary>
        private CubismAsset Asset;

        /// <summary>
        /// 描画するモデル
        /// </summary>
        private CubismModel Model;

        /// <summary>
        /// モデルが使用するクリッピングマスクのリスト
        /// </summary>
        private CubismClippingContext[] AllClippingContexts;

        /// <summary>
        /// 各Drawableが使用するクリッピングマスクのリスト。
        /// マスクを使用しないDrawableの場所にはnullが格納される。
        /// </summary>
        private CubismClippingContext[] DrawableClippingContexts;

        /// <summary>
        /// レンダラーで確保したテクスチャのリスト。
        /// </summary>
        private ICubismTexture[] RendererTextures;
    }
}
