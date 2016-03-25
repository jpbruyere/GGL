//
//  DynamicShader.cs
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
using System.IO;
using System.Text;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;
using OpenTK;

namespace GGL
{
	public class DynamicShader
	{
		public enum Precision
		{
			unset,
			highp,
			mediump,
			lowp
		}


		string version = "330";
		public Precision floatPrecision = Precision.unset;
		public List<ShaderData> VertexAttributes = new List<ShaderData>();
		public List<ShaderData> Uniforms = new List<ShaderData>();

		string vertSource = "";
		string fragSource = "";
		string geomSource = "";

		protected int vsId, fsId, gsId, pgmId;
		protected int[] uniformLocations;

		public DynamicShader ()
		{
			
		}

		public void BuildSources()
		{			
			StringBuilder sb = new StringBuilder ();
			sb.AppendLine ("#version " + version);
			if (floatPrecision != Precision.unset)
				sb.AppendLine ("precision " + floatPrecision.ToString () + " float");
			sb.AppendLine ();
			foreach (ShaderData va in VertexAttributes) {
				sb.AppendLine (
					"layout (location = " +
					VertexAttributes.IndexOf (va).ToString () +
					") in " + va.GLSLType + " " + va.name + ";");
			}
			sb.AppendLine ();
			foreach (ShaderData u in Uniforms) {
				sb.AppendLine (
					"uniform " + u.GLSLType + " " + u.name + ";");
			}
			sb.AppendLine ();
			sb.AppendLine ("void main(void) {");
			sb.AppendLine ();
			sb.AppendLine ("}");

			vertSource = sb.ToString ();
		}

		#region Public functions
		public virtual void Compile()
		{
			Dispose ();

			BuildSources ();



			pgmId = GL.CreateProgram();

			if (!string.IsNullOrEmpty(vertSource))
			{
				vsId = GL.CreateShader(ShaderType.VertexShader);
				compileShader(vsId, vertSource);
			}
			if (!string.IsNullOrEmpty(fragSource))
			{
				fsId = GL.CreateShader(ShaderType.FragmentShader);
				compileShader(fsId, fragSource);

			}
			if (!string.IsNullOrEmpty(geomSource))
			{
				gsId = GL.CreateShader(ShaderType.GeometryShader);
				compileShader(gsId,geomSource);                
			}

			if (vsId != 0)
				GL.AttachShader(pgmId, vsId);
			if (fsId != 0)
				GL.AttachShader(pgmId, fsId);
			if (gsId != 0)
				GL.AttachShader(pgmId, gsId);

			BindVertexAttributes ();

			string info;
			GL.LinkProgram(pgmId);
			GL.GetProgramInfoLog(pgmId, out info);

			if (!string.IsNullOrEmpty (info)) {
				Debug.WriteLine ("Linkage:");
				Debug.WriteLine (info);
			}

			info = null;

			GL.ValidateProgram(pgmId);
			GL.GetProgramInfoLog(pgmId, out info);
			if (!string.IsNullOrEmpty (info)) {
				Debug.WriteLine ("Validation:");
				Debug.WriteLine (info);
			}

			GL.UseProgram (pgmId);

			GetUniformLocations ();
			BindSamplesSlots ();

			Disable ();
		}

		protected virtual void BindVertexAttributes()
		{
			GL.BindAttribLocation(pgmId, 0, "in_position");						
			GL.BindAttribLocation(pgmId, 1, "in_tex");
		}
		protected virtual void GetUniformLocations()
		{
			uniformLocations = new int[Uniforms.Count];
			for (int i = 0; i < Uniforms.Count; i++) {
				uniformLocations[i] = GL.GetUniformLocation(pgmId, Uniforms[i].name);
			}
		}
		protected virtual void BindSamplesSlots(){
			GL.Uniform1(GL.GetUniformLocation (pgmId, "tex"), 0);
		}

		public virtual void Enable(){
			GL.UseProgram (pgmId);

			int i = 0;
			foreach (ShaderData<dynamic> data in Uniforms) {
				switch (data.dataType.Name) {
				case "Single":
					GL.Uniform1 (uniformLocations [i], data.value);
					break;
				case "Vector2":
					GL.Uniform2 (uniformLocations [i], data.value);
					break;
				case "Vector3":
					GL.Uniform3 (uniformLocations [i], data.value);
					break;
				case "Vector4":
					GL.Uniform4 (uniformLocations [i], data.value);
					break;
				case "Matrix4":
					Matrix4 m4 = (Matrix4)data.value;
					GL.UniformMatrix4 (uniformLocations [i], false, ref m4);
					break;
				default:
					break;
				}				

				i++;
			}
		}
		public virtual void Disable(){
			GL.UseProgram (0);
		}
		public static void Enable(Shader s)
		{
			if (s == null)
				return;
			s.Enable ();
		}
		public static void Disable(Shader s)
		{
			if (s == null)
				return;
			s.Disable ();
		}
		#endregion

		void compileShader(int shader, string source)
		{
			GL.ShaderSource(shader, source);
			GL.CompileShader(shader);

			string info;
			GL.GetShaderInfoLog(shader, out info);
			Debug.WriteLine(info);

			int compileResult;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out compileResult);
			if (compileResult != 1)
			{
				Debug.WriteLine("Compile Error!");
				Debug.WriteLine(source);
			}
		}			

		#region IDisposable implementation
		public virtual void Dispose ()
		{
			if (GL.IsProgram (pgmId))
				GL.DeleteProgram (pgmId);

			if (GL.IsShader (vsId))
				GL.DeleteShader (vsId);
			if (GL.IsShader (fsId))
				GL.DeleteShader (fsId);
			if (GL.IsShader (gsId))
				GL.DeleteShader (gsId);
		}
		#endregion
	}
}

