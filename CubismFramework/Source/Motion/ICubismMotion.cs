using System;
using System.Collections.Generic;
using System.Text;

namespace CubismFramework
{
    public abstract class ICubismMotion
    {
        /// <summary>
        /// モデルに適用するパラメータを計算する。
        /// </summary>
        /// <param name="time">モーションの再生時間[秒]</param>
        /// <param name="loop_enabled">trueのとき、ループをするものとして計算する</param>
        public abstract void Update(double time, bool loop_enabled);

        /// <summary>
        /// モーション中に発生したイベントを取得する。
        /// </summary>
        /// <param name="time">モーションの再生時間[秒]</param>
        /// <param name="previous_time">前に呼び出した時の時刻[秒]</param>
        /// <param name="loop_enabled">trueのとき、ループをするものとして計算する</param>
        /// <returns>イベント文字列のリストまたはnull</returns>
        public virtual string[] GetFiredEvent(double time, double previous_time, bool loop_enabled)
        {
            return null;
        }
        
        /// <summary>
        /// モーションの長さ[秒]。
        /// ループできるモーションはループ1回分の長さ。
        /// モーションの長さが定義できないときはPositiveInfinity。
        /// </summary>
        public virtual double Duration { get; protected set; } = double.PositiveInfinity;

        /// <summary>
        /// trueならモーションはループできる。
        /// </summary>
        public bool CanLoop { get; protected set; } = false;

        /// <summary>
        /// trueならループのときにフェードインを繰り返す。
        /// </summary>
        public bool LoopFadingEnabled { get; set; } = true;
        
        /// <summary>
        /// フェードインにかかる時間[秒]
        /// </summary>
        public double GlobalFadeInSeconds { get; set; } = 0.0;

        /// <summary>
        /// フェードアウトにかかる時間[秒]
        /// </summary>
        public double GlobalFadeOutSeconds { get; set; } = 0.0;

        /// <summary>
        /// モーションの重み(0～1)
        /// </summary>
        public double Weight { get; set; } = 1.0;
    }
}
