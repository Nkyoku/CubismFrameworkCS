using System;
using System.Collections.Generic;
using System.Text;

namespace CubismFramework
{
    public class CubismMotionManager
    {
        /// <summary>
        /// イベントのコールバックに登録できるデリゲート
        /// </summary>
        /// <param name="caller">発火したイベントを再生させたCubismMotionManager</param>
        /// <param name="event_value">発火したイベントの文字列データ</param>
        /// <param name="custom_data">コールバックに返される登録時に指定されたデータ</param>
        public delegate void CubismMotionEventFunction(CubismMotionManager caller, string event_value, object custom_data);
        
        /// <summary>
        /// イベントを受け取るデリゲートの登録をする。
        /// </summary>
        /// <param name="callback">コールバックされるデリゲート</param>
        /// <param name="parameter">コールバックに返されるデータ</param>
        public void SetEventCallback(CubismMotionEventFunction callback, object parameter = null)
        {
            EventCallbak = callback;
            EventParameter = parameter;
        }

        /// <summary>
        /// FinishMotions()に渡す、終了させるモーションが満たすべき条件式
        /// </summary>
        /// <param name="queue_entry">対象のモーションキュー項目</param>
        /// <returns></returns>
        public delegate bool ConditionFunction(CubismMotionQueueEntry queue_entry);

        /// <summary>
        /// 条件に一致する再生中のモーションを終了させる。
        /// </summary>
        /// <param name="condition">この関数がtrueを返すモーションを終了する</param>
        /// <param name="fade_out_seconds">フェードアウトする時間[秒]。0なら瞬時に終了する。</param>
        public void TerminateMotions(ConditionFunction condition, double fade_out_seconds = 0.0)
        {
            foreach(var queue_entry in MotionQueue)
            {
                if (queue_entry.Finished == true)
                {
                    continue;
                }
                if ((queue_entry.Terminated == true) && ((queue_entry.TerminatingDuration - queue_entry.TerminatingElapsed) <= fade_out_seconds))
                {
                    // このモーションはfade_out_secondsよりも早く終了するので条件式を評価しない
                    continue;
                }
                if (condition(queue_entry) == true)
                {
                    queue_entry.Terminate(fade_out_seconds);
                }
            }
        }

        /// <summary>
        /// 全ての再生中のモーションを終了させる。
        /// </summary>
        /// <param name="fade_out_seconds">フェードアウトする時間[秒]。0なら瞬時に終了する。</param>
        public void TerminateAllMotions(double fade_out_seconds = 0.0)
        {
            TerminateMotions(x => true, fade_out_seconds);
        }

        /// <summary>
        /// trueならモーションがすべて終了している。
        /// </summary>
        public bool AllFinished
        {
            get
            {
                return (MotionQueue.Count <= 0);
            }
        }

        /// <summary>
        /// 指定したモーションの再生を開始する。
        /// </summary>
        /// <param name="motion">再生するモーション</param>
        /// <param name="priority">モーションの優先度。高いほど優先される。</param>
        /// <param name="loop_enabled">trueのときループが有効なモーションではループを有効にする</param>
        /// <returns>再生が開始された場合はCubismMotionQueueEntryを返す</returns>
        public CubismMotionQueueEntry StartMotion(ICubismMotion motion, bool loop_enabled = false)
        {
            var queue_entry = new CubismMotionQueueEntry();
            queue_entry.Motion = motion;
            queue_entry.LoopEnabled = motion.CanLoop && loop_enabled;
            MotionQueue.Add(queue_entry);
            return queue_entry;
        }

        /// <summary>
        /// モーションを更新して、モデルにパラメータ値を反映する。
        /// </summary>
        /// <param name="elapsed_time">経過時間[秒]</param>
        public void Update(double elapsed_time)
        {
            foreach (var queue_entry in MotionQueue)
            {
                // モーションを計算する
                string[] event_data;
                if (queue_entry.Update(elapsed_time, out event_data) == false)
                {
                    continue;
                }

                // イベントが発生していればコールバックを呼ぶ
                if (EventCallbak != null)
                {
                    foreach (string event_value in event_data)
                    {
                        EventCallbak(this, event_value, EventParameter);
                    }
                }
            }

            // 終了したモーションをキューから除く
            MotionQueue.RemoveAll(x => x.Finished);
        }
        
        /// <summary>
        /// モーションキュー
        /// </summary>
        private List<CubismMotionQueueEntry> MotionQueue = new List<CubismMotionQueueEntry>();

        /// <summary>
        /// イベント発生時にコールバックされるデリゲート
        /// </summary>
        private CubismMotionEventFunction EventCallbak = null;

        /// <summary>
        /// イベント発生時にコールバックに渡されるデータ
        /// </summary>
        private object EventParameter = null;
    }
}
