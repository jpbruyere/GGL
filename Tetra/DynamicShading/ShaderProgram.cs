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
using System.IO;
using System.Diagnostics;

namespace Tetra.DynamicShading
{
	#region enums
	public enum Precision{
		unknown,
		lowp,
		mediump,
		highp
	}
	public enum GLType{
		GLvoid,
		GLfloat,
		GLdouble,
		GLbool,
		GLint,
		GLuint,
		GLvec2,
		GLvec3,
		GLvec4,
		GLbvec2,
		GLbvec3,
		GLbvec4,
		GLivec2,
		GLivec3,
		GLivec4,
		GLuvec2,
		GLuvec3,
		GLuvec4,
		GLdvec2,
		GLdvec3,
		GLdvec4,
		GLmat2x4,
		GLmat2,
		GLmat3,
		GLmat4,
		GLsampler1D,
		GLsampler2D,
		GLsampler3D,
		GLsamplerCube,
		GLsampler2DRect​,
		GLsampler1DArray​,
		GLsampler2DArray​,
		GLsamplerCubeArray,
		GLsamplerBuffer,
		GLsampler2DMS​,
		GLsampler2DMSArray​,
		GLsampler1DShadow,
		GLSampler2DShadow,
		GLsamplerCubeShadow,
		GLsampler2DRect​Shadow,
		GLsampler1DArray​Shadow,
		GLsampler2DArray​Shadow,
		GLsamplerCubeArrayShadow,
	}
	public enum MemoryLayout { Shared, Packed, std140, std430 }
	#endregion
	public class Uniform {
		public string Name;
		public int Location;
		int pgmId;
	}
	public class ShadingInterface{
		public int Location=-1;
		public int Index=-1;
		public GLType Type;
		public string Name;
		public MemoryLayout MemLayout;

		public ShadingInterface(MemoryLayout memLayout){
			MemLayout = memLayout;
		}

		public override string ToString ()
		{
			return string.Format ("{0} {1}", Type.ToString().Substring(2), Name);
		}
	}

	public class ShaderProgram : IDisposable
	{
		#region Default Shaders Sources
		const string defaultVertSource = @"
			#version 330
			precision lowp float;

			uniform mat4 mvp;

			layout(location = 0) in vec3 in_position;
			layout(location = 1) in vec2 in_tex;

			out vec2 texCoord;

			void main(void)
			{
				texCoord = in_tex;
				gl_Position = mvp * vec4(in_position, 1.0);
			}";

		const string defaultFragSource = @"
			#version 330
			precision lowp float;

			uniform sampler2D tex;

			in vec2 texCoord;
			out vec4 out_frag_color;

			void main(void)
			{
				out_frag_color = texture( tex, texCoord);
			}";
		string defaultGeomSource = @"";
		//			#version 330
		//			layout(triangles) in;
		//			layout(triangle_strip, max_vertices=3) out;
		//			void main()
		//			{
		//				for(int i=0; i<3; i++)
		//				{
		//					gl_Position = gl_in[i].gl_Position;
		//					EmitVertex();
		//				}
		//				EndPrimitive();
		//			}";
		#endregion

		#region GL Limits
		static int maxUniformBufferBindings;
		#endregion

		static ShaderProgram(){
			maxUniformBufferBindings = GL.GetInteger (GetPName.MaxUniformBufferBindings);
		}

		#region CTOR
		public ShaderProgram ()
		{
			Init ();
		}
		public ShaderProgram (string vertResPath, string fragResPath = null, string geomResPath = null)
		{
			if (!string.IsNullOrEmpty (vertResPath))
				VertexShader = new Shader (ShaderType.VertexShader, vertResPath);

			if (!string.IsNullOrEmpty (fragResPath))
				FragmentShader = new Shader (ShaderType.FragmentShader, fragResPath);

			if (!string.IsNullOrEmpty (geomResPath))
				GeometryShader = new Shader (ShaderType.GeometryShader, geomResPath);

			Init ();
		}
		#endregion

		public Shader	VertexShader,
							FragmentShader,
							GeometryShader;

		public Dictionary<string, ShadingInterface> Uniforms;

		public int pgmId;

		#region Public functions
		/// <summary>
		/// configure sources and compile
		/// </summary>
		public virtual void Init()
		{
			if (VertexShader == null && !string.IsNullOrEmpty(defaultVertSource))
				VertexShader = new Shader (defaultVertSource, ShaderType.VertexShader);
			if (FragmentShader == null && !string.IsNullOrEmpty(defaultFragSource))
				FragmentShader = new Shader (defaultFragSource, ShaderType.FragmentShader);
			if (GeometryShader == null && !string.IsNullOrEmpty(defaultGeomSource))
				GeometryShader = new Shader (defaultGeomSource, ShaderType.GeometryShader);

			Compile ();
		}
		public virtual void Compile()
		{
			pgmId = GL.CreateProgram();

			if (VertexShader != null)
				GL.AttachShader(pgmId, VertexShader.ShaderID);
			if (FragmentShader != null)
				GL.AttachShader(pgmId, FragmentShader.ShaderID);
			if (GeometryShader != null)
				GL.AttachShader(pgmId, GeometryShader.ShaderID);

			BindVertexAttributes ();

			string info;
			GL.LinkProgram(pgmId);
			GL.GetProgramInfoLog(pgmId, out info);

			if (!string.IsNullOrEmpty (info))
				throw new Exception ("Linkage Error: " + info);

			info = null;

			GL.ValidateProgram(pgmId);
			GL.GetProgramInfoLog(pgmId, out info);
			if (!string.IsNullOrEmpty (info))
				throw new Exception ("Validation Error: " + info);

			GL.UseProgram (pgmId);


			GetUniformLocations ();
			BindSamplesSlots ();

			Disable ();
		}

		protected virtual void BindVertexAttributes()
		{
			foreach (ShadingInterface si in VertexShader.Inputs)
				GL.BindAttribLocation(pgmId, si.Location, si.Name);
		}
		protected virtual void GetUniformLocations()
		{
			Uniforms = new Dictionary<string, ShadingInterface> ();

			if (VertexShader != null)
				VertexShader.GetUniformLocations (this);
			if (FragmentShader != null)
				FragmentShader.GetUniformLocations (this);
			if (GeometryShader != null)
				GeometryShader.GetUniformLocations (this);
		}
		protected virtual void BindSamplesSlots(){
			GL.Uniform1(GL.GetUniformLocation (pgmId, "tex"), 0);
		}

		public virtual void Enable(){
			GL.UseProgram (pgmId);


		}
		public virtual void Disable(){
			GL.UseProgram (0);
		}
		public static void Enable(ShaderProgram s)
		{
			if (s == null)
				return;
			s.Enable ();
		}
		public static void Disable(ShaderProgram s)
		{
			if (s == null)
				return;
			s.Disable ();
		}
		#endregion


		#region IDisposable implementation
		public virtual void Dispose ()
		{
			if (GL.IsProgram (pgmId))
				GL.DeleteProgram (pgmId);

			if (VertexShader != null)
				VertexShader.Dispose ();
			if (FragmentShader != null)
				FragmentShader.Dispose ();
			if (GeometryShader != null)
				GeometryShader.Dispose ();
		}
		#endregion
	}
}

