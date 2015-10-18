using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MiscUtil;

namespace GGL
{
	public struct Size<T>
    {
		public static Size<T> Zero
		{ get { return new Size<T>((T) Convert.ChangeType(0, typeof(T)), (T) Convert.ChangeType(0, typeof(T))); } }
		T _width;
		T _height;

        //public Size()
        //{ }
		public Size(T width, T height)
        {
            _width = width;
            _height = height;
        }
		public T Width
        {
            get { return _width; }
            set { _width = value; }
        }
		public T Height
        {
            get { return _height; }
            set { _height = value; }
        }
        public override string ToString()
        {
            return string.Format("({0},{1})", Width, Height);
        }
		public static implicit operator Rectangle<T>(Size<T> s)
		{
			return new Rectangle<T> (s);
		}
		public static implicit operator Size<T>(T i)
        {
			return new Size<T>(i, i);
        }
		public static bool operator ==(Size<T> s1, Size<T> s2)
        {
			if (Operator.Equal(s1.Width, s2.Width) &&
				Operator.Equal(s1.Height, s2.Height))
                return true;
            else
                return false;
        }
		public static bool operator !=(Size<T> s1, Size<T> s2)
        {
			if (Operator.Equal(s1.Width, s2.Width) &&
				Operator.Equal(s1.Height, s2.Height))
				return false;
            else
                return true;
        }
		public static bool operator >(Size<T> s1, Size<T> s2)
        {
			if (Operator.GreaterThan(s1.Width, s2.Width) &&
				Operator.GreaterThan(s1.Height, s2.Height))
                return true;
            else
                return false;
        }
		public static bool operator >=(Size<T> s1, Size<T> s2)
        {
			if (Operator.GreaterThanOrEqual(s1.Width, s2.Width) &&
				Operator.GreaterThanOrEqual(s1.Height, s2.Height))
				return true;
            else
                return false;
        }
		public static bool operator <(Size<T> s1, Size<T> s2)
		{
			if (Operator.LessThan(s1.Width, s2.Width) &&
				Operator.LessThan(s1.Height, s2.Height))
				return true;
			else
				return false;
		}
		public static bool operator <=(Size<T> s1, Size<T> s2)
		{
			if (Operator.LessThanOrEqual(s1.Width, s2.Width) &&
				Operator.LessThanOrEqual(s1.Height, s2.Height))
				return true;
			else
				return false;
		}
//		public static bool operator <(Size<T> s1, Size<T> s2)
//        {
//            if (s1.Width < s2.Width)
//                if (s1.Height <= s2.Height)
//                    return true;
//                else
//                    return false;
//            else if (s1.Width == s2.Width && s1.Height < s2.Height)
//                return true;
//
//            return false;
//        }
//		public static bool operator <(Size<T> s, T i)
//		{
//			return s.Width < i && s.Height < i ? true : false;
//		}
//		public static bool operator <=(Size<T> s, T i)
//		{
//			return s.Width <= i && s.Height <= i ? true : false;
//		}
//		public static bool operator <=(Size<T> s1, Size<T> s2)
//        {
//            if (s1.Width <= s2.Width && s1.Height <= s2.Height)
//                return true;
//            else
//                return false;
//        }
//		public static bool operator ==(Size<T> s, T i)
//        {
//            if (s.Width == i && s.Height == i)
//                return true;
//            else
//                return false;
//        }
//		public static bool operator !=(Size<T> s, T i)
//        {
//            if (s.Width == i && s.Height == i)
//                return false;
//            else
//                return true;
//		}
		public static Size<T> operator +(Size<T> s1, Size<T> s2)
		{
			return new Size<T>(Operator.Add(s1.Width, s2.Width),
				Operator.Add(s1.Height, s2.Height));
		}
//		public static Size<T> operator +(Size<T> s, T i)
//        {
//			return new Size<T>(s.Width + i, s.Height + i);
//        }
    }

}
