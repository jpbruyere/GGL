using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using OpenTK;

namespace GGL
{
    public class VoronoiTexture
    {
        public float voronoiScale = 10f;
        public int nbPoints = 50;
        public int tex;

        Shader Shader;
        int width;
        int height;
        int fbo;
        

        public int Width
        {
            get { return width; }
        }
        public int Height
        {
            get { return height; }
        }
        static Random rnd = new Random();

        public VoronoiTexture(int size, int _nbPoints = 50, float scale = 10f)
        {
            
            width = height = size;
            nbPoints = _nbPoints;
            voronoiScale = scale;
 
            Shader = new ExternalShader("simple","voronoi1", "");
            
                                    
            Vector2[] pts = new Vector2[nbPoints];
            for (int i = 0; i < nbPoints; i++)
            {
                pts[i] = new Vector2((float)rnd.NextDouble(), (float)rnd.NextDouble());
            }
                            
            double hMax = 0;
            float[] mcopy = new float[nbPoints*2];

            System.Runtime.InteropServices.GCHandle pinnedArray = System.Runtime.InteropServices.GCHandle.Alloc
                (pts, System.Runtime.InteropServices.GCHandleType.Pinned);
            System.Runtime.InteropServices.Marshal.Copy(pinnedArray.AddrOfPinnedObject(),mcopy,0,mcopy.Length);
            pinnedArray.Free();

                        
            GL.Uniform2(GL.GetUniformLocation(Shader, "points"), mcopy.Length , mcopy);
            GL.Uniform1(GL.GetUniformLocation(Shader, "nbPoints"), 1 , ref nbPoints);
            GL.Uniform1(GL.GetUniformLocation(Shader, "scale"), 1, ref voronoiScale);
            GL.Uniform2(GL.GetUniformLocation(Shader, "resolution"), (float)width, (float)height);
            
            GL.UseProgram(0);
        }

        public void Init()
        {
            CreateProceduralTextureAndFbo(width, height);
        }
        public void Update()
        {
            DrawOnFBO();
        }

        void CreateProceduralTextureAndFbo(int _width = 512, int _height = 512)
        {
            width = _width;
            height = _height;

            if (tex == 0)
            {
                GL.GenTextures(1, out tex);

                GL.BindTexture(TextureTarget.Texture2D, tex);
                
                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
                
                GL.BindTexture(TextureTarget.Texture2D, 0);
            }

            // create and bind an FBO
            GL.Ext.GenFramebuffers(1, out fbo);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext,
                TextureTarget.Texture2D, tex, 0);

            
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
    }
}
