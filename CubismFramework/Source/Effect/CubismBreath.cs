using System;
using System.Collections.Generic;
using System.Text;

namespace CubismFramework
{
    /// <summary>
    /// 呼吸のパラメータ情報。
    /// </summary>
    public class CubismBreathParameter
    {
        /// <summary>
        /// 呼吸に紐づけるパラメータ
        /// </summary>
        public CubismParameter Parameter;

        /// <summary>
        /// 呼吸を正弦波としたときの、波のオフセット
        /// </summary>
        public double Offset;

        /// <summary>
        /// 呼吸を正弦波としたときの、波の高さ
        /// </summary>
        public double Peak;

        /// <summary>
        /// 呼吸を正弦波としたときの、波の周期
        /// </summary>
        public double Cycle;

        /// <summary>
        /// パラメータへの重み
        /// </summary>
        public double Weight;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="id">パラメータID</param>
        /// <param name="offset">波のオフセット</param>
        /// <param name="peak">波の高さ</param>
        /// <param name="cycle">波の周期</param>
        /// <param name="weight">パラメータへの重み</param>
        public CubismBreathParameter(CubismParameter parameter, double offset, double peak, double cycle, double weight)
        {
            Parameter = parameter;
            Offset = offset;
            Peak = peak;
            Cycle = cycle;
            Weight = weight;
        }
    }

    public class CubismBreath : ICubismMotion
    {
        /// <summary>
        /// 呼吸のパラメータをひもづける。
        /// </summary>
        /// <param name="parameter">パラメータ</param>
        public void SetParameter(CubismBreathParameter parameter)
        {
            if (BreathParameters.Exists(p => p == parameter) == false)
            {
                BreathParameters.Add(parameter);
            }
        }

        /// <summary>
        /// ひもづけたパラメータを削除する
        /// </summary>
        /// <param name="parameter">削除するパラメータ</param>
        public void RemoveParameter(CubismBreathParameter parameter)
        {
            int index = BreathParameters.FindIndex(p => p == parameter);
            if (0 <= index)
            {
                BreathParameters.RemoveAt(index);
            }
        }

        /// <summary>
        /// モデルに適用するパラメータを計算する。
        /// </summary>
        /// <param name="time">モーションの再生時間[秒]</param>
        /// <param name="loop_enabled">trueのとき、ループをするものとして計算する</param>
        public override void Update(double time, bool loop_enabled)
        {
            double phase_time = time * 2.0 * Math.PI;
            foreach(var breath_parameter in BreathParameters)
            {
                double value = breath_parameter.Offset + breath_parameter.Peak * Math.Sin(phase_time / breath_parameter.Cycle);
                double current_value = breath_parameter.Parameter.Value;
                double new_value = current_value + value * breath_parameter.Weight;
                breath_parameter.Parameter.Value = new_value;
            }
        }

        /// <summary>
        /// パラメータのリスト
        /// </summary>
        List<CubismBreathParameter> BreathParameters = new List<CubismBreathParameter>();
    }
}
