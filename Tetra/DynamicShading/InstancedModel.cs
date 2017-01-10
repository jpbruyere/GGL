//
//  InstancedModel.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2017 jp
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

namespace Tetra.DynamicShading
{
	public class InstancedModel<T> : IDisposable where T : struct
	{
		public MeshPointer VAOPointer;
		public InstancesVBO<T> Instances;
		public int Diffuse;
		protected bool SyncVBO = true;

		public InstancedModel(MeshPointer vaoPointer){
			VAOPointer = vaoPointer;
		}

		public T[] Datas { get { return Instances.InstancedDatas; }}
		public void UpdateInstance(int index, T data){
			Instances.UpdateInstance (index, data);
			SyncVBO = true;
		}

		#region IDisposable implementation
		public void Dispose ()
		{
			Instances.Dispose ();
			if (GL.IsTexture (Diffuse))
				GL.DeleteTexture (Diffuse);
		}
		#endregion
	}
}

