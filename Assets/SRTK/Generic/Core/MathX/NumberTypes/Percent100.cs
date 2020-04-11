/************************************************************************************
| File: Percent100.cs                                                               |
| Project: SRTK.NumberTypes                                                         |
| Created Date: Tue Sep 17 2019                                                     |
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

using System;
namespace SRTK
{
    public partial class MathX
    {
        /// <summary>
        /// Percentage store by Byte(0-100) as Whole Number, while a infinitive state that keep Percentage hidden but restorable
        /// </summary>
        public struct Percent100
        {
            const float B_1PERCENT = 0.01f;
            const float B_1PERCENT_POW2 = 0.0001f;
            const float B_MAX_PERCENT = B_1PERCENT * 255;
            const sbyte B_100PERCENT = 100;
            const sbyte B_INFINITIVE = 101;

            private sbyte _percent;

            public static Percent100 Raw(sbyte percent) => new Percent100() { _percent = percent };

            public Percent100(int number) { _percent = unchecked((sbyte)number.Clamp(0, B_100PERCENT)); }

            public Percent100(float value) { _percent = unchecked((sbyte)(value.Clamp01() * B_100PERCENT).RoundToInt()); }

            public int Percent
            {
                get { return Is100 ? B_100PERCENT : _percent; }
                set { _percent = unchecked((sbyte)(value.Clamp(0, B_100PERCENT))); }
            }

            public sbyte InteralPercent
            {
                get { return unchecked((sbyte)(IsInfinitive ? ~_percent : _percent)); }
                set { _percent = unchecked((sbyte)(IsInfinitive ? ~value.ClampToPositive() : value.ClampToPositive())); }
            }

            public float Rate
            {
                get { return Percent * 0.01f; }
                set { Percent = unchecked((sbyte)(value.Clamp01() * B_100PERCENT).RoundToInt()); }
            }

            public float InteralRate
            {
                get { return InteralPercent * 0.01f; }
                set
                {
                    sbyte p = unchecked((sbyte)(value.Clamp01() * B_100PERCENT).RoundToInt());
                    _percent = unchecked((sbyte)(IsInfinitive ? ~p : p));
                }
            }

            public bool IsZero => _percent == 0;
            public bool Is100 => _percent < 0 || _percent >= B_100PERCENT;

            public bool IsInfinitive
            {
                get { return _percent < 0; }
                set { if (IsInfinitive != value) _percent = unchecked((sbyte)~_percent); }
            }

            public Percent100 AsInfinitive
            {
                get
                {
                    var p = new Percent100() { _percent = _percent };
                    p.IsInfinitive = true;
                    return p;
                }
            }

            public static implicit operator Rater(Percent100 p) => (Rater)p.Rate;

            public static float operator *(float l, Percent100 r) => l * r.Rate;
            public static float operator *(int l, Percent100 r) => l * r.Rate;

            public static implicit operator float(Percent100 p) => p.Rate;
            public static explicit operator sbyte(Percent100 p) => p._percent;
            public static explicit operator Percent100(sbyte p) => Raw(p);

            public static explicit operator Percent100(float p)
            {
                if (p <= 0 || p > B_MAX_PERCENT) throw new OverflowException();
                return new Percent100() { Rate = p };
            }

            public static explicit operator int(Percent100 p) => p.Percent;
            public static explicit operator Percent100(int p)
            {
                if (p <= 0 || p > 255) throw new OverflowException();
                return new Percent100() { Percent = p };
            }

            public static Percent100 operator +(Percent100 l, Percent100 r)
            {
                sbyte inner = unchecked((sbyte)(l.InteralPercent + r.InteralPercent).Clamp(0,sbyte.MaxValue));
                if(l.IsInfinitive||r.IsInfinitive) inner=unchecked((sbyte)~inner);
                return Raw(inner);
            }

            public static Percent100 operator &(Percent100 l, Percent100 r)
            {
                sbyte inner = unchecked((sbyte)(l.InteralPercent + r.InteralPercent).Clamp(0, sbyte.MaxValue));
                if (l.IsInfinitive && r.IsInfinitive) inner = unchecked((sbyte)~inner);
                return Raw(inner);
            }

            // public static Percent100 operator *(Percent100 l, Percent100 r)
            //     => new Percent100() { _percent = (sbyte)(l._percent * r._percent * B_1PERCENT_POW2).Clamp(0, B_100PERCENT) };

            // public static Percent100 operator /(Percent100 l, Percent100 r)
            //     => new Percent100() { _percent = unchecked((sbyte)(l._percent / r._percent).Clamp(0, 255)) };

            public override string ToString() => $"{InteralPercent}% Inf[{IsInfinitive}]";

        }
    }
}
