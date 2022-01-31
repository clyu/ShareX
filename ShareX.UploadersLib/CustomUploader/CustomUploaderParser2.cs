﻿#region License Information (GPL v3)

/*
    ShareX - A program that allows you to take screenshots and share any file type
    Copyright (c) 2007-2022 ShareX Team

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

    Optionally you can also view the license at <http://www.gnu.org/licenses/>.
*/

#endregion License Information (GPL v3)

using ShareX.HelpersLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShareX.UploadersLib
{
    public class CustomUploaderParser2
    {
        private static IEnumerable<CustomUploaderFunction> Functions = Helpers.GetInstances<CustomUploaderFunction>();

        public char SyntaxStart { get; private set; } = '{';
        public char SyntaxEnd { get; private set; } = '}';
        public char SyntaxParameterStart { get; private set; } = ':';
        public char SyntaxParameterDelimiter { get; private set; } = '|';
        public char SyntaxEscape { get; private set; } = '\\';

        public string FileName { get; set; }
        public string Input { get; set; }
        public ResponseInfo ResponseInfo { get; set; }
        public List<string> RegexList { get; set; }
        public bool URLEncode { get; set; } // Only URL encodes file name and input

        public string Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return "";
            }

            return ParseSyntax(text, 0, out _);
        }

        private string ParseSyntax(string text, int startPosition, out int endPosition)
        {
            StringBuilder sbResult = new StringBuilder();
            bool escape = false;
            int i;

            for (i = startPosition; i < text.Length; i++)
            {
                if (!escape)
                {
                    if (text[i] == SyntaxStart)
                    {
                        string parsed = ParseFunction(text, i + 1, out i);
                        sbResult.Append(parsed);
                        continue;
                    }
                    else if (text[i] == SyntaxEnd || text[i] == SyntaxParameterDelimiter)
                    {
                        break;
                    }
                    else if (text[i] == SyntaxEscape)
                    {
                        escape = true;
                        continue;
                    }
                }

                escape = false;
                sbResult.Append(text[i]);
            }

            endPosition = i;
            return sbResult.ToString();
        }

        private string ParseFunction(string text, int startPosition, out int endPosition)
        {
            StringBuilder sbFunctionName = new StringBuilder();
            bool parsingFunctionName = true;
            List<string> parameters = new List<string>();
            bool escape = false;
            int i;

            for (i = startPosition; i < text.Length; i++)
            {
                if (!escape)
                {
                    if (text[i] == SyntaxEnd)
                    {
                        break;
                    }
                    else if (text[i] == SyntaxEscape)
                    {
                        escape = true;
                        continue;
                    }

                    if (parsingFunctionName)
                    {
                        if (text[i] == SyntaxParameterStart)
                        {
                            parsingFunctionName = false;
                            continue;
                        }

                        sbFunctionName.Append(text[i]);
                    }
                    else
                    {
                        string parsed = ParseSyntax(text, i, out i);
                        parameters.Add(parsed);
                    }
                }

                escape = false;
            }

            endPosition = i;
            return CallFunction(sbFunctionName.ToString(), parameters.ToArray());
        }

        private string CallFunction(string functionName, string[] parameters)
        {
            foreach (CustomUploaderFunction function in Functions)
            {
                if (function.Name.Equals(functionName, StringComparison.OrdinalIgnoreCase))
                {
                    return function.Call(this, parameters);
                }
            }

            throw new Exception("Invalid function name: " + functionName);
        }
    }
}