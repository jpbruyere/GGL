//
//  DynamicShader.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
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
using System.Collections.Generic;
using System.Reflection;
using OpenTK;

namespace Tetra.DynamicShading
{
	public class DynamicShader : Shader
	{
		#region enums
		public enum Precision{
			lowp,
			mediump,
			highp
		}
		#endregion


		#region GL Limits
		static int maxUniformBufferBindings;
		#endregion

		public List<UBOModel> UBOModels = new List<UBOModel>();

		static DynamicShader(){
			maxUniformBufferBindings = GL.GetInteger (GetPName.MaxUniformBufferBindings);
		}

		public string GLSLVersion = "330";
		public Precision FloatPrecision = Precision.highp;

		public DynamicShader (): base()
		{
			
		}

		public void BuildSources ()
		{
			vertSource = String.Format("#version {0}\n", GLSLVersion);
			vertSource += String.Format ("precision {0} float\n", FloatPrecision.ToString());

			vertSource += genUBOs ();
			//Compile ();
		}

		string genUBOs(){
			string tmp = "";
			foreach (UBOModel uboM in UBOModels) {
				tmp += string.Format ("layout (std140) uniform {0}{{\n", uboM.Name);
				foreach (FieldInfo fi in uboM.UBODataType.GetFields()) {
					tmp += string.Format ("\t{0} {1}\n", getGLSLType(fi.FieldType), fi.Name);
				}
				tmp += "};\n";
			}
			return tmp;
		}

		public void RegisterVertexAttributeStruct(Type vas){
			
		}
		public void RegisterUBODataStruct(UBOModel uboM){
			if (uboM.GlobalBindingPoint > maxUniformBufferBindings)
				throw new Exception ("UBO Error: max UBO binding point limit reached");

			UBOModels.Add (uboM);
		}


		protected override void GetUniformLocations ()
		{
			foreach (UBOModel uboM in UBOModels)
				GL.UniformBlockBinding(pgmId, GL.GetUniformBlockIndex(pgmId, uboM.Name), uboM.GlobalBindingPoint);			
		}
		static string getGLSLType(Type t){
			if (t == typeof(float))
				return "float";			
			if (t == typeof(Vector2))
				return "vec2";
			if (t == typeof(Vector3))
				return "vec3";
			if (t == typeof(Vector4))
				return "vec4";
			if (t == typeof(Matrix4))
				return "mat4";			
			
			return "unknown type";
		}
	}
}

