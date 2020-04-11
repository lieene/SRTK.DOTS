/************************************************************************************
| File: Normalize.cs                                                                |
| Project: SRTK.MathX                                                               |
| Created Date: Mon Sep 16 2019                                                     |
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

using System;
using System.Runtime.CompilerServices;

//TODO:CS73:2019 add burst compile support

namespace SRTK
{
    public static partial class MathX
    {
        /// <summary>
        /// Map Level:[0,nfinitive) to Fraction [0,1)
        /// Plot with google: arctan(0.01*x)/(pi/2)
        /// </summary>
        /// <param name="level">Integer level</param>
        /// <param name="steepness">smaller magnitude steepness give level more resolution</param>
        /// <returns>Fraction [0,1)</returns>
        public static float SmoothProjection(int level, float steepness = 0.01f) => (float)Math.Atan(level * steepness) * HalfPi_INV;
        
        /// <summary>
        /// Inverse function of SmoothFraction01
        /// </summary>
        /// <param name="fraction"></param>
        /// <param name="steepness"></param>
        /// <returns></returns>
        public static int SmoothFraction_Inv(float fraction, float steepness = 0.01f) => (int)(Math.Tan(fraction * HalfPi) / steepness);
    }
}