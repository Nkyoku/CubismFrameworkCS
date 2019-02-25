using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

namespace CubismFramework
{
    public static class CubismCore
    {
        /// <summary>Necessary alignment for mocs (in bytes).</summary>
        public const int CsmAlignofMoc = 64;

        /// <summary>Necessary alignment for models (in bytes).</summary>
        public const int CsmAlignofModel = 16;

        /// <summary>Bit masks for non-dynamic drawable flags.</summary>
        [Flags]
        public enum ConstantDrawableFlags
        {
            /// <summary>Normal blend mode value.</summary>
            BlendNormal = 0x0,

            /// <summary>Additive blend mode mask.</summary>
            BlendAdditive = 0x1,

            /// <summary>Multiplicative blend mode mask.</summary>
            BlendMultiplicative = 0x2,

            /// <summary>Double-sidedness mask.</summary>
            IsDoubleSided = 0x4
        };

        /// <summary>Bit masks for dynamic drawable flags.</summary>
        [Flags]
        public enum DynamicDrawableFlags
        {
            /// <summary>Flag set when visible.</summary>
            IsVisible = 0x1,

            /// <summary>Flag set when visibility did change.</summary>
            VisibilityDidChange = 0x2,

            /// <summary>Flag set when opacity did change.</summary>
            OpacityDidChange = 0x4,

            /// <summary>Flag set when draw order did change.</summary>
            DrawOrderDidChange = 0x8,

            /// <summary>Flag set when render order did change.</summary>
            RenderOrderDidChange = 0x10,

            /// <summary>Flag set when vertex positions did change.</summary>
            VertexPositionsDidChange = 0x20
        };

        /// <summary>Queries Core version.</summary>
        /// <returns>Core version.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetVersion")]
        public static extern uint CsmGetVersion();

        /// <summary>バージョン情報を格納する構造体</summary>
        public struct Version
        {
            public uint Number;
            public int Major, Minor, Patch;
        }

        /// <summary>Coreのバージョン情報を返します。</summary>
        /// <returns>バージョン情報</returns>
        public static Version GetVersion()
        {
            uint version_number = CsmGetVersion();
            Version version;
            version.Number = version_number;
            version.Major = (int)((version_number >> 24) & 0xFF);
            version.Minor = (int)((version_number >> 16) & 0xFF);
            version.Patch = (int)(version_number & 0xFFFF);
            return version;
        }
        
        /// <summary>Queries log handler.</summary>
        /// <returns>Log handler.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetLogFunction")]
        public static extern IntPtr CsmGetLogFunction();
        
        /// <summary>Sets log handler.</summary>
        /// <param name="handler">Handler to use.</param>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmSetLogFunction")]
        public static extern void CsmSetLogFunction(IntPtr handler);

        /// <summary>Tries to revive a moc from bytes in place.</summary>
        /// <param name="address">Address of unrevived moc. The address must be aligned to 'csmAlignofMoc'.</param>
        /// <param name="size">Size of moc (in bytes).</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmReviveMocInPlace")]
        public static extern IntPtr CsmReviveMocInPlace(IntPtr address, int size);
        
        /// <summary>Queries size of a model in bytes.</summary>
        /// <param name="moc">Moc to query.</param>
        /// <returns>Valid size on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetSizeofModel")]
        public static extern int CsmGetSizeofModel(IntPtr moc);
        
        /// <summary>Tries to instantiate a model in place.</summary>
        /// <param name="moc">Source moc.</param>
        /// <param name="address">Address to place instance at. Address must be aligned to 'csmAlignofModel'.</param>
        /// <param name="size">Size of memory block for instance (in bytes).</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmInitializeModelInPlace")]
        public static extern IntPtr CsmInitializeModelInPlace(IntPtr moc, IntPtr address, int size);
        
        /// <summary>Updates a model.</summary>
        /// <param name="model">Model to update.</param>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmUpdateModel")]
        public static extern void CsmUpdateModel(IntPtr model);

        /// <summary>Reads info on a model canvas.</summary>
        /// <param name="model">Model to query.</param>
        /// <param name="outSizeInPixels">Canvas dimensions.</param>
        /// <param name="outOriginInPixels">Origin of model on canvas.</param>
        /// <param name="outPixelsPerUnit">Aspect used for scaling pixels to units.</param>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmReadCanvasInfo")]
        public static extern void CsmReadCanvasInfo(
            IntPtr model,
            [In, Out] float[] outSizeInPixels,
            [In, Out] float[] outOriginInPixels,
            out float outPixelsPerUnit);

