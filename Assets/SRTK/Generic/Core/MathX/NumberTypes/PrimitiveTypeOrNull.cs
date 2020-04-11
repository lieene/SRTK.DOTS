using System;
/************************************************************************************
| File: PrimitiveTypeOrNull.cs                                                      |
| Project: SRTK.NumberTypes                                                         |
| Created Date: Thu Sep 19 2019                                                     |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Mon Mar 23 2020                                                    |
| Modified By: Lieene Guo                                                           |
| -----                                                                             |
| MIT License                                                                       |
|                                                                                   |
| Copyright (c) 2019 Lieene@ShadeRealm                                              |
|                                                                                   |
| Permission is hereby granted, free of charge, to any person obtaining a copy of   |
| this software and associated documentation files (the "Software"), to deal in     |
| the Software without restriction, including without limitation the rights to      |
| use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies     |
| of the Software, and to permit persons to whom the Software is furnished to do    |
| so, subject to the following conditions:                                          |
|                                                                                   |
| The above copyright notice and this permission notice shall be included in all    |
| copies or substantial portions of the Software.                                   |
|                                                                                   |
| THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR        |
| IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,          |
| FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE       |
| AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER            |
| LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,     |
| OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE     |
| SOFTWARE.                                                                         |
|                                                                                   |
| -----                                                                             |
| HISTORY:                                                                          |
| Date      	By	Comments                                                        |
| ----------	---	----------------------------------------------------------      |
************************************************************************************/


namespace SRTK
{
    using BoN = MathX.BoolOrNull;
    using IoN = MathX.IntOrNull;
    public static partial class MathX
    {
        public struct BoolOrNull
        {
            private sbyte b;

            const sbyte _NULL = 0;
            const sbyte _TRUE = 1;
            const sbyte _FALSE = -1;

            public static readonly BoolOrNull Null = default(BoolOrNull);
            public static readonly BoolOrNull True = true;
            public static readonly BoolOrNull False = false;

            public BoolOrNull(bool isTrue)
            {
                if (isTrue) b = _TRUE;
                else b = _FALSE;
            }

            public BoolOrNull(object o)
            {
                if (o == null) b = _NULL;
                else throw new OverflowException();
            }

            public bool IsNull
            {
                get { return b == _NULL; }
                set { if (value) b = _NULL; }
            }

            public bool IsTrue
            {
                get { return b == _TRUE; }
                set { if (value) b = _TRUE; else b = _FALSE; }
            }

            public bool IsFalse
            {
                get { return b == _FALSE; }
                set { if (value) b = _FALSE; else b = _TRUE; }
            }

            public sbyte GetInteralValue() => b;
            public static BoolOrNull FromInteral(sbyte data) => new BoN { b = data };

            public BoN Not => new BoN { b = unchecked((sbyte)-b) };
            public static BoN operator !(BoN b) => b.Not;

            public static BoN operator &(bool l, BoN r) => l ? r : False;
            public static BoN operator &(BoN l, bool r) => r ? l : False;

            public static BoN operator |(bool l, BoN r) => l ? True : r;
            public static BoN operator |(BoN l, bool r) => r ? True : l;
            public static BoN operator ^(bool l, BoN r) => l ? r.Not : r;
            public static BoN operator ^(BoN l, bool r) => r ? l.Not : l;

            public static BoN operator &(BoN l, BoN r)
            {
                if (l.IsFalse || r.IsFalse) return False;
                else if (l.IsTrue && r.IsTrue) return True;
                else return Null;
            }

            public static BoN operator |(BoN l, BoN r)
            {
                if (l.IsFalse && r.IsFalse) return False;
                else if (l.IsTrue || r.IsTrue) return True;
                else return Null;
            }

            public static BoN operator ^(BoN l, BoN r)
            {
                if (l.b == r.b) return l.IsNull ? Null : False;
                else return (l.IsNull || r.IsNull) ? Null : True;
            }

            public static implicit operator BoN(bool b) => new BoN(b);
            public static explicit operator bool(BoN n)
            {
                if (!n.IsNull) return n.IsTrue;
                else throw new OverflowException();
            }

            public static explicit operator BoN(int v)
                => new BoN() { b = v == _NULL ? _NULL : v > _NULL ? _TRUE : _FALSE };


            public static bool operator ==(BoN l, BoN r) => l.b == r.b;
            public static bool operator !=(BoN l, BoN r) => l.b != r.b;


            public override bool Equals(object obj)
            {
                if (obj is bool) return ((bool)obj) ? IsTrue : IsFalse;
                if (obj is BoN) return this == (BoN)obj;
                return false;
            }

            public override int GetHashCode() => b.GetHashCode();

            public override string ToString() => IsNull ? "NULL" : IsTrue.ToString();
        }

        public struct IntOrNull : IComparable<IntOrNull>
        {
            private int i;

            public const int IntNULL = int.MinValue;
            public const int IntMinValue = int.MinValue + 1;
            public const int IntMaxValue = int.MaxValue;

            public static readonly IntOrNull Null = new IntOrNull { i = IntNULL };
            public static readonly IntOrNull MinValue = new IntOrNull { i = IntMinValue };
            public static readonly IntOrNull MaxValue = new IntOrNull { i = IntMaxValue };

            public IntOrNull(int value)
            {
                if (value != IntNULL) i = value;
                else throw new OverflowException();
            }

            public IntOrNull(object o)
            {
                if (o == null) i = IntNULL;
                else throw new OverflowException();
            }

