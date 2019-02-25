using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CubismFramework
{
    public class CubismPose
    {
        /// <summary>
        /// ストリームからポーズデータを読み込む。
        /// </summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="model">対象のモデル</param>
        public CubismPose(Stream stream, CubismModel model)
        {
            var json = CubismPoseJson.Create(stream);
            
            if (!double.IsNaN(json.FadeInTime) && (0.0 <= json.FadeInTime))
            {
                FadeTimeSeconds = json.FadeInTime;
            }

            var part_groups = new List<PartData[]>();
            foreach(var group_item in json.Groups)
            {
                var part_group = new List<PartData>();
                foreach (var item in group_item)
                {
                    CubismPart id = model.GetPart(item.Id);
                    var part_data = new PartData(model, id);
                    var linked_part_ids = new List<CubismPart>();
                    foreach (string linked_id_name in item.Link)
                    {
                        CubismPart linked_id = model.GetPart(linked_id_name);
                        if (linked_id != null)
                        {
                            linked_part_ids.Add(linked_id);
                        }
                    }
                    part_data.LinkedParts = linked_part_ids.ToArray();
                    part_group.Add(part_data);
                }
                part_groups.Add(part_group.ToArray());
            }
            PartGroups = part_groups.ToArray();

            Reset();
        }

        /// <summary>
        /// 不透明度の現在値と目標値をリセットする。
        /// </summary>
        public void Reset()
        {
            foreach (var part_group in PartGroups)
            {
                bool first = true;
                foreach(var part_data in part_group)
                {
                    double value = first ? 1.0 : 0.0;
                    part_data.Part.CurrentOpacity = value;
                    part_data.Part.TargetOpacity = value;
                    first = false;
                }
            }
        }
        
        /// <summary>
        /// モデルのパラメータを更新する。
        /// </summary>
        /// <param name="model">対象のモデル</param>
        /// <param name="delta_time_seconds">デルタ時間[秒]</param>
        /// <returns>trueなら更新されている</returns>
        public void UpdateParameters(double delta_time_seconds)
        {
            double dt = Math.Max(delta_time_seconds, 0.0);
            foreach (var part_group in PartGroups)
            {
                DoFade(dt, part_group);
            }
            CopyPartOpacities();
        }

        /// <summary>
        /// パーツの不透明度をコピーし、リンクしているパーツへ設定する。
        /// </summary>
        private void CopyPartOpacities()
        {
            foreach(var part_group in PartGroups)
            {
                foreach(var part_data in part_group)
                {
                    if (part_data.LinkedParts.Length == 0)
                    {
                        continue;
                    }
                    double opacity = part_data.Part.CurrentOpacity;
                    foreach (var linked_part in part_data.LinkedParts)
                    {
                        linked_part.CurrentOpacity = opacity;
                    }
                }
            }
        }

        /// <summary>
        /// パーツのフェード操作を行う。
        /// </summary>
        /// <param name="delta_time_seconds">デルタ時間[秒]</param>
        /// <param name="part_group">フェード操作を行うパーツグループ</param>
        private void DoFade(double delta_time_seconds, PartData[] part_group)
        {
            if (part_group.Length == 0)
            {
                return;
            }
            
            const double Epsilon = 0.001;
            const double Phi = 0.5;
            const double BackOpacityThreshold = 0.15;

            // 現在、表示状態になっているパーツを取得
            double new_opacity = 1.0;
            PartData vibible_part_data = part_group[0];
            foreach(var part_data in part_group)
            {
                if (Epsilon < part_data.Part.TargetOpacity)
                {
                    vibible_part_data = part_data;

                    // 新しい不透明度を計算
                    new_opacity = part_data.Part.CurrentOpacity;
                    new_opacity += delta_time_seconds / FadeTimeSeconds;
                    new_opacity = Math.Min(new_opacity, 1.0);
                    break;
                }
            }
            
            //  表示パーツ、非表示パーツの不透明度を設定する
            foreach (var part_data in part_group)
            {
                if (part_data == vibible_part_data)
                {
                    // 表示パーツの設定
                    part_data.Part.CurrentOpacity = new_opacity;
                }
                else
                {
                    // 非表示パーツの設定
                    double opacity = part_data.Part.CurrentOpacity;
                    double a1;

                    if (new_opacity < Phi)
                    {
                        // (0,1),(phi,phi)を通る直線式
                        a1 = new_opacity * (Phi - 1.0) / Phi + 1.0;
                    }
                    else
                    {
                        // (1,0),(phi,phi)を通る直線式
                        a1 = (1.0 - new_opacity) * Phi / (1.0 - Phi);
                    }

                    // 背景の見える割合を制限する場合
                    double back_opacity = (1.0 - a1) * (1.0 - new_opacity);
                    if (BackOpacityThreshold < back_opacity)
                    {
                        a1 = 1.0 - BackOpacityThreshold / (1.0 - new_opacity);
                    }

                    opacity = Math.Min(opacity, a1);
                    part_data.Part.CurrentOpacity = opacity;
                }
            }
        }

        /// <summary>
        /// パーツグループごとにパーツデータをまとめたリスト
        /// </summary>
        private PartData[][] PartGroups;
        
        /// <summary>
        /// フェード時間[秒]
        /// </summary>
        private double FadeTimeSeconds = 0.5;
        
        /// <summary>
        /// パーツのデータを格納するクラス
        /// </summary>
        internal class PartData
        {
            /// <summary>
            /// コンストラクタ
            /// </summary>
            /// <param name="model">初期化に使用するモデル</param>
            public PartData(CubismModel model, CubismPart part)
            {
                Part = part;
                Part.TargetOpacity = 1.0;
                LinkedParts = null;
            }

            /// <summary>
            /// パーツ
            /// </summary>
            public CubismPart Part;
            
            /// <summary>
            /// 連動するパーツ
            /// </summary>
            public CubismPart[] LinkedParts;
        }
    }
}
