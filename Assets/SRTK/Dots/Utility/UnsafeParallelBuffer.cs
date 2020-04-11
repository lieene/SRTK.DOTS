/************************************************************************************
| File: UnsafeParallelBuffer.cs                                                     |
| Project: lieene.Utility                                                           |
| Created Date: Mon Apr 6 2020                                                      |
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
| Date      	By	Comments                                                          |
| ----------	---	----------------------------------------------------------        |
************************************************************************************/

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine.Assertions;
using Unity.Entities;

namespace SRTK
{
    //[NativeContainer]
    public unsafe struct UnsafeParallelBuffer : IDisposable
    {
        public const int BlockChainCount = JobsUtility.MaxJobThreadCount;
        public const int BlockSize = 1024;//1KB - 8kb
        public const int BlockHeaderSize = 16;
        public const int MaxBlockDataSize = BlockSize - BlockHeaderSize;
        internal Allocator mAllocator;
        [NativeDisableUnsafePtrRestriction] internal ThreadBlockChain* BlockChains;

        public UnsafeParallelBuffer(Allocator allocator)
        {
            this.mAllocator = allocator;
            var allocSize = UnsafeUtility.SizeOf<ThreadBlockChain>() * BlockChainCount;
            BlockChains = (ThreadBlockChain*)UnsafeUtility.Malloc(allocSize, JobsUtility.CacheLineSize, allocator);
            UnsafeUtility.MemClear(BlockChains, allocSize);
        }
        public bool IsCreated => BlockChains != null;

        public void Dispose()
        {
            if (mAllocator.ShouldDeallocate())
            {
                for (int i = 0; i < BlockChainCount; i++)
                {
                    var chain = (BlockChains + i);
                    var block = chain->mFirstBlock;
                    while (block != null)
                    {
                        var next = block->mNextBlock;
                        UnsafeUtility.Free(block, mAllocator);
                        Assert.IsFalse(next == chain->mFirstBlock, "block loop!");
                        block = next;
                    }
                }
                UnsafeUtility.Free(BlockChains, mAllocator);
            }
            mAllocator = Allocator.Invalid;
            BlockChains = null;
        }

        public JobHandle Dispose(JobHandle dependsOn)
        {
            dependsOn = new DisposeBlocksJob()
            {
                BlockChains = BlockChains,
                allocator = mAllocator
            }.Schedule(BlockChainCount, 4, dependsOn);

            dependsOn = new DisposePointerJob()
            {
                BlockChains = BlockChains,
                allocator = mAllocator
            }.Schedule(dependsOn);
            mAllocator = Allocator.Invalid;
            BlockChains = null;
            return dependsOn;
        }
        
        struct DisposeBlocksJob : IJobParallelFor
        {
            [NativeDisableUnsafePtrRestriction] public ThreadBlockChain* BlockChains;
            public Allocator allocator;
            public void Execute(int index)
            {
                var chain = (BlockChains + index);
                var block = chain->mFirstBlock;
                while (block != null)
                {
                    var next = block->mNextBlock;
                    UnsafeUtility.Free(block, allocator);
                    block = next;
                    Assert.IsFalse(next == chain->mFirstBlock, "block loop!");
                }
            }
        }
       
        struct DisposePointerJob : IJob
        {
            [NativeDisableUnsafePtrRestriction] public ThreadBlockChain* BlockChains;
            public Allocator allocator;
            public void Execute() { UnsafeUtility.Free(BlockChains, allocator); }
        }

        [StructLayout(LayoutKind.Explicit, Size = BlockSize)]
        public struct Block
        {
            [FieldOffset(0)] internal Block* mNextBlock;
            [FieldOffset(8)] internal int mDataByteLen;
            [FieldOffset(12)] internal int mElementCount;
            [FieldOffset(BlockHeaderSize)] internal fixed byte mData[MaxBlockDataSize];

            public static Block* Allocate(Allocator allocator)
            {
                Block* pBlock = (Block*)UnsafeUtility.Malloc(BlockSize, 32, allocator);
                UnsafeUtility.MemClear(pBlock, BlockSize);
                return pBlock;
            }

            internal Block(Allocator allocator)
            {
                mDataByteLen = 0;
                mElementCount = 0;
                mNextBlock = null;
            }

            internal void Reset()
            {
                mDataByteLen = 0;
                mElementCount = 0;
                mNextBlock = null;
            }

