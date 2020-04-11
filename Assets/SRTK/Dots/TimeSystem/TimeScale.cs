/************************************************************************************
| File: TimeScale.cs                                                                |
| Project: SRTK.TimeSystem                                                          |
| Created Date: Thu Feb 27 2020                                                     |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Sat Mar 21 2020                                                    |
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

using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SRTK
{
    [BurstCompile]
    [Serializable]
    public struct TimeScale : IComponentData
    {
        public static readonly TimeScale Default = new TimeScale(1);
        public TimeScale(in float timeScale = 1, in bool KeepTimeScale = false)
        {
            value = timeScale;
            keepTimeScaleOnParentChange = KeepTimeScale ? 1 : 0;
        }
        public float value;
        public int keepTimeScaleOnParentChange;
        public bool KeepTimeScaleOnParentChange
        {
            get {  return keepTimeScaleOnParentChange == 1; }
            set { keepTimeScaleOnParentChange = value ? 1 : 0; }
        }
        public DeltaTime Scale(float dt) => new DeltaTime(dt * value);
        public bool IsRevers => value < 0;
        public static implicit operator float(TimeScale from) => from.value;
        public static implicit operator TimeScale(float from) => new TimeScale(from);

        public static explicit operator TimeScale(LocalTimeScale from) => new TimeScale(from.value);
    }
}

