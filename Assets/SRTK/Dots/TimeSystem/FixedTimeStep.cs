/************************************************************************************
| File: FrameCount.cs                                                               |
| Project: SRTK.TimeSystem                                                          |
| Created Date: Thu Feb 27 2020                                                     |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Thu Apr 09 2020                                                    |
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

using Unity.Entities;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using System.Runtime.CompilerServices;

namespace SRTK
{
    using static Unity.Mathematics.math;
    [BurstCompile]
    public struct FixedTimeStep : IComponentData
    {
        public static readonly FixedTimeStep Default = new FixedTimeStep(0.02f);

        public static FixedTimeStep PhysicsStep(float stepPreSecond) => new FixedTimeStep()
        {
            stepPreSec = stepPreSecond <= 0 ? 0 : stepPreSecond,
            aggSteps = 0,
            autoConsume = 1,
            aggStepCap = 0,
        };

        public static FixedTimeStep Timer(float timespan) => new FixedTimeStep()
        {
            stepPreSec = timespan <= 0 ? 0 : (1f / timespan),
            aggSteps = 0,
            autoConsume = 0,
            aggStepCap = 1,
        };

        public static FixedTimeStep Producer(float timePreProduct,int initialProduct=0, int storageCap = 0) => new FixedTimeStep()
        {
            stepPreSec = timePreProduct <= 0 ? 0 : (1f / timePreProduct),
            aggSteps = initialProduct,
            autoConsume = 0,
            aggStepCap = storageCap,
        };

        internal float stepPreSec;
        internal float aggSteps;
        internal float aggStepCap;
        internal int autoConsume;

        public FixedTimeStep(float stepTime, bool autoConsume = true, int stepCap = 0, in int initialSteps = 0)
        {
            stepPreSec = stepTime <= 0 ? 0 : (1f / stepTime);
            this.autoConsume = autoConsume ? 1 : 0;
            aggStepCap = stepCap;
            aggSteps = initialSteps;
        }

        public float StepTime
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => stepPreSec == 0 ? 0 : (1 / stepPreSec);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => stepPreSec = value <= 0 ? 0 : (1f / value);
        }
        public float StepPreSecond
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => stepPreSec;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => stepPreSec = value <= 0 ? 0 : value;
        }

        public int AggSteps
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => stepPreSec == 0 ? 1 : (int)floor(aggSteps);
        }

        public int DeltaStep
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AggSteps;
        }

        public bool IsTimeUp
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => AggSteps >= 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public FixedTimeStep Tick(float deltaTime)
        {
            //clear step from last frame if auto consume is on
            aggSteps = autoConsume != 0 ? frac(aggSteps) : aggSteps;
            //aggregate steptime from this frame
            aggSteps += deltaTime * stepPreSec;
            //clamp aggregated step if aggStepCap is on
            aggSteps = (aggStepCap > 0 && aggSteps > aggStepCap) ? aggStepCap : aggSteps;
            return this;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ConsumeAll()
        {
            float steps = floor(aggSteps);
            aggSteps -= steps;
            return (stepPreSec == 0) ? 1 : (int)steps;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Consume(int count)
        {
            float steps = aggSteps > count ? count : floor(aggSteps);
            aggSteps -= steps;
            return (stepPreSec == 0) ? 1 : (int)steps;
        }
    }
    //public struct TimeStepReference : ISharedComponentData { internal Entity reference; public Entity Reference => reference; }
}

