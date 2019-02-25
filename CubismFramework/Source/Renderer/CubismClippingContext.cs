using System;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra.Single;

namespace CubismFramework
{
    internal class CubismClippingContext
    {
        public CubismClippingContext(ICubismClippingMask target, int[] clipping_drawable_indices)
        {
            Target = target;
            ClippingIdList = clipping_drawable_indices;
        }

        /// <summary>
        /// このコンテキストが指し示すクリッピングマスク
        /// </summary>
        public ICubismClippingMask Target;
        
        /// <summary>
        /// 現在の描画状態でマスクの準備が必要ならtrue
        /// </summary>
        //public bool Using = false;

        /// <summary>
        /// クリッピングマスクのIDリスト
        /// </summary>
        public int[] ClippingIdList;

        /// <summary>
        /// RGBAのいずれのチャンネルにこのクリップを配置するか(0:R , 1:G , 2:B , 3:A)
        /// </summary>
        //public int LayoutChannelNo = 0;

        /// <summary>
        /// マスク用チャンネルのどの領域にマスクを入れるか(View座標-1..1, UVは0..1に直す)
        /// </summary>
        //public RectangleF LayoutBounds = new RectangleF();

        /// <summary>
        /// このクリッピングで、クリッピングされる全ての描画オブジェクトの囲み矩形（毎回更新）
        /// </summary>
        //public RectangleF AllClippedDrawRect = new RectangleF();

        /// <summary>
        /// マスクの位置計算結果を保持する行列
        /// </summary>
        public Matrix MatrixForMask = DenseMatrix.CreateIdentity(4);

        /// <summary>
        /// 描画オブジェクトの位置計算結果を保持する行列
        /// </summary>
        public Matrix MatrixForDraw = DenseMatrix.CreateIdentity(4);

        /// <summary>
        /// このマスクにクリップされる描画オブジェクトのリスト
        /// </summary>
        public List<int> ClippedDrawableIndexList = new List<int>();
        
    }
}
