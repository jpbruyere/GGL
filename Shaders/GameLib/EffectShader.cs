using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using GGL;

namespace GameLib
{
	public class EffectShader : ShadedTexture
	{
		protected int 	timeLocation;

		public EffectShader (string effectId, int _width = -1, int _height = -1) : 
			base(effectId, _width, _height)
		{
		}


		public void Update (float time = 0f)
		{
			this.Enable ();
			Time = time;
			updateFbo ();
			this.Disable ();
		}

		public float Time { set { GL.Uniform1 (timeLocation, value); }}

		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			timeLocation = GL.GetUniformLocation(pgmId, "time");
		}
	}
}

