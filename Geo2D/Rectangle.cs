using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using MiscUtil;

namespace GGL
{    
	public struct Rectangle<T>
    {
		#region private fields
		T _x;
		T _y;
		T _width;
		T _height;
		#endregion

		#region ctor
		public Rectangle(Point<T> p, Size<T> s)
        {
            _x = p.X;
            _y = p.Y;
            _width = s.Width;
            _height = s.Height;
        }
		public Rectangle(Size<T> s)
        {
			_x = (T) Convert.ChangeType(0, typeof(T));
			_y = (T) Convert.ChangeType(0, typeof(T));
            _width = s.Width;
            _height = s.Height;
        }
		public Rectangle(T x, T y, T width, T height)
        {
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }
		#endregion

		#region PROPERTIES
		[XmlIgnore]public T X{
            get { return _x; }
            set { _x = value; }
        }
		[XmlIgnore]public T Y{
            get { return _y; }
            set { _y = value; }
        }
		[XmlIgnore]public T Left{
            get { return _x; }
            set { _x = value; }
        }
		[XmlIgnore]public T Top{
            get { return _y; }
            set { _y = value; }
        }
		[XmlIgnore]public T Right{
			get { return Operator.Add(_x , _width); }
        }
		[XmlIgnore]public T Bottom{
			get { return Operator.Add(_y , _height); }
        }
		[XmlIgnore]public T Width{
            get { return _width; }
            set { _width = value; }
        }
		[XmlIgnore]public T Height{
            get { return _height; }
            set { _height = value; }
        }
		[XmlIgnore]public Size<T> Size{
			get { return new Size<T>(Width, Height); }
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }
		[XmlIgnore]public Point<T> Position{
			get { return new Point<T>(X, Y); }
			set
			{
				X = value.X;
				Y = value.Y;
			}
		}
		[XmlIgnore]public Point<T> TopLeft{
            set
            {
                X = value.X;
                Y = value.Y;
            }
			get { return new Point<T>(X, Y); }
        }
		[XmlIgnore]public Point<T> TopRight{
			get { return new Point<T>(Right, Y); }
        }
		[XmlIgnore]public Point<T> BottomLeft{
			get { return new Point<T>(X, Bottom); }
        }
		[XmlIgnore]public Point<T> BottomRight{
			get { return new Point<T>(Right, Bottom); }
			set {
				Width = Operator.Subtract (value.X, X);
				Height = Operator.Subtract (value.Y, Y);
			}
        }
		[XmlIgnore]public Point<T> Center
        {
			get {
				T halfWidth = Operator.Divide (Width, (T) Convert.ChangeType(2, typeof(T)));
				T halfHeight = Operator.Divide (Height, (T) Convert.ChangeType(2, typeof(T)));
				return new Point<T>(
					Operator.Add(Left, halfWidth),
					Operator.Add(Top, halfHeight)); }
        }
		#endregion

		#region FUNCTIONS
        public void Inflate(T xDelta, T yDelta)
        {
			this.X =Operator.Add(this.X, xDelta);
			this.Width =Operator.Add(this.Width, Operator.MultiplyAlternative(xDelta,2));
			this.Y =Operator.Subtract(this.Y, yDelta);
			this.Height = Operator.Add(this.Height, Operator.MultiplyAlternative(yDelta,2));
        }
		public void Inflate(T delta)
		{
			Inflate (delta, delta);
		}
		public bool ContainsOrIsEqual(Point<T> p)
        {
			return (Operator.GreaterThanOrEqual(p.X, X) && 
				Operator.LessThanOrEqual(p.X, this.Right) && 
				Operator.GreaterThanOrEqual(p.Y, Y) && 
				Operator.LessThanOrEqual(p.Y, this.Bottom)) ?
                true : false;
        }
        public bool ContainsOrIsEqual(Rectangle<T> r)
        {
			return Operator.GreaterThanOrEqual(r.TopLeft, this.TopLeft) && 
				Operator.LessThanOrEqual(r.BottomRight, this.BottomRight) ? true : false;
        }
        public bool Intersect(Rectangle<T> r)
        {
			T maxLeft = Operator.Max(this.Left, r.Left);
			T minRight = Operator.Min(this.Right, r.Right);
			T maxTop = Operator.Max(this.Top, r.Top);
			T minBottom = Operator.Min(this.Bottom, r.Bottom);

			return Operator.LessThan(maxLeft, minRight) && Operator.LessThan(maxTop, minBottom) ?
				true : false;
        }
//        public Rectangle<T> Intersection(Rectangle<T> r)
//        {
//            Rectangle<T> result = new Rectangle<T>();
//            
//            if (r.Left >= this.Left)
//                result.Left = r.Left;
//            else
//                result.TopLeft = this.TopLeft;
//
//            if (r.Right >= this.Right)
//                result.Width = this.Right - result.Left;
//            else
//                result.Width = r.Right - result.Left;
//
//            if (r.Top >= this.Top)
//                result.Top = r.Top;
//            else
//                result.Top = this.Top;
//
//            if (r.Bottom >= this.Bottom)
//                result.Height = this.Bottom - result.Top;
//            else
//                result.Height = r.Bottom - result.Top;
//
//            return result;
//        }
		#endregion

