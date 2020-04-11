/************************************************************************************
| File: EventTypes.cs                                                               |
| Project: lieene.Events                                                            |
| Created Date: Sun Apr 5 2020                                                      |
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

using System;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace SRTK
{
    using static EventStreamer;

    public static class EventDataTypes
    {
        //-------------------------------------------------------------------------------------------------------------------
        #region Generic Event types when passed to stream
        [StructLayout(LayoutKind.Explicit, Size = EventDataSize.ByteSize)]
        public struct EventDataSize
        {
            public static EventDataSize Empty => new EventDataSize() { LocalDataByteSize = 0, ExternalDataByteSize = 0 };
            public const int ByteSize = 4;
            public const int Alignment = 8;
            public const int MaxLocalDataByteSize = 24;
            public const int MaxLocalDataByteSizeWithExternalDataPtr = MaxLocalDataByteSize - 8;
            public const int MaxExternalDataByteSize = ushort.MaxValue;
            public const int MaxPackageByteSize = EventHeader.ByteSize + MaxExternalDataByteSize;

            internal const int LocalSizeOffset = 0;
            internal const int ExternalSizeOffset = 2;

            [FieldOffset(LocalSizeOffset)] public byte LocalDataByteSize;
            //[FieldOffset(LocalSizeOffset + 1)] internal byte CustomFlags;
            [FieldOffset(ExternalSizeOffset)] public ushort ExternalDataByteSize;
            public int LocalPackageByteSize => EventHeader.LocalDataOffset + LocalDataByteSize;
            public int PackageByteSize => EventHeader.LocalDataOffset + LocalDataByteSize + ExternalDataByteSize;
            public int AlignedPackageByteSize => PackageByteSize.Align(Alignment);
        }

        /// <summary>
        /// Event header for writing event data,External Data should follow this header
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = EventHeader.ByteSize)]
        public struct EventHeader
        {
            public EventHeader(int typeID)
            {
                this.TypeID = typeID;
                this.SizeInfo = EventDataSize.Empty;
            }
            public const int LocalDataOffset = EventDataSize.ByteSize + 4;
            public const int ByteSize = EventDataSize.MaxLocalDataByteSize + LocalDataOffset;
            [FieldOffset(0)] public EventDataSize SizeInfo;
            [FieldOffset(4)] public int TypeID;
            [FieldOffset(LocalDataOffset)] unsafe public fixed byte LocalData[EventDataSize.MaxLocalDataByteSize];//when only local data is used

            unsafe public void SetLocalDataAt<T>(int byteOffset, T data) where T : unmanaged
            {
                var sizeT = UnsafeUtility.SizeOf<T>();
                var eod = byteOffset + sizeT;
                Assert.IsTrue(eod <= EventDataSize.MaxLocalDataByteSize, "Can not Set data beyond Local Data range");
                fixed (byte* plocal = LocalData) { *(T*)(plocal + byteOffset) = data; }
                SizeInfo.LocalDataByteSize = SizeInfo.LocalDataByteSize >= eod ? SizeInfo.LocalDataByteSize : (byte)eod;
            }

            unsafe public T GetLocalDataAt<T>(int byteOffset) where T : unmanaged
            {
                var sizeT = UnsafeUtility.SizeOf<T>();
                Assert.IsTrue((byteOffset + sizeT) <= EventDataSize.MaxLocalDataByteSize, "Can not Get data beyond Local Data range");
                fixed (byte* plocal = LocalData) { return *(T*)(plocal + byteOffset); }
            }
        }

        public struct EventWriterBuffer : IDisposable
        {
            const int MaxLocalEndLocalOnly = EventHeader.ByteSize;
            const int MaxLocalEndWithExternal = GenericEvent.ExtPointerOffset;

            public static EventWriterBuffer Empty => new EventWriterBuffer()
            {
                ExternalDataOffset = EventHeader.LocalDataOffset,
                Buffer = default,
                writer = default,
            };

            int ExternalDataOffset;
            UnsafeAppendBuffer Buffer;

            internal EventWriter.BatchHandle writer;

            /// <summary>
            /// Reset buffer and start new event
            /// </summary>
            public EventWriterBuffer NewEvent(int typeID, Allocator allocator = Allocator.TempJob)
            {
                if (!Buffer.IsCreated) Buffer = new UnsafeAppendBuffer(EventHeader.ByteSize, JobsUtility.CacheLineSize, allocator);
                else Buffer.Reset();
                ExternalDataOffset = EventHeader.LocalDataOffset;
                Buffer.Add(new EventDataSize() { LocalDataByteSize = 0, ExternalDataByteSize = 0 });//temp total size
                Buffer.Add(typeID);
                return this;
            }

            /// <summary>
            /// Add next data to current event
            /// </summary>
            public EventWriterBuffer AddData<T>(T data) where T : unmanaged
            {
                Assert.IsTrue(Buffer.IsCreated, "Buffer not initialized");
                var dataSize = UnsafeUtility.SizeOf<T>();
                var NextBufferEnd = Buffer.Length + dataSize;
                Assert.IsTrue(NextBufferEnd <= EventDataSize.MaxPackageByteSize, "Can not add data beyond scheduled size");
                //                   IF(   No Locked        AND      has space for external pointer )    THEN (forward ) ELSE (    stay      )
                ExternalDataOffset = ((ExternalDataOffset > 0) & (NextBufferEnd <= MaxLocalEndWithExternal)) ? NextBufferEnd : ExternalDataOffset;
                Buffer.Add(data);
                return this;
            }

            public EventWriterBuffer StartExternalData()
            {
                Assert.IsTrue(Buffer.IsCreated, "Buffer not initialized");
                ExternalDataOffset = ~ExternalDataOffset;//Lock ExternalDataOffset
                return this;
            }

            public EventWriterBuffer WithWriter(EventWriter.BatchHandle writer)
            {
                this.writer = writer;
                return this;
            }

            public EventWriterBuffer Write()
            {
                Assert.IsTrue(Buffer.IsCreated, "Buffer not initialized");
                unsafe
                {
                    ExternalDataOffset = ExternalDataOffset < 0 ? ~ExternalDataOffset : ExternalDataOffset;//clear lock if needed
                    int localDataSize = ExternalDataOffset - EventHeader.LocalDataOffset;
                    int externalDataSize = Buffer.Length - ExternalDataOffset;
                    Assert.IsTrue(externalDataSize <= EventDataSize.MaxExternalDataByteSize, "External data size larget than scheduled size");
                    //update data byte size
                    *(ushort*)(Buffer.Ptr + EventDataSize.LocalSizeOffset) = (ushort)localDataSize;
                    *(ushort*)(Buffer.Ptr + EventDataSize.ExternalSizeOffset) = (ushort)externalDataSize;
                    //write aligned package to stream
                    var sPtr = writer.mWriter.Allocate(Buffer.Length.Align(EventDataSize.Alignment));
                    UnsafeUtility.MemCpy(sPtr, Buffer.Ptr, Buffer.Length);
                }
                return this;
            }

            public void Dispose()
            {
                if (Buffer.IsCreated) Buffer.Dispose();
                Buffer = default;
            }
        }

        /// <summary>
        /// Event data read from stream, GenericEvent has fixed size.
        /// External Data are store in external memory location reference by pointer
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = GenericEvent.ByteSize)]
        unsafe public struct GenericEvent
        {
            public const int ByteSize = EventHeader.ByteSize;
            public const int ExtPointerOffset = EventHeader.ByteSize - 8;
            [FieldOffset(0)] public EventDataSize SizeInfo;
            [FieldOffset(4)] public int TypeID;

            [FieldOffset(EventHeader.LocalDataOffset)] internal fixed byte LocalData[EventDataSize.MaxLocalDataByteSize];//in-out

            [NativeDisableUnsafePtrRestriction]
            [FieldOffset(ExtPointerOffset)] byte* externalDataPtr;//out

            public static GenericEvent ReadEvent(ref UnsafeParallelBuffer.ParallelReader reader)
            {
                GenericEvent evt = default;
                //peek size info
                var eventSize =  reader.Peek<EventDataSize>();
                var bytePtr = reader.ReadUnsafePtr(eventSize.AlignedPackageByteSize);
                var LocalPkgSize = eventSize.LocalPackageByteSize;
                UnsafeUtility.MemCpy(&evt, bytePtr, LocalPkgSize);
                evt.externalDataPtr = (eventSize.ExternalDataByteSize > 0) ? (bytePtr + LocalPkgSize) : evt.externalDataPtr;
                //we don't copy it now because
                //1. we need to know the size of all events in one frame to allocate a continua memory block
                //2. And other reason is we might want to re-order events before copy data to new buffer so we will have a sequential layout in memory
                //so this copy to from stream work is delayed
                return evt;
            }

            public bool HasExternalData => SizeInfo.ExternalDataByteSize > 0;

            //space for externalDataPtr can be used by local data when there a not external data, HasExternalData must be tested
            public byte* ExternalDataPtr => HasExternalData ? externalDataPtr : null;

            /// <summary>
            /// Copy data in external data range to new location, and point to this new location
            /// </summary>
            /// <param name="destination">new location</param>
            /// <param name="originalPtr">old location returned</param>
            /// <returns>size of data copied</returns>
            unsafe public int MoveExternalDataTo(void* destination, out void* originalPtr)
            {
                originalPtr = ExternalDataPtr;
                if (originalPtr == null) return 0;
                UnsafeUtility.MemCpy(destination, originalPtr, SizeInfo.ExternalDataByteSize);
                externalDataPtr = (byte*)destination;
                return SizeInfo.ExternalDataByteSize;
            }

            /// <summary>
            /// Copy data in external data range to <see cref="UnsafeAppendBuffer"/>, and point to new location
            /// This is unsafe as <see cref="UnsafeAppendBuffer"/> could reallocate on capacity change
            /// we must make sure that this buffer has enough capacity for all data that will be store in it
            /// </summary>
            /// <param name="destination">new location</param>
            /// <returns>size of data copied</returns>
            unsafe public int MoveExternalDataTo(ref UnsafeAppendBuffer buffer)
            {
                var originalPtr = ExternalDataPtr;
                if (originalPtr == null) return 0;

                //this is a late check just to tell that UnsafeAppendBuffer dose not have enough capacity
                //it need to be allocated with enough capacity for all data that will be store in it
                Assert.IsTrue(buffer.Capacity >= (buffer.Length + SizeInfo.ExternalDataByteSize), "Buffer don't have enough Capacity for current external data");

                externalDataPtr = buffer.Ptr + buffer.Length;
                buffer.Add(originalPtr, SizeInfo.ExternalDataByteSize);
                return SizeInfo.ExternalDataByteSize;
            }

            unsafe public T GetDataAt<T>(int byteOffset) where T : unmanaged
            {
                var dataSize = UnsafeUtility.SizeOf<T>();
                if ((byteOffset + dataSize) <= SizeInfo.LocalDataByteSize)
                { fixed (byte* localDataPtr = LocalData) { return *(T*)(localDataPtr + byteOffset); } }
                else
                {
                    var externalOffset = byteOffset - SizeInfo.LocalDataByteSize;
                    Assert.IsTrue((externalOffset + dataSize) <= SizeInfo.ExternalDataByteSize, "Can not Get Data beyond data range");
                    return *(T*)(externalDataPtr + externalOffset);
                }
            }

            unsafe public void SetDataAt<T>(int byteOffset, T data) where T : unmanaged
            {
                var dataSize = UnsafeUtility.SizeOf<T>();
                if ((byteOffset + dataSize) <= SizeInfo.LocalDataByteSize)
                { fixed (byte* localDataPtr = LocalData) { *(T*)(localDataPtr + byteOffset) = data; } }
                else
                {
                    var externalOffset = byteOffset - SizeInfo.LocalDataByteSize;
                    Assert.IsTrue((externalOffset + dataSize) <= SizeInfo.ExternalDataByteSize, "Can not Set Data beyond data range");
                    *(T*)(externalDataPtr + externalOffset) = data;
                }
            }

            unsafe public ref T DataAt<T>(int byteOffset) where T : unmanaged
            {
                var dataSize = UnsafeUtility.SizeOf<T>();
                if ((byteOffset + dataSize) <= SizeInfo.LocalDataByteSize)
                { fixed (byte* localDataPtr = LocalData) { return ref *(T*)(localDataPtr + byteOffset); } }
                else
                {
                    var externalOffset = byteOffset - SizeInfo.LocalDataByteSize;
                    Assert.IsTrue((externalOffset + dataSize) <= SizeInfo.ExternalDataByteSize, "Can not Get Data beyond data range");
                    return ref *(T*)(externalDataPtr + externalOffset);
                }
            }

            unsafe public ref T LocalDataAt<T>(int byteOffset) where T : unmanaged
            {
                Assert.IsTrue((byteOffset + UnsafeUtility.SizeOf<T>()) <= SizeInfo.LocalDataByteSize, "Can not access local Data beyond local data range");
                fixed (byte* localDataPtr = LocalData) { return ref *(T*)(localDataPtr + byteOffset); }
            }

            unsafe public ref T ExternalDataAt<T>(int byteOffset) where T : unmanaged
            {
                Assert.IsTrue((byteOffset + UnsafeUtility.SizeOf<T>()) <= SizeInfo.ExternalDataByteSize, "Can not access external Data beyond external data range");
                return ref *(T*)(externalDataPtr + byteOffset);
            }

            public ref T LocalDataAs<T>() where T : unmanaged
            {
                Assert.IsTrue(UnsafeUtility.SizeOf<T>() <= SizeInfo.LocalDataByteSize, "May not have local Data larger than local data capacity");
                unsafe { fixed (byte* pLocal = LocalData) { return ref *(T*)pLocal; } }
            }

            public ref T ExternalDataAs<T>() where T : unmanaged
            {
                Assert.IsTrue(HasExternalData && UnsafeUtility.SizeOf<T>() <= SizeInfo.ExternalDataByteSize, "May not have external Data larger than external data capacity");
                return ref *(T*)externalDataPtr;
            }

        }

        #endregion Generic Event types when passed to stream
        //-------------------------------------------------------------------------------------------------------------------
        #region Event Read/Write Extension methods

        //Write-----------------------------------------------
        unsafe public static EventWriterBuffer CreateEventBuffer(this EventWriter.BatchHandle writer)
            => EventWriterBuffer.Empty.WithWriter(writer);

        public struct EventHandle
        {
            internal int ExternalDataUsage;
            internal int ExternalDataCapacity;
            unsafe internal byte* pData;
        }

        public static EventHandle WriteHeader(this EventWriter.BatchHandle writer, EventHeader header, int externalDataCapacity)
        {
            Assert.IsTrue(externalDataCapacity == 0 || header.SizeInfo.LocalDataByteSize <= EventDataSize.MaxLocalDataByteSizeWithExternalDataPtr, "Scheduled Local Data size larger than Max allowed capacity");
            Assert.IsTrue(externalDataCapacity >= 0 && externalDataCapacity <= ushort.MaxValue, "External data size over Max allowed capacity");
            header.SizeInfo.ExternalDataByteSize = (ushort)externalDataCapacity;
            unsafe
            {
                var pData = writer.mWriter.Allocate(header.SizeInfo.AlignedPackageByteSize);
                *(EventHeader*)pData = header;
                pData += header.SizeInfo.LocalPackageByteSize;
                return new EventHandle()
                {
                    ExternalDataCapacity = externalDataCapacity,
                    ExternalDataUsage = 0,
                    pData = pData
                };
            }
        }

        public static EventHandle WriteExternalData<T>(this EventHandle handel, T data) where T : unmanaged
        {
            var sizeT = UnsafeUtility.SizeOf<T>();
            Assert.IsTrue(handel.ExternalDataUsage + sizeT <= handel.ExternalDataCapacity, "Can not write data beyond scheduled capacity");
            unsafe
            {
                *(T*)(handel.pData) = data;
                handel.pData += sizeT;
                handel.ExternalDataUsage += sizeT;
                return handel;
            }
        }

        /// <summary>
        /// Write a simple event
        /// </summary>
        public static void WriteEvent(this EventWriter.BatchHandle writer, int typeID)
        {
            var header = new EventHeader(typeID);
            writer.mWriter.Write(header);
        }

        /// <summary>
        /// Write a simple event with local data only
        /// </summary>
        public static void WriteEvent<T>(this EventWriter.BatchHandle writer, int typeID, T eventData) where T : unmanaged
        {
            Assert.IsTrue(UnsafeUtility.SizeOf<T>() <= EventDataSize.MaxLocalDataByteSize, "Event local Data size larger than max local capacity");
            var header = new EventHeader(typeID);
            header.SetLocalDataAt<T>(0, eventData);
            writer.WriteHeader(header, 0);
        }

        /// <summary>
        /// Write a event with large external data only
        /// </summary>
        public static void WriteEventExt<T>(this EventWriter.BatchHandle writer, int typeID, T eventData) where T : unmanaged
        {
            var header = new EventHeader(typeID);
            writer.WriteHeader(header, UnsafeUtility.SizeOf<T>()).WriteExternalData<T>(eventData);
        }

        /// <summary>
        /// Write a event with both local and external data
        /// </summary>
        public static void WriteEvent<TL, TX>(this EventWriter.BatchHandle writer, int typeID, TL localData, TX externalData)
            where TL : unmanaged
            where TX : unmanaged
        {
            Assert.IsTrue(UnsafeUtility.SizeOf<TL>() <= EventDataSize.MaxLocalDataByteSizeWithExternalDataPtr, "Event local Data size larger than max local capacity.(with external data pointer in use)");
            var header = new EventHeader(typeID);
            header.SetLocalDataAt(0, localData);
            writer.WriteHeader(header, UnsafeUtility.SizeOf<TX>()).WriteExternalData<TX>(externalData);
        }

        //Read-----------------------------------------------

        unsafe public static GenericEvent ReadEvent(this EventReader.BatchHandle reader) => GenericEvent.ReadEvent(ref reader.mReader);

        #endregion Event Read/Write Extension methods
        //-------------------------------------------------------------------------------------------------------------------
        #region Commen Types
        public struct EventID
        {
            public static readonly int ByteSize = UnsafeUtility.SizeOf<EventID>();
            public int ID;
            public static implicit operator int(EventID from) => from.ID;
            public static implicit operator EventID(int from) => new EventID() { ID = from };
        }

        public struct SourceEntity
        {
            public static readonly int ByteSize = UnsafeUtility.SizeOf<SourceEntity>();
            public Entity Entity;

            public static implicit operator Entity(SourceEntity s) => s.Entity;
            public static implicit operator SourceEntity(Entity e) => new SourceEntity() { Entity = e };
            public static implicit operator TargetEntity(SourceEntity e) => new TargetEntity() { Entity = e.Entity };
            public static implicit operator SourceEntity(TargetEntity e) => new SourceEntity() { Entity = e.Entity };
        }

        public struct TargetEntity
        {
            public static readonly int ByteSize = UnsafeUtility.SizeOf<TargetEntity>();
            public Entity Entity;

            public static implicit operator TargetEntity(SourceEntity s) => s.Entity;
            public static implicit operator TargetEntity(Entity e) => new TargetEntity() { Entity = e };
        }

        public struct SourceTargetPair
        {
            public static readonly int ByteSize = UnsafeUtility.SizeOf<SourceTargetPair>();
            public SourceEntity Source;
            public TargetEntity Target;
        }
        #endregion Commen Types 
        //-------------------------------------------------------------------------------------------------------------------

        [StructLayout(LayoutKind.Explicit, Size = 64)]
        unsafe public struct EventDataSegmentInfo
        {
            [FieldOffset(0)] ushort typeID;
            [FieldOffset(2)] byte dataCount;
            [FieldOffset(3)] byte isDefined;
            [FieldOffset(4)] internal fixed ushort DataSegmentEnds[30];

            public bool IsDefined
            {
                get => isDefined != 0;
                internal set => isDefined = (byte)(value ? 1 : 0);
            }

            public int TypeID
            {
                get => typeID;
                set
                {
                    Assert.IsTrue(value >= 0 || value < ushort.MaxValue, "Type ID must be a positive value smaller than 65536");
                    typeID = (ushort)value;
                }
            }
            
            public int DataCount => dataCount;

            public ref EventDataSegmentInfo RegisterNextDataType<T>() where T : unmanaged
            {
                Assert.IsTrue(dataCount < 30, "Event Type can not hold more than 30 Data Segments");
                var size = UnsafeUtility.SizeOf<T>();
                var nextEnd = (dataCount == 0 ? 0 : DataSegmentEnds[dataCount - 1]) + size;
                Assert.IsTrue(nextEnd < ushort.MaxValue, "Total Data size of event data can not be larger than 65536 bytes");
                DataSegmentEnds[dataCount++] = (ushort)nextEnd;
                fixed (EventDataSegmentInfo* pThis = &this) { return ref *pThis; }
            }

            public int GetOffset<T>(int dataIndex) where T : unmanaged
            {
                //Assert.IsTrue(dataIndex < 30, "Event Type can not hold more than 30 Data Segments");
                Assert.IsTrue(dataIndex <= dataCount, "Event Data Index beyond total data count");
                var dataStart = dataIndex == 0 ? 0 : DataSegmentEnds[dataIndex - 1];
                Assert.IsTrue((DataSegmentEnds[dataIndex] - dataStart) == UnsafeUtility.SizeOf<T>(), "Data Size miss-match");
                return dataStart;
            }

            public int GetUnsafeOffset(int dataIndex)
            {
                Assert.IsTrue(dataIndex <= dataCount, "Event Data Index beyond total data count");
                return dataIndex == 0 ? 0 : DataSegmentEnds[dataIndex - 1];
            }

            public byte GetByte(int offset)
            {
                unsafe
                {
                    fixed (ushort* pData = DataSegmentEnds)
                    {
                        return *(((byte*)pData) - 4 + offset);
                    }
                }
            }

            public T GetData<T>(int index, GenericEvent from) where T : unmanaged => from.GetDataAt<T>(GetOffset<T>(index));
            public ref T Data<T>(int index, GenericEvent from) where T : unmanaged => ref from.DataAt<T>(GetOffset<T>(index));
        }

        public struct EventTypeRegistry : IDisposable
        {
            public EventTypeRegistry(Allocator allocator) => EventTypeInfos = new UnsafeAppendBuffer(1025, JobsUtility.CacheLineSize, allocator);

            UnsafeAppendBuffer EventTypeInfos;

            public int NextUndefinedTypeID()
            {
                var reader = EventTypeInfos.AsReader();
                unsafe
                {
                    var ptr = reader.Ptr;
                    var offset = 0;
                    var lastOffset = EventTypeInfos.Length - 64;
                    while (offset < lastOffset)
                    {
                        var info = *(EventDataSegmentInfo*)(ptr + offset);
                        if (!info.IsDefined) break;
                        offset += 64;
                    }
                    return offset >> 6;
                }
            }

            public ref EventDataSegmentInfo RegisterEventType(int typeID)
            {
                unsafe
                {
                    var typeOffset = typeID << 6;
                    var targetMinLength = typeOffset + 64;
                    if (EventTypeInfos.Length < targetMinLength)
                    {
                        EventTypeInfos.SetCapacity(targetMinLength);
                        UnsafeUtility.MemClear(EventTypeInfos.Ptr + EventTypeInfos.Length, targetMinLength - EventTypeInfos.Length);
                        EventTypeInfos.Length = targetMinLength;
                    }
                    Assert.IsFalse(((EventDataSegmentInfo*)(EventTypeInfos.Ptr + typeOffset))->IsDefined, "TypeID is already registered");
                    var pInfo = (EventDataSegmentInfo*)(EventTypeInfos.Ptr + typeOffset);
                    pInfo->IsDefined = true;
                    return ref *(EventDataSegmentInfo*)(EventTypeInfos.Ptr + typeOffset);
                }
            }

            public void UpdateEventType(int typeID, EventDataSegmentInfo info)
            {
                unsafe
                {
                    var typeOffset = typeID << 6;
                    var targetMinLength = typeOffset + 64;
                    Assert.IsTrue(EventTypeInfos.Length >= targetMinLength, "TypeID not exist");
                    Assert.IsTrue(((EventDataSegmentInfo*)(EventTypeInfos.Ptr + typeOffset))->IsDefined, "TypeID not exist");
                    info.IsDefined = true;
                    *(EventDataSegmentInfo*)(EventTypeInfos.Ptr + typeOffset) = info;
                }
            }

            public EventDataSegmentInfo GetTypeInfo(int typeID)
            {
                unsafe
                {
                    var typeOffset = typeID << 6;
                    var targetMinLength = typeOffset + 64;
                    Assert.IsTrue(EventTypeInfos.Length >= targetMinLength, "TypeID not exist");
                    Assert.IsTrue(((EventDataSegmentInfo*)(EventTypeInfos.Ptr + typeOffset))->IsDefined, "TypeID not exist");
                    return *(EventDataSegmentInfo*)(EventTypeInfos.Ptr + typeOffset);
                }
            }

            public bool IsCreated => EventTypeInfos.IsCreated;
            public void Dispose()
            {
                if (EventTypeInfos.IsCreated) EventTypeInfos.Dispose();
                EventTypeInfos = default;
            }
            public JobHandle Dispose(JobHandle dependsOn)
            {
                if (EventTypeInfos.IsCreated) dependsOn = EventTypeInfos.Dispose(dependsOn);
                EventTypeInfos = default;
                return dependsOn;
            }
        }
        //-------------------------------------------------------------------------------------------------------------------
    }

}

