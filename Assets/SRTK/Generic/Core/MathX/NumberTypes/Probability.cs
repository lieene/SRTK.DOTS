using System;
/************************************************************************************
| File: TwoStateInt.cs                                                              |
| Project: SRTK.MathX.NumberTypes                                                   |
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

// Generic Utility: Object Pool
// This code is created by Lieene from ShadeRealm.
// Copyright (c) Lieene(lieene@ShadeRealm.com) ShadeRealm.

namespace SRTK
{
    public partial class MathX
    {
        public struct Probability : IComparable<Probability>, IComparable<float>
        {
            internal float p;

            public Probability(float p) { this.p = p.Clamp01(); }
            public Probability(int percent) { this.p = percent.Clamp01() * 0.01f; }
            public static Probability XOverN(int x, int n) { return n <= 0 ? 1 : x / (float)n; }

            public float P
            {
                get { return p; }
                set { p = value.Clamp01(); }
            }

            public byte Percent
            {
                get { return unchecked((byte)(p * 100f)); }
                set { p = value.Clamp01() * 0.01f; }
            }

            public bool IsZero => p == 0;
            public Probability Not => 1 - p;

            public static Probability operator +(Probability l, Probability r) => l.p + r.p;
            public static Probability operator -(Probability l, Probability r) => l.p + r.p;
            public static Probability operator *(Probability l, float r) => l.p * r;
            public static Probability operator /(Probability l, float r) => r == 0 ? 1f : l.p / r;
            public static Probability operator !(Probability p) => p.Not;

            public int CompareTo(Probability other) => p.CompareTo(other.p);
            public static bool operator ==(Probability l, Probability r) => l.CompareTo(r) == 0;
            public static bool operator !=(Probability l, Probability r) => l.CompareTo(r) != 0;
            public static bool operator >(Probability l, Probability r) => l.CompareTo(r) > 0;
            public static bool operator <(Probability l, Probability r) => l.CompareTo(r) < 0;
            public static bool operator >=(Probability l, Probability r) => l.CompareTo(r) >= 0;
            public static bool operator <=(Probability l, Probability r) => l.CompareTo(r) <= 0;


            public int CompareTo(float other) => p.CompareTo(other);
            public static bool operator ==(Probability l, float r) => l.CompareTo(r) == 0;
            public static bool operator !=(Probability l, float r) => l.CompareTo(r) != 0;
            public static bool operator >(Probability l, float r) => l.CompareTo(r) > 0;
            public static bool operator <(Probability l, float r) => l.CompareTo(r) < 0;
            public static bool operator >=(Probability l, float r) => l.CompareTo(r) >= 0;
            public static bool operator <=(Probability l, float r) => l.CompareTo(r) <= 0;

            public static implicit operator Probability(float p) => new Probability(p);
            public static explicit operator float(Probability p) => p.p;

            public override bool Equals(object obj)
            {
                if (obj is Probability) return p == ((Probability)obj).p;
                else return false;
            }

            public override int GetHashCode()
                => HashCodeX.CombineHash(p.GetHashCode(), typeof(Probability).GetHashCode());

            public override string ToString() => $"Probability[{Percent}%]";

        }
    }
}
