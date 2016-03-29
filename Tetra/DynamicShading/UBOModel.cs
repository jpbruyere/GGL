﻿//
//  UBOModel.cs
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

namespace Tetra.DynamicShading
{
	public abstract class UBOModel{
		public int	GlobalBindingPoint,
					BlockIndex;
		public string Name;

		public UBOModel (int globalBindingPoint, string name = null){
			GlobalBindingPoint = globalBindingPoint;
			Name = name;
		}

		public abstract Type UBODataType { get; }
	}
	public class UBOModel<T> : UBOModel{

		public UBOModel (int globalBindingPoint, string name = null) :
		base(globalBindingPoint, name)
		{				
			if (string.IsNullOrEmpty(Name))
				Name = typeof(T).Name;
			else
				Name = name;
		}

		#region implemented abstract members of UBOModel

		public override Type UBODataType {
			get { return typeof(T);}
		}

		#endregion
	}
}

