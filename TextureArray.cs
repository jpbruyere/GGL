using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing.Drawing2D;
using GGL;

//using GLU = OpenTK.Graphics.Glu;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace GGL
{
    [Serializable]
    public class TextureArray
    {        
        public int texRef;
		public int Width=-1;
		public int Height=-1;
		public int LayerCount = 0;
		 
		bool flipY = true;

		public TextureArray(params string[] _mapPath)			
		{
			LayerCount = _mapPath.Length;

			byte[] data = null;

			int imgSizeInByte = 0,
				offset = 0;

			foreach (string path in _mapPath) {
				
				using (Stream fs = FileSystemHelpers.GetStreamFromPath (path)) {
					Bitmap bmp = new Bitmap (fs);

					if (Width < 0) {
						Width = bmp.Width;
						Height = bmp.Height;
						imgSizeInByte = Width * Height * 4;
						data = new byte[imgSizeInByte * LayerCount];
					}

					if (Width != bmp.Width || Height != bmp.Height)
						throw new Exception ("Different size for image array");
					
					if(flipY)
						bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);


					BitmapData bmpdata = bmp.LockBits(new System.Drawing.Rectangle(0, 0, bmp.Width, bmp.Height),
						ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

					Marshal.Copy(bmpdata.Scan0, data, offset, imgSizeInByte);


					bmp.UnlockBits(bmpdata);

					offset += imgSizeInByte;
				}
			}
			GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
			IntPtr pointer = pinnedArray.AddrOfPinnedObject();

			GL.GenTextures(1, out texRef);
			GL.BindTexture(TextureTarget.Texture2DArray, texRef);
			IntPtr ptr = new IntPtr(0);
			GL.TexImage3D(TextureTarget.Texture2DArray, 0, 
				PixelInternalFormat.Rgba, Width, Height, LayerCount, 0, 
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, pointer);

			pinnedArray.Free();

			GL.GenerateMipmap (GenerateMipmapTarget.Texture2DArray);

			GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
			GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapR, (int)TextureWrapMode.Repeat);

			GL.BindTexture(TextureTarget.Texture2DArray, 0);
		}
//		public TextureArray(Stream _mapStream, bool flipY = true)
//        {
//			try {
//				Bitmap bitmap = new Bitmap(_mapStream);
//
//				if(flipY)
//					bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);
//
//
//				BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
//					ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
//
//				createTexture (data.Scan0, data.Width, data.Height);
//
//				bitmap.UnlockBits(data);
//
//				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
//				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
//
//				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
//
//			} catch (Exception ex) {
//				Debug.WriteLine ("Error loading texture: " + Map + ":" + ex.Message); 
//			}
//		}
			

		void createTexture(IntPtr data, int width, int height)
		{
			
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);			
		}
			
			
        public static implicit operator int(TextureArray t)
        { 
            return t == null ? 0: t.texRef; 
        }
    }

}