            public bool IsZero => i == 0;

            public bool IsNull
            {
                get { return i == int.MinValue; }
                set { if (value) i = int.MinValue; }
            }

            public int Value
            {
                get
                {
                    if (i != IntNULL) return i;
                    throw new OverflowException();
                }
                set
                {
                    if (value != IntNULL) i = value;
                    throw new OverflowException();
                }
            }

            public int GetInteralValue() => i;
            public static IntOrNull FromInteral(int data) => new IoN { i = data };

            public static IoN operator -(IoN i) => i.IsNull ? i : new IoN(-i.i);
            public static IoN operator ~(IoN i) => i.IsNull ? i : new IoN(~i.i);

            public static IoN operator +(IoN l, IoN r) => l.IsNull ? r : r.IsNull ? l : new IoN(l.i + r.i);
            public static IoN operator -(IoN l, IoN r) => l.IsNull ? -r : r.IsNull ? l : new IoN(l.i - r.i);
            public static IoN operator *(IoN l, IoN r) => l.IsNull ? r : r.IsNull ? l : new IoN(l.i * r.i);
            public static IoN operator /(IoN l, IoN r) => l.IsNull ? Null : r.IsNull ? l : r.i == 0 ? Null : new IoN(l.i / r.i);
            public static IoN operator %(IoN l, IoN r) => l.IsNull ? Null : r.IsNull ? l : r.i == 0 ? Null : new IoN(l.i % r.i);

            public static IoN operator ^(IoN l, IoN r) => (l.IsNull || r.IsNull) ? Null : new IoN(l.i ^ r.i);

            public static IoN operator &(IoN l, IoN r)
            {
                if (l.i == 0 || r.i == 0) return new IoN { i = 0 };//0= 0x0000....
                if (r.IsNull || r.IsNull) return Null;
                return new IoN(l.i & r.i);
            }
            public static IoN operator |(IoN l, IoN r)
            {
                if (l.i == -1 || r.i == -1) return new IoN { i = -1 }; //-1= 0xFFFF....
                if (r.IsNull || r.IsNull) return Null;
                return new IoN(l.i | r.i);
            }

            public static IoN operator <<(IoN val, int s) => val.IsNull ? Null : new IoN(val.i << s);
            public static IoN operator >>(IoN val, int s) => val.IsNull ? Null : new IoN(val.i >> s);

            public static explicit operator int(IoN n) => n.Value;
            public static explicit operator IoN(int v) => new IoN(v);

            public int CompareTo(IoN other) => Value.CompareTo(other.Value);

            public static BoN operator >(IoN l, IoN r) => (l.IsNull || r.IsNull) ? BoN.Null : l.i > r.i ? BoN.True : BoN.False;
            public static BoN operator <(IoN l, IoN r) => (l.IsNull || r.IsNull) ? BoN.Null : l.i < r.i ? BoN.True : BoN.False;
            public static BoN operator >=(IoN l, IoN r) => (l.IsNull || r.IsNull) ? BoN.Null : l.i >= r.i ? BoN.True : BoN.False;
            public static BoN operator <=(IoN l, IoN r) => (l.IsNull || r.IsNull) ? BoN.Null : l.i <= r.i ? BoN.True : BoN.False;
            public static BoN operator ==(IoN l, IoN r) => (l.IsNull || r.IsNull) ? BoN.Null : l.i == r.i ? BoN.True : BoN.False;
            public static BoN operator !=(IoN l, IoN r) => (l.IsNull || r.IsNull) ? BoN.Null : l.i != r.i ? BoN.True : BoN.False;


            public static BoN operator >(int l, IoN r) => r.IsNull ? BoN.Null : l > r.i ? BoN.True : BoN.False;
            public static BoN operator <(int l, IoN r) => r.IsNull ? BoN.Null : l < r.i ? BoN.True : BoN.False;
            public static BoN operator >=(int l, IoN r) => r.IsNull ? BoN.Null : l >= r.i ? BoN.True : BoN.False;
            public static BoN operator <=(int l, IoN r) => r.IsNull ? BoN.Null : l <= r.i ? BoN.True : BoN.False;
            public static BoN operator ==(int l, IoN r) => r.IsNull ? BoN.Null : l == r.i ? BoN.True : BoN.False;
            public static BoN operator !=(int l, IoN r) => r.IsNull ? BoN.Null : l != r.i ? BoN.True : BoN.False;

            public static BoN operator >(IoN l, int r) => l.IsNull ? BoN.Null : l.i > r ? BoN.True : BoN.False;
            public static BoN operator <(IoN l, int r) => l.IsNull ? BoN.Null : l.i < r ? BoN.True : BoN.False;
            public static BoN operator >=(IoN l, int r) => l.IsNull ? BoN.Null : l.i >= r ? BoN.True : BoN.False;
            public static BoN operator <=(IoN l, int r) => l.IsNull ? BoN.Null : l.i <= r ? BoN.True : BoN.False;
            public static BoN operator ==(IoN l, int r) => l.IsNull ? BoN.Null : l.i == r ? BoN.True : BoN.False;
            public static BoN operator !=(IoN l, int r) => l.IsNull ? BoN.Null : l.i != r ? BoN.True : BoN.False;

            public override bool Equals(object obj)
            {
                if (IsNull) return false;
                if (obj is IoN) return i == ((IoN)obj).i;
                return false;
            }

            public override int GetHashCode() => i.GetHashCode();
            public override string ToString() => IsNull ? "NULL" : i.ToString();
        }
    }
}