            public bool TryAdd<T>(T data) where T : unmanaged
            {
                var size = UnsafeUtility.SizeOf<T>();
                Assert.IsTrue(size <= MaxBlockDataSize, $"Data type larger than MaxBlockDataSize can not fit in any block! Size={size},MaxBlockDataSize={MaxBlockDataSize}");
                if (mDataByteLen + size <= MaxBlockDataSize)
                {
                    fixed (byte* pData = mData) *(T*)(pData + mDataByteLen) = data;
                    mDataByteLen += size;
                    mElementCount++;
                    return true;
                }
                else return false;
            }

            public bool TryAllocate(int size, out byte* pOffset)
            {
                Assert.IsTrue(size <= MaxBlockDataSize, "Data type larger than MaxBlockDataSize can not fit in any block");
                if (mDataByteLen + size <= MaxBlockDataSize)
                {
                    fixed (byte* pData = mData) pOffset = (pData + mDataByteLen);
                    mDataByteLen += size;
                    mElementCount++;
                    return true;
                }
                else
                {
                    pOffset = null;
                    return false;
                }
            }

            public BlockReader AsReader()
            {
                fixed (Block* pThis = &this)
                {
                    return new BlockReader()
                    {
                        mBlock = pThis,
                        readOffset = 0,
                    };
                }
            }

            public struct BlockReader
            {
                [NativeDisableUnsafePtrRestriction] internal Block* mBlock;
                internal int readOffset;

                public bool TryRead<T>(out T data) where T : unmanaged
                {
                    var size = UnsafeUtility.SizeOf<T>();
                    Assert.IsTrue(size <= MaxBlockDataSize, "Data type larger than MaxBlockDataSize can not fit in any block");
                    if (readOffset + size <= mBlock->mDataByteLen)
                    {
                        data = *(T*)(mBlock->mData + readOffset);
                        readOffset += size;
                        return true;
                    }
                    else
                    {
                        data = default;
                        return false;
                    }
                }

                public bool TryPeek<T>(out T peek) where T : unmanaged
                {
                    var size = UnsafeUtility.SizeOf<T>();
                    Assert.IsTrue(size <= MaxBlockDataSize, "Data type larger than MaxBlockDataSize can not fit in any block");
                    if (readOffset + size <= mBlock->mDataByteLen)
                    {
                        peek = *(T*)(mBlock->mData + readOffset);
                        return true;
                    }
                    else
                    {
                        peek = default;
                        return false;
                    }
                }

                public bool TryReadUnsafePtr(int size, out byte* ptr)
                {
                    Assert.IsTrue(size <= MaxBlockDataSize, "Data type larger than MaxBlockDataSize can not fit in any block");
                    if (readOffset + size <= mBlock->mDataByteLen)
                    {
                        ptr = mBlock->mData + readOffset;
                        readOffset += size;
                        return true;
                    }
                    else
                    {
                        ptr = null;
                        return false;
                    }
                }
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 32)]
        internal struct ThreadBlockChain
        {
            [FieldOffset(0)] public int mBlockUsed;//with data in it
            [FieldOffset(4)] public int mBlockAllocated;//with or with our data
            [FieldOffset(8)] public Block* mCurrentWriteBlock;
            [FieldOffset(16)] public Block* mFirstBlock;
            [FieldOffset(24)] public Block* mLastBlock;

            public int CalculateElementCount()
            {
                int elemCount = 0;
                var curBlk = mFirstBlock;
                while (curBlk != null)
                {
                    var count = curBlk->mElementCount;
                    if (count > 0) elemCount += count;
                    else return elemCount;//not data in some block means no more data at all
                    curBlk = curBlk->mNextBlock;
                    Assert.IsFalse(curBlk == mFirstBlock, "block loop!");
                }
                return elemCount;
            }

            public bool HasDataElement()
            {
                var curBlk = mFirstBlock;
                while (curBlk != null)
                {
                    if (curBlk->mElementCount > 0) return true;
                    curBlk = curBlk->mNextBlock;
                    Assert.IsFalse(curBlk == mFirstBlock, "block loop!");

                }
                return false;
            }

