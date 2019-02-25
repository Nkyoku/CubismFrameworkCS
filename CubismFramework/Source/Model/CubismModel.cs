using System;
using System.Runtime.InteropServices;

namespace CubismFramework
{
    public class CubismModel
    {
        /// <summary>
        /// コンストラクタ。
        /// バッファへのポインタとモデル構造体へのポインタからモデルオブジェクトを作成する
        /// </summary>
        /// <param name="buffer_ptr">バッファへのポインタ</param>
        /// <param name="model_ptr">モデル構造体へのポインタ</param>
        public CubismModel(IntPtr buffer_ptr, IntPtr model_ptr)
        {
            BufferPtr = buffer_ptr;
            ModelPtr = model_ptr;
            InitializeParameters();
            InitializeParts();
            InitializeDrawables();
        }

        /// <summary>
        /// コアからパラメータ情報を読み出す。
        /// </summary>
        private void InitializeParameters()
        {
            
            int count = CubismCore.CsmGetParameterCount(ModelPtr);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            // パラメータの現在値、最大値、最小値、デフォルト値を取得する
            ParameterValues = IntPtrToFloatArray(CubismCore.CsmGetParameterValues(ModelPtr), count);
            var maximum_values = IntPtrToFloatArray(CubismCore.CsmGetParameterMaximumValues(ModelPtr), count);
            var minimum_values = IntPtrToFloatArray(CubismCore.CsmGetParameterMinimumValues(ModelPtr), count);
            var default_values = IntPtrToFloatArray(CubismCore.CsmGetParameterDefaultValues(ModelPtr), count);

            // パラメータIDを取得し、IDマネージャを作成する
            string[] id_name_list = IntPtrToStringArray(CubismCore.CsmGetParameterIds(ModelPtr), count);
            ParameterManager = new CubismIdManager<CubismParameter>(count);
            for (int index = 0; index < count; index++)
            {
                var id = new CubismParameter(id_name_list[index], index, minimum_values[index], maximum_values[index], default_values[index], ParameterValues);
                ParameterManager.RegisterId(id);
            }
        }

        /// <summary>
        /// コアからパーツ情報を読み出す。
        /// </summary>
        private void InitializeParts()
        {
            int count = CubismCore.CsmGetPartCount(ModelPtr);
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException();
            }

            // パーツの不透明度を取得する
            PartOpacities = IntPtrToFloatArray(CubismCore.CsmGetPartOpacities(ModelPtr), count);

            // パーツIDを取得し、IDマネージャを作成する
            string[] id_name_list = IntPtrToStringArray(CubismCore.CsmGetPartIds(ModelPtr), count);
            PartManager = new CubismIdManager<CubismPart>(count);
            for (int index = 0; index < count; index++)
            {
                var id = new CubismPart(id_name_list[index], index, PartOpacities);
                PartManager.RegisterId(id);
            }
        }
        
        /// <summary>
        /// コアからDrawable情報を読み出す。
        /// </summary>
        private void InitializeDrawables()
        {
            int drawable_count = CubismCore.CsmGetDrawableCount(ModelPtr);
            Drawables = new CubismDrawable[drawable_count];

            // DrawableのID名リストを取得する
            string[] drawable_name_list = IntPtrToStringArray(CubismCore.CsmGetDrawableIds(ModelPtr), drawable_count);

            // テクスチャ番号を読み出す
            int[] texture_indexes = DrawableTextureIndexList;

            // 静的フラグを読み出す
            byte[] constant_flags = DrawableConstantFlagsList;

            // クリッピングマスクを読み出す
            int[] mask_counts = DrawableClippingMaskCountList;
            IntPtr[] mask_ptrs = DrawableClippingMaskPointerList;

            // Drawablesを構築する
            for (int index = 0; index < drawable_count; index++)
            {
                string name = drawable_name_list[index];
                int texture_index = texture_indexes[index];
                var flags = (CubismCore.ConstantDrawableFlags)constant_flags[index];
                var masks = IntPtrToIntArray(mask_ptrs[index], mask_counts[index]);
                Drawables[index] = new CubismDrawable(index, name, texture_index, flags, masks);
            }
        }

        /// <summary>
        /// デストラクタ。
        /// </summary>
        ~CubismModel()
        {
            if (BufferPtr != null)
            {
                Marshal.FreeCoTaskMem(BufferPtr);
            }
        }

