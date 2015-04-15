using System;
using OpenTK.Graphics.OpenGL;

namespace GGL
{
	public class MultiSpriteShader : MaterialShader
	{
		int _frame = 0;
		int _startFrame = 0;

		public MultiSpriteShader(string _vsPath = "", string _fsPath = "", string _gsPath = ""):
			base(_vsPath,_fsPath,_gsPath)
		{
			GL.Uniform1(GL.GetUniformLocation (shaderProgram, "diffuseTexture"),0);
		}

		public int SpriteCountW {
			set { GL.Uniform1(GL.GetUniformLocation (shaderProgram, "wSprites"),value); }
		}
		public int SpriteCountH {
			set { GL.Uniform1(GL.GetUniformLocation (shaderProgram, "hSprites"),value); }
		}

		public int frame
		{
			get { return _frame; }
			set 
			{ 
				_frame = value;

				Enable ();
				GL.Uniform1 (GL.GetUniformLocation (shaderProgram, "time"), _frame);
				Disable ();
			}
		}
		public int startFrame
		{ set { _startFrame = value; } }

		public override void Enable ()
		{
			base.Enable ();
			GL.Uniform1(GL.GetUniformLocation (shaderProgram, "startTime"),_startFrame); 
		}

	}
}

