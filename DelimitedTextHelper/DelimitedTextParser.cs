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
        private static Regex rexCsvSplitter = new Regex(@",(?=(?:[^""]*""[^""]*"")*(?![^""]*""))");
        private static Regex rexRunOnLine = new Regex(@"^[^""]*(?:""[^""]*""[^""]*)*""[^""]*$");

        private const string QUOTE = "\"";
        private const string ESCAPED_QUOTE = "\"\"";
        private char[] CHARACTERS_THAT_MUST_BE_QUOTED = { ',', '"', '\n' };

        public DelimitedTextParser(TextReader reader):this(reader, ',')
        {

        }

        public DelimitedTextParser(TextReader reader, char delimiter)
        {
            _delimiter = delimiter;
            _reader = reader;
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
                    var row = _reader.ReadLine();
                    if (!string.IsNullOrEmpty(row))
                    {
                        record = GetRow(row);
                    }
                }

                return record;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public IEnumerable<string> SplitRow(string row, char delimiter = ',')
        {
            var currentString = new StringBuilder();
            var inQuotes = false;
            var quoteIsEscaped = false; //Store when a quote has been escaped.
            row = string.Format("{0}{1}", row, delimiter); //We add new cells at the delimiter, so append one for the parser.
            foreach (var character in row.ToCharArray().Select((val, index) => new { val, index }))
            {
                if (character.val == delimiter) //We hit a delimiter character...
                {
                    if (!inQuotes) //Are we inside quotes? If not, we've hit the end of a cell value.
                    {
                        //Console.WriteLine(currentString);
                        yield return currentString.ToString();
                        currentString.Clear();
                    }
                    else
                    {
                        currentString.Append(character.val);
                    }
                }
                else
                {
                    if (character.val != ' ')
                    {
                        if (character.val == '"') //If we've hit a quote character...
                        {
                            if (character.val == '"' && inQuotes) //Does it appear to be a closing quote?
                            {
                                if (row[character.index + 1] == character.val && !quoteIsEscaped) //If the character afterwards is also a quote, this is to escape that (not a closing quote).
                                {
                                    quoteIsEscaped = true; //Flag that we are escaped for the next character. Don't add the escaping quote.
                                }
                                else if (quoteIsEscaped)
                                {
                                    quoteIsEscaped = false; //This is an escaped quote. Add it and revert quoteIsEscaped to false.
                                    currentString.Append(character.val);
                                }
                                else
                                {
                                    inQuotes = false;
                                }
                            }
                            else
                            {
                                if (!inQuotes)
                                {
                                    inQuotes = true;
                                }
                                else
                                {
                                    currentString.Append(character.val); //...It's a quote inside a quote.
                                }
                            }
                        }
                        else
                        {
                            currentString.Append(character.val);
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(currentString.ToString())) //Append only if not new cell
                        {
                            currentString.Append(character.val);
                        }
                    }
                }
            }
        }

        //public string[] GetRow(string sLine)
        //{


        //        string[] values = rexCsvSplitter.Split(sLine);

        //    for (int i = 0; i < values.Length; i++)
        //    {
        //        values[i] = Unescape(values[i]);
        //    }

        //    return values;
        //}

        public string[] GetRow(string csvText)
        {
            List<string> tokens = new List<string>();

            int last = -1;
            int current = 0;
            bool inText = false;

            while (current < csvText.Length)
            {
                switch (csvText[current])
                {
                    case '"':
                        inText = !inText; break;
                    case ',':
                        if (!inText)
                        {
                            tokens.Add(Unescape(csvText.Substring(last + 1, (current - last)).Trim(',')));
                            last = current;
                        }
                        break;
                    default:
                        break;
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