            public Block* GetWritingBlock(Allocator allocator)
            {
                if (mCurrentWriteBlock == null)
                {
                    if (mFirstBlock == null || mLastBlock == null)
                    {//fresh start
                        Assert.IsTrue(mFirstBlock == null || mLastBlock == null, "First/Last block has one missing while while other allocate!");
                        mCurrentWriteBlock = mFirstBlock = mLastBlock = Block.Allocate(allocator);
                        mBlockAllocated++;
                    }
                    else
                    {
                        var curBlk = mFirstBlock;
                        while (curBlk != null)
                        {
                            var nextBlk = curBlk->mNextBlock;
                            if (nextBlk->mElementCount == 0)
                            {
                                mCurrentWriteBlock = curBlk;
                                return mCurrentWriteBlock;
                            }
                            Assert.IsFalse(nextBlk == mFirstBlock, "block loop!");
                            curBlk = nextBlk;
                        }
                        if (curBlk == null)//all block in use
                        {
                            mLastBlock->mNextBlock = curBlk = Block.Allocate(allocator);
                            mCurrentWriteBlock = mLastBlock = curBlk;
                            mBlockAllocated++;
                        }
                    }
                }
                return mCurrentWriteBlock;
            }
        }

        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter()
            {
                ThreadID = 0,
                currentBlock = null,
                allocator = mAllocator,
                BlockChains = BlockChains,
                currentChain = null,
            };
        }
        public struct ParallelWriter
        {
            [NativeSetThreadIndex] internal int ThreadID;
            [NativeDisableUnsafePtrRestriction] internal ThreadBlockChain* BlockChains;
            [NativeDisableUnsafePtrRestriction] internal ThreadBlockChain* currentChain;
            [NativeDisableUnsafePtrRestriction] internal Block* currentBlock;
            internal Allocator allocator;

            public void BeginBatch()
            {
                Assert.IsTrue(currentChain == null, "Can not begin new batch write, when batch writing is started, Call EndBatch");
                Assert.IsTrue(ThreadID <= BlockChainCount, "Thread ID must be smaller than BlockChainCount");
                currentChain = BlockChains + ThreadID;
                currentBlock = currentChain->GetWritingBlock(allocator);
            }

            void StartNextBlock()
            {
                currentChain->mBlockUsed++;
                if (currentBlock->mNextBlock == null)
                {
                    currentBlock->mNextBlock = Block.Allocate(allocator);
                    currentChain->mBlockAllocated++;
                }
                currentBlock = currentBlock->mNextBlock;
            }

            public void Write<T>(T data) where T : unmanaged
            {
                Assert.IsTrue(currentChain != null, "Can not write before calling BeginBatch, target batch is not defined");
                if (!currentBlock->TryAdd(data))
                {
                    StartNextBlock();
                    currentBlock->TryAdd(data);
                }
            }

            public byte* Allocate(int size)
            {
                Assert.IsTrue(currentChain != null, "Can not write before calling BeginBatch, target batch is not defined");
                byte* pData;
                if (!currentBlock->TryAllocate(size, out pData))
                {
                    StartNextBlock();
                    currentBlock->TryAllocate(size, out pData);
                }
                return pData;
            }

            public void EndBatch()
            {
                Assert.IsTrue(currentChain != null, "Writing not started by calling BeginBatch, target batch is not defined");
                if (currentBlock->mDataByteLen > 0) currentChain->mBlockUsed++;
                //currentChain->mLastBlock = currentBlock;
                currentChain = null;
                currentBlock = null;
            }
        }

        public int CalculateElementCount()
        {
            int elemCount = 0;
            for (int i = 0; i < BlockChainCount; i++) { elemCount += (BlockChains + i)->CalculateElementCount(); }
            return elemCount;
        }

        public int CalculateBlockCount()
        {
            int blkCount = 0;
            for (int i = 0; i < BlockChainCount; i++)
            {
                var blockChain = BlockChains + i;
                blkCount += (BlockChains + i)->mBlockUsed;
            }
            return blkCount;
        }

        public JobHandle CollectBatchIDToRead(out NativeList<int> batch2Read, Allocator allocator, JobHandle dependsOn = default)
        {
            batch2Read = new NativeList<int>(BlockChainCount, allocator);
            return new CollectThreadIDToReadJob()
            {
                BlockChains = BlockChains,
                ThreadIDs = batch2Read.AsParallelWriter()
            }.ScheduleBatch(BlockChainCount, 4, dependsOn);
        }

        internal struct CollectThreadIDToReadJob : IJobParallelForBatch
        {
            [NativeDisableUnsafePtrRestriction] public ThreadBlockChain* BlockChains;
            public NativeList<int>.ParallelWriter ThreadIDs;
            public void Execute(int startIndex, int count)
            {
                Assert.IsTrue((startIndex + count) <= BlockChainCount, "Chain ID must be smaller than BlockChainCount");
                for (int i = 0; i < count; i++)
                {
                    var ChainID = startIndex + i;
                    var blockChain = BlockChains + ChainID;
                    bool hasData = false;
                    var curBlk = blockChain->mFirstBlock;
                    while (curBlk != null)
                    {
                        if (curBlk->mElementCount > 0)
                        {
                            hasData = true;
                            break;
                        }
                        curBlk = curBlk->mNextBlock;
                        Assert.IsFalse(curBlk == blockChain->mFirstBlock, "block loop!");
                    }
                    if (hasData) ThreadIDs.AddNoResize(ChainID);
                }
            }
        }

