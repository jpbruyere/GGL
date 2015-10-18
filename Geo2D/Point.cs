using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiscUtil;

namespace GGL
{
	public struct Point<T>
    {
		T _x;
		T _y;

		public T X
        {
            get { return _x; }
            set { _x = value; }
        }
		public T Y
        {
            get { return _y; }
            set { _y = value; }
        }
		public Point(T x, T y)
        {
            _x = x;
            _y = y;
        }
		public static Point<T> Zero
		{ get { return new Point<T> (
				(T)Convert.ChangeType (0, typeof(T)), 
				(T)Convert.ChangeType (0, typeof(T))); } }
		public static implicit operator OpenTK.Vector3(Point<T> p)
        {
			return new OpenTK.Vector3(
				(float) Convert.ChangeType(p.X, typeof(float)),
				(float) Convert.ChangeType(p.Y, typeof(float)),0f);
        }
		public static implicit operator Point<T>(OpenTK.Vector2 p) 
        {
			return new Point<T>(
				(T) Convert.ChangeType(p.X, typeof(T)), 
				(T) Convert.ChangeType(p.Y, typeof(T)));
        }
//		public static implicit operator Cairo.Point(Point<T> p)
//        {
//            return new Cairo.Point(p.X, p.Y);
//        }
//		public static implicit operator Cairo.PointD(Point<T> p)
//        {
//            return new Cairo.PointD(p.X, p.Y);
//        }
//		public static implicit operator System.Drawing.Point(Point<T> p)
//        {
//            return new System.Drawing.Point(p.X, p.Y);
//        }
//		public static implicit operator Point<T>(System.Drawing.Point p)
//        {
//			return new Point<T>(p.X, p.Y);
//        }
		public static implicit operator Point<T>(T i)
        {
			return new Point<T>(i, i);
        }
		public static Point<T> operator /(Point<T> p, T d)
        {
			return new Point<T>(Operator.DivideAlternative(p.X, d), Operator.DivideAlternative(p.Y, d));
        }
		public static Point<T> operator *(Point<T> p, T d)
        {
			return new Point<T>(Operator.MultiplyAlternative(p.X, d), 
				Operator.MultiplyAlternative(p.Y, d));
        }
		public static Point<T> operator /(Point<T> p, double d)
        {
			return new Point<T>((T)(Operator.DivideAlternative(p.X, d)),
				(T)(Operator.DivideAlternative(p.Y, d)));
        }
		public static Point<T> operator *(Point<T> p, double d)
        {
			return new Point<T>((T)(Operator.MultiplyAlternative(p.X, d)),
				(T)(Operator.MultiplyAlternative(p.Y, d)));
        }
		public static Point<T> operator +(Point<T> p1, Point<T> p2)
        {
			return new Point<T>(Operator.Add(p1.X, p2.X),
				Operator.Add(p1.Y, p2.Y));
        }
		public static Point<T> operator +(Point<T> p, T i)
		{
			return new Point<T>(Operator.AddAlternative(p.X, i),
				Operator.AddAlternative(p.Y, i));
		}
		public static Point<T> operator -(Point<T> p1, Point<T> p2)
        {
			return new Point<T>(Operator.Subtract(p1.X, p2.X),
				Operator.Subtract(p1.Y, p2.Y));
        }
		public static bool operator >=(Point<T> p1, Point<T> p2)
        {
			return Operator.GreaterThanOrEqual(p1.X, p2.X) &&
				Operator.GreaterThanOrEqual(p1.Y, p2.Y) ? true : false;
        }
		public static bool operator <=(Point<T> p1, Point<T> p2)
        {
			return Operator.LessThanOrEqual(p1.X, p2.X) &&
				Operator.LessThanOrEqual(p1.Y, p2.Y) ? true : false;
        }
		public static bool operator <(Point<T> p1, Point<T> p2)
		{
			return Operator.LessThan(p1.X, p2.X) &&
				Operator.LessThan(p1.Y, p2.Y) ? true : false;
		}
		public static bool operator >(Point<T> p1, Point<T> p2)
		{
			return Operator.GreaterThan(p1.X, p2.X) &&
				Operator.GreaterThan(p1.Y, p2.Y) ? true : false;
		}

		//		public static bool operator ==(Point<T> s, T i)
//        {
//			if (Operator s.X == i && s.Y == i)
//                return true;
//            else
//                return false;
//        }
//		public static bool operator !=(Point<T> s, T i)
//        {
//            if (s.X == i && s.Y == i)
//                return false;
//            else
//                return true;
//        }
//		public static bool operator >(Point<T> s, T i)
//        {
//            if (s.X > i && s.Y > i)
//                return true;
//            else
//                return false;
//        }
//		public static bool operator <(Point<T> s, T i)
//        {
//            if (s.X < i && s.Y < i)
//                return true;
//            else
//                return false;
//        }
		public static bool operator ==(Point<T> s1, Point<T> s2)
        {
			if (Operator.Equal(s1.X, s2.X)  &&
				Operator.Equal(s1.Y, s2.Y))
                return true;
            else
                return false;
        }
		public static bool operator !=(Point<T> s1, Point<T> s2)
        {
			if (Operator.Equal(s1.X, s2.X)  &&
				Operator.Equal(s1.Y, s2.Y))
				return false;
            else
                return true;
        }

        public override string ToString()
        {
            return string.Format("({0},{1})", X, Y);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

}
