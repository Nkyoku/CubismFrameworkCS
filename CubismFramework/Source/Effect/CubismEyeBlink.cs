using System;
using System.Collections.Generic;
using System.Text;

namespace CubismFramework
{
    public class CubismEyeBlink : ICubismMotion
    {
        /// <summary>
        /// まばたきの状態を表す列挙型
        /// </summary>
        enum EyeState
        {
            /// <summary>
            /// 初期状態
            /// </summary>
            First = 0,

            /// <summary>
            /// まばたきしていない状態
            /// </summary>
            Interval,

            /// <summary>
            /// まぶたが閉じていく途中の状態
            /// </summary>
            Closing,

            /// <summary>
            /// まぶたが閉じている状態
            /// </summary>
            Closed,

            /// <summary>
            /// まぶたが開いていく途中の状態
            /// </summary>
            Opening
        }

        /// <summary>
        /// モデル設定から自動まばたきを設定する
        /// </summary>
        /// <param name="parameter_ids">パラメータのリスト</param>
        public CubismEyeBlink(CubismParameter[] parameters)
        {
            Parameters = parameters;
        }
        
        /// <summary>
        /// まばたきさせるパラメータIDのリストを取得する。
        /// </summary>
        /// <returns>パラメータのリスト</returns>
        public CubismParameter[] GetParameters()
        {
            return Parameters;
        }

        /// <summary>
        /// モデルに適用するパラメータを計算する。
        /// </summary>
        /// <param name="time">モーションの再生時間[秒]</param>
        /// <param name="loop_enabled">trueのとき、ループをするものとして計算する</param>
        public override void Update(double time, bool loop_enabled)
        {
            if (Parameters == null)
            {
                return;
            }
            
            double value;
            double t = 0.0;

            switch (BlinkState)
            {
            case EyeState.Closing:
                t = ((time - StateStartTimeSeconds) / ClosingSeconds);
                if (1.0 <= t)
                {
                    t = 1.0;
                    BlinkState = EyeState.Closed;
                    StateStartTimeSeconds = time;
                }
                value = 1.0 - t;
                break;

            case EyeState.Closed:
                t = ((time - StateStartTimeSeconds) / ClosedSeconds);
                if (1.0 <= t)
                {
                    BlinkState = EyeState.Opening;
                    StateStartTimeSeconds = time;
                }
                value = 0.0;
                break;

            case EyeState.Opening:
                t = ((time - StateStartTimeSeconds) / OpeningSeconds);
                if (1.0 <= t)
                {
                    t = 1.0;
                    BlinkState = EyeState.Interval;
                    NextBlinkingTime = time + DeterminNextBlinkingTiming();
                }
                value = t;
                break;

            case EyeState.Interval:
                if (NextBlinkingTime < time)
                {
                    BlinkState = EyeState.Closing;
                    StateStartTimeSeconds = time;
                }
                value = 1.0;
                break;

            case EyeState.First:
            default:
                BlinkState = EyeState.Interval;
                NextBlinkingTime = time + DeterminNextBlinkingTiming();
                value = 1.0;
                break;
            }

            if (InverseParameter == true)
            {
                value = -value;
            }
            
            foreach (var parameter in Parameters)
            {
                parameter.Value = value;
            }
        }

        /// <summary>
        /// 次のまばたきのタイミングを決定する。
        /// </summary>
        /// <returns>次のまばたきを行う時刻[秒]</returns>
        private double DeterminNextBlinkingTiming()
        {
            double r = new Random().NextDouble();
            return (r * (2.0 * BlinkIntervalSeconds - 1.0));
        }

        /// <summary>
        /// 現在のまばたきの状態
        /// </summary>
        private EyeState BlinkState = EyeState.First;

        /// <summary>
        /// 操作対象のパラメータのリスト
        /// </summary>
        private CubismParameter[] Parameters;

        /// <summary>
        /// 次のまばたきの時刻[秒]
        /// </summary>
        private double NextBlinkingTime = 0.0;

        /// <summary>
        /// 現在の状態が開始した時刻[秒]
        /// </summary>
        private double StateStartTimeSeconds = 0.0;

        /// <summary>
        /// まばたきの間隔[秒]
        /// </summary>
        public double BlinkIntervalSeconds = 2.0;//4.0;

        /// <summary>
        /// まぶたを閉じる動作の所要時間[秒]
        /// </summary>
        public double ClosingSeconds = 0.1;

        /// <summary>
        /// まぶたを閉じている動作の所要時間[秒]
        /// </summary>
        public double ClosedSeconds = 0.05;

        /// <summary>
        /// まぶたを開く動作の所要時間[秒]
        /// </summary>
        public double OpeningSeconds = 0.15;

        /// <summary>
        /// デルタ時間の積算値[秒]
        /// </summary>
        //private double UserTimeSeconds = 0.0;

        /// <summary>
        /// trueのとき動作を反転する。
        /// </summary>
        public bool InverseParameter = false;
    }
}
