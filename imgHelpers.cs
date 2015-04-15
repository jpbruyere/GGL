using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
//using System.Windows.Media.Imaging;
using System.Windows;
using System.Drawing;

namespace imgHelpers
{
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe")]
    public interface IShellItem
    {
        void BindToHandler(IntPtr pbc,
            [MarshalAs(UnmanagedType.LPStruct)]Guid bhid,
            [MarshalAs(UnmanagedType.LPStruct)]Guid riid,
            out IntPtr ppv);

        void GetParent(out IShellItem ppsi);

        void GetDisplayName(SIGDN sigdnName, out IntPtr ppszName);

        void GetAttributes(uint sfgaoMask, out uint psfgaoAttribs);

        void Compare(IShellItem psi, uint hint, out int piOrder);
    };
    [ComImportAttribute()]
    [GuidAttribute("bcc18b79-ba16-442f-80c4-8a59c30c463b")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IShellItemImageFactory
    {
        void GetImage(
        [In, MarshalAs(UnmanagedType.Struct)] SIZE size,
        [In] SIIGBF flags,
        [Out] out IntPtr phbm);
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE
    {
        public int cx;
        public int cy;

        public SIZE(int cx, int cy)
        {
            this.cx = cx;
            this.cy = cy;
        }
    }
    [Flags]
    public enum SIIGBF
    {
        SIIGBF_RESIZETOFIT = 0x00,
        SIIGBF_BIGGERSIZEOK = 0x01,
        SIIGBF_MEMORYONLY = 0x02,
        SIIGBF_ICONONLY = 0x04,
        SIIGBF_THUMBNAILONLY = 0x08,
        SIIGBF_INCACHEONLY = 0x10,
    }
    public enum SIGDN : uint
    {
        NORMALDISPLAY = 0,
        PARENTRELATIVEPARSING = 0x80018001,
        PARENTRELATIVEFORADDRESSBAR = 0x8001c001,
        DESKTOPABSOLUTEPARSING = 0x80028000,
        PARENTRELATIVEEDITING = 0x80031001,
        DESKTOPABSOLUTEEDITING = 0x8004c000,
        FILESYSPATH = 0x80058000,
        URL = 0x80068000
    }

    public static class imgHelpers
    {
#if _WIN32 || _WIN64
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void SHCreateItemFromParsingName(
                [In][MarshalAs(UnmanagedType.LPWStr)] string pszPath,
                [In] IntPtr pbc,
                [In][MarshalAs(UnmanagedType.LPStruct)] Guid riid,
                [Out][MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out IShellItem ppv);
#elif __linux__
#endif

        public static Bitmap getIcon(string file, int iconSize = 256)
        {
#if _WIN32 || _WIN64
            IShellItem ppsi = null;
            IntPtr hbitmap = IntPtr.Zero;
            // GUID of IShellItem.
            Guid uuid = new Guid("43826d1e-e718-42ee-bc55-a1e261c37bfe");
            SHCreateItemFromParsingName(file, IntPtr.Zero, uuid, out ppsi);
            ((IShellItemImageFactory)ppsi).GetImage(new SIZE(iconSize, iconSize), 0x0, out hbitmap);
            return Bitmap.FromHbitmap(hbitmap, IntPtr.Zero);
#elif __linux__
			return new Bitmap(iconSize, iconSize);
#endif
        }
		/// <summary>
		/// bitmap flip on y axis, used for opengl textures
		/// </summary>
		/// <param name="source"></param>
		/// <param name="stride"></param>
		/// <param name="height"></param>
		/// <returns>bitmap bytes</returns>
		public static byte[] flitY(byte[] source, int stride, int height)
		{
			byte[] bmp = new byte[source.Length];
			source.CopyTo(bmp, 0);

			for (int y = 0; y < height / 2; y++)
			{
				for (int x = 0; x < stride; x++)
				{
					byte tmp = bmp[y * stride + x];
					bmp[y * stride + x] = bmp[(height - 1 - y) * stride + x];
					bmp[(height - y - 1) * stride + x] = tmp;
				}
			}
			return bmp;
		}
		public static void flipY(IntPtr ptr, int stride, int height)
		{
			int size = stride * height;
			byte[] source = new byte[size];
			System.Runtime.InteropServices.Marshal.Copy(ptr, source, 0, size);
			for (int y = 0; y < height / 2; y++)
			{
				for (int x = 0; x < stride; x++)
				{
					byte tmp = source[y * stride + x];
					source[y * stride + x] = source[(height - 1 - y) * stride + x];
					source[(height - y - 1) * stride + x] = tmp;
				}
			}
			System.Runtime.InteropServices.Marshal.Copy(source, 0,ptr, size);
		}

    }
}