        public JobHandle CollectElementsToRead(out NativeList<int> batch2Read, out NativeCounter elementCount, Allocator allocator, JobHandle dependsOn = default)
        {
            batch2Read = new NativeList<int>(BlockChainCount, allocator);
            elementCount = new NativeCounter(allocator);
            return new CollectElementsToReadJob()
            {
                BlockChains = BlockChains,
                BatchIDs = batch2Read.AsParallelWriter(),
                ElementCounter = elementCount,
            }.ScheduleBatch(BlockChainCount, 4, dependsOn);
        }
        internal struct CollectElementsToReadJob : IJobParallelForBatch
        {
            [NativeDisableUnsafePtrRestriction] public ThreadBlockChain* BlockChains;
            public NativeList<int>.ParallelWriter BatchIDs;
            public NativeCounter ElementCounter;
            public void Execute(int startIndex, int count)
            {
                Assert.IsTrue((startIndex + count) <= BlockChainCount, "Chain ID must be smaller than BlockChainCount");
                for (int i = 0; i < count; i++)
                {
                    var ThreadID = startIndex + i;
                    var blockChain = BlockChains + ThreadID;
                    bool hasData = false;
                    var curBlk = blockChain->mFirstBlock;
                    while (curBlk != null)
                    {
                        if (curBlk->mElementCount > 0)
                        {
                            ElementCounter.Add(curBlk->mElementCount);
                            hasData = true;
                        }
                        curBlk = curBlk->mNextBlock;
                        Assert.IsFalse(curBlk == blockChain->mFirstBlock, "block loop!");
                    }
                    if (hasData) BatchIDs.AddNoResize(ThreadID);
                }
            }
        }

        public ParallelReader AsParallelReader()
        {
            return new ParallelReader()
            {
                currentBlock = default,
                BlockChains = BlockChains,
                currentChain = null,
            };
        }
        public struct ParallelReader
        {
            [NativeDisableUnsafePtrRestriction] internal Block.BlockReader currentBlock;
            [NativeDisableUnsafePtrRestriction] internal ThreadBlockChain* BlockChains;
            [NativeDisableUnsafePtrRestriction] internal ThreadBlockChain* currentChain;
            public int BeginBatch(int batchID)
            {
                Assert.IsTrue(currentChain == null, "Can not begin new batch read, when batch reading is started, Call EndBatch");
                Assert.IsTrue(batchID < BlockChainCount, "Thread ID must be smaller than BlockChainCount");
                currentChain = BlockChains + batchID;
                if (currentChain->mFirstBlock == null || currentChain->mBlockUsed <= 0) return 0;//no data here at all
                else
                {
                    Assert.IsTrue(currentChain->mFirstBlock != null && currentChain->mLastBlock != null, "Missing blocks, there should be at lest one block when there are data");
                    currentBlock = currentChain->mFirstBlock->AsReader();
                    return currentChain->CalculateElementCount();
                }
            }

            void StartNextBlock()
            {
                var nextBlock = currentBlock.mBlock->mNextBlock;//cache next block
                currentBlock.mBlock->Reset();//break the chain
                if (currentChain->mLastBlock != currentBlock.mBlock) currentChain->mLastBlock->mNextBlock = currentBlock.mBlock;//put currnet block to linklist end, so the memory can be reused;

                Assert.IsTrue(nextBlock != null, "No more Blocks to read! stick to the number returned by BeginBatch");//should not happend as we have loop cache
                Assert.IsTrue(nextBlock->mElementCount > 0, "No more elemnet to read! stick to the number returned by BeginBatch");//to avoid loop back, no data means end of current read
                currentChain->mFirstBlock = nextBlock;
                currentChain->mBlockUsed--;
                currentBlock = nextBlock->AsReader();

            }

            public T Read<T>() where T : unmanaged
            {
                Assert.IsTrue(currentChain != null, "Can not read before calling BeginBatch, target batch is not defined");
                Assert.IsTrue(currentBlock.mBlock != null, "Current block is null");
                T data;
                if (currentBlock.TryRead<T>(out data)) return data;
                else
                {
                    StartNextBlock();
                    var success = currentBlock.TryRead<T>(out data);
                    Assert.IsTrue(success, "No more data to read! stick to the number returned by BeginBatch");
                    return data;
                }
            }

