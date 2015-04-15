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

namespace GGL
{
    [Serializable]
    public class Texture
    {
        public string Map;
        public int texRef;        
		        
		public Texture(string _mapPath, bool flipY = true)
        {
			try {
	            Map = _mapPath;

				Bitmap bitmap = new Bitmap(Map);

				if(flipY)
					bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);


				BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
					ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				createTexture (data.Scan0, data.Width, data.Height);

				bitmap.UnlockBits(data);

				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
				GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

				GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

			} catch (Exception ex) {
				Debug.WriteLine ("Error loading texture: " + Map + ":" + ex.Message); 
			}
		}

		public Texture(int width, int height)
		{
			createTexture (IntPtr.Zero, width, height);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
		}

		void createTexture(IntPtr data, int width, int height)
		{
			GL.GenTextures(1, out texRef);
			GL.BindTexture(TextureTarget.Texture2D, texRef);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
				OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data);			
		}

        public void reload(Bitmap bitmap)
        {
            GL.BindTexture(TextureTarget.Texture2D, texRef);

            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);        
        }
			
        public static implicit operator int(Texture t)
        { 
            return t == null ? 0: t.texRef; 
        }
    }

}
