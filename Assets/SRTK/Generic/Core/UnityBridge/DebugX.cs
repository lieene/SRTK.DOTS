/************************************************************************************
| File: DebugX.cs                                                                   |
| Project: SRTK.UnityBridge                                                         |
| Created Date: Fri Sep 6 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified: Tue Oct 22 2019                                                    |
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
| Date      	By	Comments                                                    |
| ----------	---	----------------------------------------------------------  |
************************************************************************************/


using System.Diagnostics;
namespace SRTK
{
    public static class DebugX
    {
        public static void LogError(this string data)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogError(data);
#else
        Debug.Fail(data);
#endif
        }


        public static void LogWarning(this string data)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.LogWarning(data);
#else
        Debug.Write(data, "Warning");
#endif
        }

        public static void Log(this string data)
        {
#if UNITY_5_3_OR_NEWER
            UnityEngine.Debug.Log(data);
#else
        Debug.Write(data, "Info");
#endif
        }
    }
}
