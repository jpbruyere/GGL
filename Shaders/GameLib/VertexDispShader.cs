//
//  VertexDispShader.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;

namespace GameLib
{
	public class VertexDispShader : Shader
	{
		public VertexDispShader (string vertResId, string fragResId = null) :
			base(vertResId,fragResId)
		{
		}

		protected int   mapSizeLoc, heightScaleLoc, lightPosLoc, fogColorLoc;

		public int DisplacementMap;
		public int DiffuseTexture;
		public int SplatTexture;

		Vector4 lightPos;
		Vector2 mapSize;
		float heightScale = 1f;

		public Vector2 MapSize {
			set { mapSize = value; }
		}
		public float HeightScale {
			set { heightScale = value; }
		}
		public Vector4 LightPos {
			set { lightPos = value; }
		}

		protected override void GetUniformLocations ()
		{
			GL.UniformBlockBinding(pgmId, GL.GetUniformBlockIndex(pgmId, "fogData"), 10);
			base.GetUniformLocations ();


			lightPosLoc = GL.GetUniformLocation (pgmId, "lightPos");
			mapSizeLoc = GL.GetUniformLocation (pgmId, "mapSize");
			heightScaleLoc = GL.GetUniformLocation (pgmId, "heightScale");
		}
		protected override void BindSamplesSlots ()
		{
			base.BindSamplesSlots ();

			GL.Uniform1(GL.GetUniformLocation (pgmId, "heightMap"),1);
			GL.Uniform1(GL.GetUniformLocation (pgmId, "splatTex"),2);
		}
		public override void Enable ()
		{
			base.Enable ();

			GL.Uniform2 (mapSizeLoc, mapSize);
			GL.Uniform1 (heightScaleLoc, heightScale);
			GL.Uniform4 (lightPosLoc, lightPos);

			GL.ActiveTexture (TextureUnit.Texture2);
			GL.BindTexture(TextureTarget.Texture2D, SplatTexture);
			GL.ActiveTexture (TextureUnit.Texture1);
			GL.BindTexture(TextureTarget.Texture2D, DisplacementMap);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture(TextureTarget.Texture2DArray, DiffuseTexture);
		}
	}
}

