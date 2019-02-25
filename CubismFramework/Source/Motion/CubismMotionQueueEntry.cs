using System;
using System.Collections.Generic;
using System.Text;

namespace CubismFramework
{
    public class CubismMotionQueueEntry
    {
        /// <summary>
        /// モーション
        /// </summary>
        public ICubismMotion Motion { get; internal set; } = null;

        /// <summary>
        /// ループするフラグ。
        /// </summary>
        public bool LoopEnabled { get; internal set; } = false;

        /// <summary>
        /// モデルに適用するパラメータを計算する。
        /// </summary>
        /// <param name="elapsed_time">経過時間[秒]</param>
        /// <param name="event_data">イベント</param>
        internal bool Update(double elapsed_time, out string[] event_data)
        {
            if (Finished == true)
            {
                event_data = null;
                return false;
            }

            // モーションのパラメータを計算する
            Motion.Update(Time, LoopEnabled);
            event_data = Motion.GetFiredEvent(Time, PreviousTime, LoopEnabled);
            PreviousTime = Time;
            Time += elapsed_time * UserSpeed * SystemSpeed;

            // モーションの終了判定をする
            if (LoopEnabled == false)
            {
                if (Motion.Duration <= Time)
                {
                    Finish();
                }
            }

            // 強制終了中の場合はフェードアウト処理を行う
            if (State == StateType.Terminated)
            {
                if (0.0 < TerminatingDuration)
                {
                    SystemWeight = 1.0 - CubismMath.EaseSine(TerminatingElapsed / TerminatingDuration);
                }
                else
                {
                    SystemWeight = 0.0;
                }
                TerminatingElapsed += elapsed_time;
                if (TerminatingDuration <= TerminatingElapsed)
                {
                    Finish();
                }
            }

            return true;
        }
        
        /// <summary>
        /// モーションの重み。
        /// 1が標準の重み
        /// </summary>
        public double Weight
        {
            get { return UserWeight; }
            set { UserWeight = value; }
        }
        private double UserWeight = 1.0;
        private double SystemWeight = 1.0;

        /// <summary>
        /// モーションの再生速度。
        /// 1が標準の速度。
        /// </summary>
        public double Speed
        {
            get { return UserSpeed; }
            set {
                if ((value < 0.0) || double.IsNaN(value))
                {
                    throw new ArgumentOutOfRangeException();
                }
                UserSpeed = value;
            }
        }
        private double UserSpeed = 1.0;
        private double SystemSpeed = 1.0;

        /// <summary>
        /// モーションを静止する。
        /// 静止中、モーションの値はモデルに適用され続ける。
        /// </summary>
        /// <param name="enabled">trueなら静止する。falseなら静止を解除する。</param>
        public void Pause(bool enabled = true)
        {
            if (enabled == true)
            {
                if (State == StateType.Playing)
                {
                    SystemSpeed = 0.0;
                    State = StateType.Paused;
                }
            }
            else
            {
                if (State == StateType.Paused)
                {
                    SystemSpeed = 1.0;
                    State = StateType.Playing;
                }
            }
        }

        /// <summary>
        /// モーションを一時中断する。
        /// 中断中、モーションの値はモデルに適用されない。
        /// </summary>
        /// <param name="enabled">trueなら一時中断する。falseなら一時中断を解除する。</param>
        public void Suspened(bool enabled = true)
        {
            if (enabled == true)
            {
                if ((State == StateType.Playing) || (State == StateType.Paused))
                {
                    SystemWeight = 0.0;
                    SystemSpeed = 0.0;
                    State = StateType.Suspended;
                }
            }
            else
            {
                if (State == StateType.Suspended)
                {
                    SystemWeight = 1.0;
                    SystemSpeed = 1.0;
                    State = StateType.Playing;
                }
            }
        }
        
        /// <summary>
        /// モーションの静止や一時中断を解除する。
        /// </summary>
        public void Resume()
        {
            if ((State == StateType.Paused) || (State == StateType.Suspended))
            {
                SystemWeight = 1.0;
                SystemSpeed = 1.0;
                State = StateType.Playing;
            }
        }

