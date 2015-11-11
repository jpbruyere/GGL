//
//  SkyboxShader.cs
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

namespace Tetra
{
	public class SkyboxShader : Shader
	{
		public SkyboxShader () 
		{			
			_vertSource = @"
			#version 330
			precision lowp float;

			uniform mat4 mvp;

			layout(location = 0) in vec3 in_position;			

			out vec3 texCoord;
			
			void main(void)
			{    
    			texCoord = in_position;				
				gl_Position = mvp * vec4(in_position, 1.0);
			}";

			_fragSource = @"
			#version 330
			precision lowp float;

			uniform samplerCube cubeTexture;
			
			in vec3 texCoord;
			out vec4 out_frag_color;

			void main(void)
			{				
				out_frag_color = texture( cubeTexture, texCoord);				
			}";
			Compile ();						
		}

		public int CubeTexture;

		protected override void BindSamplesSlots ()
		{			
			GL.Uniform1(GL.GetUniformLocation (pgmId, "cubeTexture"), 0);
		}

		public override void Enable ()
		{
			base.Enable ();

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.TextureCubeMap, CubeTexture);
		}
	}
}

