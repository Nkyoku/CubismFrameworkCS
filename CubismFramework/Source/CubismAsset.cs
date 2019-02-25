using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Drawing;
using MathNet.Numerics.LinearAlgebra.Single;

namespace CubismFramework
{
    /// <summary>
    /// 指定したファイルパスのファイルのストリームを返す。
    /// CubismAsset.Load()に渡して、追加のデータ読み込みを行うために使われる。
    /// </summary>
    /// <param name="file_path">読み込むファイルパス</param>
    /// <returns>ファイルのストリーム。ファイルを開けなかった場合はnullを返す。</returns>
    public delegate Stream CubismFileLoader(string file_path);
    
    public class CubismAsset : IDisposable
    {
        /// <summary>
        /// アセットを読み込む。
        /// </summary>
        /// <param name="model_file_path">読み込むmodel3.jsonへのファイルパス</param>
        /// <param name="reader">ファイルからデータを読み込むのに使うデリゲート</param>
        /// <returns>trueなら読み込みが成功したことを示す。</returns>
        public CubismAsset(string model_file_path, CubismFileLoader reader)
        {
            string base_dir = Path.GetDirectoryName(Path.GetFullPath(model_file_path));
            var model_setting = CubismModelSettingJson.Create(reader(model_file_path));
            
            // mocファイルを読み込み、CubismModelを作成する
            using (Stream stream = reader(Path.Combine(base_dir, model_setting.FileReferences.Moc)))
            {
                byte[] stream_bytes = new byte[stream.Length];
                stream.Read(stream_bytes, 0, (int)stream.Length);
                Moc = new CubismMoc(stream_bytes);
            }
            Model = Moc.CreateModel();

            // パラメータIDグループを読み込む
            // (まばたきやリップシンクに紐づいたパラメータIDのリスト)
            ParameterGroups = new Dictionary<string, CubismParameter[]>();
            if (model_setting.Groups != null)
            {
                foreach (var group in model_setting.Groups)
                {
                    if (group.Target == "Parameter")
                    {
                        // 重複除去のためにHashSetに読み込む
                        var id_names = new HashSet<string>(group.Ids);
                        var ids = new CubismParameter[id_names.Count];
                        int index = 0;
                        foreach (var id_name in id_names)
                        {
                            ids[index] = Model.GetParameter(id_name);
                            index++;
                        }
                        ParameterGroups.Add(group.Name, ids);
                    }
                }
            }
            Model.SaveParameters();

            // 呼吸を設定する
            //EyeBlinkController = new CubismEyeBlink(ParameterGroups["EyeBlink"]);
            //BreathController = new CubismBreath();
            //BreathController.SetParameter(new CubismBreathParameter(Model.GetParameter("ParamAngleX"), 0.0, 15.0, 6.5345, 0.5));
            //BreathController.SetParameter(new CubismBreathParameter(Model.GetParameter("ParamAngleY"), 0.0, 8.0, 3.5345, 0.5));
            //BreathController.SetParameter(new CubismBreathParameter(Model.GetParameter("ParamAngleZ"), 0.0, 10.0, 5.5345, 0.5));
            //BreathController.SetParameter(new CubismBreathParameter(Model.GetParameter("ParamBodyAngleX"), 0.0, 4.0, 15.5345, 0.5));
            //BreathController.SetParameter(new CubismBreathParameter(Model.GetParameter("ParamBreath"), 0.5, 0.5, 3.2345, 0.5));

            // モーションを読み込む
            MotionGroups = new Dictionary<string, ICubismMotion[]>();
            if (model_setting.FileReferences.Motions != null)
            {
                foreach (var item in model_setting.FileReferences.Motions)
                {
                    var group_name = item.Key;
                    var motion_group_item = item.Value;
                    var motion_group = new List<ICubismMotion>();
                    foreach (var motion_item in motion_group_item)
                    {
                        using (Stream stream = reader(Path.Combine(base_dir, motion_item.File)))
                        {
                            var motion = LoadMotion(stream, motion_item);
                            if (motion != null)
                            {
                                motion_group.Add(motion);
                            }
                        }
                    }
                    MotionGroups.Add(group_name, motion_group.ToArray());
                }
            }
            
            // 表情モーションを読み込む
            Expressions = new Dictionary<string, ICubismMotion>();
            if (model_setting.FileReferences.Expressions != null)
            {
                foreach (var expression_item in model_setting.FileReferences.Expressions)
                {
                    ICubismMotion expression;
                    using (Stream stream = reader(Path.Combine(base_dir, expression_item.File)))
                    {
                        expression = new CubismExpressionMotion(stream, Model);
                    }
                    if (expression != null)
                    {
                        Expressions.Add(expression_item.Name, expression);
                    }
                }
            }
            
            // ユーザーデータを読み込む
            UserData = new CubismUserData();
            if (string.IsNullOrEmpty(model_setting.FileReferences.UserData) == false)
            {
                using (Stream stream = reader(Path.Combine(base_dir, model_setting.FileReferences.UserData)))
                {
                    UserData = new CubismUserData(stream);
                }
            }
            
            // ヒットエリアを読み込む
            HitAreas = new Dictionary<string, CubismDrawable>();
            if (model_setting.HitAreas != null)
            {
                foreach (var hit_area in model_setting.HitAreas)
                {
                    CubismDrawable drawable = Model.GetDrawable(hit_area.Id);
                    if (drawable != null)
                    {
                        HitAreas.Add(hit_area.Name, drawable);
                    }
                }
            }

            // テクスチャを読み込む
            TextureByteArrays = new byte[model_setting.FileReferences.Textures.Length][];
            for (int index = 0; index < TextureByteArrays.Length; index++)
            {
                string path = Path.Combine(base_dir, model_setting.FileReferences.Textures[index]);
                using (Stream stream = reader(path))
                {
                    byte[] buffer = new byte[stream.Length];
                    stream.Read(buffer, 0, buffer.Length);
                    TextureByteArrays[index] = buffer;
                } 
            }

            // 物理演算データを読み込む
            // To do.

            // ポーズを読み込む
            if (string.IsNullOrEmpty(model_setting.FileReferences.Pose) == false)
            {
                using (Stream stream = reader(Path.Combine(base_dir, model_setting.FileReferences.Pose)))
                {
                    PoseController = new CubismPose(stream, Model);
                }
            }

            // モデル行列を計算する
            var (canvas_width, canvas_height) = Model.GetCanvasSize();
            ModelMatrix = new CubismModelMatrix(canvas_width, canvas_height);
            ModelMatrix.SetupFromLayout(model_setting.Layout);

            // モーションマネージャーを初期化する
            BaseMotionManager = new CubismMotionManager();
            ExpressionMotionManager = new CubismMotionManager();
            EffectMotionManager = new CubismMotionManager();
        }

