/************************************************************************************
| File: StringEx.cs                                                                 |
| Project: SRTK.Regex                                                               |
| Created Date: Fri Sep 6 2019                                                      |
| Author: Lieene Guo                                                                |
| -----                                                                             |
| Last Modified:                                                                    |
| Modified By:                                                                      |
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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SRTK.Utility
{
    public static partial class StringEx
    {
        //----------------------------------------------------------------------
        #region Const strings
        public const char eof = '\0';
        public const string EOF = "\0";
        public const string NEW_LINE = "\r\n";
        public const string EMPTY = "";
        #endregion Const strings
        //----------------------------------------------------------------------
        #region Index Postfix
        /// <summary>
        /// Get positive index at the end of the string
        /// </summary>
        /// <param name="s">input string</param>
        /// <returns>index at the end as int, 0 if no index was found</returns>
        public static int GetPostfixIndex(this string s)
        {
            Match match = Regex.Match(s, @"\d+$");
            if (match.Success) return int.Parse(match.Value);
            else return 0;
        }

        /// <summary>
        /// Get positive index at the end of the string
        /// </summary>
        /// <param name="s">input string</param>
        /// <param name="withOutPostfix">string with index removed if there is any</param>
        /// <returns>index at the end as int, 0 if no index was found</returns>
        public static int GetPostfixIndex(this string s, out string withOutPostfix)
        {
            Match match = Regex.Match(s, @"\d+$");
            if (match.Success)
            {
                withOutPostfix = s.Substring(0, s.Length - match.Length);
                return int.Parse(match.Value);
            }
            else
            {
                withOutPostfix = s;
                return 0;
            }
        }

        /// <summary>
        /// try to get positive index at the end of the string
        /// </summary>
        /// <param name="s">input string</param>
        /// <param name="index">index postfix found as int</param>
        /// <param name="withOutPostfix">string with index removed if there is any</param>
        /// <returns>true if there is a number index at the end, false otherwise</returns>
        public static bool TryGetPostfixIndex(this string s, out int index, out string withOutPostfix)
        {
            Match match = Regex.Match(s, @"\d+$");
            if (match.Success)
            {
                index = int.Parse(match.Value);
                withOutPostfix = s.Substring(0, s.Length - match.Length);
                return true;
            }
            else
            {
                index = 0;
                withOutPostfix = s;
                return false;
            }
        }

        /// <summary>
        /// try to get positive index at the end of the string
        /// </summary>
        /// <param name="s">input string</param>
        /// <param name="index">index postfix found as int</param>
        /// <returns>true if there is a number index at the end, false otherwise</returns>
        public static bool TryGetPostfixIndex(this string s, out int index)
        {
            Match match = Regex.Match(s, @"\d+$");
            if (match.Success)
            {
                index = int.Parse(match.Value);
                return true;
            }
            else
            {
                index = 0;
                return false;
            }
        }

        /// <summary>
        /// Return a unique name witch is differen from all names in <paramref name="existingNames"/>
        /// add increasing numerical index as postfix at the end of <paramref name="name"/> is any name conflict found 
        /// </summary>
        /// <param name="name">input name</param>
        /// <param name="existingNames">existing names</param>
        /// <returns>unique name</returns>
        public static string UniqueIndexedName(this string name, params string[] existingNames)
        {
            if (existingNames.Contains(name))
            {
                string bareName;
                int index = name.GetPostfixIndex(out bareName) + 1;
                string uniqueName = bareName + index.ToString();
                while (existingNames.Contains(uniqueName)) uniqueName = bareName + (++index).ToString();
                return uniqueName;
            }
            else return name;
        }

        /// <summary>
        /// Return a unique name witch is differen from all names in <paramref name="existingNames"/>
        /// add increasing numerical index as postfix at the end of <paramref name="name"/> is any name conflict found 
        /// </summary>
        /// <param name="name">input name</param>
        /// <param name="conflictIndex">index where the conflict name is found in <paramref name="existingNames"/> </param>
        /// <param name="existingNames">existing names</param>
        /// <returns>unique name</returns>
        public static string UniqueIndexedName(this string name, out int conflictIndex, params string[] existingNames)
        {
            conflictIndex = Array.IndexOf(existingNames, name);
            if (conflictIndex >= 0)
            {
                string bareName;
                int index = name.GetPostfixIndex(out bareName) + 1;
                string uniqueName = bareName + index.ToString();
                while (existingNames.Contains(uniqueName)) uniqueName = bareName + (++index).ToString();
                return uniqueName;
            }
            else return name;
        }
        #endregion Index Postfix
        //----------------------------------------------------------------------
        #region Name Converter

        public const string RemoveStartingSpaceReplaceRegex = @"^\s+";
        public const string InvlidCharReplaceRegex = @"[^A-Za-z0-9_ ]+";
        public const string CamelCaseFromAnyStringRegex = @"(?<=^\s*)(_+)|(?=[A-Z])|\s+|_+";

        public const string DefineCaseMatchRegex = @"^\s*[A-Z_]+$";
        public const string DefineCaseSplitRegex = @"(^_+)|_+|\s+";

        public const string CamelCaseMatchRigidRegex = @"^_*[A-Za-z][a-z0-9]*(?:[A-Z][a-z0-9]*)*$";
        public const string CamelCaseMatchRegex = @"^\s*_*[A-Za-z][a-z0-9]*(?:[A-Z][a-z0-9]*)*$";
        public const string CamelCaseSplitRegex = @"(^_+)|(?<!^)(?=[A-Z])";

        public const string DisplayCaseMatchRegex = @"^\s*(?:[A-Za-z0-9]|(?:\s*))+$";
        public const string DisplayCaseSplitRegex = @"\s+|(?<!^)(?=[A-Z])";

        public static string ToDefineCase(this string camelCase, bool noTesting = false)
        {
            string defCase = camelCase;
            defCase = Regex.Replace(defCase, RemoveStartingSpaceReplaceRegex, "");
            defCase = Regex.Replace(defCase, InvlidCharReplaceRegex, "_");
            string[] elems;
            if (noTesting || Regex.IsMatch(defCase, CamelCaseMatchRegex))
            {
                elems = Regex.Split(defCase, CamelCaseSplitRegex);
                defCase = string.Empty;
                bool is1St = true;
                foreach (var s in elems)
                {
                    if (s.Length == 0) continue;
                    if (!is1St) defCase += "_";
                    defCase += s.ToUpper();
                    if (!s.StartsWith("_")) is1St = false;
                }
                return defCase;
            }
            else if (Regex.IsMatch(defCase, DefineCaseMatchRegex)) return defCase;
            else
            {
                $"Input string not valide!\n{camelCase}".LogError();
                return camelCase;
            }
        }

        public static string ToDisplayCase(this string camelCase, bool noTesting = false)
        {
            string displayCase = camelCase;
            displayCase = Regex.Replace(displayCase, RemoveStartingSpaceReplaceRegex, "");
            displayCase = Regex.Replace(displayCase, InvlidCharReplaceRegex, "_");
            string[] elems;
            if (noTesting || Regex.IsMatch(displayCase, CamelCaseMatchRegex))
            {
                elems = Regex.Split(displayCase, CamelCaseSplitRegex);
                displayCase = string.Empty;
                bool is1St = true;
                foreach (var s in elems)
                {
                    if (s.Length == 0) continue;
                    if (s.StartsWith("_")) continue;
                    if (!is1St) displayCase += " ";
                    string us = s.ToUpper();
                    string ls = s.ToLower();
                    displayCase += us.Substring(0, 1) + ls.Substring(1, ls.Length - 1);
                    is1St = false;
                }
                return displayCase;
            }
            else if (Regex.IsMatch(displayCase, DisplayCaseMatchRegex)) return displayCase;
            else
            {
                $"Input string not valide!\n{camelCase}".LogError();
                return camelCase;
            }
        }

        public static string DefineToCamelCase(this string defineCase, bool noTesting = false)
        {
            string camelCase = defineCase;
            camelCase = Regex.Replace(camelCase, RemoveStartingSpaceReplaceRegex, "");
            camelCase = Regex.Replace(camelCase, InvlidCharReplaceRegex, "_");
            string[] elems;
            if (noTesting || Regex.IsMatch(camelCase, DefineCaseMatchRegex))
            {
                elems = Regex.Split(camelCase, DefineCaseSplitRegex);
                camelCase = string.Empty;
                bool is1St = true;
                foreach (var s in elems)
                {
                    if (s.Length == 0) continue;
                    string us = s.ToUpper();
                    string ls = s.ToLower();
                    if (is1St) camelCase += s.Substring(0, 1) + ls.Substring(1, ls.Length - 1);
                    else camelCase += us.Substring(0, 1) + ls.Substring(1, ls.Length - 1);
                    if (!s.StartsWith("_")) is1St = false;
                }
                return camelCase;
            }
            else if (Regex.IsMatch(camelCase, CamelCaseMatchRegex)) return camelCase;
            else
            {
                $"Input string not valide!\n{defineCase}".LogError();
                return defineCase;
            }
        }

        public static string DisplayToCamelCase(this string displayCase, bool noTesting = false)
        {
            string camelCase = displayCase;
            camelCase = Regex.Replace(camelCase, RemoveStartingSpaceReplaceRegex, "");
            camelCase = Regex.Replace(camelCase, InvlidCharReplaceRegex, "_");
            string[] elems;
            if (noTesting || Regex.IsMatch(camelCase, DisplayCaseMatchRegex))
            {
                elems = Regex.Split(camelCase, DisplayCaseSplitRegex);
                camelCase = string.Empty;
                bool is1St = true;
                foreach (var s in elems)
                {
                    if (s.Length == 0) continue;
                    string us = s.ToUpper();
                    string ls = s.ToLower();
                    if (is1St) camelCase += s.Substring(0, 1) + ls.Substring(1, ls.Length - 1);
                    else camelCase += us.Substring(0, 1) + ls.Substring(1, ls.Length - 1);
                    if (!s.StartsWith("_")) is1St = false;
                }
                return camelCase;
            }
            else if (Regex.IsMatch(camelCase, CamelCaseMatchRegex)) return camelCase;
            else
            {
                $"Input string not valide!\n{displayCase}".LogError();
                return displayCase;
            }
        }

        public static string ValidateCamelCase(this string input)
        {
            if (Regex.IsMatch(input, CamelCaseMatchRigidRegex)) return input;

            string camelCase = input;
            camelCase = Regex.Replace(camelCase, RemoveStartingSpaceReplaceRegex, "");
            camelCase = Regex.Replace(camelCase, InvlidCharReplaceRegex, "_");
            var elems = Regex.Split(camelCase, CamelCaseFromAnyStringRegex);
            camelCase = string.Empty;
            bool is1St = true;
            foreach (var s in elems)
            {
                if (s.Length == 0) continue;
                string us = s.ToUpper();
                string ls = s.ToLower();
                if (is1St) camelCase += s.Substring(0, 1) + ls.Substring(1, ls.Length - 1);
                else camelCase += us.Substring(0, 1) + ls.Substring(1, ls.Length - 1);
                if (!s.StartsWith("_")) is1St = false;
            }
            return camelCase;
        }

        public static bool IsCamelCase(this string input)
        { return Regex.IsMatch(input, CamelCaseMatchRegex); }

        public static bool IsDefineCase(this string input)
        { return Regex.IsMatch(input, DefineCaseMatchRegex); }

        public static bool IsDisplayCase(this string input)
        { return Regex.IsMatch(input, DisplayCaseMatchRegex); }

        #endregion Name Converter
        //----------------------------------------------------------------------
        #region Escape

        public static string EscapeForCSharp(this string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
                switch (c)
                {
                    case '\'':
                    case '"':
                    case '\\':
                        sb.Append(c.EscapeForCSharp());
                        break;
                    default:
                        if (char.IsControl(c))
                            sb.Append(c.EscapeForCSharp());
                        else
                            sb.Append(c);
                        break;
                }
            return sb.ToString();
        }
        public static string EscapeForCSharp(this char chr)
        {
            switch (chr)
            {//first catch the special cases with C# shortcut escapes.
                case '\'':
                    return @"\'";
                case '"':
                    return "\\\"";
                case '\\':
                    return @"\\";
                case '\0':
                    return @"\0";
                case '\a':
                    return @"\a";
                case '\b':
                    return @"\b";
                case '\f':
                    return @"\f";
                case '\n':
                    return @"\n";
                case '\r':
                    return @"\r";
                case '\t':
                    return @"\t";
                case '\v':
                    return @"\v";
                default:
                    //we need to escape surrogates with they're single chars,
                    //but in strings we can just use the character they produce.
                    if (char.IsControl(chr) || char.IsHighSurrogate(chr) || char.IsLowSurrogate(chr))
                        return @"\u" + ((int)chr).ToString("X4");
                    else
                        return new string(chr, 1);
            }
        }
        #endregion Escape
        //----------------------------------------------------------------------
    }
}