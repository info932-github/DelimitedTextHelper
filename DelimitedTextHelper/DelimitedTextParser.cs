using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DelimitedTextHelper
{
    public class DelimitedTextParser : IDisposable
    {
        private TextReader _reader;
        private char _delimiter;        

        private const string QUOTE = "\"";
        private const string ESCAPED_QUOTE = "\"\"";
        private char[] CHARACTERS_THAT_MUST_BE_QUOTED;
        private bool _skipComments = false;
        public DelimitedTextParser(TextReader reader):this(reader, ',')
        {

        }

        public DelimitedTextParser(TextReader reader, char delimiter):this(reader, delimiter, false)
        {

        }

        public DelimitedTextParser(TextReader reader, char delimiter, bool skipComments)
        {
            _delimiter = delimiter;
            _reader = reader;
            CHARACTERS_THAT_MUST_BE_QUOTED = new char[]{ _delimiter, '"', '\n' };
            _skipComments = skipComments;
        }

        public virtual string[] Read()
        {
            try
            {
                var row = ReadLine();
                
                return row;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        protected virtual string[] ReadLine()
        {
            try
            {
                string[] record = null;
                if (_reader != null)
                {
                    bool done = false;
                    while (!done)
                    {
                        var row = _reader.ReadLine();
                        if (_skipComments && row.StartsWith("#"))
                        {
                            continue;
                        }
                        if (!string.IsNullOrEmpty(row))
                        {
                            return record = GetRow(row);
                        }
                        else
                        {
                            return null;
                        }
                    }
                }

                return record;
            }
            catch (Exception)
            {
                throw;
            }
        }
        public string[] GetRow(string csvText)
        {
            List<string> tokens = new List<string>();

            int last = -1;
            int current = 0;
            bool inText = false;

            while (current < csvText.Length)
            {
                if(csvText[current] == '"')
                {
                    inText = !inText;
                }
                else if (csvText[current] == _delimiter)
                {
                    if (!inText)
                    {
                        tokens.Add(Unescape(csvText.Substring(last + 1, (current - last)).Trim(_delimiter)));
                        last = current;
                    }
                }
                current++;
            }

            if (last != csvText.Length - 1)
            {
                tokens.Add(Unescape(csvText.Substring(last + 1)));
            }

            return tokens.ToArray();
        }

        public string Escape(string s)
        {
            if (s.Contains(QUOTE))
                s = s.Replace(QUOTE, ESCAPED_QUOTE);

            if (s.IndexOfAny(CHARACTERS_THAT_MUST_BE_QUOTED) > -1)
                s = QUOTE + s + QUOTE;

            return s;
        }

        public string Unescape(string s)
        {
            if (s.StartsWith(QUOTE) && s.EndsWith(QUOTE))
            {
                s = s.Substring(1, s.Length - 2);

                if (s.Contains(ESCAPED_QUOTE))
                    s = s.Replace(ESCAPED_QUOTE, QUOTE);
            }

            return s;
        }

        

        public void Dispose()
        {
            if(_reader != null)
            {
                _reader = null;
            }
        }
    }

}
