//
//  SkyBox.cs
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
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using OpenTK;

namespace Tetra
{
	public class SkyBox
	{
		public SkyboxShader shader;
		int positionVboHandle;
		int eboHandle;
		int vaoHandle;

		#region Vertices
		Vector3[] positions = new Vector3[] {
			new Vector3(-1.0f, -1.0f, -1.0f),
			new Vector3(-1.0f, -1.0f,  1.0f),
			new Vector3( 1.0f, -1.0f,  1.0f),
			new Vector3( 1.0f, -1.0f, -1.0f),
			new Vector3(-1.0f,  1.0f, -1.0f),
			new Vector3(-1.0f,  1.0f,  1.0f),
			new Vector3( 1.0f,  1.0f,  1.0f),
			new Vector3( 1.0f,  1.0f, -1.0f),			
		};

		ushort[] indices = new ushort[]{
			2,	6,	7,	7,	3,	2,
			0,	4,	5,	5,	1,	0,
			5,	4,	7,	7,	6,	5,
			0,	1,	2,	2,	3,	0,
			0,	4,	7,	7,	3,	0,
			2,	6,	5,	5,	1,	2,
		};
		#endregion

		#region CTOR
		public SkyBox(){
			GL.Enable (EnableCap.TextureCubeMapSeamless);

			createVBO ();
			createVAO ();
			shader = new Tetra.SkyboxShader ();
		}

		public SkyBox (string left, string right, string front, string back, string top, string bottom = null)
			: this()
		{
			shader.CubeTexture = Tetra.Texture.LoadCubeMap (left, right, front, back, top, bottom);
		}			
		#endregion

		#region VAO creation
		void createVBO(){
			positionVboHandle = GL.GenBuffer();
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.BufferData<Vector3>(BufferTarget.ArrayBuffer,
				new IntPtr(positions.Length * Vector3.SizeInBytes),
				positions, BufferUsageHint.StaticDraw);

			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			eboHandle = GL.GenBuffer ();
			GL.BindBuffer (BufferTarget.ElementArrayBuffer, eboHandle);
			GL.BufferData (BufferTarget.ElementArrayBuffer,
				new IntPtr (sizeof(ushort) * indices.Length),
				indices, BufferUsageHint.StaticDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
		}
		void createVAO(){
			vaoHandle = GL.GenVertexArray();

			GL.BindVertexArray(vaoHandle);

			GL.EnableVertexAttribArray(0);
			GL.BindBuffer(BufferTarget.ArrayBuffer, positionVboHandle);
			GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, true, Vector3.SizeInBytes, 0);

			GL.BindBuffer(BufferTarget.ElementArrayBuffer, eboHandle);

			GL.BindVertexArray(0);
		}
		#endregion

		public void Render(){
			shader.Enable();
			GL.BindVertexArray(vaoHandle);
			GL.DrawElements (BeginMode.Triangles, indices.Length,
				DrawElementsType.UnsignedShort, IntPtr.Zero);
			GL.BindVertexArray(0);
		}
	}
}

