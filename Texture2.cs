using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using System.IO;
using System.Drawing.Drawing2D;
using OTKGL;

//using GLU = OpenTK.Graphics.Glu;

namespace OTKGL
{
    [Serializable]
    public class Texture2
    {
        public string Map;
        public int texRef;
        public TextureUnit texUnit = TextureUnit.Texture0;

        //public Texture() { }
        public Texture2(bool flipY, string _mapPath, bool MipMapping = false, int shader = 0, TextureUnit textUnit = TextureUnit.Texture0, string uniformName = "")
        {
            //Game.PrintActiveTexturing("new texture ");
            texUnit = textUnit;
            Map = _mapPath;
            initTexture(MipMapping, shader, uniformName, flipY);
            //Game.PrintActiveTexturing("new texture after text init ");
        }
        public Texture2(string _mapPath, bool MipMapping = false, int shader = 0, TextureUnit textUnit = TextureUnit.Texture0, string uniformName = "")
        {
            texUnit = textUnit;
            Map = _mapPath;
            initTexture(MipMapping, shader, uniformName,true);
            //Game.PrintActiveTexturing("new texture after text init ");
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
        void initTexture(bool flipY = true)
        {
            Bitmap bitmap = new Bitmap(Map);

            if(flipY)
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            GL.GenTextures(1, out texRef);
            GL.BindTexture(TextureTarget.Texture2D, texRef);

            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        }

        void initTexture(bool MipMapping, int shader, string uniformName, bool flipY )
        {
            Bitmap bitmap;
            try
            {
                bitmap = new Bitmap(Map);
            }
            catch (Exception)
            {
                char[] sep = { '\\' };

                string[] s = Map.Split(sep);
                bitmap = new Bitmap(s[s.Length - 1]);
            }
            
            if(flipY)
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            GL.GenTextures(1, out texRef);

            	GL.ActiveTexture(texUnit);
            GL.BindTexture(TextureTarget.Texture2D, texRef);
            

            if (shader > 0)
                GL.Uniform1(GL.GetUniformLocation(shader, uniformName), texUnit - TextureUnit.Texture0 );

            if (MipMapping)
            {
                BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

//                GLU.Build2DMipmap(OpenTK.Graphics.TextureTarget.Texture2D,
//                            (int)PixelInternalFormat.Rgba,
//                            bitmap.Width, bitmap.Height,
//                            OpenTK.Graphics.PixelFormat.Bgra,
//                            OpenTK.Graphics.PixelType.UnsignedByte, data.Scan0);
                //GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapLinear);

                bitmap.UnlockBits(data);
            }
            else
            {
                AssignMipMap(bitmap, 0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);                
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);                
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);


            }

        }
        void initTextureOld(bool MipMapping, int shader, string uniformName, bool flipY)
        {
            Bitmap bitmap = new Bitmap(Map);

            if (flipY)
                bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

            GL.GenTextures(1, out texRef);
            GL.ActiveTexture(texUnit);
            GL.BindTexture(TextureTarget.Texture2D, texRef);


            if (shader > 0)
                GL.Uniform1(GL.GetUniformLocation(shader, uniformName), texUnit - TextureUnit.Texture0);

            if (MipMapping)
            {
                int mm = 0;
                while (bitmap != null)
                {
                    AssignMipMap(bitmap, mm);
                    bitmap = ScaleBitmap(bitmap);
                    //tex.Save(Path.GetFileNameWithoutExtension(Map) + mm + Path.GetExtension(Map));
                    mm++;
                }
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapNearest);
            }
            else
            {
                AssignMipMap(bitmap, 0);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);


            }

        }

        void AssignMipMap(Bitmap bitmap, int level)
        {
            BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height),
            ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            GL.TexImage2D(TextureTarget.Texture2D, level, PixelInternalFormat.Rgba, data.Width, data.Height, 0,
            OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);
        }

        public static Bitmap ScaleBitmap(Bitmap image, double scale = 0.5)
        {            
            //int width = (int)Math.Sqrt(image.Width);
            //int height = (int)Math.Sqrt(image.Height);
            int width = (int)(image.Width * scale);
            int height = (int)(image.Height * scale);

            if (width == 0)
                return null;

            Bitmap bmp = new Bitmap((int)width, (int)height);
            Graphics graph = Graphics.FromImage(bmp);

            // uncomment for higher quality output
            graph.InterpolationMode = InterpolationMode.High;
            graph.CompositingQuality = CompositingQuality.HighQuality;
            graph.SmoothingMode = SmoothingMode.AntiAlias;

            //graph.FillRectangle(brush, new RectangleF(0, 0, width, height));
            graph.DrawImage(image, new System.Drawing.Rectangle(0, 0, (int)width, (int)height));
            
            return bmp;
        }

        //public void BindTexture(ref int shader, string UniformName, TextureUnit textureUnit)
        //{
        //    GL.ActiveTexture(textureUnit);
        //    GL.BindTexture(TextureTarget.Texture2D, texRef);
        //    GL.Uniform1(GL.GetUniformLocation(shader, UniformName), textureUnit - TextureUnit.Texture0 );
        //}
        //public void BindTexture(ref int shader, string UniformName)
        //{
        //    GL.BindTexture(TextureTarget.Texture2D, texRef);
        //    GL.Uniform1(GL.GetUniformLocation(shader, UniformName),texRef);
        //}

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

        public static implicit operator int(Texture2 t)
        { 
            return t == null ? 0: t.texRef; 
        }
    }

}
