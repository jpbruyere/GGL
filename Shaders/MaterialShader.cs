
using System;
using OpenTK.Graphics.OpenGL;

namespace GGL
{
	/// <summary>
	/// Initializes default texture location for materials, maybe not necessary
	/// </summary>
	public class MaterialShader : ExternalShader
	{
		public MaterialShader(string _vsPath = "", string _fsPath = "", string _gsPath = ""):
			base(_vsPath,_fsPath,_gsPath)
		{
			Enable ();
			GL.Uniform1(GL.GetUniformLocation (shaderProgram, "diffuseTexture"),0);
			GL.Uniform1(GL.GetUniformLocation (shaderProgram, "normalTexture"),1);
			Disable ();
		}
	}
}

