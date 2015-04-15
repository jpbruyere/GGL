using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using System.Drawing.Imaging;
using OpenTK;

namespace GGL
{
    public class TextureCopier
    {
        public int destination;
        public int source;

        int size;
        int fbo;
        
        static Random rnd = new Random();

        public TextureCopier(int _source, int _destination, int _size)
        {
            destination = _destination;
            source = _source;
            size = _size;
            Init();                      
        }

        public void Init()
        {
            GL.Ext.GenFramebuffers(1, out fbo);
            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);
            GL.Ext.FramebufferTexture2D(FramebufferTarget.FramebufferExt, FramebufferAttachment.ColorAttachment0Ext,
                TextureTarget.Texture2D, destination, 0);


            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, 0);
        }
        public void Update()
        {
            DrawOnFBO();
        }
        void DrawOnFBO()
        {
            int savePgm = GL.GetInteger(GetPName.CurrentProgram);

            GL.Ext.BindFramebuffer(FramebufferTarget.FramebufferExt, fbo);
            GL.PushAttrib(AttribMask.ViewportBit);
            {
                GL.UseProgram(0);
                GL.Viewport(0, 0, size, size);

                GL.MatrixMode(MatrixMode.Projection);
                GL.PushMatrix();
                OpenTK.Matrix4 ortho = OpenTK.Matrix4.CreateOrthographicOffCenter(-1, 1, -1, 1, 1, -1);
                GL.LoadMatrix(ref ortho);

                GL.MatrixMode(MatrixMode.Modelview);
                GL.PushMatrix();
                GL.LoadIdentity();
                
                GL.BindTexture(TextureTarget.Texture2D, source);
                UnityQuad.Draw();
                GL.BindTexture(TextureTarget.Texture2D, 0);
                
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