        #region operators
//        public static implicit operator Rectangle<T>(System.Drawing.Rectangle r)
//        {
//            return new Rectangle<T>(r.X, r.Y, r.Width, r.Height);
//        }
//        public static implicit operator System.Drawing.Rectangle(Rectangle<T> r)
//        {
//            return new System.Drawing.Rectangle(r.X, r.Y, r.Width, r.Height);
//        }
//        public static implicit operator Cairo.Rectangle(Rectangle<T> r)
//        {
//            return new Cairo.Rectangle((double)r.X, (double)r.Y, (double)r.Width, (double)r.Height);
//        }
        public static Rectangle<T> operator +(Rectangle<T> r1, Rectangle<T> r2)
        {
			T x = Operator.Min(r1.X, r2.X);
			T y = Operator.Min(r1.Y, r2.Y);
			T x2 = Operator.Max(r1.Right, r2.Right);
			T y2 = Operator.Max(r1.Bottom, r2.Bottom);
			return new Rectangle<T>(x, y,Operator.Subtract(x2, x),Operator.Subtract(y2, y));
        }
		public static Rectangle<T> operator +(Rectangle<T> r, Point<T> p)
		{
			return new Rectangle<T>(
				Operator.Add(r.X, p.X),Operator.Add(r.Y, p.Y),
				r.Width, r.Height);
		}
        public static bool operator ==(Rectangle<T> r1, Rectangle<T> r2)
        {
			return Operator.Equal(r1.TopLeft, r2.TopLeft) && 
				Operator.Equal(r1.Size, r2.Size) ? true : false;
        }
        public static bool operator !=(Rectangle<T> r1, Rectangle<T> r2)
        {
			return Operator.Equal(r1.TopLeft, r2.TopLeft) && 
				Operator.Equal(r1.Size, r2.Size) ? false : true;        
		}
        #endregion

        

		public static Rectangle<T> Zero
        {
			get { return new Rectangle<T>(
				(T) Convert.ChangeType(0, typeof(T)),
				(T) Convert.ChangeType(0, typeof(T)),
				(T) Convert.ChangeType(0, typeof(T)),
				(T) Convert.ChangeType(0, typeof(T))); }
        }
		public static Rectangle<T> Empty
        {
            get { return Zero; }
        }
        public override string ToString()
        {
            return string.Format("{0};{1};{2};{3}", X, Y, Width, Height);
        }
		public static Rectangle<T> Parse(string s)
        {
            string[] d = s.Split(new char[] { ';' });
			return new Rectangle<T> (
				(T)typeof(T).GetMethod ("Parse").Invoke (null, new object[]{ d [0] }),
				(T)typeof(T).GetMethod ("Parse").Invoke (null, new object[]{ d [1] }),
				(T)typeof(T).GetMethod ("Parse").Invoke (null, new object[]{ d [2] }),
				(T)typeof(T).GetMethod ("Parse").Invoke (null, new object[]{ d [3] }));
        }
    }

}
