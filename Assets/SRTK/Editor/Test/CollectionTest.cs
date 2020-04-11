using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using SRTK;
// using SRTK.WarHead;
using Unity.Collections;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Entities;
using System.Runtime.InteropServices;
using Unity.Burst;

namespace Tests
{
    using static EventDataTypes;
    public class CollectionTest
    {
        [Test]
        public void ListAsArrayBug()
        {
            Unity.Mathematics.Random rand = new Unity.Mathematics.Random((uint)System.Math.Abs(System.DateTime.Now.Millisecond));
            //Debug.Log("No Inserted OP");
            //ListAsArrayBug(rand, 0,Allocator.Temp);
            //Debug.Log("Inserted Native List OP");
            ListAsArrayBug(rand, 1, Allocator.TempJob);
            // Debug.Log("Inserted Native Array OP");
            // ListAsArrayBug(rand, 2,Allocator.Temp);
        }

        void ListAsArrayBug(Unity.Mathematics.Random rand, int InsertedOpType, Allocator allocator)
        {
            //make NativeList and Use it AsArray
            var list = new NativeList<int>(allocator);
            var size = rand.NextInt(10, 50);
            for (int i = 0; i < size; i++) list.Add(rand.NextInt(11));
            var asArray = list.AsArray();

            //Print the AsArray
            var sb = new System.Text.StringBuilder("Initial As Array|");
            for (int i = 0; i < size; i++) sb.Append($"{asArray[i]}|");
            Debug.Log(sb.ToString());
            sb.Clear();

            //Insert some operation
            int otherSize;
            NativeList<int> otherList = default;
            NativeArray<int> otherArray = default;
            switch (InsertedOpType)
            {
                case 1:
                    otherSize = rand.NextInt(1, 50);
                    otherList = new NativeList<int>(4, allocator);
                    for (int i = 0; i < otherSize; i++) otherList.Add(rand.NextInt(11));
                    break;

                case 2:
                    otherSize = rand.NextInt(1, 50);
                    otherArray = new NativeArray<int>(otherSize, allocator);
                    for (int i = 0; i < otherSize; i++) otherArray[i] = (rand.NextInt(11));
                    break;
            }

            //Try Access the AsArray again after interted OP
            try
            {
                sb.Append("AsArray After Inserted OP|");
                for (int i = 0; i < size; i++) sb.Append($"{asArray[i]}|");
                Debug.Log(sb.ToString());
            }
            catch (System.Exception e)
            {
                Debug.Log("Array Access Failed:" + e.ToString());
            }
            sb.Clear();

            //Print original list
            sb.Append("the List After Inserted OP|");
            for (int i = 0; i < size; i++) sb.Append($"{list[i]}|");
            Debug.Log(sb.ToString());
            sb.Clear();

            //Print original list as a new AsArray
            sb.Append("New AsArray After Inserted OP|");
            asArray = list.AsArray();
            for (int i = 0; i < size; i++) sb.Append($"{asArray[i]}|");
            Debug.Log(sb.ToString());
            sb.Clear();

            //Print original list pre element via AsArray... just because I'm mad with this bug...
            sb.Append("via AsArray After Inserted OP|");
            for (int i = 0; i < size; i++) sb.Append($"{list.AsArray()[i]}|");
            Debug.Log(sb.ToString());

            if (list.IsCreated) list.Dispose();
            if (otherList.IsCreated) otherList.Dispose();
            if (otherArray.IsCreated) otherList.Dispose();
        }

