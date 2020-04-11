/************************************************************************************
| File: Swap.cs                                                                     |
| Project: SRTK.MathX                                                               |
| Created Date: Mon Sep 16 2019                                                     |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Mon Oct 14 2019                                                    |
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
using System.Runtime.CompilerServices;

//TODO:CS73:2019 add burst compile support

namespace SRTK
{
    //Swap
    public static partial class MathX
    {
        //----------------------------------------------------------------------------------
        #region Ref
        //Ref-------------------------------------------------------------------------------

        public static void Swap<T>(ref T l, ref T r) { T t = l; l = r; r = t; }
        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterSwap<T>(ref T l, ref T r) where T : IComparable<T>
        {
            if (l.CompareTo(r) > 0)
            {
                Swap(ref l, ref r);
                return true;
            }
            else return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterSwap<T>(ref T l, ref T r, IComparer<T> comparer)
        {
            if (comparer.Compare(l, r) > 0)
            {
                Swap(ref l, ref r);
                return true;
            }
            else return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterSwap<T>(ref T l, ref T r, Comparison<T> compare)
        {
            if (compare(l, r) > 0)
            {
                Swap(ref l, ref r);
                return true;
            }
            else return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GreaterSwap<T, P>(ref T l, ref T r)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            if (l.Priority.CompareTo(r.Priority) > 0)
            {
                Swap(ref l, ref r);
                return true;
            }
            else return false;
        }

        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SmallerSwap<T>(ref T l, ref T r) where T : IComparable<T>
        {
            if (l.CompareTo(r) < 0)
            {
                Swap(ref l, ref r);
                return true;
            }
            else return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SmallerSwap<T>(ref T l, ref T r, IComparer<T> comparer)
        {
            if (comparer.Compare(l, r) < 0)
            {
                Swap(ref l, ref r);
                return true;
            }
            else return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SmallerSwap<T>(ref T l, ref T r, Comparison<T> compare)
        {
            if (compare(l, r) < 0)
            {
                Swap(ref l, ref r);
                return true;
            }
            else return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SmallerSwap<T, P>(ref T l, ref T r)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            if (l.Priority.CompareTo(r.Priority) < 0)
            {
                Swap(ref l, ref r);
                return true;
            }
            else return false;
        }
        //Ref--------------------------------------------------------------------------------------------------
        #endregion Ref
        //----------------------------------------------------------------------------------
        #region IList
        //IList--------------------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(in IList<T> list, int i, int j)
        { T t = list[i]; list[i] = list[j]; list[j] = t; }

        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T>(in IList<T> list, int i, int j) where T : IComparable<T>
        {
            T l = list[i], r = list[j];
            int comp = l.CompareTo(r);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T>(in IList<T> list, int i, int j, IComparer<T> comparer)
        {
            T l = list[i], r = list[j];
            int comp = comparer.Compare(l, r);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T>(in IList<T> list, int i, int j, Comparison<T> compare)
        {
            T l = list[i], r = list[j];
            int comp = compare(l, r);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T, P>(in IList<T> list, int i, int j)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            T l = list[i], r = list[j];
            int comp = l.Priority.CompareTo(r.Priority);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T>(in IList<T> list, int i, int j) where T : IComparable<T>
        {
            T l = list[i], r = list[j];
            int comp = l.CompareTo(r);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T>(in IList<T> list, int i, int j, IComparer<T> comparer)
        {
            T l = list[i], r = list[j];
            int comp = comparer.Compare(l, r);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T>(in IList<T> list, int i, int j, Comparison<T> compare)
        {
            T l = list[i], r = list[j];
            int comp = compare(l, r);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T, P>(in IList<T> list, int i, int j)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            T l = list[i], r = list[j];
            int comp = l.Priority.CompareTo(r.Priority);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }
        //IList--------------------------------------------------------------------------------------------------
        #endregion IList
        //----------------------------------------------------------------------------------
        #region Array
        //Array--------------------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(in T[] list,ref int i,ref int j) { T t = list[i]; list[i] = list[j]; list[j] = t; }

        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T>(in T[] list,ref int i,ref int j) where T : IComparable<T>
        {
            T l = list[i], r = list[j];
            int comp = l.CompareTo(r);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T>(in T[] list,ref int i,ref int j, IComparer<T> comparer)
        {
            T l = list[i], r = list[j];
            int comp = comparer.Compare(l, r);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T>(in T[] list,ref int i,ref int j, Comparison<T> compare)
        {
            T l = list[i], r = list[j];
            int comp = compare(l, r);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T, P>(in T[] list,ref int i,ref int j)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            T l = list[i], r = list[j];
            int comp = l.Priority.CompareTo(r.Priority);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }
        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T>(in T[] list,ref int i,ref int j) where T : IComparable<T>
        {
            T l = list[i], r = list[j];
            int comp = l.CompareTo(r);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T>(in T[] list,ref int i,ref int j, IComparer<T> comparer)
        {
            T l = list[i], r = list[j];
            int comp = comparer.Compare(l, r);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T>(in T[] list,ref int i,ref int j, Comparison<T> compare)
        {
            T l = list[i], r = list[j];
            int comp = compare(l, r);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T, P>(in T[] list,ref int i,ref int j)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            T l = list[i], r = list[j];
            int comp = l.Priority.CompareTo(r.Priority);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }
        //----------------------------------------------------------------------------------
        //Array--------------------------------------------------------------------------------------------------
        #endregion Array
        //----------------------------------------------------------------------------------
        #region Array Long
        //Array--------------------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Swap<T>(in T[] list,ref long i,ref long j) { T t = list[i]; list[i] = list[j]; list[j] = t; }

        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T>(in T[] list,ref long i,ref long j) where T : IComparable<T>
        {
            T l = list[i], r = list[j];
            int comp = l.CompareTo(r);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T>(in T[] list,ref long i,ref long j, IComparer<T> comparer)
        {
            T l = list[i], r = list[j];
            int comp = comparer.Compare(l, r);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T>(in T[] list,ref long i,ref long j, Comparison<T> compare)
        {
            T l = list[i], r = list[j];
            int comp = compare(l, r);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GreaterSwap<T, P>(in T[] list,ref long i,ref long j)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            T l = list[i], r = list[j];
            int comp = l.Priority.CompareTo(r.Priority);
            if (comp > 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }
        //----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T>(in T[] list,ref long i,ref long j) where T : IComparable<T>
        {
            T l = list[i], r = list[j];
            int comp = l.CompareTo(r);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T>(in T[] list,ref long i,ref long j, IComparer<T> comparer)
        {
            T l = list[i], r = list[j];
            int comp = comparer.Compare(l, r);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T>(in T[] list,ref long i,ref long j, Comparison<T> compare)
        {
            T l = list[i], r = list[j];
            int comp = compare(l, r);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SmallerSwap<T, P>(in T[] list,ref long i,ref long j)
            where T : IPriority<P>
            where P : IComparable<P>
        {
            T l = list[i], r = list[j];
            int comp = l.Priority.CompareTo(r.Priority);
            if (comp < 0)
            {
                list[i] = r;
                list[j] = l;
            }
            return comp;
        }
        //----------------------------------------------------------------------------------
        //Array--------------------------------------------------------------------------------------------------
        #endregion Array Long
        //----------------------------------------------------------------------------------
    }
}
