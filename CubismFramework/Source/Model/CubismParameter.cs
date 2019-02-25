using System;
using System.Diagnostics;

namespace CubismFramework
{
    /// <summary>
    /// パラメータオブジェクトクラス
    /// </summary>
    [DebuggerDisplay("Parameter={Name}({Index})")]
    public class CubismParameter : CubismId
    {
        /// <summary>
        /// ID名とインデックスを指定してパラメータオブジェクトを作成する。
        /// </summary>
        /// <param name="name">ID名</param>
        /// <param name="index">インデックス</param>
        internal CubismParameter(string name, int index, double min, double max, double def, float[] original_array)
            : base(name, index)
        {
            Minimum = min;
            Maximum = max;
            Default = def;
            OriginalArray = original_array;
        }

        /// <summary>
        /// 最小値
        /// </summary>
        public readonly double Minimum;

        /// <summary>
        /// 最大値
        /// </summary>
        public readonly double Maximum;

        /// <summary>
        /// デフォルト値
        /// </summary>
        public readonly double Default;

        /// <summary>
        /// 値
        /// </summary>
        public double Value
        {
            get { return OriginalArray[Index]; }
            set { OriginalArray[Index] = (float)Math.Max(Minimum, Math.Min(value, Maximum)); }
        }

        /// <summary>
        /// この値が含まれる元の配列
        /// </summary>
        private readonly float[] OriginalArray;
    }
}
