using System;
using System.Collections;
using System.Text;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;
using SRTK;
using System.Runtime.InteropServices;

namespace Tests
{
    public static class NativeContainerTestExt
    {
        public static string Join<T>(this NativeArray<T> array, string slipter = "|") where T : struct
        {
            var sb = new StringBuilder(slipter);
            for (int i = 0, len = array.Length; i < len; i++) sb.Append(array[i] + slipter);
            return sb.ToString();
        }
    }
    public class ECSEditorTest
    {
        // // A Test behaves as an ordinary method
        // [Test]
        // public void ECSEditorTestSimplePasses()
        // {
        //     // Use the Assert class to test conditions
        // }

        // // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
        // // `yield return null;` to skip a frame.
        // [UnityTest]
        // public IEnumerator ECSEditorTestWithEnumeratorPasses()
        // {
        //     // Use the Assert class to test conditions.
        //     // Use yield to skip a frame.
        //     yield return null;
        // }


        #region Generic Function has no walk around
        public delegate T GenericDelegate<T>(T input);
        [Test]
        public void ProblemWithFunctionPointer()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                GenericDelegate<int> d = (int i) => i++;
                IntPtr pFunc = Marshal.GetFunctionPointerForDelegate(d);
                var dmd = Marshal.GetDelegateForFunctionPointer<GenericDelegate<int>>(pFunc);
                int r = dmd.Invoke(1);
                Assert.AreEqual(2, r);
                Debug.Log(r);

                FunctionPointerDuplicate<GenericDelegate<int>> fpFuncDup = new FunctionPointerDuplicate<GenericDelegate<int>>(pFunc);
                r = fpFuncDup.InvokeWithGenericMarshalFunc(1);
                Assert.AreEqual(2, r);
                Debug.Log(r);

                FunctionPointer<GenericDelegate<int>> fpFunc = new FunctionPointer<GenericDelegate<int>>(pFunc);
                r = fpFunc.Invoke(1);
                Assert.AreEqual(2, r);
                Debug.Log(r);
            });
        }

        public readonly struct FunctionPointerDuplicate<T> : IFunctionPointer
        {
            [Unity.Collections.LowLevel.Unsafe.NativeDisableUnsafePtrRestriction]
            private readonly IntPtr _ptr;

            /// <summary>
            /// Creates a new instance of this function pointer with the following native pointer.
            /// </summary>
            /// <param name="ptr"></param>
            public FunctionPointerDuplicate(IntPtr ptr)
            {
                _ptr = ptr;
            }

            /// <summary>
            /// Gets the underlying pointer.
            /// </summary>
            public IntPtr Value => _ptr;

            /// <summary>
            /// Gets the delegate associated to this function pointer in order to call the function pointer.
            /// This delegate can be called from a Burst Job or from regular C#.
            /// If calling from regular C#, it is recommended to cache the returned delegate of this property
            /// instead of using this property every time you need to call the delegate.
            /// </summary>
            public T Invoke => (T)(object)Marshal.GetDelegateForFunctionPointer(_ptr, typeof(T));

            public T InvokeWithGenericMarshalFunc => (T)(object)Marshal.GetDelegateForFunctionPointer<T>(_ptr);

            /// <summary>
            /// Whether the function pointer is valid.
            /// </summary>
            public bool IsCreated => _ptr != IntPtr.Zero;

            IFunctionPointer IFunctionPointer.FromIntPtr(IntPtr ptr) => new FunctionPointer<T>(ptr);
        }
        #endregion Generic Function has no walk around
        //----------------------------------------------------------------------------------------------------------
    }
}
