using System.Text;

namespace Core.Extensions
{
    internal enum TokenType
    {
        Comma,
        Quote,
        Value
    }

    internal class CsvToken
    {
        public CsvToken(TokenType type, string value)
        {
            Value = value;
            Type = type;
        }

        public String Value { get; private set; }
        public TokenType Type { get; private set; }
    }

    internal class StreamTokenizer : IEnumerable<CsvToken>
    {
        private readonly TextReader _reader;

        public StreamTokenizer(TextReader reader)
        {
            _reader = reader;
        }

        public IEnumerator<CsvToken> GetEnumerator()
        {
            string line;
            var value = new StringBuilder();

            while ((line = _reader.ReadLine()) != null)
            {
                foreach (var c in line)
                {
                    switch (c)
                    {
                        case '\'':
                        case '"':
                            if (value.Length > 0)
                            {
                                yield return new CsvToken(TokenType.Value, value.ToString());
                                value.Length = 0;
                            }
                            yield return new CsvToken(TokenType.Quote, c.ToString());
                            break;
                        case ',':
                            if (value.Length > 0)
                            {
                                yield return new CsvToken(TokenType.Value, value.ToString());
                                value.Length = 0;
                            }
                            yield return new CsvToken(TokenType.Comma, c.ToString());
                            break;
                        default:
                            value.Append(c);
                            break;
                    }
                }

                // Thanks, dpan
                if (value.Length > 0)
                {
                    yield return new CsvToken(TokenType.Value, value.ToString());
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public class CsvParser : IEnumerable<string>
    {
        private readonly StreamTokenizer _tokenizer;

        public CsvParser(Stream data)
        {
            _tokenizer = new StreamTokenizer(new StreamReader(data));
        }

        public CsvParser(String data)
        {
            _tokenizer = new StreamTokenizer(new StringReader(data));
        }

        public IEnumerator<string> GetEnumerator()
        {
            var inQuote = false;
            var result = new StringBuilder();

            foreach (CsvToken token in _tokenizer)
            {
                switch (token.Type)
                {
                    case TokenType.Comma:
                        if (inQuote)
                        {
                            result.Append(token.Value);
                        }
                        else
                        {
                            yield return result.ToString();
                            result.Length = 0;
                        }
                        break;
                    case TokenType.Quote:
                        // Toggle quote state
                        inQuote = !inQuote;
                        break;
                    case TokenType.Value:
                        result.Append(token.Value);
                        break;
                    default:
                        throw new InvalidOperationException("Unknown token type: " + token.Type);
                }
            }

            if (result.Length > 0)
            {
                yield return result.ToString();
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
