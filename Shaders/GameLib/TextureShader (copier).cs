using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GGL;

namespace GameLib
{
	public class TextureShader : ShadedTexture
	{
		int in_tex;

		public TextureShader (string effectId, int _width = -1, int _height = -1) : 
			base(effectId, _width, _height)
		{
		}
			
		public int IN_Texture { get { return in_tex; } }


		public override void Dispose ()
		{
			base.Dispose ();
			GL.DeleteTexture (in_tex);
		}
	}
}

