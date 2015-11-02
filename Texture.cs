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
    public class Texture
    {
		public static TextureMinFilter DefaultMinFilter = TextureMinFilter.Linear;
		public static TextureMagFilter DefaultMagFilter = TextureMagFilter.Linear;
		public static TextureWrapMode DefaultWrapMode = TextureWrapMode.Clamp;
		public static bool GenerateMipMaps = true;
		public static bool FlipY = true;

        public string MapPath;
		public TextureTarget TexTarget = TextureTarget.Texture2D;
        public uint texRef;
		public int Width = -1;
		public int Height = -1;
		public int LayerCount = 0;

		public Texture(int width, int height)
		{
			Width = width;
			Height = height;

			createTexture (IntPtr.Zero);
		}

		public Texture(){
		}

		void createTexture(IntPtr data)
		{
			GL.GenTextures(1, out texRef);
			GL.BindTexture(TexTarget, texRef);
			GL.TexImage2D(TexTarget, 0, PixelInternalFormat.Rgba, Width, Height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);
		}
		void configureTexParameters()
		{
			if (GenerateMipMaps) {
				GL.GenerateMipmap((GenerateMipmapTarget)TexTarget);
				GL.TexParameter(TexTarget, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			}else
				GL.TexParameter(TexTarget, TextureParameterName.TextureMinFilter, (int)DefaultMinFilter);
			GL.TexParameter(TexTarget, TextureParameterName.TextureMagFilter, (int)DefaultMagFilter);
			GL.TexParameter(TexTarget, TextureParameterName.TextureWrapS, (int)DefaultWrapMode);
			GL.TexParameter(TexTarget, TextureParameterName.TextureWrapT, (int)DefaultWrapMode);
		}
						
        public static implicit operator int(Texture t)
        { 
			return t == null ? 0: (int)t.texRef; 
        }
			
		public static Texture Load(string path)
		{
			Texture tmp = null;

			try {
				Stream stream = FileSystemHelpers.GetStreamFromPath (path);

				if (path.EndsWith (".dds", StringComparison.InvariantCultureIgnoreCase)) {
					byte[] imgbuff = new byte[stream.Length];
					stream.Read (imgbuff, 0, (int)stream.Length);

					TextureLoaders.ImageDDS.LoadFromByteArray (imgbuff, out tmp);

					tmp.MapPath = path;
				} else {
					Bitmap bitmap = new Bitmap(stream);

					if(FlipY)
						bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

					BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
						ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

					tmp = new Texture ();
					tmp.MapPath = path;
					tmp.Width = data.Width;
					tmp.Height = data.Height;
					tmp.createTexture (data.Scan0);

					bitmap.UnlockBits(data);				
				}

				tmp.configureTexParameters ();

			}
			catch (Exception ex) {
				throw new Exception ("Error loading texture: " + path + ":" + ex.Message);
			}
			return tmp;
		}
		public static Texture Load(TextureTarget textureTarget, params string[] _mapPath)
		{

			byte[] data = null;

			int imgSizeInByte = 0,
				offset = 0;

			Texture tmp = new Texture () {
				TexTarget = textureTarget,
				LayerCount = _mapPath.Length
			};

			try {				

				foreach (string path in _mapPath) {

					using (Stream fs = FileSystemHelpers.GetStreamFromPath (path)) {
						Bitmap bmp = new Bitmap (fs);

						if (tmp.Width < 0) {
							tmp.Width = bmp.Width;
							tmp.Height = bmp.Height;
							imgSizeInByte = tmp.Width * tmp.Height * 4;
							data = new byte[imgSizeInByte * tmp.LayerCount];
						}

						if (tmp.Width != bmp.Width || tmp.Height != bmp.Height)
							throw new Exception ("Different size for image array");

						if(FlipY)
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

				GL.GenTextures(1, out tmp.texRef);
				GL.BindTexture(tmp.TexTarget, tmp.texRef);
				IntPtr ptr = new IntPtr(0);
				GL.TexImage3D(tmp.TexTarget, 0, 
					PixelInternalFormat.Rgba, tmp.Width, tmp.Height, tmp.LayerCount, 0, 
					OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, pointer);

				pinnedArray.Free();

				tmp.configureTexParameters ();

				GL.TexParameter(tmp.TexTarget, TextureParameterName.TextureWrapR, (int)DefaultWrapMode);

				GL.BindTexture(tmp.TexTarget, 0);

			} catch (Exception ex) {
				throw new Exception ("Error loading textures: " + ex.Message);
			}

			return tmp;
		}
		public void Save(string file)
		{
			GL.BindTexture (TexTarget, texRef);

			byte[] data = new byte[Width*Height*4];

			GL.GetTexImage (TexTarget, 0, 
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);

			GL.BindTexture (TexTarget, 0);

			Cairo.Surface bmp = new Cairo.ImageSurface(data, Cairo.Format.Argb32, Width, Height, Width*4);
			bmp.WriteToPng (file);

			bmp.Dispose ();
		}
    }

}
