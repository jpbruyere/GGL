//
//  Shader.cs
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
using System.IO;
using OpenTK;
using System.Diagnostics;

namespace Tetra.DynamicShading
{
	public class Shader : IDisposable
	{
		public string GLSLVersion = "unknown";
		public Precision FloatPrecision = Precision.highp;
		public Precision IntPrecision = Precision.highp;
		//TODO: parse head `layout(Packed)`
		public MemoryLayout DefaultMemoryLayout = MemoryLayout.Shared;
		public int ShaderID;
		public ShaderType Type;
		public string Source;
		public string Path;
		public List<ShadingInterface> Inputs;
		public List<ShadingInterface> Outputs;
		public List<ShadingInterface> Uniforms;

		public Shader(string _source, ShaderType _type){
			Type = _type;
			Source = _source;

			compile ();
		}
		public Shader(ShaderType _type, string _sourcePath = null){
			Type = _type;
			Path = _sourcePath;

			Reload ();
		}
		public void Reload(){
			load ();
			compile ();
		}
		public void GetUniformLocations(ShaderProgram program)
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
						si = new ShadingInterface (DefaultMemoryLayout);
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
									else if (string.Equals (qualifier, "index", StringComparison.Ordinal))
										si.Index = int.Parse (value);
									else if (string.Equals (qualifier, "std140", StringComparison.Ordinal))
										si.MemLayout = MemoryLayout.std140;
									else if (string.Equals (qualifier, "std430", StringComparison.Ordinal))
										si.MemLayout = MemoryLayout.std430;
									else if (string.Equals (qualifier, "Shared", StringComparison.Ordinal))
										si.MemLayout = MemoryLayout.Shared;
									else if (string.Equals (qualifier, "Packed", StringComparison.Ordinal))
										si.MemLayout = MemoryLayout.Packed;
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

						//TODO:could be uniform block name instead
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
}