/************************************************************************************
| File: NativeDelegate.cs                                                           |
| Project: lieene.Utility                                                           |
| Created Date: Sat Apr 4 2020                                                      |
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Unity.Entities;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine.Assertions;

namespace SRTK
{
    public static class NativeDelegateExt
    {
        /// <summary>
        /// Pin the delegate so it can be used before it get Disposed
        /// </summary>
        internal static (GCHandle handle, IntPtr ptr) Pin<T>(T d) where T : Delegate => (GCHandle.Alloc(d), Marshal.GetFunctionPointerForDelegate(d));

        /// <summary>
        /// Try to compile target delegate to burst version delegate and wrap the new delegate with a <see cref="NativeDelegate<T>"/>
        /// in cases when burst cannot compile target delegate, it will be pinned and warped with a <see cref="NativeDelegate<T>"/>
        /// Note: Burst can only compile static and None-Generic Methods!
        /// </summary>
        public static NativeDelegate<T> Compile<T>(this T d) where T : Delegate
        {
            if (d.Method.IsGenericMethod) throw new ArgumentException("Generic now supported");
#if NET_DOTS
            return Wrap(d);
#else
            //case where Burst can compile
            if (d.Method.IsStatic && !d.Method.IsGenericMethod)
            {
                var fPtr = BurstCompiler.CompileFunctionPointer(d);
                return new NativeDelegate<T>() { pDelegate = fPtr.Value, handle = default };
            }
            //cases where Burst can not compile target function, we will simply
            else return Wrap(d);
#endif
        }

        /// <summary>
        /// No Compile return a <see cref="NativeDelegate<T>"/> pointing to a pinned delegate, managed or un-managed
        /// </summary>
        public static NativeDelegate<T> Wrap<T>(this T d) where T : Delegate
        {
            if (d.Method.IsGenericMethod) throw new ArgumentException("Generic now supported");
            (var handle, var ptr) = Pin(d);
            return new NativeDelegate<T>() { pDelegate = ptr, handle = handle };
        }

        /// <summary>
        /// Try to compile target delegate to burst version delegate and wrap the new delegate with a <see cref="NativeTypeLessDelegate"/>
        /// in cases when burst cannot compile target delegate, it will be pinned and warped with a <see cref="NativeTypeLessDelegate"/>
        /// Note: Burst can only compile static and None-Generic Methods!
        /// </summary>
        public static NativeTypeLessDelegate CompileTypeLess<T>(this T d) where T : Delegate
        {
            if (d.Method.IsGenericMethod) throw new ArgumentException("Generic now supported");
#if NET_DOTS
            return WrapTypeless(d);
#else
            //case where Burst can compile
            if (d.Method.IsStatic && !d.Method.IsGenericMethod)
            {
                var fPtr = BurstCompiler.CompileFunctionPointer(d);
                return new NativeTypeLessDelegate() { pDelegate = fPtr.Value, handle = default };
            }
            //cases where Burst can not compile target function, we will simply
            else return WrapTypeLess(d);
#endif
        }

        /// <summary>
        /// No Compile return a <see cref="Wrap<T>"/> pointing to a pinned delegate, managed or un-managed
        /// </summary>
        public static NativeTypeLessDelegate WrapTypeLess<T>(this T d) where T : Delegate
        {
            if (d.Method.IsGenericMethod) throw new ArgumentException("Generic now supported");
            (var handle, var ptr) = Pin(d);
            return new NativeTypeLessDelegate() { pDelegate = ptr, handle = handle };
        }
    }


    public struct FreeGCHandleJob : IJob
    {
        public GCHandle handle;
        public void Execute() { if (handle.IsAllocated) handle.Free(); }
    }

    /// <summary>
    /// Native version of delegate that can be used in jobs
    /// </summary>
    public struct NativeDelegate<T> : IDisposable where T : Delegate
    {
        public static NativeDelegate<T> Compile(T d) => NativeDelegateExt.Compile<T>(d);
        public static NativeDelegate<T> Wrap(T d) => NativeDelegateExt.Wrap<T>(d);

        internal GCHandle handle;
        [NativeDisableUnsafePtrRestriction] internal IntPtr pDelegate;

        public bool IsCreated => pDelegate != IntPtr.Zero;
        public bool IsCompiled => pDelegate != IntPtr.Zero && !handle.IsAllocated;

        public FunctionPointer<T> AsFunctionPointer() => new FunctionPointer<T>(pDelegate);
        public T AsNetDelegate() => IsCreated ? Marshal.GetDelegateForFunctionPointer<T>(pDelegate) : null;
        public NativeTypeLessDelegate AsTyplessDelegate() => new NativeTypeLessDelegate() { pDelegate = pDelegate, handle = handle };


        public void Dispose()
        {
            if (handle.IsAllocated) handle.Free();//for instance method, as they may hold reference to class/struct instance
            handle = default;
        }
        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (handle.IsAllocated)
            {//for instance method, as they may hold reference to class/struct instance
                dependsOn = new FreeGCHandleJob() { handle = handle }.Schedule(dependsOn);
                handle = default;
                return dependsOn;
            }
            else return dependsOn;
        }
    }

    //-------------------------------------------------------------------------------------------------------------------
    /// <summary>
    /// Typeless Native delegate, that need to be converted to the strage Type on use
    /// Used when the NativeDelegate need to be stored in a uni-typed native container
    /// Do not need Dispose
    /// </summary>
    unsafe public struct NativeTypeLessDelegate : IDisposable
    {
        public static NativeTypeLessDelegate Compile<T>(T d) where T : Delegate => NativeDelegateExt.CompileTypeLess<T>(d);
        public static NativeTypeLessDelegate Wrap<T>(T d) where T : Delegate => NativeDelegateExt.WrapTypeLess<T>(d);
        internal GCHandle handle;
        [NativeDisableUnsafePtrRestriction] internal IntPtr pDelegate;

        public bool IsCreated => pDelegate != IntPtr.Zero;
        public bool IsCompiled => pDelegate != IntPtr.Zero && !handle.IsAllocated;

        public FunctionPointer<T> AsFunctionPointer<T>() where T : Delegate => new FunctionPointer<T>(pDelegate);
        public T AsNetDelegate<T>() where T : Delegate => IsCreated ? Marshal.GetDelegateForFunctionPointer<T>(pDelegate) : null;
        public NativeDelegate<T> AsNativeDelegate<T>() where T : Delegate => new NativeDelegate<T>() { pDelegate = pDelegate, handle = handle };

        public void Dispose()
        {
            if (handle.IsAllocated) handle.Free();//for instance method, as they may hold reference to class/struct instance
            handle = default;
        }
        public JobHandle Dispose(JobHandle dependsOn)
        {
            if (handle.IsAllocated)
            {//for instance method, as they may hold reference to class/struct instance
                dependsOn = new FreeGCHandleJob() { handle = handle }.Schedule(dependsOn);
                handle = default;
                return dependsOn;
            }
            else return dependsOn;
        }
    }
}