        struct IntPairCompare : IComparer<int2> { public int Compare(int2 x, int2 y) => x.x - y.x; }
        // A Test behaves as an ordinary method
        [Test]
        public void GroupTest()
        {
            Unity.Mathematics.Random rand = new Unity.Mathematics.Random((uint)System.Math.Abs(System.DateTime.Now.Millisecond));
            for (int x = 0; x < 100; x++)
            {
                var size = rand.NextInt(0, 50);
                var list = new NativeList<int2>(size, Allocator.TempJob);
                var allValues = new System.Collections.Generic.HashSet<int2>();
                for (int i = 0; i < size; i++)
                {
                    var next = new int2(rand.NextInt(11), i);
                    list.Add(next);
                    allValues.Add(next);
                }
                var sb = new System.Text.StringBuilder("Before Group|");
                for (int i = 0; i < size; i++)
                {
                    sb.Append(list[i]);
                    sb.Append('|');
                }
                var ranges = list.GroupRange(new IntPairCompare(), Allocator.Temp);
                var groupCount = ranges.Length;
                sb.Append("\nAfter Group|");
                for (int i = 0; i < size; i++)
                {
                    sb.Append(list[i]);
                    sb.Append('|');
                }
                Debug.Log(sb.ToString());

                if (size > 0)
                {
                    var prev = list[0].x;
                    var equalSpanOffsets = new System.Collections.Generic.List<int>();
                    var uniqueValues = new System.Collections.Generic.HashSet<int>();
                    uniqueValues.Add(prev);
                    equalSpanOffsets.Add(0);
                    for (int i = 1; i < size; i++)
                    {
                        if (prev != list[i].x)
                        {
                            equalSpanOffsets.Add(i);
                            prev = list[i].x;
                        }
                        uniqueValues.Add(list[i].x);
                    }
                    Assert.IsTrue(equalSpanOffsets.Count == groupCount, $"Count Missmatch group:{groupCount} equal span checker:{equalSpanOffsets.Count}");
                    Assert.IsTrue(uniqueValues.Count == groupCount, $"Count Missmatch group:{groupCount} unique value checker:{uniqueValues.Count}");
                    for (int i = 1; i < groupCount; i++)
                    {
                        Assert.IsTrue(equalSpanOffsets[i] == ranges[i].Start, $"range:{i} start Missmatch group:{ranges[i].Start} equal span checker:{equalSpanOffsets[i]}");
                        var iNext = i + 1;
                        var equalSpanLen = (iNext == groupCount ? size - equalSpanOffsets[i] : equalSpanOffsets[iNext] - equalSpanOffsets[i]);
                        Assert.IsTrue(equalSpanLen == ranges[i].Length, $"range:{i} Length Missmatch group:{ranges[i].End} equal span checker:{equalSpanLen}");
                    }
                    foreach (var v in allValues) Assert.IsTrue(list.AsArray().IndexOf(v) >= 0);

                }
                else Assert.IsTrue(groupCount == 0);
                list.Dispose();
            }
        }


        struct OddData
        {
            public byte Data0;
            public byte Data1;
            public byte Data2;
        }

        struct Data11
        {
            public Data11(ulong a, ushort b, byte c)
            {
                this.Data0_8 = a;
                this.Data8_10 = b;
                this.Data10_11 = c;
            }
            public ulong Data0_8;//0:8
            public ushort Data8_10;//8:10
            public byte Data10_11;//10:11
        }

        [StructLayout(LayoutKind.Explicit, Size = 11)]
        struct Data11_Explicit
        {
            [FieldOffset(0)] public ulong Data0_8;
            [FieldOffset(8)] public ushort Data8_10;
            [FieldOffset(10)] public byte Data10_11;
        }

        struct SomeEventData
        {
            public SourceTargetPair st;
            public int Damage;
            public ulong LargeData0;
            public ulong LargeData1;
            public ulong LargeData2;
            public ulong LargeData3;
        }

