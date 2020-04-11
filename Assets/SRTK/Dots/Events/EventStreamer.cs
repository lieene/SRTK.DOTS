/************************************************************************************
| File: EventStreamer.cs                                                            |
| Project: lieene.Utility                                                           |
| Created Date: Thu Apr 2 2020                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Fri Apr 10 2020                                                    |
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
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;


namespace SRTK
{

    public struct EventWriter
    {
        internal UnsafeParallelBuffer.ParallelWriter mParallelWriter;

        unsafe public BatchHandle BeginBatch()
        {
            fixed (UnsafeParallelBuffer.ParallelWriter* pWriter = &mParallelWriter)
            {
                pWriter->BeginBatch();
                return new BatchHandle() { pWriter = pWriter };
            }
        }

        unsafe public struct BatchHandle
        {
            [NativeDisableUnsafePtrRestriction] internal UnsafeParallelBuffer.ParallelWriter* pWriter;
            public ref UnsafeParallelBuffer.ParallelWriter mWriter => ref *pWriter;
            //public void EndBatch() => pWriter->EndBatch();
            public void EndBatch() => pWriter->EndBatch();
        }
    }

    public struct EventStreamer : IDisposable
    {

        //TODO: Use UnsafeParallelChainBuffer
        //      batchCounter not needed any more

        //internal NativeMaxCounter batchCounter;
        internal UnsafeParallelBuffer mParallelBuffer;
        internal Allocator mAllocator;
        internal int mWriteRequestCount;

        // internal static EventStreamer Delayed(Allocator allocator) => new EventStreamer()
        // {
        //     ParallelBuffer = default,
        //     mAllocator = allocator,
        //     WriteAccessCount = 0,
        // };

        public EventStreamer(Allocator allocator)
        {
            mParallelBuffer = new UnsafeParallelBuffer(allocator);
            mAllocator = allocator;
            mWriteRequestCount = 0;
        }

        public EventWriter AsWriter()
        {
            mWriteRequestCount++;
            Assert.IsTrue(mParallelBuffer.IsCreated, "internal stream not allocated");
            return new EventWriter() { mParallelWriter = mParallelBuffer.AsParallelWriter() };
        }

        public EventReader AsReader()
        {
            Assert.IsTrue(mParallelBuffer.IsCreated, "internal stream not allocated");
            return new EventReader() { mParallelReader = mParallelBuffer.AsParallelReader() };
        }

        public struct EventReader
        {
            internal UnsafeParallelBuffer.ParallelReader mParallelReader;

            unsafe public BatchHandle BeginBatch(int batchIndex)
            {
                fixed (UnsafeParallelBuffer.ParallelReader* pReader = &mParallelReader)
                {
                    var count = pReader->BeginBatch(batchIndex);
                    return new BatchHandle() { pReader = pReader, ElementCount = count };
                }
            }

            unsafe public struct BatchHandle
            {
                [NativeDisableUnsafePtrRestriction] internal UnsafeParallelBuffer.ParallelReader* pReader;
                public ref UnsafeParallelBuffer.ParallelReader mReader => ref *pReader;
                public int ElementCount;
                //public void EndBatch() => pReader->EndBatch();
                public void EndBatch() => pReader->EndBatch();
            }

        }

        public bool IsCreated => mParallelBuffer.IsCreated;
        public JobHandle CollectElementsToRead(out NativeList<int> batch2Read, out NativeCounter elementCount, Allocator allocator, JobHandle dependsOn = default)
             => mParallelBuffer.CollectElementsToRead(out batch2Read, out elementCount, allocator, dependsOn);

        public int CalculateEventCount() => mParallelBuffer.CalculateElementCount();
        
        public int CalculateBlockCount() => mParallelBuffer.CalculateBlockCount();

        public JobHandle CollectBatchIDToRead(out NativeList<int> batch2Read, Allocator allocator, JobHandle dependsOn = default)
        => mParallelBuffer.CollectBatchIDToRead(out batch2Read, allocator, dependsOn);


        public bool IsDirty { get => mWriteRequestCount > 0; }
        public int NewWriteRequests => mWriteRequestCount;
        public void ClearDirty() => mWriteRequestCount = 0;

        public void Dispose()
        {
            if (mParallelBuffer.IsCreated) { mParallelBuffer.Dispose(); }
            mParallelBuffer = default;
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (mParallelBuffer.IsCreated) { dependsOn = mParallelBuffer.Dispose(dependsOn); }
            mParallelBuffer = default;
            return dependsOn;
        }
    }
}