            public T Peek<T>() where T : unmanaged
            {
                Assert.IsTrue(currentChain != null, "Can not read before calling BeginBatch, target batch is not defined");
                Assert.IsTrue(currentBlock.mBlock != null, "Current block is null");
                T data;
                if (currentBlock.TryPeek<T>(out data)) return data;
                else
                {
                    StartNextBlock();
                    var success = currentBlock.TryPeek<T>(out data);
                    Assert.IsTrue(success, "No more data to read! stick to the number returned by BeginBatch");
                    return data;
                }
            }

            public byte* ReadUnsafePtr(int size)
            {
                Assert.IsTrue(currentChain != null, "Can not read before calling BeginBatch, target batch is not defined");
                Assert.IsTrue(currentBlock.mBlock != null, "Current block is null");
                byte* pData;
                if (currentBlock.TryReadUnsafePtr(size, out pData)) return pData;
                else
                {
                    StartNextBlock();
                    var success = currentBlock.TryReadUnsafePtr(size, out pData);
                    Assert.IsTrue(success, "No more data to read! stick to the number returned by BeginBatch");
                    return pData;
                }
            }

            public void EndBatch()
            {
                Assert.IsTrue(currentChain != null, "Reading not start by calling BeginBatch, target batch is not defined");
                if (currentBlock.mBlock == null) return;
                //Assert.IsTrue(currentBlock.mBlock != null, "Current block is null");
                if (currentBlock.readOffset > 0)
                {
                    if (currentBlock.readOffset < currentBlock.mBlock->mDataByteLen)
                    {//drop unread block data
                        JobLogger.LogWarning("There are still ", (currentBlock.mBlock->mDataByteLen - currentBlock.readOffset), " bytes of remaining data in current block left unread, these data will be lost");
                    }
                    var nextBlock = currentBlock.mBlock->mNextBlock;//cache next block
                    currentBlock.mBlock->Reset();//break the chain
                    if (currentChain->mLastBlock != currentBlock.mBlock) currentChain->mLastBlock->mNextBlock = currentBlock.mBlock;//put currnet block to linklist end, so the memory can be reused;

                    currentChain->mFirstBlock = nextBlock == null ? currentChain->mLastBlock : nextBlock;
                    currentChain->mBlockUsed--;
                }
                //else no read ever started on this block, no need to change currentChain

                if (currentChain->mBlockUsed > 0)
                {
                    JobLogger.LogWarning("There are still ", currentChain->mBlockUsed, " block left unread, block data will be kept until the next read!");
                }
                currentChain = null;
                currentBlock = default;
            }
        }

        public override string ToString()
        {
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
            for (int j = 0; j < BlockChainCount; j++)
            {
                var pC = BlockChains + j;
                if (pC->mBlockAllocated > 0)
                {
                    stringBuilder.Append($"Chain[{j}:{HexString(pC)}] [A:{pC->mBlockAllocated} | U:{pC->mBlockUsed} | W:{HexString(pC->mCurrentWriteBlock)} | 1st:{HexString(pC->mFirstBlock)} | last:{HexString(pC->mLastBlock)}]\n");
                    int index = 0;
                    var curBlk = pC->mFirstBlock;
                    while (curBlk != null)
                    {
                        // [FieldOffset(0)] internal Block* mNextBlock;
                        // [FieldOffset(8)] internal int DataByteLen;
                        // [FieldOffset(12)] internal int mElementCount;
                        // [FieldOffset(BlockHeaderSize)] fixed byte mData[MaxBlockDataSize];
                        stringBuilder.Append($"Block[{HexString(index)}] [N:{HexString(curBlk->mNextBlock)} | L:{HexString(curBlk->mDataByteLen)} | C:{HexString(curBlk->mElementCount)}]");
                        for (int i = 0; i < curBlk->mDataByteLen; i++)
                        {
                            if (i % 4 == 0) stringBuilder.Append("\n####|");
                            if (i % 4 != 0) stringBuilder.Append("_");
                            stringBuilder.Append(HexString(curBlk->mData[i]));
                        }
                        index++;
                        curBlk = curBlk->mNextBlock;
                        if (curBlk == pC->mFirstBlock)
                        {
                            stringBuilder.Append("\n!!!!!Block Loop!!!!!!");
                            break;
                        }
                    }
                    stringBuilder.Append("\n==============\n");
                }
            }
            return stringBuilder.ToString();
        }
        static string HexString(int value) => "0x" + value.ToString("X2");
        static string HexString(void* ptr) => "0x" + new IntPtr(ptr).ToInt64().ToString("X16");
    }
}