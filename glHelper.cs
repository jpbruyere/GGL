using System;
using OpenTK;

namespace GGL
{
	public static class glHelper
	{
		public static Vector4 UnProject(ref Matrix4 projection, ref Matrix4 view, int[] viewport, Vector2 mouse)
		{
			Vector4 vec;

			vec.X = 2.0f * mouse.X / (float)viewport[2] - 1;
			vec.Y = -(2.0f * mouse.Y / (float)viewport[3] - 1);
			vec.Z = 0f;
			vec.W = 1.0f;

			Matrix4 viewInv = Matrix4.Invert(view);
			Matrix4 projInv = Matrix4.Invert(projection);

			Vector4.Transform(ref vec, ref projInv, out vec);
			Vector4.Transform(ref vec, ref viewInv, out vec);

			if (vec.W > float.Epsilon || vec.W < float.Epsilon)
			{
				vec.X /= vec.W;
				vec.Y /= vec.W;
				vec.Z /= vec.W;
			}

			return vec;
		}

		public static Vector2 Project(
			Vector3 pos, 
			Matrix4 viewMatrix, 
			Matrix4 projectionMatrix,
			Matrix4 modelMatrix,
			int screenWidth, 
			int screenHeight)
		{
			pos = Vector3.Transform(pos, modelMatrix);
			pos = Vector3.Transform(pos, viewMatrix);
			pos = Vector3.Transform(pos, projectionMatrix);
			pos.X /= pos.Z;
			pos.Y /= pos.Z;
			pos.X = (pos.X + 1) * screenWidth / 2;
			pos.Y = (pos.Y + 1) * screenHeight / 2;

			return new Vector2(pos.X, pos.Y);
		}
		public static Vector2 Project(
			Vector3 pos, 
			Matrix4 matrix, 
			int screenWidth, 
			int screenHeight)
		{
			pos = Vector3.Transform(pos, matrix);
			pos.X /= pos.Z;
			pos.Y /= pos.Z;
			pos.X = (pos.X + 1) * screenWidth / 2;
			pos.Y = (pos.Y + 1) * screenHeight / 2;

			return new Vector2(pos.X, pos.Y);
		}
	}
}

