//
//  DualQuaternion.cs
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
using OpenTK;

namespace GGL
{
	public class DualQuaternion
	{
		public Quaternion m_real;
		public Quaternion m_dual;

		public DualQuaternion()
		{
			m_real = new Quaternion(0,0,0,1);
			m_dual = new Quaternion(0,0,0,0);
		}
		public DualQuaternion(Quaternion r, Quaternion d )
		{
			m_real = Quaternion.Normalize( r );
			m_dual = d;
		}
		public DualQuaternion( Quaternion r, Vector3 t )
		{
			m_real = Quaternion.Normalize( r );
			m_dual = ( new Quaternion( t, 0 ) * m_real ) * 0.5f;
		}
		public static float Dot(DualQuaternion a, DualQuaternion b)
		{
			throw new NotImplementedException ();
			//return Quaternion.Dot( a.m_real, b.m_real );
		}
		public static DualQuaternion operator *(DualQuaternion q, float scale)
		{
			DualQuaternion ret = q;
			ret.m_real *= scale;
			ret.m_dual *= scale;
			return ret;
		}
//		public static DualQuaternion Normalize( DualQuaternion q)
//		{
//			float mag = Quaternion.Dot( q.m_real, q.m_real );
//			Debug_c.Assert( mag > 0.000001f );
//			DualQuaternion_c ret = q;
//			ret.m_real *= 1.0f / mag;
//			ret.m_dual *= 1.0f / mag;
//			return ret;
//		}
//		public static DualQuaternion_c operator + (DualQuaternion_c
//			lhs, DualQuaternion_c rhs)
//		{
//			return new DualQuaternion_c(lhs.m_real + rhs.m_real,
//				lhs.m_dual + rhs.m_dual);
//		}
//		// Multiplication order - left to right
//		public static DualQuaternion_c operator * (DualQuaternion_c
//			lhs, DualQuaternion_c rhs)
//		{
//			return new DualQuaternion_c(rhs.m_real*lhs.m_real,
//				rhs.m_dual*lhs.m_real + rhs.m_real*lhs.m_dual);
//		}
		public static DualQuaternion Conjugate(DualQuaternion q)
		{
			return
				new
				DualQuaternion(
					Quaternion.Conjugate(
						q.m_real ), Quaternion.Conjugate( q.m_dual ) );
		}
		public Quaternion Rotation
		{
			get { return m_real;}
		}
		public Vector3 Translation
		{
			get {
				Quaternion t = (m_dual * 2.0f) * Quaternion.Conjugate (m_real);
				return new Vector3 (t.X, t.Y, t.Z);
			}
		}
//		public static Matrix
//		DualQuaternionToMatrix(
//			DualQuaternion_c q )
//		{
//			q = DualQuaternion_c.Normalize( q );
//			Matrix M = Matrix.Identity;
//			float w = q.m_real.W;
//			float x = q.m_real.X;
//			float y = q.m_real.Y;
//			float z = q.m_real.Z;
//			// Extract rotational information
//			M.M11 = w*w + x*x - y*y - z*z;
//			M.M12 = 2*x*y + 2*w*z;
//			M.M13 = 2*x*z - 2*w*y;
//			M.M21 = 2*x*y - 2*w*z;
//			M.M22 = w*w + y*y - x*x - z*z;
//			M.M23 = 2*y*z + 2*w*x;
//			M.M31 = 2*x*z + 2*w*y;
//			M.M32 = 2*y*z - 2*w*x;
//			M.M33 = w*w + z*z - x*x - y*y;
//			// Extract translation information
//			Quaternion t = (q.m_dual * 2.0f) * Quaternion.Conjugate(
//				q.m_real);
//			M.M41 = t.X;
//			M.M42 = t.Y;
//			M.M43 = t.Z;
//			return M;}
	}
}

