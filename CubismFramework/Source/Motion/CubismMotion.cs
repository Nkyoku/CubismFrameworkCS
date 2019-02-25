using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace CubismFramework
{
    public class CubismMotion : ICubismMotion
    {
        /// <summary>
        /// ストリームからモーションを生成する
        /// </summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="setting_motion_item">CubismModelSettingJsonのこのモーションの設定</param>
        /// <param name="id_manager">IDマネージャー</param>
        public CubismMotion(Stream stream, CubismModelSettingJson.FileReferencesItem.MotionItem setting_motion_item, CubismModel model)
        {
            // モーションデータを読み込む
            var json = CubismMotionJson.Create(stream);
            
            Duration = json.Meta.Duration;
            CanLoop = json.Meta.Loop;

            // 標準のフェード時間を取得する。
            // モデルのjsonファイルで指定されている値を優先的に適用するが、
            // 無かったらモーションのjsonファイルの値を使う。
            GlobalFadeInSeconds = (setting_motion_item != null) ? setting_motion_item.FadeInTime : 0.0;
            GlobalFadeInSeconds = !double.IsNaN(GlobalFadeInSeconds) ? GlobalFadeInSeconds : json.Meta.FadeInTime;
            GlobalFadeInSeconds = !double.IsNaN(GlobalFadeInSeconds) ? GlobalFadeInSeconds : 0.0;
            GlobalFadeOutSeconds = (setting_motion_item != null) ? setting_motion_item.FadeOutTime : 0.0;
            GlobalFadeOutSeconds = !double.IsNaN(GlobalFadeOutSeconds) ? GlobalFadeOutSeconds : json.Meta.FadeOutTime;
            GlobalFadeOutSeconds = !double.IsNaN(GlobalFadeOutSeconds) ? GlobalFadeOutSeconds : 0.0;
            
            // モーションカーブを内部形式に変換する
            var curve_list = new List<CubismMotionCurve>();
            foreach(var item in json.Curves)
            {
                var curve = ParseCurve(item, model);
                if (curve == null)
                {
                    continue;
                }
                if (curve.Target == CubismMotionCurve.TargetType.Model)
                {
                    // ターゲットがモデルのとき、エフェクトIDが対象となる
                    // これらはパラメータや不透明度を対象としたカーブより先に処理しなければいけないため別に管理する
                    EffectCurves[curve.Effect] = curve;
                }
                else
                {
                    curve_list.Add(curve);
                }
            }
            Curves = curve_list.ToArray();

            // ユーザーデータを読み込む
            if ((json.UserData != null) && (json.Meta.UserDataCount == json.UserData.Length))
            {
                int userdata_count = json.Meta.UserDataCount;
                Events = new CubismMotionEvent[userdata_count];
                for (int index = 0; index < userdata_count; index++)
                {
                    var userdata = new CubismMotionEvent();
                    userdata.FireTime = json.UserData[index].Time;
                    userdata.Value = json.UserData[index].Value;
                    Events[index] = userdata;
                }
            }
            else
            {
                Events = new CubismMotionEvent[0];
            }
        }

        private static CubismMotionCurve ParseCurve(CubismMotionJson.CurveItem item, CubismModel model)
        {
            CubismMotionCurve curve = new CubismMotionCurve();
            if (item.Target == "Model")
            {
                curve.Target = CubismMotionCurve.TargetType.Model;
                curve.Effect = new CubismId(item.Id);
            }
            else if(item.Target == "Parameter")
            {
                curve.Target = CubismMotionCurve.TargetType.Parameter;
                curve.Parameter = model.GetParameter(item.Id);
                if (curve.Parameter == null)
                {
                    return null;
                }
            }
            else if (item.Target == "PartOpacity")
            {
                curve.Target = CubismMotionCurve.TargetType.PartOpacity;
                curve.Part = model.GetPart(item.Id);
                if (curve.Part == null)
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
            curve.FadeInTime = item.FadeInTime;
            curve.FadeOutTime = item.FadeOutTime;

            // 制御点を読み込む
            // 初めの2アイテムは最初の制御点を示す
            var segment_item = item.Segments;
            int segment_count = item.Segments.Length;
            CubismMotionPoint last_point = new CubismMotionPoint(segment_item[0], segment_item[1]);
            
            // 以降の制御点を読み込む
            var segments = new List<ICubismMotionSegment>();
            for (int segment_index = 2; segment_index < segment_count;)
            {
                switch ((CubismMotionSegmentType)segment_item[segment_index])
                {
                case CubismMotionSegmentType.Linear:
                {
                    var segment = new CubismMotionLinearSegment();
                    segment.Points[0] = last_point;
                    segment.Points[1] = new CubismMotionPoint(segment_item[segment_index + 1], segment_item[segment_index + 2]);
                    segments.Add(segment);
                    last_point = segment.Points[1];
                    segment_index += 3;
                    break;
                }
                case CubismMotionSegmentType.Bezier:
                {
                    var segment = new CubismMotionBezierSegment();
                    segment.Points[0] = last_point;
                    segment.Points[1] = new CubismMotionPoint(segment_item[segment_index + 1], segment_item[segment_index + 2]);
                    segment.Points[2] = new CubismMotionPoint(segment_item[segment_index + 3], segment_item[segment_index + 4]);
                    segment.Points[3] = new CubismMotionPoint(segment_item[segment_index + 5], segment_item[segment_index + 6]);
                    segments.Add(segment);
                    last_point = segment.Points[3];
                    segment_index += 7;
                    break;
                }
                case CubismMotionSegmentType.Stepped:
                {
                    var segment = new CubismMotionSteppedSegment();
                    segment.Points[0] = last_point;
                    segment.Points[1] = new CubismMotionPoint(segment_item[segment_index + 1], segment_item[segment_index + 2]);
                    segments.Add(segment);
                    last_point = segment.Points[1];
                    segment_index += 3;
                    break;
                }
                case CubismMotionSegmentType.InverseStepped:
                {
                    var segment = new CubismMotionInverseSteppedSegment();
                    segment.Points[0] = last_point;
                    segment.Points[1] = new CubismMotionPoint(segment_item[segment_index + 1], segment_item[segment_index + 2]);
                    segments.Add(segment);
                    last_point = segment.Points[1];
                    segment_index += 3;
                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
                }
            }

            curve.Segments = segments.ToArray();

            return curve;
        }

        /// <summary>
        /// エフェクトID名からエフェクトIDオブジェクトを取得する。
        /// </summary>
        /// <param name="effect_id_name">エフェクトID名</param>
        /// <returns>エフェクトID</returns>
        public CubismId GetEffect(string effect_id_name)
        {
            foreach(var effect_curve in EffectCurves)
            {
                if (effect_curve.Key.CompareTo(effect_id_name) == true)
                {
                    return effect_curve.Key;
                }
            }
            return null;
        }
        
        /// <summary>
        /// エフェクトがかかっているパラメータリストを設定する。
        /// </summary>
        /// <param name="effect_id">エフェクトID</param>
        /// <param name="effect_parameter_ids">エフェクトがかかっているパラメータIDのリスト</param>
        /// <returns>モーションに指定されたエフェクトが使用されていればtrueを返す</returns>
        public bool SetEffectParameters(CubismId effect_id, CubismParameter[] effect_parameters)
        {
            if (EffectCurves.ContainsKey(effect_id) == false)
            {
                return false;
            }

            // 該当するパラメータを操作するカーブがあればエフェクトIDを設定する
            bool[] used_ids = new bool[effect_parameters.Length];
            foreach (var curve in Curves)
            {
                curve.Effect = null;
                for(int index = 0; index < effect_parameters.Length; index++)
                {
                    if (curve.Parameter == effect_parameters[index])
                    {
                        curve.Effect = effect_id;
                        used_ids[index] = true;
                        break;
                    }
                }
            }

            // このモーションで制御しないパラメータはEffectUnusedIdsに格納する
            EffectUnusedParameters.RemoveAll(x => x.Item1 == effect_id);
            for (int index = 0; index < effect_parameters.Length; index++)
            {
                if (used_ids[index] == false)
                {
                    EffectUnusedParameters.Add((effect_id, effect_parameters[index]));
                }
            }

            return true;
        }

        /// <summary>
        /// モデルに適用するパラメータを計算する。
        /// </summary>
        /// <param name="time">モーションの再生時間[秒]</param>
        /// <param name="loop_enabled">trueのとき、ループをするものとして計算する</param>
        public override void Update(double time, bool loop_enabled)
        {
            // モーションデータ中での時刻を計算する
            double time_in_motion = time % Duration;

            // モーション全体に掛けるフェードの重みを計算する
            double global_fade_in_weight = CalculateFadeInWeight(time, Duration, GlobalFadeInSeconds, loop_enabled, LoopFadingEnabled);
            double global_fade_out_weight = CalculateFadeOutWeight(time, Duration, GlobalFadeOutSeconds, loop_enabled, LoopFadingEnabled);

            // エフェクトに対してカーブが適用するパラメータの重みを計算する
            Dictionary<CubismId, double> effect_values = new Dictionary<CubismId, double>();
            foreach(var curve in EffectCurves)
            {
                effect_values[curve.Key] = curve.Value.Evaluate(time);
            }

            // モーションカーブを計算する
            int index;
            for (index = 0; index < Curves.Length; index++)
            {
                var curve = Curves[index];
                double value, weight;
                if (curve.Target == CubismMotionCurve.TargetType.Parameter)
                {
                    // 動きに関するパラメータに対する操作
                    value = curve.Evaluate(time_in_motion);

                    // エフェクトに関するパラメータなら値に重みを掛ける
                    if ((object)curve.Effect != null)
                    {
                        double effect_value;
                        if (effect_values.TryGetValue(curve.Effect, out effect_value) == true)
                        {
                            value *= effect_value;
                        }
                    }
                    
                    // パラメータごとのフェード
                    if (double.IsNaN(curve.FadeInTime) && double.IsNaN(curve.FadeOutTime))
                    {
                        // モーション全体のフェードを適用
                        weight = global_fade_in_weight * global_fade_out_weight;
                    }
                    else
                    {
                        // パラメータに対してフェードインかフェードアウトが設定してある場合はそちらを適用
                        weight = Weight;
                        if (double.IsNaN(curve.FadeInTime) == true)
                        {
                            weight *= global_fade_in_weight;
                        }
                        else
                        {
                            weight *= CalculateFadeInWeight(time, Duration, curve.FadeInTime, loop_enabled, LoopFadingEnabled);
                        }

                        if (double.IsNaN(curve.FadeOutTime))
                        {
                            weight *= global_fade_out_weight;
                        }
                        else
                        {
                            weight *= CalculateFadeOutWeight(time, Duration, curve.FadeInTime, loop_enabled, LoopFadingEnabled);
                        }
                    }
                    double current_value = curve.Parameter.Value;
                    double new_value = current_value + (value - current_value) * weight;
                    curve.Parameter.Value = new_value;
                }
                else if (curve.Target == CubismMotionCurve.TargetType.PartOpacity)
                {
                    // 不透明度に対する操作
                    value = curve.Evaluate(time_in_motion);
                    weight = 1.0;
                    curve.Part.TargetOpacity = value;
                }
            }

            // エフェクトに属しているがカーブで個別に制御されていないIDに値を設定する
            foreach(var (effect_id, target) in EffectUnusedParameters)
            {
                double value, weight;
                if (effect_values.TryGetValue(effect_id, out value) == true)
                {
                    weight = global_fade_in_weight * global_fade_out_weight;
                    double current_value = target.Value;
                    double new_value = current_value + (value - current_value) * weight;
                    target.Value = new_value;
                }
                index++;
            }
        }

        /// <summary>
        /// フェードインの重みを計算する。
        /// </summary>
        /// <param name="time">モーションの再生時刻[秒]</param>
        /// <param name="duration">モーションの長さ[秒]</param>
        /// <param name="fading_time">フェードインの長さ[秒]</param>
        /// <param name="loop_enabled">trueならループ再生が有効化されている</param>
        /// <param name="loop_fading">trueならループ再生時にもフェードインをする</param>
        /// <returns>重み</returns>
        private static double CalculateFadeInWeight(double time, double duration, double fading_time, bool loop_enabled, bool loop_fading)
        {
            if (fading_time <= 0.0)
            {
                return 1.0;
            }
            if (!loop_enabled || !loop_fading)
            {
                return CubismMath.EaseSine(time / fading_time);
            }
            else
            {
                return CubismMath.EaseSine((time % duration) / fading_time);
            }
        }

        /// <summary>
        /// フェードアウトの重みを計算する。
        /// </summary>
        /// <param name="time">モーションの再生時刻[秒]</param>
        /// <param name="duration">モーションの長さ[秒]</param>
        /// <param name="fading_time">フェードインの長さ[秒]</param>
        /// <param name="loop_enabled">trueならループ再生が有効化されている</param>
        /// <param name="loop_fading">trueならループ再生時にもフェードインをする</param>
        /// <returns>重み</returns>
        private static double CalculateFadeOutWeight(double time, double duration, double fading_time, bool loop_enabled, bool loop_fading)
        {
            if (fading_time <= 0.0)
            {
                return 1.0;
            }
            if (loop_enabled == false)
            {
                return CubismMath.EaseSine((duration - time) / fading_time);
            }
            else
            {
                if (loop_fading == false)
                {
                    return 1.0;
                }
                else
                {
                    return CubismMath.EaseSine((duration - (time % duration)) / fading_time);
                }
            }
        }

        /// <summary>
        /// モーション中に発生したイベントを取得する。
        /// </summary>
        /// <param name="time">モーションの再生時間[秒]</param>
        /// <param name="previous_time">前に呼び出した時の時刻[秒]</param>
        /// <param name="loop_enabled">trueのとき、ループをするものとして計算する</param>
        /// <returns>イベント文字列のリストまたはnull</returns>
        public override string[] GetFiredEvent(double time, double previous_time, bool loop_enabled)
        {
            var result = new List<string>();
            if (loop_enabled == false)
            {
                foreach (var event_data in Events)
                {
                    if ((previous_time < event_data.FireTime) && (event_data.FireTime <= time))
                    {
                        result.Add(event_data.Value);
                    }
                }
            }
            else
            {
                double time_in_motion = time % Duration;
                double previous_time_in_motion = previous_time % Duration;
                foreach (var event_data in Events)
                {
                    if (((previous_time_in_motion < event_data.FireTime) && (event_data.FireTime <= time_in_motion))
                        || ((time_in_motion < previous_time_in_motion) && ((event_data.FireTime <= time_in_motion) || (previous_time_in_motion <= event_data.FireTime))))
                    {
                        result.Add(event_data.Value);
                    }
                }
            }
            return result.ToArray();
        }
        
        /// <summary>
        /// カーブのリスト
        /// </summary>
        private CubismMotionCurve[] Curves;

        /// <summary>
        /// イベントのリスト
        /// </summary>
        private CubismMotionEvent[] Events;

        /// <summary>
        /// エフェクトに紐づいたカーブのマップ
        /// </summary>
        private Dictionary<CubismId, CubismMotionCurve> EffectCurves = new Dictionary<CubismId, CubismMotionCurve>();

        /// <summary>
        /// エフェクトに紐づいているがカーブで個別に制御されないIDのマップ
        /// </summary>
        private List<(CubismId, CubismParameter)> EffectUnusedParameters = new List<(CubismId, CubismParameter)>();
    }
}