        /// <summary>
        /// モデルのパラメータを更新する。
        /// </summary>
        public void Update()
        {
            // 現在のパラメータ設定値をコアに戻す
            Marshal.Copy(ParameterValues, 0, CubismCore.CsmGetParameterValues(ModelPtr), ParameterCount);
            Marshal.Copy(PartOpacities, 0, CubismCore.CsmGetPartOpacities(ModelPtr), PartCount);

            // 次のパラメータを計算する
            CubismCore.CsmUpdateModel(ModelPtr);

            // 新しいパラメータ設定値をコアから読み出す
            Marshal.Copy(CubismCore.CsmGetParameterValues(ModelPtr), ParameterValues, 0, ParameterCount);
            Marshal.Copy(CubismCore.CsmGetPartOpacities(ModelPtr), PartOpacities, 0, PartCount);

            // Drawableの情報をコアから読み出す
            UpdateDrawables();

            // 動的フラグをクリアする
            CubismCore.CsmResetDrawableDynamicFlags(ModelPtr);
        }

        /// <summary>
        /// コアからデータを読み出してDrawablesを更新する。
        /// </summary>
        private void UpdateDrawables()
        {
            int drawable_count = Drawables.Length;

            // 動的フラグのリストを読み出す
            byte[] dynamic_flags = DrawableDynamicFlagsList;

            // 不透明度のリストを読み出す
            float[] opacities = DrawableOpacityList;

            // インデックスバッファのポインタを読み出す
            int[] index_counts = DrawableIndexBufferLengthList;
            IntPtr[] index_buffer_ptrs = DrawableIndexBufferPointerList;

            // 頂点バッファとUVバッファのポインタを読み出す
            int[] vertex_counts = DrawableVertexBufferLengthList;
            IntPtr[] vertex_buffer_ptrs = DrawableVertexBufferPointerList;
            IntPtr[] uv_buffer_ptrs = DrawableUvBufferPointerList;

            foreach (var drawable in Drawables)
            {
                int index = drawable.Index;

                // インデックスバッファや頂点バッファへデータをコピー
                // 配列サイズが変わらないならばバッファを再利用する
                int index_length = index_counts[index];
                int vertex_length = 2 * vertex_counts[index];
                bool reuse_index_buffer = (drawable.IndexBuffer != null) && (drawable.IndexBuffer.Length == index_length);
                bool reuse_vertex_buffer = (drawable.VertexBuffer != null) && (drawable.VertexBuffer.Length == vertex_length);
                bool reuse_uv_buffer = (drawable.UvBuffer != null) && (drawable.UvBuffer.Length == vertex_length);
                short[] index_buffer = reuse_index_buffer ? drawable.IndexBuffer : new short[index_length];
                float[] vertex_buffer = reuse_vertex_buffer ? drawable.VertexBuffer : new float[vertex_length];
                float[] uv_buffer = reuse_uv_buffer ? drawable.UvBuffer : new float[vertex_length];
                Marshal.Copy(index_buffer_ptrs[index], index_buffer, 0, index_length);
                Marshal.Copy(vertex_buffer_ptrs[index], vertex_buffer, 0, vertex_length);
                Marshal.Copy(uv_buffer_ptrs[index], uv_buffer, 0, vertex_length);

                // 更新する
                double opacity = opacities[index];
                var flags = (CubismCore.DynamicDrawableFlags)dynamic_flags[index];
                drawable.Update(opacity, vertex_buffer, uv_buffer, index_buffer, flags);
            }
        }

        /// <summary>
        /// キャンバスのサイズを取得する。
        /// </summary>
        /// <returns>キャンバスの幅、高さ</returns>
        public (double width, double height) GetCanvasSize()
        {
            float[] size_in_pixels = new float[2];
            float[] origin_in_pixels = new float[2];
            CubismCore.CsmReadCanvasInfo(ModelPtr, size_in_pixels, origin_in_pixels, out float pixels_per_unit);
            return (size_in_pixels[0] / pixels_per_unit, size_in_pixels[1] / pixels_per_unit);
        }
        
        /// <summary>
        /// パーツの個数。
        /// </summary>
        public int PartCount
        {
            get { return PartManager.Count; }
        }

        /// <summary>
        /// パーツインデックスからパーツオブジェクトを取得する。
        /// </summary>
        /// <param name="name">パーツインデックス</param>
        /// <returns>パーツオブジェクト</returns>
        public CubismPart GetPart(int index)
        {
            return PartManager.GetId(index);
        }

        /// <summary>
        /// パーツID名からパーツオブジェクトを取得する。
        /// </summary>
        /// <param name="name">パーツID名</param>
        /// <returns>パーツオブジェクト</returns>
        public CubismPart GetPart(string name)
        {
            return PartManager.GetId(name);
        }
        
        /// <summary>
        /// パラメータの個数。
        /// </summary>
        public int ParameterCount
        {
            get { return ParameterManager.Count; }
        }

        /// <summary>
        /// パラメータインデックスからパラメータオブジェクトを取得する。
        /// </summary>
        /// <param name="name">パラメータインデックス</param>
        /// <returns>パラメータオブジェクト</returns>
        public CubismParameter GetParameter(int index)
        {
            return ParameterManager.GetId(index);
        }

