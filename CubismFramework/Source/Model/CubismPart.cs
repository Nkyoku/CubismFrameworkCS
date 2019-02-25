using System;
using System.Diagnostics;

namespace CubismFramework
{
    /// <summary>
    /// パーツオブジェクトクラス
    /// </summary>
    [DebuggerDisplay("Part={Name}({Index})")]
    public class CubismPart : CubismId
    {
        /// <summary>
        /// ID名とインデックスを指定してパーツオブジェクトを作成する。
        /// </summary>
        /// <param name="name">ID名</param>
        /// <param name="index">インデックス</param>
        internal CubismPart(string name, int index, float[] original_array)
            : base(name, index)
        {
            OriginalArray = original_array;
            if (0 <= index)
            {
                TargetOpacity = OriginalArray[index];
            }
        }

        /// <summary>
        /// 現在の不透明度
        /// </summary>
        public double CurrentOpacity
        {
            get { return OriginalArray[Index]; }
            set { OriginalArray[Index] = (float)Math.Max(0.0, Math.Min(value, 1.0)); }
        }

        /// <summary>
        /// 目標とする不透明度。
        /// CubismPoseクラスはCurrentOpacityがTargetOpacityになるように不透明度を滑らかに制御する。
        /// </summary>
        public double TargetOpacity
        {
            get { return TargetOpacityInternal; }
            set { TargetOpacityInternal = (float)Math.Max(0.0, Math.Min(value, 1.0)); }
        }
        private double TargetOpacityInternal = 0.0;

        /// <summary>
        /// この値が含まれる元の配列
        /// </summary>
        private readonly float[] OriginalArray;
    }
}
