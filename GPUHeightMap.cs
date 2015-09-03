//
//  GPUHeightMap.cs
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

namespace GGL
{
	public class GPUHeightMap
	{
		public static GameLib.EffectShader voronoiShader;

		static GPUHeightMap()
		{
			voronoiShader = new GameLib.EffectShader ("GGL.Shaders.GameLib.voronoi",1024,1024);			
		}

		public GPUHeightMap ()
		{
			
		}
	}
}

