/************************************************************************************
| File: Range.cs                                                                    |
| Project: lieene.NumberTypes                                                       |
| Created Date: Thu Mar 26 2020                                                     |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Thu Mar 26 2020                                                    |
| Modified By: Lieene Guo                                                           |
| -----                                                                             |
| MIT License                                                                       |
|                                                                                   |
| Copyright (c) 2020 Lieene@ShadeRealm                                              |
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
        public struct Range
        {
            public Range(int start, int count)
            {
                this.start = start;
                this.count = count;
            }

            public int Start
            {
                get => start;
                set => start = value;
            }
            internal int start;

            public int Length
            {
                get => count;
                set
                {
                    start = value < 0 ? start + value : start;
                    count = value < 0 ? -value : value;
                }
            }
            internal int count;

            public int End
            {
                get => start + count;
                set
                {
                    var flip = value < start;
                    count = flip ? start - value : value - start;
                    start = flip ? value : start;
                }
            }
            public static Range operator *(Range r, float scale) => new Range(r.start, (int)(r.count * scale));
            public override string ToString() => $"Range[{start},{End})";
        }
    }
}