        [Test]
        public void EventStreamerTest()
        {
            Debug.Log($"sizeof {nameof(Data11)} is {UnsafeUtility.SizeOf<Data11>()}");
            Debug.Log($"sizeof {nameof(Data11_Explicit)} is {UnsafeUtility.SizeOf<Data11_Explicit>()}");
            Debug.Log($"sizeof {nameof(OddData)} is {UnsafeUtility.SizeOf<OddData>()}");

            var registry = new EventDataTypes.EventTypeRegistry(Allocator.TempJob);
            var tid = registry.NextUndefinedTypeID();
            Assert.AreEqual(0, tid);
            tid = registry.NextUndefinedTypeID();
            Assert.AreEqual(0, tid);
            var info = registry.RegisterEventType(tid)
                .RegisterNextDataType<float>()
                .RegisterNextDataType<float>()
                .RegisterNextDataType<float>()
                .RegisterNextDataType<float>()
                .RegisterNextDataType<int>()
                .RegisterNextDataType<int>()
                .RegisterNextDataType<int>()
                .RegisterNextDataType<int>();


            var streamer = new EventStreamer(Allocator.TempJob);

            //Write events---------------------------------------------------------------------------------------------------------------------------
            var w = streamer.AsWriter();
            var evtHdrIn = new EventHeader(0);
            evtHdrIn.SetLocalDataAt<float>(0, 1.5f);
            evtHdrIn.SetLocalDataAt<float>(4, 2.5f);
            evtHdrIn.SetLocalDataAt<float>(8, 3.5f);
            evtHdrIn.SetLocalDataAt<float>(12, 4.5f);

            var handle = w.BeginBatch();
            //event 0
            handle.WriteHeader(evtHdrIn, 16).WriteExternalData<int>(3).WriteExternalData<int>(4).WriteExternalData<int>(5).WriteExternalData<int>(6);


            var buffer = handle.CreateEventBuffer();
            //event 1
            buffer.NewEvent(1)
                .AddData<int>(0).AddData<int>(1).AddData<int>(2).AddData<int>(3).AddData<int>(4).AddData<int>(5)
                .AddData<int>(6).AddData<int>(7).AddData<int>(8).AddData<int>(9).AddData<int>(10).AddData<int>(11)
                .Write();

            //event 2
            buffer.NewEvent(2)
                .AddData<OddData>(new OddData() { Data0 = 11, Data1 = 24, Data2 = 3 })
                .AddData<OddData>(new OddData() { Data0 = 12, Data1 = 25, Data2 = 2 })
                .AddData<OddData>(new OddData() { Data0 = 13, Data1 = 26, Data2 = 1 })
                .Write();

            //event 3
            buffer.NewEvent(3).AddData<int>(0).Write();

            //event 4
            buffer.NewEvent(4).AddData<int>(-1).StartExternalData().AddData<int>(1).AddData<int>(2).AddData<int>(3).Write();

            //event 5
            buffer.NewEvent(5).AddData<SomeEventData>
            (
                new SomeEventData()
                {
                    st = new SourceTargetPair()
                    {
                        Source = new Entity() { Index = 10, Version = 11 },
                        Target = new Entity() { Index = 20, Version = 21 },
                    },
                    Damage = 99,
                    LargeData0 = 50,
                    LargeData1 = 50,
                    LargeData2 = 50,
                    LargeData3 = 50,
                }
            ).Write();


            buffer.Dispose();
            //event 6,7
            handle.WriteEvent(6, 10);
            handle.WriteEvent(7, (ulong)32767, (ulong)65535);

            handle.EndBatch();

            //Read events---------------------------------------------------------------------------------------------------------------------------


            streamer.CollectElementsToRead(out var batch2Read, out var elementCount, Allocator.TempJob).Complete();
            Debug.Log($"Stream has {elementCount.Value} Items in {batch2Read.Length} batches, start Reading");

            var r = streamer.AsReader();
            var eventsOut = new NativeArray<GenericEvent>(elementCount.Value, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            int totalExternalSize = 0;
            for (int b = 0, lenB = batch2Read.Length; b < lenB; b++)
            {
                var rh = r.BeginBatch(batch2Read[b]);
                for (int i = 0, lenE = rh.ElementCount; i < lenE; i++)
                {
                    var e = rh.ReadEvent();
                    totalExternalSize += e.SizeInfo.ExternalDataByteSize;
                    eventsOut[i] = e;
                }
                rh.EndBatch();
            }
            elementCount.Dispose();
            batch2Read.Dispose();

            Debug.Log($"Stream has {streamer.CalculateEventCount()} Items in {streamer.CalculateBlockCount()} Blocks, after Reading");


            //Move Data from stream to buffer --------------------------------------------------------------------------------------------------------

            var externalCache = new UnsafeAppendBuffer(totalExternalSize, Unity.Jobs.LowLevel.Unsafe.JobsUtility.CacheLineSize, Allocator.TempJob);
            var checkSum = 0;
            for (int i = 0, len = eventsOut.Length; i < len; i++)
            {
                var e = eventsOut[i];
                checkSum += e.MoveExternalDataTo(ref externalCache);
                eventsOut[i] = e;
            }
            Assert.AreEqual(totalExternalSize, checkSum);

            //Release stream  memory ---------------------------------------
            streamer.Dispose();

            int intData = default;
            float floatData = default;
            OddData oddData = default;

            var evtOut = eventsOut[0];
            LogEventInfo(evtOut, 0);
            Assert.AreEqual(0, evtOut.TypeID);

            Debug.Log((floatData = info.GetData<float>(0, evtOut)).ToString());
            Assert.AreEqual(1.5f, floatData);
            Debug.Log((floatData = info.GetData<float>(1, evtOut)).ToString());
            Assert.AreEqual(2.5f, floatData);
            Debug.Log((floatData = info.GetData<float>(2, evtOut)).ToString());
            Assert.AreEqual(3.5f, floatData);
            Debug.Log((floatData = info.GetData<float>(3, evtOut)).ToString());
            Assert.AreEqual(4.5f, floatData);

            Debug.Log((intData = info.GetData<int>(4, evtOut)).ToString());
            Assert.AreEqual(3, intData);
            Debug.Log((intData = info.GetData<int>(5, evtOut)).ToString());
            Assert.AreEqual(4, intData);
            Debug.Log((intData = info.GetData<int>(6, evtOut)).ToString());
            Assert.AreEqual(5, intData);
            Debug.Log((intData = info.GetData<int>(7, evtOut)).ToString());
            Assert.AreEqual(6, intData);

            evtOut = eventsOut[1];
            LogEventInfo(evtOut, 1);
            Assert.AreEqual(1, evtOut.TypeID);
            for (int i = 0; i < 12; i++)
            {
                Debug.Log((intData = evtOut.GetDataAt<int>(i * 4)).ToString());
                Assert.AreEqual(i, intData);
            }
            Debug.Log((intData = evtOut.LocalDataAt<int>(12)).ToString());
            Assert.AreEqual(3, intData);
            Debug.Log((intData = evtOut.ExternalDataAt<int>(0)).ToString());
            Assert.AreEqual(4, intData);

            evtOut = eventsOut[2];
            LogEventInfo(evtOut, 2);
            Assert.AreEqual(2, evtOut.TypeID);
            var oddSize = UnsafeUtility.SizeOf<OddData>();
            for (int i = 0; i < 3; i++)
            {
                oddData = evtOut.GetDataAt<OddData>(i * oddSize);
                Debug.Log(oddData.Data0.ToString());
            }
            // Debug.Log((oddData = evtOut.LocalDataAt<OddData>(0)).Data0.ToString());
            // Debug.Log((oddData = evtOut.ExternalDataAt<OddData>(0)).Data0.ToString());

            evtOut = eventsOut[3];
            LogEventInfo(evtOut, 3);
            Assert.AreEqual(3, evtOut.TypeID);

            evtOut = eventsOut[4];
            LogEventInfo(evtOut, 4);
            Assert.AreEqual(4, evtOut.TypeID);
            Debug.Log((intData = evtOut.LocalDataAt<int>(0)).ToString());
            Assert.AreEqual(-1, intData);
            Debug.Log((intData = evtOut.ExternalDataAt<int>(0)).ToString());
            Assert.AreEqual(1, intData);
            Debug.Log((intData = evtOut.ExternalDataAt<int>(4)).ToString());
            Assert.AreEqual(2, intData);
            Debug.Log((intData = evtOut.ExternalDataAt<int>(8)).ToString());
            Assert.AreEqual(3, intData);

            evtOut = eventsOut[5];
            LogEventInfo(evtOut, 5);
            var sd = evtOut.ExternalDataAs<SomeEventData>();

            Debug.Log($"some data: source[{sd.st.Source.Entity.Index}:{sd.st.Source.Entity.Version}] target[{sd.st.Target.Entity.Index}:{sd.st.Target.Entity.Version}] Damage{sd.Damage}");
            Debug.Log($"some data: LargeData0[{sd.LargeData0}] LargeData1[{sd.LargeData1}] LargeData2[{sd.LargeData2}] LargeData3[{sd.LargeData3}]");

            evtOut = eventsOut[6];
            LogEventInfo(evtOut, 6);
            Debug.Log($"Local:{evtOut.LocalDataAs<int>()}");
            Assert.AreEqual(10, evtOut.LocalDataAs<int>());


            evtOut = eventsOut[7];
            LogEventInfo(evtOut, 7);
            Debug.Log($"Local:{evtOut.LocalDataAs<ulong>()} External:{evtOut.ExternalDataAs<ulong>()}");
            Assert.AreEqual(32767, evtOut.LocalDataAs<ulong>());
            Assert.AreEqual(65535, evtOut.ExternalDataAs<ulong>());

            // wh.WriteEvent(6, 10);
            // wh.WriteEvent(7, (ulong)32767, (ulong)65535);


            if (streamer.IsCreated) streamer.Dispose();
            if (eventsOut.IsCreated) eventsOut.Dispose();
            if (externalCache.IsCreated) externalCache.Dispose();
            if (registry.IsCreated) registry.Dispose();
        }
        void LogEventInfo(GenericEvent evt, int index) => Debug.Log($"Event:{index} TypeID:{evt.TypeID} Size[L:{evt.SizeInfo.LocalDataByteSize},E:{evt.SizeInfo.ExternalDataByteSize}, H:{evt.SizeInfo.LocalPackageByteSize},P:{evt.SizeInfo.PackageByteSize},A:{evt.SizeInfo.AlignedPackageByteSize}]");


        [Test]
        public void EventTypeRegistryTest()
        {
            var registry = new EventDataTypes.EventTypeRegistry(Allocator.TempJob);

            Debug.Log($"Size of EventDataSegment Type : {UnsafeUtility.SizeOf<EventDataTypes.EventDataSegmentInfo>()}");
            Debug.Log($"Registering TypeID:{2}");
            Debug.Log($"Adding int type to TypeID:{2}");

            registry.RegisterEventType(2).RegisterNextDataType<int>().RegisterNextDataType<ulong>();

            var infoBack = registry.GetTypeInfo(2);
            Debug.Log($"Offsets of TypeID:2 is {infoBack.GetOffset<int>(0)}:{infoBack.GetOffset<ulong>(1)}:{infoBack.GetUnsafeOffset(2)}");

            Assert.AreEqual(0, infoBack.GetOffset<int>(0));
            Assert.AreEqual(4, infoBack.GetOffset<ulong>(1));
            Assert.AreEqual(12, infoBack.GetUnsafeOffset(2));
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => infoBack.GetOffset<int>(2));
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => infoBack.GetUnsafeOffset(3));

            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => registry.GetTypeInfo(0));
            Assert.Throws<UnityEngine.Assertions.AssertionException>(() => registry.GetTypeInfo(1));
        }

        //UnsafeParallelChainBuffer

        [Test]
        public void UnsafeParallelChainBufferTest()
        {
            UnsafeParallelBuffer buffer = new UnsafeParallelBuffer(Allocator.TempJob);

            Debug.Log(buffer);
            JobHandle dependsOn = default;

            #region BigDataTest

            dependsOn = new TestBigDataWriterJob
            {
                JobWriter = buffer.AsParallelWriter(),
            }.Schedule(2, 1);

            dependsOn = buffer.CollectBatchIDToRead(out var cwd, Allocator.TempJob, dependsOn);

            dependsOn = new TestBigDataReaderJob
            {
                ChainIDWithData = cwd,
                JobReader = buffer.AsParallelReader(),
            }.Schedule(1, 1, dependsOn);

            cwd.Dispose(dependsOn);

            #endregion

            #region SumDataTest
            UnsafeParallelBuffer buffer1 = new UnsafeParallelBuffer(Allocator.TempJob);

            dependsOn = new TestSumDataWriterJob
            {
                JobWriter = buffer1.AsParallelWriter()
            }.Schedule(2, 1);

            dependsOn = buffer1.CollectBatchIDToRead(out var cwd1, Allocator.TempJob, dependsOn);

            dependsOn = new TestSumDataReaderJob
            {
                ChainIDWithData = cwd1,
                JobReader = buffer1.AsParallelReader(),
            }.Schedule(1, 1, dependsOn);
            cwd1.Dispose(dependsOn);

            #endregion

            dependsOn = buffer1.Dispose(dependsOn);
            dependsOn = buffer.Dispose(dependsOn);
            dependsOn.Complete();


            #region mainTest
            UnsafeParallelBuffer buffer2 = new UnsafeParallelBuffer(Allocator.TempJob);
            var w = buffer2.AsParallelWriter();
            w.BeginBatch();
            for (int i = 0; i < 64; i++)
            { w.Write<int>(i); }
            w.EndBatch();


            Debug.Log(buffer2);

            int intValue = 0;

            var r = buffer2.AsParallelReader();
            JobHandle dep = default;

            dep = buffer2.CollectBatchIDToRead(out var chainIDs, Allocator.TempJob, dep);

            var count = r.BeginBatch(0);

            for (int i = 0; i < count; i++)
            {
                intValue = r.Read<int>();
                Debug.Log($"data[{i}] is {intValue}");
                Assert.AreEqual(i, intValue);
            }
            r.EndBatch();

            Debug.Log(buffer2);

            buffer2.Dispose(dep);
            chainIDs.Dispose(dep);
            JobHandle.ScheduleBatchedJobs();
            #endregion


        }

        [BurstCompile]
        struct TestBigDataWriterJob : IJobParallelFor
        {
            public UnsafeParallelBuffer.ParallelWriter JobWriter;

            public void Execute(int index)
            {
                JobWriter.BeginBatch();
                for (int i = 0; i < 16; i++)
                {
                    var data = new BigDataTest();
                    unsafe { data.BigLong[0] = (byte)i; }
                    JobWriter.Write(data);
                }
                JobWriter.EndBatch();


            }
        }
        [BurstCompile]
        struct TestSumDataWriterJob : IJobParallelFor
        {
            public UnsafeParallelBuffer.ParallelWriter JobWriter;
            public void Execute(int index)
            {
                JobWriter.BeginBatch();
                for (int i = 0; i < 1024; i++)
                {
                    var t = new Hpp { hp = i, dd = 3.141592653 };
                    JobWriter.Write(t);
                }
                JobWriter.EndBatch();

            }
        }



        [BurstCompile]
        struct TestBigDataReaderJob : IJobParallelFor
        {
            public UnsafeParallelBuffer.ParallelReader JobReader;
            [ReadOnly] public NativeList<int> ChainIDWithData;
            public void Execute(int index)
            {
                foreach (var t1 in ChainIDWithData)
                {
                    var count = JobReader.BeginBatch(t1);
                    for (int j = 0; j < count; j++)
                    {
                        var t = JobReader.Read<BigDataTest>();
                        unsafe
                        {
                            JobLogger.Log(t.BigLong[0]);
                            Assert.AreEqual(j, t.BigLong[0]);
                        }

                    }
                    JobReader.EndBatch();
                }
            }
        }

        struct TestSumDataReaderJob : IJobParallelFor
        {
            public UnsafeParallelBuffer.ParallelReader JobReader;
            [ReadOnly] public NativeList<int> ChainIDWithData;
            public void Execute(int index)
            {
                foreach (var t1 in ChainIDWithData)
                {
                    var count = JobReader.BeginBatch(t1);
                    for (int j = 0; j < count; j++)
                    {
                        var t = JobReader.Read<Hpp>();
                        JobLogger.Log("t.hp= ", t.hp, "t.dd= ", t.dd);
                        Assert.AreEqual(j, t.hp);
                    }
                    JobReader.EndBatch();
                }
            }
        }

    }

    public struct Hpp
    {
        public int hp;
        public bool bb;
        public double dd;
    }

    public unsafe struct BigDataTest
    {
        public fixed byte BigLong[1008];
    }


}
