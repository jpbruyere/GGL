using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;

namespace GGL
{
    public class ProceduralTexture
    {
        public int Shader = 0;
        int width;
        int height;
        int fbo;
        int tex;

        public int Width
        {
            get { return width; }
        }
        public int Height
        {
            get { return height; }
        }

        public ProceduralTexture(int size, int _shader)
        {
            CreateProceduralTextureAndFbo(size, size);
            Shader = _shader;
            
            GL.Uniform2(GL.GetUniformLocation(Shader, "resolution"), (float)Width, (float)Height);
        }

        public void Update()
        {
            DrawOnFBO();
        }

        void CreateProceduralTextureAndFbo(int _width = 512, int _height = 512)
        {
            width = _width;
            height = _height;
            // load texture 
            GL.GenTextures(1, out tex);

            // Still required else TexImage2D will be applyed on the last bound texture
            GL.BindTexture(TextureTarget.Texture2D, tex);

            // generate null texture
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
            OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);


            // create and bind an FBO
            GL.Ext.GenFramebuffers(1, out fbo);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext,
                TextureTarget.Texture2D, tex, 0);

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0); // disable rendering into the FBO
        }

        void DrawOnFBO()
        {
            int savePgm = GL.GetInteger(GetPName.CurrentProgram);

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);
            GL.PushAttrib(AttribMask.ViewportBit);
            {
                GL.UseProgram(Shader);
                GL.Uniform2(GL.GetUniformLocation(Shader, "resolution"), (float)width, (float)height);
                GL.Viewport(0, 0, width, height);

                GL.MatrixMode(MatrixMode.Projection);
                GL.PushMatrix();
                OpenTK.Matrix4 ortho = OpenTK.Matrix4.CreateOrthographicOffCenter(-1, 1, -1, 1, 1, -1);
                GL.LoadMatrix(ref ortho);

                GL.MatrixMode(MatrixMode.Modelview);
                GL.PushMatrix();
                GL.LoadIdentity();


                //GL.BindTexture(TextureTarget.Texture2D, tex);

                UnityQuad.Draw();

                
                GL.MatrixMode(MatrixMode.Projection);
                GL.PopMatrix();
                GL.MatrixMode(MatrixMode.Modelview);
                GL.PopMatrix();
            }
            GL.PopAttrib();

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0); // disable rendering into the FB            

            GL.UseProgram(savePgm);
        }

        float[,] getHeightMapFromCurrentTexture()
        {
            GL.BindTexture(TextureTarget.Texture2D, tex);
            System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            BitmapData data = bmp.LockBits(new System.Drawing.Rectangle(0, 0, width, height),
                    ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);

            int bmpSizeInPixels = width * height;
            int bmpSizeInBytes = bmpSizeInPixels * 4;
            byte[] pixels = new byte[bmpSizeInBytes];

            System.Runtime.InteropServices.Marshal.Copy(data.Scan0, pixels, 0, bmpSizeInBytes);
            bmp.UnlockBits(data);

            float[,] hm = new float[width + 1, height + 1];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    hm[x, y] = pixels[(x + y * width) * 4];
                }
            }
            return hm;
        }

        public SimpleTerrain CreateTerrainFromTexture()
        {                        
            float[,] hm = getHeightMapFromCurrentTexture();
            
            return new SimpleTerrain(hm);
        }
        public static implicit operator int(ProceduralTexture pt)
        {
            return pt.tex;
        }
    }
}
