using System;
using System.Diagnostics;

namespace CubismFramework
{
    /// <summary>
    /// 基本IDオブジェクトクラス
    /// </summary>
    [DebuggerDisplay("Id={Name}({Index})")]
    public class CubismId
    {
        /// <summary>
        /// ID名とインデックスを指定してIDオブジェクトを作成する。
        /// </summary>
        /// <param name="name">ID名</param>
        /// <param name="index">インデックス</param>
        internal CubismId(string name, int index = -1)
        {
            Name = name;
            Index = index;
        }
        
        /// <summary>
        /// IDオブジェクトのID名を比較する。
        /// </summary>
        /// <param name="id">対象のID</param>
        /// <returns>trueならID名が同じ</returns>
        public bool CompareTo(CubismId id)
        {
            return (Name == id.Name);
        }

        /// <summary>
        /// 文字列とID名を比較する。
        /// </summary>
        /// <param name="name">文字列</param>
        /// <returns>trueならID名が同じ</returns>
        public bool CompareTo(string name)
        {
            return (Name == name);
        }

        /// <summary>
        /// ID名
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// インデックス
        /// </summary>
        public readonly int Index;
    }
}