        /// <summary>
        /// パラメータID名からパラメータオブジェクトを取得する。
        /// </summary>
        /// <param name="name">パラメータID名</param>
        /// <returns>パラメータオブジェクト</returns>
        public CubismParameter GetParameter(string name)
        {
            return ParameterManager.GetId(name);
        }
        
        /// <summary>
        /// Drawableの数。
        /// </summary>
        public int DrawableCount
        {
            get { return Drawables.Length; }
        }

        /// <summary>
        /// インデックスからCubismDrawableを取得する。
        /// </summary>
        /// <param name="drawable_index">Drawableのインデックス</param>
        /// <returns>CubismDrawableオブジェクト</returns>
        public CubismDrawable GetDrawable(int drawable_index)
        {
            if ((0 <= drawable_index) && (drawable_index < DrawableCount))
            {
                return Drawables[drawable_index];
            }
            return null;
        }

        /// <summary>
        /// ID名からCubismDrawableを取得する。
        /// </summary>
        /// <param name="drawable_name">DrawableのID名</param>
        /// <returns>CubismDrawableオブジェクト</returns>
        public CubismDrawable GetDrawable(string drawable_name)
        {
            foreach(var drawable in Drawables)
            {
                if (drawable.Name == drawable_name)
                {
                    return drawable;
                }
            }
            return null;
        }
        
        /// <summary>
        /// Drawableの描画順リストを取得する。
        /// </summary>
        /// <returns>Drawableの描画順リスト</returns>
        public int[] GetDrawableRenderOrders()
        {
            return IntPtrToIntArray(CubismCore.CsmGetDrawableRenderOrders(ModelPtr), DrawableCount);
        }

