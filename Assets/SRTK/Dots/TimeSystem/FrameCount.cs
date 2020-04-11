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
using Unity.Burst;
using Unity.Mathematics;

namespace SRTK
{
    using static Unity.Mathematics.math;
    [BurstCompile]
    [System.Serializable]
    public struct Counter
    {
        public Counter(in int initCount = 0)
        {
            count = initCount;
            allowInversFrame = false;
#if SRTK_INSTANCE_RUN_FOR_MONTHS
            //this handles integer overflow, should alway be zero normally
            //instance run in 60 fps for 1 year reaches: 1,892,160,000
            //        which is still under int.MaxValue: 2,147,483,647
            cycle = 0;
#endif
        }
        internal int count;
        public int Count
        {
            get => count;
            set => count = allowInversFrame ? value : max(0, value);
        }
        internal bool allowInversFrame;

#if SRTK_INSTANCE_RUN_FOR_MONTHS
        //this handles integer overflow, should alway be zero in a day
        internal short cycle;
        public short Cycle => cycle;
#endif
        public Counter Tick(in int count = 1)
        {
#if SRTK_INSTANCE_RUN_FOR_MONTHS
            long next = frame + (allowInversFrame ? count : max(0, count));
            //handles integer overflow
            while (next > int.MaxValue)
            {
                next -= int.MaxValue;
                cycle++;
            }
            //handles negative integer overflow
            while (allowInversFrame && next < int.MinValue)
            {
                next -= int.MaxValue;
                cycle--;
            }
            frame = unchecked((int)next);
#endif
            this.count += (allowInversFrame ? count : max(0, count));
            return this;
        }
        public static implicit operator int(Counter from) => from.count;

    }
    [BurstCompile]
    [System.Serializable]
    public struct FrameCounter : IComponentData
    {
        public static readonly FrameCounter Zero = new FrameCounter(0);
        public FrameCounter(in int initCount = 0) { counter = new Counter(initCount); }
        internal Counter counter;
        public int Count
        {
            get => counter.count;
            set => counter.Count = value;
        }
        internal bool allowInversFrame
        {
            get => counter.allowInversFrame;
            set => counter.allowInversFrame = value;
        }

        public FrameCounter Tick(in int count = 1) { counter.Tick(count); return this; }
        public static implicit operator int(FrameCounter from) => from.counter;
    }


    //public struct FrameCountReference : ISharedComponentData { internal Entity reference; public Entity Reference => reference; }
}

