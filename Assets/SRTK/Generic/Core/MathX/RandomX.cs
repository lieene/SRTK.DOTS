/************************************************************************************
| File: RandomX.cs                                                                  |
| Project: SRTK.MathX                                                               |
| Created Date: Wed Sep 4 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Mon Sep 23 2019                                                    |
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
    public class RandomX
    {
        static System.Random rand = new System.Random(System.DateTime.Now.Millisecond);

        public static int PositiveInt => rand.Next();
        public static float Sample01 => (float)rand.NextDouble();

        public static int Integer(int from, int to) => rand.Next(from,to);
        public static int Range(int from, int to) => rand.Next(from, to);
        public static float Range(float from, float to) => ((float)rand.NextDouble()).Lerp(from, to);
        public static double Range(double from, double to) => rand.NextDouble().Lerp(from, to);

        // public int this[int a, int b]
        // {
        //     get
        //     {

        //     }
        // }
    }
}