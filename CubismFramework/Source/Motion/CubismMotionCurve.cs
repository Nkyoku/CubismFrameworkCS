using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace CubismFramework
{
    /// <summary>
    /// モーションカーブのセグメントの種類。
    /// </summary>
    public enum CubismMotionSegmentType
    {
        /// <summary>
        /// リニア
        /// </summary>
        Linear = 0,

        /// <summary>
        /// ベジェ曲線
        /// </summary>
        Bezier = 1,

        /// <summary>
        /// ステップ
        /// </summary>
        Stepped = 2,

        /// <summary>
        /// インバースステップ
        /// </summary>
        InverseStepped = 3
    }

    /// <summary>
    /// モーションカーブの制御点。
    /// </summary>
    internal struct CubismMotionPoint
    {
        /// <summary>
        /// 時刻[秒]
        /// </summary>
        public double Time;

        /// <summary>
        /// 値
        /// </summary>
        public double Value;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="time">時刻[秒]</param>
        /// <param name="value">値</param>
        public CubismMotionPoint(double time, double value)
        {
            Time = time;
            Value = value;
        }
    }
    
    /// <summary>
    /// モーションカーブのセグメントの抽象クラス。
    /// </summary>
    internal abstract class ICubismMotionSegment
    {
        /// <summary>
        /// セグメントの制御点のリスト
        /// </summary>
        public CubismMotionPoint[] Points { get; protected set; }
        
        /// <summary>
        /// 指定時刻でのセグメントの値を計算する。
        /// </summary>
        /// <param name="time">時刻[秒]</param>
        /// <returns>値</returns>
        public abstract double Evaluate(double time);
    };

    /// <summary>
    /// モーションカーブの線形補間セグメント。
    /// </summary>
    internal class CubismMotionLinearSegment: ICubismMotionSegment
    {
        public CubismMotionLinearSegment()
        {
            Points = new CubismMotionPoint[2];
        }

        public override double Evaluate(double time)
        {
            double t = (time - Points[0].Time) / (Points[1].Time - Points[0].Time);
            t = Math.Max(t, 0.0);
            return Points[0].Value + ((Points[1].Value - Points[0].Value) * t);
        }
    }

    /// <summary>
    /// モーションカーブのベジェ関数セグメント。
    /// </summary>
    internal class CubismMotionBezierSegment : ICubismMotionSegment
    {
        public CubismMotionBezierSegment()
        {
            Points = new CubismMotionPoint[4];
        }

        public override double Evaluate(double time)
        {
            double t = (time - Points[0].Time) / (Points[3].Time - Points[0].Time);
            t = Math.Max(t, 0.0);
            CubismMotionPoint p01 = LerpPoints(Points[0], Points[1], t);
            CubismMotionPoint p12 = LerpPoints(Points[1], Points[2], t);
            CubismMotionPoint p23 = LerpPoints(Points[2], Points[3], t);
            CubismMotionPoint p012 = LerpPoints(p01, p12, t);
            CubismMotionPoint p123 = LerpPoints(p12, p23, t);
            return LerpPoints(p012, p123, t).Value;
        }

        public static CubismMotionPoint LerpPoints(CubismMotionPoint a, CubismMotionPoint b, double t)
        {
            CubismMotionPoint result;
            result.Time = a.Time + ((b.Time - a.Time) * t);
            result.Value = a.Value + ((b.Value - a.Value) * t);
            return result;
        }
    }

    /// <summary>
    /// モーションカーブのステップ関数セグメント。
    /// </summary>
    internal class CubismMotionSteppedSegment : ICubismMotionSegment
    {
        public CubismMotionSteppedSegment()
        {
            Points = new CubismMotionPoint[2];
        }

        public override double Evaluate(double time)
        {
            return Points[0].Value;
        }
    }

    /// <summary>
    /// モーションカーブの逆ステップ関数セグメント。
    /// </summary>
    internal class CubismMotionInverseSteppedSegment : ICubismMotionSegment
    {
        public CubismMotionInverseSteppedSegment()
        {
            Points = new CubismMotionPoint[2];
        }

        public override double Evaluate(double time)
        {
            return Points[1].Value;
        }
    }
    
    /// <summary>
    /// モーションカーブ。
    /// </summary>
    internal class CubismMotionCurve
    {
        /// <summary>
        /// カーブの対象
        /// </summary>
        public TargetType Target;

        /// <summary>
        /// 対象のエフェクト、あるいは紐づいたエフェクト、またはnull。
        /// </summary>
        public CubismId Effect;

        /// <summary>
        /// 対象のパーツ、またはnull
        /// </summary>
        public CubismPart Part;

        /// <summary>
        /// 対象のパラメータ、またはnull
        /// </summary>
        public CubismParameter Parameter;
        
        /// <summary>
        /// セグメントのリスト
        /// </summary>
        public ICubismMotionSegment[] Segments;

        /// <summary>
        /// フェードインにかかる時間[秒]。
        /// 既定のフェードアウト時間を使用するならNaN。
        /// </summary>
        public double FadeInTime;

        /// <summary>
        /// フェードアウトにかかる時間[秒]。
        /// 既定のフェードアウト時間を使用するならNaN。
        /// </summary>
        public double FadeOutTime;
        
        /// <summary>
        /// 指定時刻でのカーブの値を計算する。
        /// </summary>
        /// <param name="time">時刻[秒]</param>
        /// <returns>値</returns>
        public double Evaluate(double time)
        {
            // 指定された時刻に該当するセグメントを検索する
            foreach (var segment in Segments)
            {
                CubismMotionPoint[] points = segment.Points;
                if (time <= points.Last().Time)
                {
                    if (points[0].Time <= time)
                    {
                        return segment.Evaluate(time);
                    }
                    else
                    {
                        // 時刻がモーション再生前なので初期値を返す
                        return points[0].Value;
                    }
                }
            }
            // 時刻がモーション終了後なので最終値を返す
            return Segments.Last().Points.Last().Value;
        }

        /// <summary>
        /// モーションカーブの対象
        /// </summary>
        public enum TargetType
        {
            Model,
            Parameter,
            PartOpacity
        }
    }

    /// <summary>
    /// イベント。
    /// </summary>
    internal struct CubismMotionEvent
    {
        internal double FireTime;
        internal string Value;
    }
}
