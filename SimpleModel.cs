using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using Jitter.Collision.Shapes;
using System.Drawing;
using OpenTK;
using Jitter.LinearMath;
using Jitter.Dynamics;

namespace GGL
{
    public class SimpleModel : Model
    {
        public BOquads meshe;        

        public Texture texture;

        public SimpleModel(BOquads _mesh)
        {
            meshe = _mesh;
            
        }
        public override int verticesCount
        {
            get
            {
                return meshe.Vertices.Length;
            }
        }

        public override void Prepare()
        {
            meshe.Prepare();
            bounds = meshe.bounds;            
        }
        public override void Render()
        {
            GL.PushAttrib(AttribMask.EnableBit);

            if (texture != null)
            {
                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.Enable(EnableCap.Texture2D);
            }

            meshe.Render();

            GL.PopAttrib();
        }       
    }
}