        /// <summary>
        /// Drawableのテクスチャインデックスのリスト。
        /// </summary>
        private int[] DrawableTextureIndexList
        {
            get {
                int[] result = new int[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableTextureIndices(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// Drawableのインデックスバッファの要素数のリスト。
        /// ポリゴン1つあたり3。
        /// </summary>
        internal int[] DrawableIndexBufferLengthList
        {
            get
            {
                int[] result = new int[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableIndexCounts(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// Drawableの頂点バッファの要素数のリスト。
        /// 頂点1つあたり1。
        /// </summary>
        internal int[] DrawableVertexBufferLengthList
        {
            get
            {
                int[] result = new int[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableVertexCounts(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// Drawableのインデックスバッファへのポインタのリスト。
        /// </summary>
        internal IntPtr[] DrawableIndexBufferPointerList
        {
            get
            {
                IntPtr[] result = new IntPtr[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableIndices(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// Drawableの頂点バッファへのポインタのリスト。
        /// </summary>
        internal IntPtr[] DrawableVertexBufferPointerList
        {
            get
            {
                IntPtr[] result = new IntPtr[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableVertexPositions(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// DrawableのUVバッファへのポインタのリスト。
        /// </summary>
        internal IntPtr[] DrawableUvBufferPointerList
        {
            get
            {
                IntPtr[] result = new IntPtr[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableVertexUvs(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// Drawableの不透明度のリスト。
        /// </summary>
        private float[] DrawableOpacityList
        {
            get
            {
                float[] result = new float[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableOpacities(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// Drawableの静的フラグのリスト。
        /// CubismCore.ConstantDrawableFlagsにキャストして使用する。
        /// </summary>
        private byte[] DrawableConstantFlagsList
        {
            get
            {
                byte[] result = new byte[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableConstantFlags(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// Drawableの動的フラグのリスト。
        /// CubismCore.DynamicDrawableFlagsにキャストして使用する。
        /// </summary>
        private byte[] DrawableDynamicFlagsList
        {
            get
            {
                byte[] result = new byte[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableDynamicFlags(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// Drawableのクリッピングマスクの個数のリスト。
        /// </summary>
        private int[] DrawableClippingMaskCountList
        {
            get
            {
                int[] result = new int[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableMaskCounts(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }

        /// <summary>
        /// Drawableのクリッピングマスクのリストへのポインタのリスト。
        /// </summary>
        private IntPtr[] DrawableClippingMaskPointerList
        {
            get
            {
                IntPtr[] result = new IntPtr[DrawableCount];
                Marshal.Copy(CubismCore.CsmGetDrawableMasks(ModelPtr), result, 0, DrawableCount);
                return result;
            }
        }
        
        /// <summary>
        /// クリッピングマスクを使用しているかどうか？
        /// </summary>
        public bool IsUsingClippingMask
        {
            get
            {
                foreach (int count in DrawableClippingMaskCountList)
                {
                    if (0 < count)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        
        /// <summary>
        /// 現在のパラメータの値を退避する。
        /// </summary>
        public void SaveParameters()
        {
            if ((SavedParameterValues == null) || (SavedParameterValues.Length != ParameterCount))
            {
                SavedParameterValues = new float[ParameterCount];
            }
            Array.Copy(ParameterValues, SavedParameterValues, ParameterCount);
        }

        /// <summary>
        /// 退避したパラメータの値を復帰する。
        /// </summary>
        public void RestoreSavedParameters()
        {
            if (SavedParameterValues != null)
            {
                Array.Copy(SavedParameterValues, ParameterValues, ParameterCount);
            }
        }

        /// <summary>
        /// パラメータに初期値を入れる。
        /// </summary>
        public void RestoreDefaultParameters()
        {
            for (int index = 0; index < ParameterCount; index++)
            {
                ParameterValues[index] = (float)ParameterManager.GetId(index).Default;
            }
        }

        /// <summary>
        /// ネイティブポインタからbyte配列をコピーする
        /// </summary>
        /// <param name="ptr">コピー元の配列へのポインタ</param>
        /// <param name="count">コピー元の配列の項目数</param>
        /// <returns>コピーされた配列</returns>
        private static byte[] IntPtrToByteArray(IntPtr ptr, int count)
        {
            var result = new byte[count];
            Marshal.Copy(ptr, result, 0, count);
            return result;
        }

        /// <summary>
        /// ネイティブポインタからshort配列をコピーする
        /// </summary>
        /// <param name="ptr">コピー元の配列へのポインタ</param>
        /// <param name="count">コピー元の配列の項目数</param>
        /// <returns>コピーされた配列</returns>
        internal static short[] IntPtrToShortArray(IntPtr ptr, int count)
        {
            var result = new short[count];
            Marshal.Copy(ptr, result, 0, count);
            return result;
        }

        /// <summary>
        /// ネイティブポインタからint配列をコピーする
        /// </summary>
        /// <param name="ptr">コピー元の配列へのポインタ</param>
        /// <param name="count">コピー元の配列の項目数</param>
        /// <returns>コピーされた配列</returns>
        private static int[] IntPtrToIntArray(IntPtr ptr, int count)
        {
            var result = new int[count];
            Marshal.Copy(ptr, result, 0, count);
            return result;
        }

        /// <summary>
        /// ネイティブポインタからfloat配列をコピーする
        /// </summary>
        /// <param name="ptr">コピー元の配列へのポインタ</param>
        /// <param name="count">コピー元の配列の項目数</param>
        /// <returns>コピーされた配列</returns>
        internal static float[] IntPtrToFloatArray(IntPtr ptr, int count)
        {
            var result = new float[count];
            Marshal.Copy(ptr, result, 0, count);
            return result;
        }
        
        /// <summary>
        /// ネイティブポインタからstring配列をコピーする
        /// </summary>
        /// <param name="ptr">コピー元の配列へのポインタ</param>
        /// <param name="count">コピー元の配列の項目数</param>
        /// <returns>コピーされた配列</returns>
        private static string[] IntPtrToStringArray(IntPtr ptr, int count)
        {
            var result = new string[count];
            for(int index = 0; index < count; index++)
            {
                IntPtr id_name_ptr = Marshal.PtrToStructure<IntPtr>(ptr);
                ptr = IntPtr.Add(ptr, IntPtr.Size);
                result[index] = Marshal.PtrToStringAnsi(id_name_ptr);
            }
            return result;
        }

        /// <summary>
        /// モデルデータを格納するバッファへのポインタ
        /// </summary>
        private IntPtr BufferPtr;

        /// <summary>
        /// モデルデータへのポインタ
        /// </summary>
        private IntPtr ModelPtr;

        /// <summary>
        /// パラメータIDマネージャー
        /// </summary>
        private CubismIdManager<CubismParameter> ParameterManager;

        /// <summary>
        /// パーツIDマネージャー
        /// </summary>
        private CubismIdManager<CubismPart> PartManager;
        
        /// <summary>
        /// パラメータの値のリスト。
        /// この値はParameterIdManagerで管理されるパラメータIDオブジェクトと連動する。
        /// </summary>
        private float[] ParameterValues;

        /// <summary>
        /// パーツの不透明度のリスト。
        /// この値はPartIdManagerで管理されるパーツIDオブジェクトと連動する
        /// </summary>
        private float[] PartOpacities;

        /// <summary>
        /// 保存されたパラメータの値
        /// </summary>
        private float[] SavedParameterValues;

        /// <summary>
        /// Drawableへのアクセスオブジェクトのリスト
        /// </summary>
        private CubismDrawable[] Drawables;
    }
}
