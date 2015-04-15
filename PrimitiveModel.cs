using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics.OpenGL;
using Jitter.Collision.Shapes;
using Examples.Shapes;

namespace GGL
{
    public class PrimitiveModel : Model
    {
        public DrawableShape Mesh;        

        public Texture texture;

        public PrimitiveModel(DrawableShape _shape)
        {
            Mesh = _shape;
        }
        //public override int verticesCount
        //{
        //    get
        //    {
        //        return -1;
        //    }
        //}
        public override void Prepare()
        {
            //throw new NotImplementedException();
        }
        public override void Render()
        {
            GL.PushAttrib(AttribMask.EnableBit);

            if (texture != null)
            {
                GL.BindTexture(TextureTarget.Texture2D, texture);
                GL.Enable(EnableCap.Texture2D);
            }

            Mesh.Draw();

            GL.PopAttrib();
        }
    }
}
