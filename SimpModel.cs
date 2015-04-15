using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using Jitter.Collision.Shapes;

namespace OTKGL
{
    public class SimpleModel : Model
    {
        public BOquads Mesh;        

        public Texture texture;

        public SimpleModel(BOquads _mesh)
        {
            Mesh = _mesh;
            
        }
        public override int verticesCount
        {
            get
            {
                return Mesh.Vertices.Length;
            }
        }

        public override void Prepare()
        {
            Mesh.Prepare();
            bounds = Mesh.bounds;            
        }
        public override void Render()
        {
            GL.PushAttrib(AttribMask.EnableBit);

            if (texture != null)
            {
                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.Enable(EnableCap.Texture2D);
            }

            Mesh.Render();

            GL.PopAttrib();
        }
    }
}
