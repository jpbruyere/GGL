using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using go;

namespace GGL
{
	public static class AxesHelper
	{
		static int dlId = 0;
		public static void Render()
		{
			if (dlId > 0) {
				GL.CallList (dlId);
				return;
			}

			dlId = GL.GenLists(1);
			GL.NewList(dlId, ListMode.CompileAndExecute);

			GL.PushAttrib(AttribMask.EnableBit);
			GL.Disable(EnableCap.Lighting);
			GL.Disable(EnableCap.Texture2D);
			GL.UseProgram(0);
			GL.LineWidth (2f);

			GL.Begin(PrimitiveType.Lines);
			{
				GL.Color3(Color.Red);
				GL.Vertex3(Vector3.Zero);
				GL.Vertex3(Vector3.UnitX);

				GL.Color3(Color.Green);
				GL.Vertex3(Vector3.Zero);
				GL.Vertex3(Vector3.UnitY);

				GL.Color3(Color.Blue);
				GL.Vertex3(Vector3.Zero);
				GL.Vertex3(Vector3.UnitZ);
			}
			GL.End();            
			GL.PopAttrib();
			GL.EndList();
		}
	}
}

