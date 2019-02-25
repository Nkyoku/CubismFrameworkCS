using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace CubismFramework
{
    public class CubismExpressionMotion : ICubismMotion
    {
        /// <summary>
        /// 表情パラメータ値の計算方式
        /// </summary>
        public enum BlendType
        {
            /// <summary>
            /// 加算
            /// </summary>
            Add = 0,

            /// <summary>
            /// 乗算
            /// </summary>
            Multiply = 1,

            /// <summary>
            /// 上書き
            /// </summary>
            Overwrite = 2
        };

        /// <summary>
        /// 表情のパラメータ情報の構造体。
        /// </summary>
        private struct ExpressionParameter
        {
            /// <summary>
            /// パラメータ
            /// </summary>
            internal CubismParameter Parameter;

            /// <summary>
            /// パラメータの演算種類
            /// </summary>
            internal BlendType Blend;

            /// <summary>
            /// 値
            /// </summary>
            internal double Value;
        };

        /// <summary>
        /// ストリームから表情モーションを生成する
        /// </summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="model">対象のモデル</param>
        public CubismExpressionMotion(Stream stream, CubismModel model)
        {
            // 表情モーションデータを読み込む
            var json = CubismExpressionJson.Create(stream);
            GlobalFadeInSeconds = Math.Min(Math.Max(json.FadeInTime, 0.0), 1.0);
            GlobalFadeOutSeconds = Math.Min(Math.Max(json.FadeOutTime, 0.0), 1.0);

            // パラメータを読み取る
            Parameters = new ExpressionParameter[json.Parameters.Length];
            for(int index = 0; index < json.Parameters.Length; index++)
            {
                var parameter = new ExpressionParameter();
                var parameter_item = json.Parameters[index];
                parameter.Parameter = model.GetParameter(parameter_item.Id);
                switch (parameter_item.Blend)
                {
                case "Add":
                default:
                    parameter.Blend = BlendType.Add;
                    break;

                case "Multiply":
                    parameter.Blend = BlendType.Multiply;
                    break;

                case "Overwrite":
                    parameter.Blend = BlendType.Overwrite;
                    break;
                }
                parameter.Value = parameter_item.Value;
                Parameters[index] = parameter;
            }
        }
        
        /// <summary>
        /// モデルに適用するパラメータを計算する。
        /// </summary>
        /// <param name="time">モーションの再生時間[秒]</param>
        /// <param name="loop_enabled">trueのとき、ループをするものとして計算する</param>
        public override void Update(double time, bool loop_enabled)
        {
            foreach(var breath_parameter in Parameters)
            {
                var parameter = breath_parameter.Parameter;
                switch (breath_parameter.Blend)
                {
                case BlendType.Add:
                    parameter.Value += parameter.Value + breath_parameter.Value * Weight;
                    break;

                case BlendType.Multiply:
                    parameter.Value *= (breath_parameter.Value - 1.0) * Weight + 1.0;
                    break;

                case BlendType.Overwrite:
                    parameter.Value = breath_parameter.Value * (1.0 - Weight) + breath_parameter.Value * Weight;
                    break;
                }
            }
        }
        
        /// <summary>
        /// 表情のパラメータ情報リスト
        /// </summary>
        private ExpressionParameter[] Parameters;
    }
}