        /// <summary>Gets number of parameters.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid count on success; '-1' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetParameterCount")]
        public static extern int CsmGetParameterCount(IntPtr model);

        /// <summary>Gets parameter IDs. All IDs are null-terminated ANSI strings.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetParameterIds")]
        public static extern IntPtr CsmGetParameterIds(IntPtr model);

        /// <summary>Gets minimum parameter values.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetParameterMinimumValues")]
        public static extern IntPtr CsmGetParameterMinimumValues(IntPtr model);

        /// <summary>Gets maximum parameter values.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetParameterMaximumValues")]
        public static extern IntPtr CsmGetParameterMaximumValues(IntPtr model);

        /// <summary>Gets default parameter values.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetParameterDefaultValues")]
        public static extern IntPtr CsmGetParameterDefaultValues(IntPtr model);

        /// <summary>Gets read/write parameter values buffer.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetParameterValues")]
        public static extern IntPtr CsmGetParameterValues(IntPtr model);

        /// <summary>Gets number of parts.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid count on success; '-1' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetPartCount")]
        public static extern int CsmGetPartCount(IntPtr model);

        /// <summary>Gets parts IDs. All IDs are null-terminated ANSI strings.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetPartIds")]
        public static extern IntPtr CsmGetPartIds(IntPtr model);

        /// <summary>Gets read/write part opacities buffer.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetPartOpacities")]
        public static extern IntPtr CsmGetPartOpacities(IntPtr model);

        /// <summary>Gets part's parent part indices.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetPartParentPartIndices")]
        public static extern IntPtr CsmGetPartParentPartIndices(IntPtr model);

        /// <summary>Gets number of drawables.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid count on success; '-1' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableCount")]
        public static extern int CsmGetDrawableCount(IntPtr model);

        /// <summary>Gets drawable IDs. All IDs are null-terminated ANSI strings.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableIds")]
        public static extern IntPtr CsmGetDrawableIds(IntPtr model);

        /// <summary>Gets constant drawable flags.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableConstantFlags")]
        public static extern IntPtr CsmGetDrawableConstantFlags(IntPtr model);

        /// <summary>Gets dynamic drawable flags.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableDynamicFlags")]
        public static extern IntPtr CsmGetDrawableDynamicFlags(IntPtr model);

        /// <summary>Gets drawable texture indices.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableTextureIndices")]
        public static extern IntPtr CsmGetDrawableTextureIndices(IntPtr model);

        /// <summary>Gets drawable draw orders.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableDrawOrders")]
        public static extern IntPtr CsmGetDrawableDrawOrders(IntPtr model);

        /// <summary>Gets drawable render orders. The higher the order, the more up front a drawable is.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableRenderOrders")]
        public static extern IntPtr CsmGetDrawableRenderOrders(IntPtr model);

        /// <summary>Gets drawable opacities.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableOpacities")]
        public static extern IntPtr CsmGetDrawableOpacities(IntPtr model);

        /// <summary>Gets numbers of masks of each drawable.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableMaskCounts")]
        public static extern IntPtr CsmGetDrawableMaskCounts(IntPtr model);

        /// <summary>Gets mask indices of each drawable.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableMasks")]
        public static extern IntPtr CsmGetDrawableMasks(IntPtr model);

        /// <summary>Gets number of vertices of each drawable.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableVertexCounts")]
        public static extern IntPtr CsmGetDrawableVertexCounts(IntPtr model);

        /// <summary>Gets vertex position data of each drawable.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableVertexPositions")]
        public static extern IntPtr CsmGetDrawableVertexPositions(IntPtr model);

        /// <summary>Gets texture coordinate data of each drawables.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableVertexUvs")]
        public static extern IntPtr CsmGetDrawableVertexUvs(IntPtr model);

        /// <summary>Gets number of triangle indices for each drawable.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableIndexCounts")]
        public static extern IntPtr CsmGetDrawableIndexCounts(IntPtr model);

        /// <summary>Gets triangle index data for each drawable.</summary>
        /// <param name="model">Model to query.</param>
        /// <returns>Valid pointer on success; '0' otherwise.</returns>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmGetDrawableIndices")]
        public static extern IntPtr CsmGetDrawableIndices(IntPtr model);

        /// <summary>Resets all dynamic drawable flags.</summary>
        /// <param name="model">Model containing flags.</param>
        [DllImport("Live2DCubismCore.dll", EntryPoint = "csmResetDrawableDynamicFlags")]
        public static extern void CsmResetDrawableDynamicFlags(IntPtr model);
    }
}
