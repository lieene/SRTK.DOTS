/************************************************************************************
| File: ArraySeg.cs                                                                 |
| Project: SRTK.Pool                                                                |
| Created Date: Sun Sep 8 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Tue Oct 15 2019                                                    |
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
using System.Collections;
using System.Collections.Generic;

namespace SRTK.Pool
{
    public interface IShrink
    {
        void ShrinkStep();
        void ShrinkToBare();
        void Clear();
    }
    public interface IPreWorm
    {
        void PreWarmStep();
        void PreWarm(uint count);
    }


    public interface IFree<T> { void Free(T item); }

    public interface IFreeRef<T> : IFree<T> { void Free(ref T item); }

    /// <summary>
    /// Allocate one item without parameter
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    public interface IAlloc<T> { T Allocate(); }

    /// <summary>
    /// Allocate one item with one parameter
    /// </summary>
    /// <typeparam name="T">Item type</typeparam>
    /// <typeparam name="TP">Parameter type</typeparam>
    public interface IAlloc<T, TP> { T Allocate(TP param); }



    //----------------------------------------------------------------------------------

    //TODO: 
    public interface IDeepFree
    {
        void SubFree<T>(T item) where T : class;
    }

    //TODO: 
    public interface ICanDeepFree
    {
        IEnumerator<T> FieldsToFree<T>();
    }

    //----------------------------------------------------------------------------------

    public interface IObjectPool<T>
        : IAlloc<T>, IFree<T>, IFreeRef<T>
        where T : class
    { }


    //----------------------------------------------------------------------------------

    public interface ICache<T>
        : IAlloc<T>, IFree<T>, IFreeRef<T>
        where T : class
    { }

    public interface IArrayCache<T>
        : IAlloc<ArraySeg<T>, int>,
          IFree<ArraySeg<T>>, IFreeRef<ArraySeg<T>>
    { }

    public interface IBufferCache
    {
        BufferX<T> Allocate<T>(int size) where T : unmanaged;
        void Free<T>(BufferX<T> buf) where T : unmanaged;
        void Free<T>(ref BufferX<T> buf) where T : unmanaged;
        RawBuffer AllocateRaw(int byteSize);
        void FreeRaw(RawBuffer buf);
        void FreeRaw(ref RawBuffer buf);
        void Release();
    }
}