        /// <summary>
        /// モーションの強制終了を開始する。
        /// モーションが再生中や静止中の場合はフェードアウトして終了するが、一時中断のときはすぐに終了する。
        /// </summary>
        /// <param name="fade_out_time">フェードアウトして終了する時間</param>
        public void Terminate(double fade_out_time)
        {
            if ((State == StateType.Playing) || (State == StateType.Paused))
            {
                if (0.0 < fade_out_time)
                {
                    TerminatingElapsed = 0.0;
                    TerminatingDuration = fade_out_time;
                    State = StateType.Terminated;
                }
                else
                {
                    Finish();
                }
            }
            else if (State == StateType.Suspended)
            {
                Finish();
            }
            else if (State == StateType.Terminated)
            {
                if (fade_out_time < (TerminatingDuration - TerminatingElapsed))
                {
                    // 現在の残りフェードアウト時間よりも新しいフェードアウト時間のほうが短かったら、
                    // 重みを維持しつつフェードアウトを早くする
                    double fade_weight = TerminatingElapsed / TerminatingDuration;
                    TerminatingElapsed = (fade_weight * fade_out_time) / (1.0 - fade_weight);
                    TerminatingDuration = TerminatingElapsed + fade_out_time;
                }
            }
            else
            {
                return;
            }
        }

        /// <summary>
        /// モーションを終了する。
        /// </summary>
        private void Finish()
        {
            SystemWeight = 0.0;
            SystemSpeed = 0.0;
            State = StateType.Finished;
        }
        
        /// <summary>
        /// 再生中フラグ。
        /// </summary>
        public bool Playing
        {
            get { return State == StateType.Playing; }
        }

        /// <summary>
        /// 静止フラグ。
        /// </summary>
        public bool Paused
        {
            get { return State == StateType.Paused; }
        }

        /// <summary>
        /// 一時中断フラグ。
        /// </summary>
        public bool Suspended
        {
            get { return State == StateType.Suspended; }
        }
        
        /// <summary>
        /// 強制終了フラグ。
        /// trueならモーションはフェードアウトしてじきに終了する。
        /// </summary>
        public bool Terminated
        {
            get { return State == StateType.Terminated; }
        }
        
        /// <summary>
        /// 終了フラグ
        /// trueならモーションの再生は終了している。
        /// </summary>
        public bool Finished
        {
            get { return State == StateType.Finished; }
        }

        /// <summary>
        /// モーションの現在時刻[秒] (再生速度に影響される)
        /// </summary>
        public double Time
        {
            get
            {
                return TimeInternal;
            }
            set
            {
                if (value < 0.0)
                {
                    throw new ArgumentOutOfRangeException();
                }
                TimeInternal = value;
                PreviousTime = value;
            }
        }
        private double TimeInternal = 0.0;

        /// <summary>
        /// 前にモーションを計算した時刻[秒] (再生速度に影響される)
        /// </summary>
        internal double PreviousTime = 0.0;

        /// <summary>
        /// 強制終了の進行時間[秒] (実時間)
        /// </summary>
        internal double TerminatingElapsed = 0.0;

        /// <summary>
        /// 強制終了の終了時刻[秒] (実時間)
        /// </summary>
        internal double TerminatingDuration = double.PositiveInfinity;
        
        /// <summary>
        /// モーションの再生状態
        /// </summary>
        public StateType State { get; internal set; } = StateType.Playing;

        /// <summary>
        /// モーションの再生状態
        /// </summary>
        public enum StateType
        {
            /// <summary>
            /// モーションは再生中で、モーションの値はモデルに適用される。
            /// </summary>
            Playing,

            /// <summary>
            /// モーションは静止していて、モーションの値はモデルに適用される。
            /// </summary>
            Paused,

            /// <summary>
            /// モーションの時間は止まっていて、モーションの値はモデルに適用されない。
            /// </summary>
            Suspended,

            /// <summary>
            /// モーションは強制終了中で、強制的なフェードアウトが進行している。
            /// </summary>
            Terminated,

            /// <summary>
            /// モーションは終了した。
            /// </summary>
            Finished
        }
    }
}
