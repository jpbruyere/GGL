//
//  CircleShader.cs
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
using GameLib;
using OpenTK.Graphics.OpenGL;

namespace Ottd3D
{
	public class CircleShader : ShadedTexture
	{
		public CircleShader (string effectId, int _width = -1, int _height = -1):
		base(effectId,_width,_height)
		{
		}

		int radiusLoc;

		float radius = 0.5f;

		public float Radius { 
			set { radius = value; }
			get { return radius; }
		}


		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			radiusLoc = GL.GetUniformLocation(pgmId, "radius");
		}
		public override void Enable ()
		{
			base.Enable ();
			GL.Uniform1(radiusLoc, radius);
		}
	}
}

