using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using CubismFramework;

namespace CubismFramework
{
    public class CubismMoc
    {
        // Mocデータを格納するバッファへのポインタ
        private IntPtr BufferPtr;

        // Mocデータへのポインタ
        private IntPtr MocPtr;
        
        // Mocデータから作られたモデルの個数
        private int ModelCount = 0;
        
        /// <summary>
        /// コンストラクタ。
        /// MocデータからMocを生成する。
        /// </summary>
        /// <param name="moc_bytes"></param>
        public CubismMoc(byte[] moc_bytes)
        {
            // バッファを確保し、CsmAlignofMocで指定されたバイト数にアライメントする
            BufferPtr = Marshal.AllocCoTaskMem(moc_bytes.Length + CubismCore.CsmAlignofMoc - 1);
            IntPtr aligned_buffer = AlignPointer(BufferPtr, CubismCore.CsmAlignofMoc);

            // Mocを生成する
            Marshal.Copy(moc_bytes, 0, aligned_buffer, moc_bytes.Length);
            MocPtr = CubismCore.CsmReviveMocInPlace(aligned_buffer, moc_bytes.Length);
            if (MocPtr == IntPtr.Zero)
                throw new ArgumentException();
        }
        
        /// <summary>
        /// デストラクタ。
        /// このMocから作成されたモデルがすべて削除されていない場合は例外を返す。
        /// </summary>
        ~CubismMoc()
        {
            Debug.Assert(ModelCount == 0);
            if (BufferPtr != null)
                Marshal.FreeCoTaskMem(BufferPtr);
        }
        
        /// <summary>
        /// モデルを作成する。
        /// </summary>
        /// <returns>作成されたモデル</returns>
        public CubismModel CreateModel()
        {
            // バッファを確保し、CsmAlignofModelで指定されたバイト数にアライメントする
            int size = CubismCore.CsmGetSizeofModel(MocPtr);
            IntPtr model_buffer_ptr = Marshal.AllocCoTaskMem(size + CubismCore.CsmAlignofModel - 1);
            IntPtr aligned_model_buffer_ptr = AlignPointer(model_buffer_ptr, CubismCore.CsmAlignofModel);
            IntPtr model_ptr = CubismCore.CsmInitializeModelInPlace(MocPtr, aligned_model_buffer_ptr, size);
            if (MocPtr == IntPtr.Zero)
            {

                return null;
            }
            ModelCount++;
            return new CubismModel(model_buffer_ptr, model_ptr);
        }

        /// <summary>
        /// モデルを削除する。
        /// </summary>
        /// <param name="model">削除するモデル</param>
        public void DeleteModel(CubismModel model)
        {
            if (model != null)
            {
                ModelCount--;
                Debug.Assert(0 <= ModelCount);
            }   
            model = null;
        }

        /// <summary>
        /// ポインタをアライメントに合わせる。
        /// </summary>
        /// <param name="unaligned_pointer">アライメントを合わせたいポインタ</param>
        /// <param name="alignment">アライメント</param>
        /// <returns>アライメントの合ったポインタ</returns>
        private IntPtr AlignPointer(IntPtr unaligned_pointer, int alignment)
        {
            IntPtr aligned_pointer;
            int offset = 0;
            do
            {
                aligned_pointer = IntPtr.Add(unaligned_pointer, offset);
                offset++;
            }
            while (((ulong)aligned_pointer % (ulong)alignment) != 0);
            return aligned_pointer;
        }
    }
}