        /// <summary>
        /// デストラクタ
        /// </summary>
        ~CubismAsset()
        {
            Dispose(false);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if ((Moc != null) && (Model != null))
                {
                    Moc.DeleteModel(Model);
                    Model = null;
                }
                if (disposing == true)
                {
                    TextureByteArrays = null;
                }
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion

        /// <summary>
        /// ストリームから'.motion3.json'形式のモーションを読み込む。
        /// </summary>
        /// <param name="stream">ストリーム</param>
        /// <param name="motion_item">モデル設定ファイル中の設定値</param>
        /// <returns>モーション</returns>
        internal CubismMotion LoadMotion(Stream stream, CubismModelSettingJson.FileReferencesItem.MotionItem motion_item)
        {
            // モーションを作成する
            var motion = new CubismMotion(stream, motion_item, Model);

            // パラメータグループをエフェクトに適用する
            foreach (var parameter_group in ParameterGroups)
            {
                var effect_id = motion.GetEffect(parameter_group.Key);
                if (effect_id != null)
                {
                    motion.SetEffectParameters(effect_id, parameter_group.Value);
                }
            }

            return motion;
        }

        /// <summary>
        /// ストリームから'.motion3.json'形式のモーションを読み込む。
        /// </summary>
        /// <param name="stream">ストリーム</param>
        /// <returns>モーション</returns>
        public CubismMotion LoadMotion(Stream stream)
        {
            return LoadMotion(stream, null);
        }


        /// <summary>
        /// モーションの再生を開始する。
        /// </summary>
        /// <param name="type">モーションの種類</param>
        /// <param name="motion">モーション</param>
        /// <param name="loop_enabled">ループが使えるなら有効化する。</param>
        /// <returns></returns>
        public CubismMotionQueueEntry StartMotion(MotionType type, ICubismMotion motion, bool loop_enabled = false)
        {
            switch (type)
            {
            case MotionType.Base:
                return BaseMotionManager.StartMotion(motion, loop_enabled);

            case MotionType.Expression:
                return ExpressionMotionManager.StartMotion(motion, loop_enabled);

            case MotionType.Effect:
                return EffectMotionManager.StartMotion(motion, loop_enabled);

            default:
                return null;
            }
        }





        /// <summary>
        /// モデルを更新する。
        /// モーション、ポーズなども更新される。
        /// </summary>
        /// <param name="elapsed_seconds">前回の更新時刻からの経過時間[秒]</param>
        public void Update(double elapsed_seconds)
        {
            if (elapsed_seconds < 0.0)
            {
                throw new ArgumentException();
            }
            
            // モーションを更新する
            // ベースモーションの更新後は、次の更新のために表情やエフェクトの影響を受ける前のパラメータを保存しておく
            Model.RestoreSavedParameters();
            BaseMotionManager.Update(elapsed_seconds);
            Model.SaveParameters();
            ExpressionMotionManager.Update(elapsed_seconds);
            EffectMotionManager.Update(elapsed_seconds);
            
            if (PoseController != null)
            {
                PoseController.UpdateParameters(elapsed_seconds);
            }

            Model.Update();


        }

        /// <summary>
        /// 指定した領域とのヒットテストを行う。
        /// 座標計算の際、モデル行列は考慮されるがMVP行列は考慮されない。
        /// </summary>
        /// <param name="name">ヒットテストする領域名</param>
        /// <param name="x">モデル座標系でのX座標</param>
        /// <param name="y">モデル座標系でのY座標</param>
        /// <returns></returns>
        public bool HitTest(string name, double x, double y)
        {
            if (HitAreas.TryGetValue(name, out CubismDrawable drawable) == false)
            {
                return false;
            }
            var model_matrix = ModelMatrix.Matrix;
            double tx = ((x - model_matrix[3, 0]) / model_matrix[0, 0]);
            double ty = ((x - model_matrix[3, 1]) / model_matrix[1, 1]);
            return drawable.BoundingBox.Contains((float)tx, (float)ty);
        }


        






        /// <summary>
        /// Mocデータ
        /// </summary>
        private CubismMoc Moc;

        /// <summary>
        /// Modelデータ
        /// </summary>
        public CubismModel Model { get; private set; }

        /// <summary>
        /// ポーズコントローラ
        /// </summary>
        private CubismPose PoseController;

        /// <summary>
        /// MotionType.Baseのモーションを制御する
        /// </summary>
        private CubismMotionManager BaseMotionManager;

        /// <summary>
        /// MotionType.Expressionのモーションを制御する
        /// </summary>
        private CubismMotionManager ExpressionMotionManager;

        /// <summary>
        /// MotionType.Effectのモーションを制御する
        /// </summary>
        private CubismMotionManager EffectMotionManager;

        //public CubismEyeBlink EyeBlinkController;

        //public CubismBreath BreathController;

        /// <summary>
        /// ベースモーションの連想配列
        /// </summary>
        public Dictionary<string, ICubismMotion[]> MotionGroups;

        /// <summary>
        /// 表情モーションの連想配列
        /// </summary>
        public Dictionary<string, ICubismMotion> Expressions;

        /// <summary>
        /// ユーザーデータ
        /// </summary>
        public CubismUserData UserData;

        /// <summary>
        /// パラメータグループの連想配列
        /// </summary>
        public Dictionary<string, CubismParameter[]> ParameterGroups;

        /// <summary>
        /// ヒットエリアの連想配列
        /// </summary>
        public Dictionary<string, CubismDrawable> HitAreas;

        /// <summary>
        /// テクスチャファイルのバイト配列のリスト
        /// </summary>
        internal byte[][] TextureByteArrays;
        
        /// <summary>
        /// モデル行列
        /// </summary>
        private CubismModelMatrix ModelMatrix;
        
        /// <summary>
        /// モデルの色。
        /// RGBAの4つの要素からなる配列。
        /// </summary>
        public float[] ModelColor = { 1.0f, 1.0f, 1.0f, 1.0f };

        /// <summary>
        /// モーションの種別
        /// </summary>
        public enum MotionType
        {
            /// <summary>
            /// ベースモーション。
            /// 前フレームで行われたパラメータの変更に引き続きパラメータを更新する。
            /// </summary>
            Base,

            /// <summary>
            /// 表情モーション。
            /// エフェクトが及ぼしたパラメータへの変更は次のフレームには引き継がれない。
            /// </summary>
            Expression,

            /// <summary>
            /// エフェクトとなるモーション。
            /// エフェクトが及ぼしたパラメータへの変更は次のフレームには引き継がれない。
            /// </summary>
            Effect
        }
    }
}
