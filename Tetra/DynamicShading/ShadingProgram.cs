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

namespace Tetra.DynamicShading
{
	#region enums
	public enum Precision{
		unknown,
		lowp,
		mediump,
		highp
	}
	#endregion
	public class Uniform {
		public string Name;
		public int Location;
		int pgmId;
	}
	public class ShaderSource : IDisposable
	{		
		public string GLSLVersion = "unknown";
		public Precision FloatPrecision = Precision.unknown;
		public int ShaderID;
		public ShaderType Type;
		public string Source;
		public string Path;

		public ShaderSource(string _source, ShaderType _type){
			Type = _type;
			Source = _source;

			compile ();
		}
		public ShaderSource(ShaderType _type, string _sourcePath = null){
			Type = _type;
			Path = _sourcePath;

			Reload ();
		}
		public void Reload(){
			load ();
			compile ();
		}
		void load(){
			if (!string.IsNullOrEmpty (Path)) {
				Stream s = GGL.FileSystemHelpers.GetStreamFromPath (Path);
				using (StreamReader sr = new StreamReader (s))
					Source = sr.ReadToEnd ();
			}			
		}
		void compile()
		{
			anaylyse ();

			ShaderID = GL.CreateShader(Type);
			GL.ShaderSource(ShaderID, Source);
			GL.CompileShader(ShaderID);

			string info;
			GL.GetShaderInfoLog(ShaderID, out info);

			int compileResult;
			GL.GetShader(ShaderID, ShaderParameter.CompileStatus, out compileResult);
			if (compileResult != 1)
				throw new Exception("Compile Error!\n" + info);
		}
		#region Analyse
		void anaylyse(){
			string[] lines;

			int i = 0;
			char c;
			string tok;

			while(!eof(i)){
				if (!passSpaces (ref i))
					break;
				switch (Source[i++]) {
				case '#':
					tok = nextToken (ref i);
					if (string.Equals (tok, "version", StringComparison.Ordinal)) {
						if (!passSpaces (ref i))
							throw new Exception ("Syntax error");
						GLSLVersion = nextToken (ref i);
						readLine (ref i);
					}
					break;
				case '/':
					c = peek (i);	
					if (c == '/') {
						i++;
						readLine (ref i);
					} else if (c == '*') {
						i++;
						readBlockComment (ref i);
					}
					break;
				}
			}
		}
		char peek(int i){
			if (eof(i))
				return (char)0;
			return Source [i];
		}
		string nextToken(ref int i){			
			string tmp = "";
			while(!eof(i)){
				char c = Source [i++];
				if (char.IsWhiteSpace (c))
					return tmp;
				else if (c == '\r' || c == '\n')
					return tmp;
				tmp += c;
			}
			return tmp;
		}
		string readLine(ref int i){
			string tmp = "";
			while(!eof(i)){
				char c = Source [i++];
				if (c == '\r')
					continue;
				else if (c == '\n')
					return tmp;
				tmp += c;
			}
			return tmp;
		}
		string readBlockComment(ref int i){
			string tmp = "";
			while(!eof(i)){
				char c = Source [i++];
				if (c == '\r')
					continue;
				else if (c == '*') {
					if (peek (i) == '/')
						return tmp;					
				}					
				tmp += c;
			}
			return tmp;
		}
		bool passSpaces(ref int i){
			while (i < Source.Length) {
				if (char.IsWhiteSpace (Source [i]))
					i++;
				else
					return true;
			}
			return false;
		}
		bool eof(int i){
			return i >= Source.Length;
		}
		#endregion

		static string OtkToGlslType(Type t){
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

		#region IDisposable implementation
		public void Dispose ()
		{
			if (GL.IsShader (ShaderID))
				GL.DeleteShader (ShaderID);
		}
		#endregion
	}
	public class ShadingProgram : IDisposable
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

		static ShadingProgram(){
			maxUniformBufferBindings = GL.GetInteger (GetPName.MaxUniformBufferBindings);
		}
			
		#region CTOR
		public ShadingProgram ()
		{
			Init ();
		}
		public ShadingProgram (string vertResPath, string fragResPath = null, string geomResPath = null)
		{
			if (!string.IsNullOrEmpty (vertResPath))
				VertexShader = new ShaderSource (ShaderType.VertexShader, vertResPath);			

			if (!string.IsNullOrEmpty (fragResPath))				
				FragmentShader = new ShaderSource (ShaderType.FragmentShader, fragResPath);

			if (!string.IsNullOrEmpty (geomResPath))				
				GeometryShader = new ShaderSource (ShaderType.GeometryShader, geomResPath);

			Init ();
		}
		#endregion

		public ShaderSource	VertexShader,
							FragmentShader,
							GeometryShader;



		#region Private and protected fields
		protected int vsId, fsId, gsId, pgmId, mvpLocation;

		Matrix4 mvp = Matrix4.Identity;
		#endregion

		public Matrix4 MVP{
			set { mvp = value; }
			get { return mvp; }
		}

		#region Public functions
		/// <summary>
		/// configure sources and compile
		/// </summary>
		public virtual void Init()
		{
			if (VertexShader == null && !string.IsNullOrEmpty(defaultVertSource))
				VertexShader = new ShaderSource (defaultVertSource, ShaderType.VertexShader);
			if (FragmentShader == null && !string.IsNullOrEmpty(defaultFragSource))
				FragmentShader = new ShaderSource (defaultFragSource, ShaderType.FragmentShader);
			if (GeometryShader == null && !string.IsNullOrEmpty(defaultGeomSource))
				GeometryShader = new ShaderSource (defaultGeomSource, ShaderType.GeometryShader);
			
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
			GL.BindAttribLocation(pgmId, 0, "in_position");
			GL.BindAttribLocation(pgmId, 1, "in_tex");
		}
		protected virtual void GetUniformLocations()
		{
			mvpLocation = GL.GetUniformLocation(pgmId, "mvp");
		}
		protected virtual void BindSamplesSlots(){
			GL.Uniform1(GL.GetUniformLocation (pgmId, "tex"), 0);
		}

		public virtual void Enable(){
			GL.UseProgram (pgmId);

			GL.UniformMatrix4(mvpLocation, false, ref mvp);
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

