//
//  ShaderData.cs
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
	public class ShaderData<T> : ShaderData{
		public T value;
		public ShaderData(string _name) : base(typeof(T), _name){
			
		}
	}
	public class ShaderData{
		public Type dataType;
		public string name;
		public ShaderData(Type _type, string _name){
			dataType = _type;
			name = _name;
		}
		public string GLSLType
		{
			get {
				switch (dataType.Name) {
				case "Single":
					return "float";
				case "Vector2":
					return "vec2";
				case "Vector3":
					return "vec3";
				case "Vector4":
					return "vec4";
				case "Matrix3":
					return "mat3";			
				case "Matrix4":
					return "mat4";
				default:
					throw new Exception ("No corresponding glsl type for " + dataType.Name);
				}
			}
		}
	}
}

