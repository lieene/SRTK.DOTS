/************************************************************************************
| File: R.Defines.cs                                                                |
| Project: SRTK.Regex                                                               |
| Created Date: Wed Sep 4 2019                                                      |
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


namespace SRTK.Utility.RegexBuilder
{
    // RegX Keyword Definitions
    public partial struct R
    {
        /* Generater(Generated Code)
         #define NiceAndShort
         - /// <summary>
         - /// Comment.
         - /// can use <see cref="ShortName"/> for short.
         - /// </summary>"
         - public static readonly R NiceName = Expression;
         - 
    	 - /// <summary>
       	 - /// Comment.
       	 - /// short for <see cref="NiceName"/>.
       	 - /// </summary>"
       	 - public static readonly R ShortName = Expression;
         -
         #define NiceOnly
         - /// <summary>
         - /// Comment.
         - /// </summary>"
         - public static readonly R NiceName = Expression;
         -
         #define Unicode
         - /// <summary>
         - /// Comment.
         - /// can use <see cref="ShortName"/> for short.
         - /// </summary>"
         - public R NiceName { get { return Cata("Expression"); } }
         -
    	 - /// <summary>
       	 - /// Comment.
       	 - /// </summary>"
       	 - public R ShortName { get { return Cata("Expression"); } }
         -
         #use NiceAndShort
         #region Ankor
         ~ NiceName         ShortName   Expression      Comment
         * LineStart        LSt         @"^"            [Ankor] At Line or String start
         * LineEnd          LEd         @"$"            [Ankor] At Line or String end or before \n at end 
         * StrStart         SSt         @"\A"           [Ankor] At String start 
         * StrEnd           SEd         @"\Z"           [Ankor] At String end or before \n at end 
         * StrEndStrict     SEx         @"\z"           [Ankor] At String End Strict
         * PrevMatchEnd     MEd         @"\G"           [Ankor] At Previous match end
         * WordBound        Wb          @"\b"           [Ankor] At word bound
         * NonWordBound     _Wb         @"\B"           [Ankor] Not at word bound
         #region Single Character Set
         ~ NiceName         ShortName   Expression      Comment
         #use_once NiceOnly
         * Any              Any         @"."            [Set] Any except new line[^\n]
         * Word             Wd          @"\w"           [Set] Word charactors
         * NoneWord         _Wd         @"\W"           [Set] None-Word charactors
         * Digit            D           @"\d"           [Set] Decimal digit charactors
         * NoneDigit        _D          @"\D"           [Set] None-Decimal digit charactors
         * Space            Sp          @"\s"           [Set] Any space charactors
         * NoneSpace        _Sp         @"\S"           [Set] None-space charactors
         #region Control Character
         ~ NiceName         ShortName   Expression      Comment
         #use_once NiceOnly
         * Alam             Alam        @"\a"           [Single] Bell character \u0007
         * BackSpc          BS          @"\b"           Set-only Single] back space \u0008
         * Tab              T           @"\t"           Single] Horizontal tab \u0009
         * NewLine          Nl          @"\n"           Single] New line \u000A
         * VTab             VT          @"\v"           Set-only Single] Vertical Tab \u00B
         * FormFeed         Ff          @"\f"           Set-only Single] Form feed \u000C
         * CReturn          CR          @"\r"           Single] Carriage return \u000D
         * Escape           Esc         @"\e"           Single] Escape \x001B
         #region Special Single Symbols
         ~ NiceName         ShortName   Expression      Comment
         * UnderScore       US          @"_"            _ UnderScore symbol
         * Nagetive         Minus       @"\-"           - Minus symbol
         * Positive         Plus        @"\+"           + Plus symbol
         #use_once NiceOnly
         * Equal            Equal       @"\="           = Equal(Assign) symbol
         * Period           Dot         @"\."           . Period symbol
         * Comma            Cma         @"\,"           , Comma symbol
         * Question         Qst         @"\?"           ? Question symbol
         * Slash            Sl          @"\/"           / Slash symbol
         * BackSlash        _Sl         @"\\"           \ Back Slash symbol
         * DoubleQuote      Quu         "\\\""          " Double quote symbol
         * SingleQuote      Qu          @"\'"           ' Single quote symbol
         * Colon            Cl          @"\:"           : Colon symbol
         * SemiColon        SCl         @"\;"           ; Semi-Colon symbol
         #use_once NiceOnly
         * At               At          @"\@"           @ At symbol
         * Exclamation      Ex          @"\!"           ! Exclamation symbol
         * BackQuote        BQu         @"\`"           ` BackQuote symbol
         * Tilde            Tl          @"\~"           ~ Tilde symbol
         * Sharp            Shp         @"\#"           # Sharp symbol
         * Dollar           Dlr         @"\$"           $ Dollar symbol
         * Percent          Prc         @"\%"           % Percent symbol
         * Caret            Ca          @"\^"           ^ Caret symbol
         #use_once NiceOnly
         * And              And         @"\&"           & And symbol
         * Asterisk         Star        @"\*"           * Asterisk symbol
         * Bar              Br          @"\|"           | Bar symbol
         * OpenPrth         Pr_         @"\("           ( Left Parentheses symbol
         * ClosePrth        Pr          @"\)"           ) Right Parentheses symbol
         * OpenBrc          Bc_         @"\{"           { Left Braces symbol
         * CloseBrc         Bc          @"\}"           } Right Braces symbol
         * OpenABrk         Ab_         @"\<"           < Left Angle Brackets symbol
         * CloseABrk        Ab          @"\>"           > Right Angle Brackets symbol
         * OpenBrk          Bk_         @"\["           [ Left Brackets symbol
         * CloseBrk         Bk          @"\]"           ] Right Brackets symbol
         #region Unicode Catagorys
         #use Unicode
         -public partial struct UnicodeC
         -{//++
             ~ NiceName         ShortName   Expression      Comment
             * UpperLetter      Lu          Lu              Lu Upper-case letters
             * LowerLetter      Ll          Ll              Ll lower-case letters
             * Letter           L           L               L All Letters
         -}//--
         #region Fequentlly Used Sequence
         #use NiceAndShort
         ~ NiceName         ShortName   Expression      Comment
         * CrNewLine        Cn          @"\r\n"         \r\n Carriage return and Newline
         * AnyX             _           @"[^\r\n]"       [^\r\n] Any except new line and Carriage return
         * SomeSpace        Sps         R.Sp._1n        one or more space
         * AnySpace         Spa         R.Sp._0n        zero or more space
         * AnyNewLine       N           @"\r?\n"        \r?\n Newline with or without Carriage return
        */
        #region Generated Code
        //----------------------------------------------------------------------
        #region  Ankor
        /// <summary>
        /// [Ankor] At Line or String start.
        /// can use <see cref="LSt"/> for short.
        /// </summary>"
        public static readonly R LineStart = @"^";

        /// <summary>
        /// [Ankor] At Line or String start.
        /// short for <see cref="LineStart"/>.
        /// </summary>"
        public static readonly R LSt = @"^";

        /// <summary>
        /// [Ankor] At Line or String end or before \n at end.
        /// can use <see cref="LEd"/> for short.
        /// </summary>"
        public static readonly R LineEnd = @"$";

        /// <summary>
        /// [Ankor] At Line or String end or before \n at end.
        /// short for <see cref="LineEnd"/>.
        /// </summary>"
        public static readonly R LEd = @"$";

        /// <summary>
        /// [Ankor] At String start.
        /// can use <see cref="SSt"/> for short.
        /// </summary>"
        public static readonly R StrStart = @"\A";

        /// <summary>
        /// [Ankor] At String start.
        /// short for <see cref="StrStart"/>.
        /// </summary>"
        public static readonly R SSt = @"\A";

        /// <summary>
        /// [Ankor] At String end or before \n at end.
        /// can use <see cref="SEd"/> for short.
        /// </summary>"
        public static readonly R StrEnd = @"\Z";

        /// <summary>
        /// [Ankor] At String end or before \n at end.
        /// short for <see cref="StrEnd"/>.
        /// </summary>"
        public static readonly R SEd = @"\Z";

        /// <summary>
        /// [Ankor] At String End Strict.
        /// can use <see cref="SEx"/> for short.
        /// </summary>"
        public static readonly R StrEndStrict = @"\z";

        /// <summary>
        /// [Ankor] At String End Strict.
        /// short for <see cref="StrEndStrict"/>.
        /// </summary>"
        public static readonly R SEx = @"\z";

        /// <summary>
        /// [Ankor] At Previous match end.
        /// can use <see cref="MEd"/> for short.
        /// </summary>"
        public static readonly R PrevMatchEnd = @"\G";

        /// <summary>
        /// [Ankor] At Previous match end.
        /// short for <see cref="PrevMatchEnd"/>.
        /// </summary>"
        public static readonly R MEd = @"\G";

        /// <summary>
        /// [Ankor] At word bound.
        /// can use <see cref="Wb"/> for short.
        /// </summary>"
        public static readonly R WordBound = @"\b";

        /// <summary>
        /// [Ankor] At word bound.
        /// short for <see cref="WordBound"/>.
        /// </summary>"
        public static readonly R Wb = @"\b";

        /// <summary>
        /// [Ankor] Not at word bound.
        /// can use <see cref="_Wb"/> for short.
        /// </summary>"
        public static readonly R NonWordBound = @"\B";

        /// <summary>
        /// [Ankor] Not at word bound.
        /// short for <see cref="NonWordBound"/>.
        /// </summary>"
        public static readonly R _Wb = @"\B";

        #endregion  Ankor
        //----------------------------------------------------------------------
        #region  Single Character Set
        /// <summary>
        /// [Set] Any except new line[^\n].
        /// </summary>"
        public static readonly R Any = @".";

        /// <summary>
        /// [Set] Word charactors.
        /// can use <see cref="Wd"/> for short.
        /// </summary>"
        public static readonly R Word = @"\w";

        /// <summary>
        /// [Set] Word charactors.
        /// short for <see cref="Word"/>.
        /// </summary>"
        public static readonly R Wd = @"\w";

        /// <summary>
        /// [Set] None-Word charactors.
        /// can use <see cref="_Wd"/> for short.
        /// </summary>"
        public static readonly R NoneWord = @"\W";

        /// <summary>
        /// [Set] None-Word charactors.
        /// short for <see cref="NoneWord"/>.
        /// </summary>"
        public static readonly R _Wd = @"\W";

        /// <summary>
        /// [Set] Decimal digit charactors.
        /// can use <see cref="D"/> for short.
        /// </summary>"
        public static readonly R Digit = @"\d";

        /// <summary>
        /// [Set] Decimal digit charactors.
        /// short for <see cref="Digit"/>.
        /// </summary>"
        public static readonly R D = @"\d";

        /// <summary>
        /// [Set] None-Decimal digit charactors.
        /// can use <see cref="_D"/> for short.
        /// </summary>"
        public static readonly R NoneDigit = @"\D";

        /// <summary>
        /// [Set] None-Decimal digit charactors.
        /// short for <see cref="NoneDigit"/>.
        /// </summary>"
        public static readonly R _D = @"\D";

        /// <summary>
        /// [Set] Any space charactors.
        /// can use <see cref="Sp"/> for short.
        /// </summary>"
        public static readonly R Space = @"\s";

        /// <summary>
        /// [Set] Any space charactors.
        /// short for <see cref="Space"/>.
        /// </summary>"
        public static readonly R Sp = @"\s";

        /// <summary>
        /// [Set] None-space charactors.
        /// can use <see cref="_Sp"/> for short.
        /// </summary>"
        public static readonly R NoneSpace = @"\S";

        /// <summary>
        /// [Set] None-space charactors.
        /// short for <see cref="NoneSpace"/>.
        /// </summary>"
        public static readonly R _Sp = @"\S";

        #endregion  Single Character Set
        //----------------------------------------------------------------------
        #region  Control Character
        /// <summary>
        /// [Single] Bell character \u0007.
        /// </summary>"
        public static readonly R Alam = @"\a";

        /// <summary>
        /// Set-only Single] back space \u0008.
        /// can use <see cref="BS"/> for short.
        /// </summary>"
        public static readonly R BackSpc = @"\b";

        /// <summary>
        /// Set-only Single] back space \u0008.
        /// short for <see cref="BackSpc"/>.
        /// </summary>"
        public static readonly R BS = @"\b";

        /// <summary>
        /// Single] Horizontal tab \u0009.
        /// can use <see cref="T"/> for short.
        /// </summary>"
        public static readonly R Tab = @"\t";

        /// <summary>
        /// Single] Horizontal tab \u0009.
        /// short for <see cref="Tab"/>.
        /// </summary>"
        public static readonly R T = @"\t";

        /// <summary>
        /// Single] New line \u000A.
        /// can use <see cref="Nl"/> for short.
        /// </summary>"
        public static readonly R NewLine = @"\n";

        /// <summary>
        /// Single] New line \u000A.
        /// short for <see cref="NewLine"/>.
        /// </summary>"
        public static readonly R Nl = @"\n";

        /// <summary>
        /// Set-only Single] Vertical Tab \u00B.
        /// can use <see cref="VT"/> for short.
        /// </summary>"
        public static readonly R VTab = @"\v";

        /// <summary>
        /// Set-only Single] Vertical Tab \u00B.
        /// short for <see cref="VTab"/>.
        /// </summary>"
        public static readonly R VT = @"\v";

        /// <summary>
        /// Set-only Single] Form feed \u000C.
        /// can use <see cref="Ff"/> for short.
        /// </summary>"
        public static readonly R FormFeed = @"\f";

        /// <summary>
        /// Set-only Single] Form feed \u000C.
        /// short for <see cref="FormFeed"/>.
        /// </summary>"
        public static readonly R Ff = @"\f";

        /// <summary>
        /// Single] Carriage return \u000D.
        /// can use <see cref="CR"/> for short.
        /// </summary>"
        public static readonly R CReturn = @"\r";

        /// <summary>
        /// Single] Carriage return \u000D.
        /// short for <see cref="CReturn"/>.
        /// </summary>"
        public static readonly R CR = @"\r";

        /// <summary>
        /// Single] Escape \x001B.
        /// can use <see cref="Esc"/> for short.
        /// </summary>"
        public static readonly R Escape = @"\e";

        /// <summary>
        /// Single] Escape \x001B.
        /// short for <see cref="Escape"/>.
        /// </summary>"
        public static readonly R Esc = @"\e";

        #endregion  Control Character
        //----------------------------------------------------------------------
        #region  Special Single Symbols
        /// <summary>
        /// _ UnderScore symbol.
        /// can use <see cref="US"/> for short.
        /// </summary>"
        public static readonly R UnderScore = @"_";

        /// <summary>
        /// _ UnderScore symbol.
        /// short for <see cref="UnderScore"/>.
        /// </summary>"
        public static readonly R US = @"_";

        /// <summary>
        /// - Minus symbol.
        /// can use <see cref="Minus"/> for short.
        /// </summary>"
        public static readonly R Nagetive = @"\-";

        /// <summary>
        /// - Minus symbol.
        /// short for <see cref="Nagetive"/>.
        /// </summary>"
        public static readonly R Minus = @"\-";

        /// <summary>
        /// + Plus symbol.
        /// can use <see cref="Plus"/> for short.
        /// </summary>"
        public static readonly R Positive = @"\+";

        /// <summary>
        /// + Plus symbol.
        /// short for <see cref="Positive"/>.
        /// </summary>"
        public static readonly R Plus = @"\+";

        /// <summary>
        /// = Equal(Assign) symbol.
        /// </summary>"
        public static readonly R Equal = @"\=";

        /// <summary>
        /// . Period symbol.
        /// can use <see cref="Dot"/> for short.
        /// </summary>"
        public static readonly R Period = @"\.";

        /// <summary>
        /// . Period symbol.
        /// short for <see cref="Period"/>.
        /// </summary>"
        public static readonly R Dot = @"\.";

        /// <summary>
        /// , Comma symbol.
        /// can use <see cref="Cma"/> for short.
        /// </summary>"
        public static readonly R Comma = @"\,";

        /// <summary>
        /// , Comma symbol.
        /// short for <see cref="Comma"/>.
        /// </summary>"
        public static readonly R Cma = @"\,";

        /// <summary>
        /// ? Question symbol.
        /// can use <see cref="Qst"/> for short.
        /// </summary>"
        public static readonly R Question = @"\?";

        /// <summary>
        /// ? Question symbol.
        /// short for <see cref="Question"/>.
        /// </summary>"
        public static readonly R Qst = @"\?";

        /// <summary>
        /// / Slash symbol.
        /// can use <see cref="Sl"/> for short.
        /// </summary>"
        public static readonly R Slash = @"\/";

        /// <summary>
        /// / Slash symbol.
        /// short for <see cref="Slash"/>.
        /// </summary>"
        public static readonly R Sl = @"\/";

        /// <summary>
        /// \ Back Slash symbol.
        /// can use <see cref="_Sl"/> for short.
        /// </summary>"
        public static readonly R BackSlash = @"\\";

        /// <summary>
        /// \ Back Slash symbol.
        /// short for <see cref="BackSlash"/>.
        /// </summary>"
        public static readonly R _Sl = @"\\";

        /// <summary>
        /// " Double quote symbol.
        /// can use <see cref="Quu"/> for short.
        /// </summary>"
        public static readonly R DoubleQuote = "\\\"";

        /// <summary>
        /// " Double quote symbol.
        /// short for <see cref="DoubleQuote"/>.
        /// </summary>"
        public static readonly R Quu = "\\\"";

        /// <summary>
        /// ' Single quote symbol.
        /// can use <see cref="Qu"/> for short.
        /// </summary>"
        public static readonly R SingleQuote = @"\'";

        /// <summary>
        /// ' Single quote symbol.
        /// short for <see cref="SingleQuote"/>.
        /// </summary>"
        public static readonly R Qu = @"\'";

        /// <summary>
        /// : Colon symbol.
        /// can use <see cref="Cl"/> for short.
        /// </summary>"
        public static readonly R Colon = @"\:";

        /// <summary>
        /// : Colon symbol.
        /// short for <see cref="Colon"/>.
        /// </summary>"
        public static readonly R Cl = @"\:";

        /// <summary>
        /// ; Semi-Colon symbol.
        /// can use <see cref="SCl"/> for short.
        /// </summary>"
        public static readonly R SemiColon = @"\;";

        /// <summary>
        /// ; Semi-Colon symbol.
        /// short for <see cref="SemiColon"/>.
        /// </summary>"
        public static readonly R SCl = @"\;";

        /// <summary>
        /// @ At symbol.
        /// </summary>"
        public static readonly R At = @"\@";

        /// <summary>
        /// ! Exclamation symbol.
        /// can use <see cref="Ex"/> for short.
        /// </summary>"
        public static readonly R Exclamation = @"\!";

        /// <summary>
        /// ! Exclamation symbol.
        /// short for <see cref="Exclamation"/>.
        /// </summary>"
        public static readonly R Ex = @"\!";

        /// <summary>
        /// ` BackQuote symbol.
        /// can use <see cref="BQu"/> for short.
        /// </summary>"
        public static readonly R BackQuote = @"\`";

        /// <summary>
        /// ` BackQuote symbol.
        /// short for <see cref="BackQuote"/>.
        /// </summary>"
        public static readonly R BQu = @"\`";

        /// <summary>
        /// ~ Tilde symbol.
        /// can use <see cref="Tl"/> for short.
        /// </summary>"
        public static readonly R Tilde = @"\~";

        /// <summary>
        /// ~ Tilde symbol.
        /// short for <see cref="Tilde"/>.
        /// </summary>"
        public static readonly R Tl = @"\~";

        /// <summary>
        /// # Sharp symbol.
        /// can use <see cref="Shp"/> for short.
        /// </summary>"
        public static readonly R Sharp = @"\#";

        /// <summary>
        /// # Sharp symbol.
        /// short for <see cref="Sharp"/>.
        /// </summary>"
        public static readonly R Shp = @"\#";

        /// <summary>
        /// $ Dollar symbol.
        /// can use <see cref="Dlr"/> for short.
        /// </summary>"
        public static readonly R Dollar = @"\$";

        /// <summary>
        /// $ Dollar symbol.
        /// short for <see cref="Dollar"/>.
        /// </summary>"
        public static readonly R Dlr = @"\$";

        /// <summary>
        /// % Percent symbol.
        /// can use <see cref="Prc"/> for short.
        /// </summary>"
        public static readonly R Percent = @"\%";

        /// <summary>
        /// % Percent symbol.
        /// short for <see cref="Percent"/>.
        /// </summary>"
        public static readonly R Prc = @"\%";

        /// <summary>
        /// ^ Caret symbol.
        /// can use <see cref="Ca"/> for short.
        /// </summary>"
        public static readonly R Caret = @"\^";

        /// <summary>
        /// ^ Caret symbol.
        /// short for <see cref="Caret"/>.
        /// </summary>"
        public static readonly R Ca = @"\^";

        /// <summary>
        /// & And symbol.
        /// </summary>"
        public static readonly R And = @"\&";

        /// <summary>
        /// * Asterisk symbol.
        /// can use <see cref="Star"/> for short.
        /// </summary>"
        public static readonly R Asterisk = @"\*";

        /// <summary>
        /// * Asterisk symbol.
        /// short for <see cref="Asterisk"/>.
        /// </summary>"
        public static readonly R Star = @"\*";

        /// <summary>
        /// | Bar symbol.
        /// can use <see cref="Br"/> for short.
        /// </summary>"
        public static readonly R Bar = @"\|";

        /// <summary>
        /// | Bar symbol.
        /// short for <see cref="Bar"/>.
        /// </summary>"
        public static readonly R Br = @"\|";

        /// <summary>
        /// ( Left Parentheses symbol.
        /// can use <see cref="Pr_"/> for short.
        /// </summary>"
        public static readonly R OpenPrth = @"\(";

        /// <summary>
        /// ( Left Parentheses symbol.
        /// short for <see cref="OpenPrth"/>.
        /// </summary>"
        public static readonly R Pr_ = @"\(";

        /// <summary>
        /// ) Right Parentheses symbol.
        /// can use <see cref="Pr"/> for short.
        /// </summary>"
        public static readonly R ClosePrth = @"\)";

        /// <summary>
        /// ) Right Parentheses symbol.
        /// short for <see cref="ClosePrth"/>.
        /// </summary>"
        public static readonly R Pr = @"\)";

        /// <summary>
        /// { Left Braces symbol.
        /// can use <see cref="Bc_"/> for short.
        /// </summary>"
        public static readonly R OpenBrc = @"\{";

        /// <summary>
        /// { Left Braces symbol.
        /// short for <see cref="OpenBrc"/>.
        /// </summary>"
        public static readonly R Bc_ = @"\{";

        /// <summary>
        /// } Right Braces symbol.
        /// can use <see cref="Bc"/> for short.
        /// </summary>"
        public static readonly R CloseBrc = @"\}";

        /// <summary>
        /// } Right Braces symbol.
        /// short for <see cref="CloseBrc"/>.
        /// </summary>"
        public static readonly R Bc = @"\}";

        /// <summary>
        /// < Left Angle Brackets symbol.
        /// can use <see cref="Ab_"/> for short.
        /// </summary>"
        public static readonly R OpenABrk = @"\<";

        /// <summary>
        /// < Left Angle Brackets symbol.
        /// short for <see cref="OpenABrk"/>.
        /// </summary>"
        public static readonly R Ab_ = @"\<";

        /// <summary>
        /// > Right Angle Brackets symbol.
        /// can use <see cref="Ab"/> for short.
        /// </summary>"
        public static readonly R CloseABrk = @"\>";

        /// <summary>
        /// > Right Angle Brackets symbol.
        /// short for <see cref="CloseABrk"/>.
        /// </summary>"
        public static readonly R Ab = @"\>";

        /// <summary>
        /// [ Left Brackets symbol.
        /// can use <see cref="Bk_"/> for short.
        /// </summary>"
        public static readonly R OpenBrk = @"\[";

        /// <summary>
        /// [ Left Brackets symbol.
        /// short for <see cref="OpenBrk"/>.
        /// </summary>"
        public static readonly R Bk_ = @"\[";

        /// <summary>
        /// ] Right Brackets symbol.
        /// can use <see cref="Bk"/> for short.
        /// </summary>"
        public static readonly R CloseBrk = @"\]";

        /// <summary>
        /// ] Right Brackets symbol.
        /// short for <see cref="CloseBrk"/>.
        /// </summary>"
        public static readonly R Bk = @"\]";

        #endregion  Special Single Symbols
        //----------------------------------------------------------------------
        #region  Unicode Catagorys
        public partial struct UnicodeC
        {
            /// <summary>
            /// Lu Upper-case letters.
            /// can use <see cref="Lu"/> for short.
            /// </summary>"
            public R UpperLetter { get { return Cata("Lu"); } }

            /// <summary>
            /// Lu Upper-case letters.
            /// </summary>"
            public R Lu { get { return Cata("Lu"); } }

            /// <summary>
            /// Ll lower-case letters.
            /// can use <see cref="Ll"/> for short.
            /// </summary>"
            public R LowerLetter { get { return Cata("Ll"); } }

            /// <summary>
            /// Ll lower-case letters.
            /// </summary>"
            public R Ll { get { return Cata("Ll"); } }

            /// <summary>
            /// L All Letters.
            /// can use <see cref="L"/> for short.
            /// </summary>"
            public R Letter { get { return Cata("L"); } }

            /// <summary>
            /// L All Letters.
            /// </summary>"
            public R L { get { return Cata("L"); } }

        }
        #endregion  Unicode Catagorys
        //----------------------------------------------------------------------
        #region  Fequentlly Used Sequence
        /// <summary>
        /// \r\n Carriage return and Newline.
        /// can use <see cref="Cn"/> for short.
        /// </summary>"
        public static readonly R CrNewLine = @"\r\n";

        /// <summary>
        /// \r\n Carriage return and Newline.
        /// short for <see cref="CrNewLine"/>.
        /// </summary>"
        public static readonly R Cn = @"\r\n";

        /// <summary>
        /// [^\r\n] Any except new line and Carriage return.
        /// can use <see cref="_"/> for short.
        /// </summary>"
        public static readonly R AnyX = @"[^\r\n]";

        /// <summary>
        /// [^\r\n] Any except new line and Carriage return.
        /// short for <see cref="AnyX"/>.
        /// </summary>"
        public static readonly R _ = @"[^\r\n]";

        /// <summary>
        /// one or more space.
        /// can use <see cref="Sps"/> for short.
        /// </summary>"
        public static readonly R SomeSpace = R.Sp._1n;

        /// <summary>
        /// one or more space.
        /// short for <see cref="SomeSpace"/>.
        /// </summary>"
        public static readonly R Sps = R.Sp._1n;

        /// <summary>
        /// zero or more space.
        /// can use <see cref="Spa"/> for short.
        /// </summary>"
        public static readonly R AnySpace = R.Sp._0n;

        /// <summary>
        /// zero or more space.
        /// short for <see cref="AnySpace"/>.
        /// </summary>"
        public static readonly R Spa = R.Sp._0n;

        /// <summary>
        /// \r?\n Newline with or without Carriage return.
        /// can use <see cref="N"/> for short.
        /// </summary>"
        public static readonly R AnyNewLine = @"\r?\n";

        /// <summary>
        /// \r?\n Newline with or without Carriage return.
        /// short for <see cref="AnyNewLine"/>.
        /// </summary>"
        public static readonly R N = @"\r?\n";

        #endregion  Fequentlly Used Sequence
        //----------------------------------------------------------------------
        #endregion Generated Code
        //----------------------------------------------------------------------

        /// <summary>
        /// \ Special character Escaper
        /// </summary>
        public const string C_ESC = @"\";

        //----------------------------------------------------------------------
        #region Regex control
        //----------------------------------------------------------------------
        #region Groups
        /// <summary>
        /// Indexed group
        /// </summary>
        internal const string INDX_GROUP_START = @"(";// (exp)
        /// <summary>
        /// Named group
        /// </summary>
        internal const string NAME_GROUP_START = @"(?<{0}>";// (?<name>exp)
        /// <summary>
        /// Balancing group
        /// </summary>
        internal const string BLNC_GROUP_START = @"(?<{0}-{1}>";// (?<name1-name2>exp)
        /// <summary>
        /// Noncapturing group
        /// </summary>
        internal const string NOCP_GROUP_START = @"(?:";// (?:exp)
        /// <summary>
        /// Zero-width positive lookahead
        /// </summary>
        internal const string FWD_GROUP = @"(?={0})";// (?=exp)
        /// <summary>
        /// Zero-width negative lookahead
        /// </summary>
        internal const string FWD_N_GROUP = @"(?!{0})";// (?!exp)
        /// <summary>
        /// Zero-width positive lookbehind
        /// </summary>
        internal const string BWD_GROUP = @"(?<={0})";// (?<=exp)
        /// <summary>
        /// Zero-width negative lookbehind
        /// </summary>
        internal const string BWD_N_GROUP = @"(?<!{0})";// (?<!exp)
        /// <summary>
        /// Non-backtracking(greedy) 
        /// </summary>
        internal const string NON_BACK_GROUP_START = @"(?>";// (?>exp)
        /// <summary>
        /// End of any group
        /// </summary>
        internal const string GROUP_END = ")";
        #endregion Groups
        //----------------------------------------------------------------------
        #region Unicode Category
        /// <summary>
        /// In that Unicode category or block
        /// </summary>
        internal const string UNICODE_START = @"\p{";
        /// <summary>
        /// Not in that Unicode category or block
        /// </summary>
        internal const string UNICODE_N_START = @"\P{";
        internal const string UNICODE_END = @"}";
        #endregion Unicode Category
        //----------------------------------------------------------------------
        #region Single Character Set
        /// <summary>
        /// [set] In that set
        /// </summary>
        internal const string SET_START = @"[";
        /// <summary>
        /// [^set] Not in that set
        /// </summary>
        internal const string SET_N_START = @"[^";
        internal const string SET_END = @"]";
        /// <summary>
        /// char1-char2 range
        /// </summary>
        internal const string SET_RANGE = @"-";

        /// <summary>
        /// \c Control Charactor escape
        /// </summary>
        internal const string CTRL_CHAR = @"\c";
        #endregion Single Character Set
        //----------------------------------------------------------------------
        #region Back Reference
        /// <summary>
        /// indexed Back Reference
        /// </summary>
        internal const string BR_INDEX = @"\";
        /// <summary>
        /// named Back Reference
        /// </summary>
        internal const string BR_NAME = @"\k<{0}>";
        #endregion Back Reference
        //----------------------------------------------------------------------
        #region Alternation
        /// <summary>
        /// | Alternation
        /// </summary>
        internal const string OR = @"|";
        /// <summary>
        /// (?(condition) yes | no ) if condition is matched yes otherwise no
        /// condition can be group name / group number / expression / zero-width assertion (start with"?=")
        /// </summary>
        internal const string COND_START = @"(?(";
        internal const string COND_END = @")";
        #endregion Alternation
        //----------------------------------------------------------------------
        #region NONE-ASCII
        /// <summary>
        /// \x hex	2-digit hex character code
        /// </summary>
        internal const string HEX2 = @"\x";
        /// <summary>
        /// \u hex	4-digit hex character code
        /// </summary>
        internal const string HEX4 = @"\u";
        #endregion NONE-ASCII
        //----------------------------------------------------------------------
        #region Quantifiers
        /// <summary>
        /// zero or more times
        /// </summary>
        internal const string ZERO_MORE = @"*";
        /// <summary>
        /// zero or more times lazy
        /// </summary>
        internal const string ZERO_MORE_LAZY = @"*?";
        /// <summary>
        /// one or more times
        /// </summary>
        internal const string ONE_MORE = @"+";
        /// <summary>
        /// one or more times lazy
        /// </summary>
        internal const string ONE_MORE_LAZY = @"+?";
        /// <summary>
        /// zero or one times
        /// </summary>
        internal const string ZERO_ONE = @"?";
        /// <summary>
        /// zero or one times lazy
        /// </summary>
        internal const string ZERO_ONE_LAZY = @"??";
        /// <summary>
        /// exactly or from n to m times start {n} {n}? {n,m} {n,m}?
        /// </summary>
        internal const string TIMES_START = @"{";
        /// <summary>
        /// At least n times {n,} {n,}?
        /// </summary>
        internal const string TIMES_RANGE = @",";
        /// <summary>
        /// exactly or from n to m times end
        /// </summary>
        internal const string TIMES_END = @"}";
        /// <summary>
        /// exactly or from n to m times lazy end
        /// </summary>
        internal const string TIMES_END_LAZY = @"}?";
        #endregion Quantifiers
        //----------------------------------------------------------------------
        #region Inline options
        /// <summary>
        /// Case-insensitive
        /// </summary>
        internal const char IGNOR_CASE = 'i';
        /// <summary>
        /// Multiline mode, ^ $ mach only line beginning and end, dont match string beginning and end
        /// </summary>
        internal const char MUILTI_LINE = 'm';
        /// <summary>
        /// Single-line mode, dot "." match \n
        /// </summary>
        internal const char SINGLE_LINE = 's';
        /// <summary>
        /// Do not capture unnamed groups
        /// </summary>
        internal const char EXPLICIT = 'n';
        /// <summary>
        /// Ignore white space
        /// </summary>
        internal const char IGNOR_SPC = 'x';

        internal const string OPT_START = @"(?";
        internal const string OPT_DISABLE = "-";
        internal const string OPT_EXP_START = @":";
        internal const string OPT_END = @")";
        #endregion Inline options
        //----------------------------------------------------------------------
        #endregion Regex control
        //----------------------------------------------------------------------
    }
}