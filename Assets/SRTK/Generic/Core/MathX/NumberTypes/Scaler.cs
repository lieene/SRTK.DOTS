/************************************************************************************
| File: TwoStateInt.cs                                                              |
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



namespace SRTK
{
    public partial class MathX
    {
        public struct Scaler
        {
            private float _scale;

            public bool IsZero => _scale == 0;

            public Scaler(float scale)
            { _scale = scale; }

            public float Scale
            {
                get { return _scale; }
                set { _scale = value; }
            }

            public float Rate
            {
                get { return _scale - 1; }
                set { _scale = value + 1; }
            }

            public static float operator *(float a, Scaler b) => a * b.Scale;

            public static Scaler operator +(Scaler a, Scaler b) => new Scaler() { Rate = a.Rate + b.Rate };
            public static Scaler operator -(Scaler a, Scaler b) => new Scaler() { Rate = a.Rate - b.Rate };
            public static Scaler operator *(Scaler a, Scaler b) => new Scaler(a._scale * b._scale);
            public static Scaler operator /(Scaler a, Scaler b) => new Scaler(a._scale / b._scale);

            public static implicit operator float(Scaler scale) => scale._scale;
            public static explicit operator Rater(Scaler scale) => new Rater(scale.Rate);
            public static explicit operator Scaler(float scale) => new Scaler(scale);

            public override string ToString() => $"Scaler[{_scale}]";

        }
    }
}
