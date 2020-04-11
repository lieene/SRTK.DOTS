/************************************************************************************
| File: Rater.cs                                                                    |
| Project: SRTK.NumberTypes                                                         |
| Created Date: Wed Sep 4 2019                                                      |
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
    public partial class MathX
    {
        public struct Rater 
        {
            private float _rate;

            public bool IsZero => _rate == 0;

            public Rater(float rate)
            { _rate = rate.MinAt(-1); }

            public float Scale
            {
                get { return 1 + _rate; }
                set { _rate = (value.ClampToPositive() - 1); }
            }

            public float Rate
            {
                get { return _rate; }
                set { _rate = (value.MinAt(-1)); }
            }
            public static float operator *(float a, Rater b) => a * b.Scale;

            public static Rater operator +(Rater a, Rater b) => new Rater(a._rate + b._rate);
            public static Rater operator -(Rater a, Rater b) => new Rater(a._rate - b._rate);
            public static Rater operator *(Rater a, Rater b) => new Rater() { Scale = a.Scale * b.Scale };
            public static Rater operator /(Rater a, Rater b) => new Rater() { Scale = a.Scale / b.Scale };

            public static implicit operator float(Rater rate) => rate._rate;
            public static explicit operator Scaler(Rater rate) => new Scaler(rate.Scale);
            public static explicit operator Rater(float rate) => new Rater(rate);

            public override string ToString() => $"Rater[{_rate}]";


        }
    }
}
