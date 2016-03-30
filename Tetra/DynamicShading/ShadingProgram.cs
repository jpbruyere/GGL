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
	#endregion
	public class Uniform {
		public string Name;
		public int Location;
		int pgmId;
	}
	public class ShadingInterface{
		public int Location=-1;
		public GLType Type;
		public string Name;

		public override string ToString ()
		{
			return string.Format ("{0} {1}", Type.ToString().Substring(2), Name);
		}
	}
	public class ShaderSource : IDisposable
	{
		public string GLSLVersion = "unknown";
		public Precision FloatPrecision = Precision.highp;
		public Precision IntPrecision = Precision.highp;
		public int ShaderID;
		public ShaderType Type;
		public string Source;
		public string Path;
		public List<ShadingInterface> Inputs;
		public List<ShadingInterface> Outputs;
		public List<ShadingInterface> Uniforms;

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
		public void GetUniformLocations(ShadingProgram program)
		{
			foreach (ShadingInterface si in Uniforms) {
				if (program.Uniforms.ContainsKey (si.Name))
					continue;
				program.Uniforms [si.Name] = si;
				if (si.Location < 0)
					si.Location = GL.GetUniformLocation (program.pgmId, si.Name);
			}
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
			int i = 0;
			char c;
			string tok;

			Inputs = new List<ShadingInterface> ();
			Outputs = new List<ShadingInterface> ();
			Uniforms = new List<ShadingInterface> ();

			while(!eof(i)){
				if (!skipSpaces (ref i))
					break;
				switch (peek(i)) {
				case '#':
					i++;
					tok = nextToken (ref i);
					if (string.Equals (tok, "version", StringComparison.Ordinal)) {
						if (!skipSpaces (ref i))
							throw new Exception ("Syntax error");
						GLSLVersion = nextToken (ref i);
						readLine (ref i);
					}
					break;
				case '/':
					i++;
					c = peek (i);
					if (c == '/') {
						i++;
						readLine (ref i);
					} else if (c == '*') {
						i++;
						readBlockComment (ref i);
					}
					break;
				case '\n':
				case '\r':
					break;
				case '{':
					i++;
					while (!eof (i)) {
						tok = nextToken(ref i, true);
						if (string.Equals (tok, "}", StringComparison.Ordinal)) {
							break;
						}
					}
					break;
				default:
					ShadingInterface si = null;
					tok = nextToken (ref i);
					GLType t;
					if (!Enum.TryParse ("GL" + tok, out t)) {
						if (string.Equals (tok, "precision", StringComparison.Ordinal)) {
							Precision tmp;
							if (!Enum.TryParse (nextToken (ref i, true), out tmp))
								throw new Exception ("Syntax error");
							tok = nextToken (ref i, true);
							if (string.Equals (tok, "float", StringComparison.Ordinal))
								FloatPrecision = tmp;
							else if (string.Equals (tok, "int", StringComparison.Ordinal))
								IntPrecision = tmp;
							else
								throw new Exception ("Syntax error");

							checkAndSkipSemiColon (ref i);
							break;
						}
						si = new ShadingInterface ();
						if (string.Equals (tok, "layout", StringComparison.Ordinal)) {
							tok = nextToken (ref i, true);
							if (string.Equals (tok, "(", StringComparison.Ordinal)) {
								//process layout qualifier
								while (!eof (i)) {
									string qualifier, value = "";
									qualifier = nextQualifierName (ref i, true);
									tok = nextToken (ref i, true);
									if (string.Equals (tok, "=", StringComparison.Ordinal)) {
										value = nextValue (ref i, true);
										tok = nextToken (ref i, true);
									}
									if (string.Equals (qualifier, "location", StringComparison.Ordinal))
										si.Location = int.Parse (value);
									else if (string.Equals (qualifier, "std140", StringComparison.Ordinal))
										si.Location = int.Parse (value);
									else
										throw new Exception ("Unknown qualifier");

									Debug.WriteLine ("qualifier: " + qualifier + " value: " + value);
									if (string.Equals (tok, ")", StringComparison.Ordinal)) {
										tok = nextToken (ref i, true);
										break;
									}
									if (!string.Equals (tok, ",", StringComparison.Ordinal))
										throw new Exception ("Syntax error");
								}
							}
						}
						if (string.Equals (tok, "uniform", StringComparison.Ordinal))
							Uniforms.Add (si);
						else if (string.Equals (tok, "in", StringComparison.Ordinal))
							Inputs.Add (si);
						else if (string.Equals (tok, "out", StringComparison.Ordinal))
							Outputs.Add (si);

						tok = nextToken (ref i, true);
						if (!Enum.TryParse ("GL" + tok, out si.Type))
							throw new Exception ("Unknown type: " + tok);
					}
					tok = nextToken (ref i, true);

					if (si != null)
						si.Name = tok;

					tok = nextToken (ref i, true);
					if (string.Equals (tok, "(", StringComparison.Ordinal)) {
						//function
						while (!eof (i)) {
							tok = nextToken (ref i, true);
							if (string.Equals (tok, ")", StringComparison.Ordinal)) {
								break;
							}
						}
						break;
					} else if (string.Equals (tok, ";", StringComparison.Ordinal))
						break;

					checkAndSkipSemiColon (ref i);
					break;
				}
			}
		}
		bool checkAndSkipSemiColon(ref int i){
			return skipSpaces (ref i, true) ? Source [i++] == ';' :  false;
		}
		char peek(int i){
			if (eof(i))
				return (char)0;
			return Source [i];
		}
		string nextValue(ref int i, bool skipWhiteSpaces = false){
			return nextToken(ref i, skipWhiteSpaces);
		}
		string nextQualifierName(ref int i, bool skipWhiteSpaces = false){
			return nextToken(ref i, skipWhiteSpaces);
		}
		string nextName(ref int i, bool skipWhiteSpaces = false){
			return nextToken(ref i, skipWhiteSpaces);
		}
		string nextToken(ref int i, bool skipWhiteSpaces = false){
			if (skipWhiteSpaces){
				if (!skipSpaces (ref i))
					return null;
			}
			string tmp = "";
			while(!eof(i)){
				char c = peek(i);
				if (char.IsWhiteSpace (c))
					return tmp;
				else if (c == '\n')
					return tmp;
				else if (char.IsLetterOrDigit (c) || c == '_')
					tmp += c;
				else {
					if (string.IsNullOrEmpty (tmp)) {
						tmp += c;
						i++;
					}
					return tmp;
				}
				i++;
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
		bool skipSpaces(ref int i, bool skipLineBreak = false){
			while (i < Source.Length) {
				if (char.IsWhiteSpace (Source [i]) || Source [i] == '\r')
					i++;
				else if (skipLineBreak && Source [i] == '\n')
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

		public Dictionary<string, ShadingInterface> Uniforms;

		public int pgmId;

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

