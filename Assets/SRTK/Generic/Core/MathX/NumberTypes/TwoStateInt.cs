using System.Collections.Generic;
/************************************************************************************
| File: TwoStateInt.cs                                                              |
| Project: SRTK.NumberTypes                                                         |
| Created Date: Mon Sep 16 2019                                                     |
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


using System;
namespace SRTK
{
    public static partial class MathX
    {
        public struct TwoStateInt
        {
            public TwoStateInt(int value)
            { _value = value; }

            public TwoStateInt(int value, bool state1)
            {
                value = value.ClampToPositive();
                _value = state1 ? value : ~value;
            }

            public TwoStateInt(uint value, bool state1)
            {
                int v = unchecked((int)value.MaxAt<uint>(int.MaxValue));
                _value = state1 ? v : ~v;
            }
            public bool IsZero => _value == 0 || _value == -1;

            private int _value;

            public int RawValue { get => _value; set => _value = value; }

            public uint ValueUInt
            {
                get => unchecked((uint)(IsState1 ? _value : ~_value));
                set { int v = unchecked((int)value.MaxAt((uint)int.MaxValue)); _value = IsState1 ? v : ~v; }
            }

            public int Value
            {
                get => IsState1 ? _value : ~_value;
                set { value = value.ClampToPositive(); _value = IsState1 ? value : ~value; }
            }


            public bool IsState1 { get => _value >= 0; set { if (value == IsState2) Flip(); } }
            public bool IsState2 { get => _value < 0; set { if (value == IsState1) Flip(); } }
            public void Flip() => _value = ~_value;

            public TwoStateInt Add1Over2(TwoStateInt other) => Combine1Over2(this, other, (l, r) => l + r, (l, r) => l + r);
            public TwoStateInt Add2Over1(TwoStateInt other) => Combine2Over1(this, other, (l, r) => l + r, (l, r) => l + r);
            public TwoStateInt Substract1Over2(TwoStateInt other) => Combine1Over2(this, other, (l, r) => l - r, (l, r) => l - r);
            public TwoStateInt Substract2Over1(TwoStateInt other) => Combine2Over1(this, other, (l, r) => l - r, (l, r) => l - r);

            public static TwoStateInt Combine1Over2(TwoStateInt l, TwoStateInt r, Func<int, int, int> OnStat11, Func<int, int, int> OnStat22)
            {
                if (l.IsState1)
                {
                    if (r.IsState1) return new TwoStateInt(OnStat11(l.Value, r.Value), true);//state 1,1
                    else return l;//state 1,2
                }
                else if (r.IsState1) return r;//state 2,1
                else return new TwoStateInt(~OnStat22(l.Value, r.Value), false);//state 2,2
            }

            public static TwoStateInt Combine2Over1(TwoStateInt l, TwoStateInt r, Func<int, int, int> OnStat11, Func<int, int, int> OnStat22)
            {
                if (l.IsState1)
                {
                    if (r.IsState1) return new TwoStateInt(OnStat11(l.Value, r.Value), true);//state 1,1
                    else return r;//state 1,2
                }
                else if (r.IsState1) return l;//state 2,1
                else return new TwoStateInt(~OnStat22(l.Value, r.Value), false);//state 2,2
            }

            public static TwoStateInt CombineRaw(
                TwoStateInt l, TwoStateInt r,
                Func<int, int, int> OnStat11,
                Func<int, int, int> OnStat12,
                Func<int, int, int> OnStat21,
                Func<int, int, int> OnStat22)
            {
                if (l.IsState1)
                {
                    if (r.IsState1) return new TwoStateInt(OnStat11(l._value, r._value)); //state 1,1
                    else return new TwoStateInt(OnStat12(l._value, r._value)); ; //state 1,2
                }
                else if (r.IsState1) return new TwoStateInt(OnStat21(l._value, r._value)); //state 2,1
                else return new TwoStateInt(OnStat22(l._value, r._value)); //state 2,2
            }

            public static TwoStateInt operator ~(TwoStateInt v) => new TwoStateInt(~v._value);
            public static TwoStateInt operator +(TwoStateInt l, TwoStateInt r) => new TwoStateInt(l.ValueUInt + r.ValueUInt, l.IsState1);
            public static TwoStateInt operator -(TwoStateInt l, TwoStateInt r) => new TwoStateInt(l.ValueUInt - r.ValueUInt, l.IsState1);
            public static TwoStateInt operator *(TwoStateInt v, float m) => new TwoStateInt((int)(v.ValueUInt * m));
            public static TwoStateInt operator /(TwoStateInt v, float m) => new TwoStateInt((int)(v.ValueUInt / m));

            public static bool operator ==(TwoStateInt l, TwoStateInt r) => l._value == r._value;
            public static bool operator !=(TwoStateInt l, TwoStateInt r) => l._value != r._value;

            public static bool operator ==(TwoStateInt l, int r) => l.Value == r;
            public static bool operator !=(TwoStateInt l, int r) => l.Value != r;

            public static bool operator ==(TwoStateInt l, uint r) => l.ValueUInt == r;
            public static bool operator !=(TwoStateInt l, uint r) => l.ValueUInt != r;

            public static explicit operator int(TwoStateInt v) => unchecked((int)v.ValueUInt);
            public static explicit operator uint(TwoStateInt v) => v.ValueUInt;
            public static explicit operator TwoStateInt(int v) => new TwoStateInt(v, true);
            public static explicit operator TwoStateInt(uint v) => new TwoStateInt(v, true);

            public override bool Equals(object obj) => obj is TwoStateInt s && this == s;
            public override int GetHashCode() => this.CombineTypeHash(_value);

            public override string ToString() => $"{ValueUInt}:State{(IsState1 ? 1.ToString() : 2.ToString())}";
        }
    }
}