using System;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using CubismFramework;
using MathNet.Numerics.LinearAlgebra.Single;
using OpenGL;

namespace TestForms
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        
        private void OnCreated(object sender, OpenGL.GlControlEventArgs e)
        {
            // モデルをリソースから読み込む
            // 第2引数にファイル名からリソースを読み込む関数を与える
            Asset = new CubismAsset(@"hiyori_free_t06.model3.json", (string file_path) =>
            {
                // リソースは拡張子を持たず、他のピリオドもアンダーバーに変換されるのでそれに応じて文字列を加工する
                // 普通、画像ファイルはBitmapとして読み込まれるが、resxファイルを編集してあるのでbyte[]として読み込まれる
                string file_name = Path.GetFileNameWithoutExtension(file_path);
                string resource_name = file_name.Replace('.', '_');
                byte[] byte_array = (byte[])Hiyori.ResourceManager.GetObject(resource_name);
                return new MemoryStream(byte_array);
            });

            // 自動まばたきを設定する
            // 自動まばたきで設定するパラメータはmodel3.json中にパラメータグループ"EyeBlink"で指定されている
            var eye_blink_controller = new CubismEyeBlink(Asset.ParameterGroups["EyeBlink"]);
            Asset.StartMotion(CubismAsset.MotionType.Effect, eye_blink_controller);

            // OpenGL.Netを使ったレンダラーを作成する
            Renderer = new CubismOpenGlNetRenderer();
            RenderingManager = new CubismRenderingManager(Renderer, Asset);

            Timer = Stopwatch.StartNew();
        }

        private void OnDestroying(object sender, OpenGL.GlControlEventArgs e)
        {
            RenderingManager.Dispose();
            RenderingManager = null;
            Asset.Dispose();
            Asset = null;
            Renderer.Dispose();
            Renderer = null;
        }

        private void OnUpdate(object sender, OpenGL.GlControlEventArgs e)
        {

        }

        private void OnRender(object sender, OpenGL.GlControlEventArgs e)
        {
            GlControl gl_control = (GlControl)sender;


            if ((LastMotion == null) || (LastMotion.Finished == true))
            {
                // モーションをランダムに再生する
                // 名前なしのモーショングループから0～9番のモーションを乱数で選ぶ
                var motion_group = Asset.MotionGroups[""];
                int number = new Random().Next() % motion_group.Length;
                var motion = (CubismMotion)motion_group[number];
                LastMotion = Asset.StartMotion(CubismAsset.MotionType.Base, motion, false);
            }

            // モデルを更新する
            var elapsed = Timer.Elapsed;
            Timer.Restart();
            Asset.Update(elapsed.TotalSeconds);

            // モデルを描画する
            Gl.ClearColor(0.0f, 0.5f, 0.5f, 1.0f);
            Gl.Clear(ClearBufferMask.ColorBufferBit);
            Gl.Viewport(0, 0, gl_control.Width, gl_control.Height);
            Matrix mvp_matrix = DenseMatrix.CreateIdentity(4);
            mvp_matrix[0, 0] = 2.0f;
            mvp_matrix[1, 1] = 2.0f * gl_control.Width / gl_control.Height;
            RenderingManager.Draw(mvp_matrix);
        }

        CubismAsset Asset;

        CubismRenderingManager RenderingManager;

        CubismOpenGlNetRenderer Renderer;

        CubismMotionQueueEntry LastMotion;

        Stopwatch Timer;
    }
}
