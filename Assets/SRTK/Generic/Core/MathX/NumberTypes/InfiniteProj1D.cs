/************************************************************************************
| File: InfiniteProj1D.cs                                                           |
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


using System;

namespace SRTK
{
    public static partial class MathX
    {
        public struct MagnitudeValue 
        {
            public float Value;
            public float Magnitude
            {
                get { return (float)-Math.Log10(Value); }
                set { Value = (float)Math.Pow(10, -value); }
            }

            public static implicit operator float(MagnitudeValue m) => m.Value;
            public static implicit operator MagnitudeValue(float m) => new MagnitudeValue{Value=m};
        }
        
        public struct InfiniteProj1D<T> 
        {
            public static MagnitudeValue SteepnessMagnitude = 0.01f;
            private float _value_Np1;
            private int _lvl_NpInf;

            public InfiniteProj1D(int level)
            {
                _lvl_NpInf = level;
                if (_lvl_NpInf == int.MaxValue) _value_Np1 = 1f;
                else if (_lvl_NpInf == int.MinValue) _value_Np1 = -1f;
                else _value_Np1 = SmoothProjection(_lvl_NpInf, SteepnessMagnitude.Value);
            }
            public InfiniteProj1D(int level, int midPointMagnitude)
            {
                _lvl_NpInf = level;
                if (_lvl_NpInf == int.MaxValue) _value_Np1 = 1f;
                else if (_lvl_NpInf == int.MinValue) _value_Np1 = -1f;
                else _value_Np1 = SmoothProjection(_lvl_NpInf, SteepnessMagnitude.Value);
            }

            public float Fract => _value_Np1;
            public int Level
            {
                get { return _lvl_NpInf; }
                set
                {
                    _lvl_NpInf = value;
                    if (_lvl_NpInf == int.MaxValue) _value_Np1 = 1f;
                    else if (_lvl_NpInf == int.MinValue) _value_Np1 = -1f;
                    else _value_Np1 = SmoothProjection(_lvl_NpInf, SteepnessMagnitude.Value);
                }
            }
            public static InfiniteProj1D<T> operator +(InfiniteProj1D<T> a, InfiniteProj1D<T> b) => new InfiniteProj1D<T>(a._lvl_NpInf + b._lvl_NpInf);
            public static InfiniteProj1D<T> operator +(InfiniteProj1D<T> a, int b) => new InfiniteProj1D<T>(a._lvl_NpInf + b);
            public static InfiniteProj1D<T> operator -(InfiniteProj1D<T> a, InfiniteProj1D<T> b) => new InfiniteProj1D<T>(a._lvl_NpInf - b._lvl_NpInf);
            public static InfiniteProj1D<T> operator -(InfiniteProj1D<T> a, int b) => new InfiniteProj1D<T>(a._lvl_NpInf - b);

            public static InfiniteProj1D<T> operator *(InfiniteProj1D<T> a, int b) => new InfiniteProj1D<T>(a._lvl_NpInf * b);

        }
        public struct InfProj1D 
        {
            public static MagnitudeValue Steepness = 0.01f;
            private float _frac;
            private int _lvl;

            public InfProj1D(int level)
            {
                Steepness = 0.01f;
                _lvl = level;
                if (_lvl == int.MaxValue) _frac = 1f;
                else if (_lvl == int.MinValue) _frac = -1f;
                else _frac = SmoothProjection(_lvl, Steepness);
            }
            public InfProj1D(int level, int midPointMagnitude)
            {
                Steepness = (float)Math.Pow(10, -midPointMagnitude);
                _lvl = level;
                if (_lvl == int.MaxValue) _frac = 1f;
                else if (_lvl == int.MinValue) _frac = -1f;
                else _frac = SmoothProjection(_lvl, Steepness);
            }

            public float Fract => _frac;
            public int Level
            {
                get { return _lvl; }
                set
                {
                    _lvl = value;
                    if (_lvl == int.MaxValue) _frac = 1f;
                    else if (_lvl == int.MinValue) _frac = -1f;
                    else _frac = SmoothProjection(_lvl, Steepness);
                }
            }
            public static InfProj1D operator +(InfProj1D a, InfProj1D b) => new InfProj1D(a._lvl + b._lvl);
            public static InfProj1D operator +(InfProj1D a, int b) => new InfProj1D(a._lvl + b);
            public static InfProj1D operator -(InfProj1D a, InfProj1D b) => new InfProj1D(a._lvl - b._lvl);
            public static InfProj1D operator -(InfProj1D a, int b) => new InfProj1D(a._lvl - b);

            public static InfProj1D operator *(InfProj1D a, int b) => new InfProj1D(a._lvl * b);

        }
    }
}