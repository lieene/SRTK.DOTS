/************************************************************************************
| File: LocalTimeScale.cs                                                           |
| Project: lieene.TimeSystem                                                        |
| Created Date: Wed Mar 11 2020                                                     |
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

using System;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace SRTK
{

    [BurstCompile]
    [Serializable]
    [WriteGroup(typeof(TimeScale))]
    public struct LocalTimeScale : IComponentData
    {
        public static readonly LocalTimeScale Default = new LocalTimeScale(1);
        public LocalTimeScale(in float LocalTimeScale = 1) { value = LocalTimeScale; }
        public float value;
        public DeltaTime Scale(float dt) => new DeltaTime(dt * value);
        public bool KeepTimeScale => value < 0;
        public LocalTimeScale RecordParentChange(bool keepLocal2WorldTimeScale)
        {
            value = keepLocal2WorldTimeScale?-value:value;
            return this;
        }
        public static implicit operator float(LocalTimeScale from) => from.value;
        public static implicit operator LocalTimeScale(float from) => new LocalTimeScale(from);

        public static implicit operator LocalTimeScale(TimeScale from) => new LocalTimeScale(from.value);
    }
}
