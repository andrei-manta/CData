namespace CDataTranspiler;

public sealed class TokenStream
{
    public List<Token> Tokens { get; } = new(256);
    public string Source { get; }
    public string? Filename { get; }

    public TokenStream(string source, string? filename)
    {
        Source = source;
        Filename = filename;
    }

    public int Count => Tokens.Count;
    public Token this[int i] => Tokens[i];
}

public sealed class Lexer
{
    readonly string src;
    readonly string? file;

    int pos;
    int line = 1;
    int col  = 1;
    bool atLineStart = true;

    public int ErrorCount { get; private set; }

    public Lexer(string source, string? filename = null)
    {
        src  = source;
        file = filename;
    }

    public TokenStream Run()
    {
        var ts = new TokenStream(src, file);

        while (true)
        {
            var t = NextToken();
            if (t.Kind != TokenKind.LineComment &&
                t.Kind != TokenKind.BlockComment &&
                t.Kind != TokenKind.Eof)
            {
                atLineStart = false;
            }
            ts.Tokens.Add(t);
            if (t.Kind == TokenKind.Eof) break;
        }

        return ts;
    }

    bool AtEnd => pos >= src.Length;

    char Peek(int offset = 0)
    {
        int p = pos + offset;
        return p < src.Length ? src[p] : '\0';
    }

    void Advance()
    {
        char c = src[pos];
        if (c == '\n') { line++; col = 1; atLineStart = true; }
        else           { col++; }
        pos++;
    }

    bool Match(char expected)
    {
        if (AtEnd || src[pos] != expected) return false;
        Advance();
        return true;
    }

    void Report(int errLine, int errCol, string msg)
    {
        var name = file ?? "<input>";
        Console.Error.WriteLine($"{name}:{errLine}:{errCol}: error: {msg}");
        ErrorCount++;
    }

    static Token Mk(TokenKind k, int start, int length, int line, int col) =>
        new(k, start, length, line, col);

    static bool IsDigit(char c) => c >= '0' && c <= '9';
    static bool IsHex(char c)   => IsDigit(c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
    static bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || c == '_';
    static bool IsAlnum(char c) => IsAlpha(c) || IsDigit(c);

    void SkipWhitespace()
    {
        while (!AtEnd)
        {
            char c = src[pos];
            if (c == ' ' || c == '\t' || c == '\r' || c == '\n') Advance();
            else break;
        }
    }

    Token NextToken()
    {
        SkipWhitespace();
        if (AtEnd) return Mk(TokenKind.Eof, pos, 0, line, col);

        int start = pos;
        int sLine = line;
        int sCol  = col;
        char c    = src[pos];

        if (c == '#')
        {
            if (atLineStart) return LexIncludeOrError();
            Advance();
            Report(sLine, sCol, "stray '#' (only '#include' is allowed, at line start)");
            return Mk(TokenKind.Error, start, 1, sLine, sCol);
        }

        if (IsAlpha(c)) return LexIdentOrKeyword();
        if (IsDigit(c)) return LexNumber();
        if (c == '.' && IsDigit(Peek(1))) return LexNumber();
        if (c == '"')  return LexString();
        if (c == '\'') return LexChar();

        Advance();

        switch (c)
        {
            case '(': return Mk(TokenKind.LParen,    start, 1, sLine, sCol);
            case ')': return Mk(TokenKind.RParen,    start, 1, sLine, sCol);
            case '{': return Mk(TokenKind.LBrace,    start, 1, sLine, sCol);
            case '}': return Mk(TokenKind.RBrace,    start, 1, sLine, sCol);
            case '[': return Mk(TokenKind.LBracket,  start, 1, sLine, sCol);
            case ']': return Mk(TokenKind.RBracket,  start, 1, sLine, sCol);
            case ';': return Mk(TokenKind.Semicolon, start, 1, sLine, sCol);
            case ',': return Mk(TokenKind.Comma,     start, 1, sLine, sCol);
            case '.': return Mk(TokenKind.Dot,       start, 1, sLine, sCol);
            case '~': return Mk(TokenKind.Tilde,     start, 1, sLine, sCol);

            case '+':
                if (Match('=')) return Mk(TokenKind.PlusAssign, start, 2, sLine, sCol);
                if (Match('+')) return Mk(TokenKind.PlusPlus,   start, 2, sLine, sCol);
                return Mk(TokenKind.Plus, start, 1, sLine, sCol);

            case '-':
                if (Match('=')) return Mk(TokenKind.MinusAssign, start, 2, sLine, sCol);
                if (Match('-')) return Mk(TokenKind.MinusMinus,  start, 2, sLine, sCol);
                if (Match('>')) return Mk(TokenKind.Arrow,       start, 2, sLine, sCol);
                return Mk(TokenKind.Minus, start, 1, sLine, sCol);

            case '*':
                if (Match('=')) return Mk(TokenKind.StarAssign, start, 2, sLine, sCol);
                return Mk(TokenKind.Star, start, 1, sLine, sCol);

            case '/':
                if (Peek() == '/') { pos--; col--; return LexLineComment();  }
                if (Peek() == '*') { pos--; col--; return LexBlockComment(); }
                if (Match('='))    return Mk(TokenKind.SlashAssign, start, 2, sLine, sCol);
                return Mk(TokenKind.Slash, start, 1, sLine, sCol);

            case '%': return Mk(TokenKind.Percent, start, 1, sLine, sCol);

            case '=':
                if (Match('=')) return Mk(TokenKind.Eq, start, 2, sLine, sCol);
                return Mk(TokenKind.Assign, start, 1, sLine, sCol);

            case '!':
                if (Match('=')) return Mk(TokenKind.Neq, start, 2, sLine, sCol);
                return Mk(TokenKind.Bang, start, 1, sLine, sCol);

            case '<':
                if (Match('=')) return Mk(TokenKind.Leq,    start, 2, sLine, sCol);
                if (Match('<')) return Mk(TokenKind.LShift, start, 2, sLine, sCol);
                return Mk(TokenKind.Lt, start, 1, sLine, sCol);

            case '>':
                if (Match('=')) return Mk(TokenKind.Geq,    start, 2, sLine, sCol);
                if (Match('>')) return Mk(TokenKind.RShift, start, 2, sLine, sCol);
                return Mk(TokenKind.Gt, start, 1, sLine, sCol);

            case '&':
                if (Match('&')) return Mk(TokenKind.AmpAmp, start, 2, sLine, sCol);
                return Mk(TokenKind.Amp, start, 1, sLine, sCol);

            case '|':
                if (Match('|')) return Mk(TokenKind.PipePipe, start, 2, sLine, sCol);
                return Mk(TokenKind.Pipe, start, 1, sLine, sCol);

            case '^': return Mk(TokenKind.Caret, start, 1, sLine, sCol);
            case '@': return Mk(TokenKind.At,    start, 1, sLine, sCol);

            default:
            {
                var msg = (c >= 32 && c < 127)
                    ? $"unexpected character '{c}' (0x{(int)c:x2})"
                    : $"unexpected character (0x{(int)c:x2})";
                Report(sLine, sCol, msg);
                return Mk(TokenKind.Error, start, 1, sLine, sCol);
            }
        }
    }

    Token LexLineComment()
    {
        int start = pos, sLine = line, sCol = col;
        Advance(); Advance();
        while (!AtEnd && src[pos] != '\n') Advance();

        int length = pos - start;
        // trim trailing \r so crlf files don't mess up the lexeme
        if (length > 0 && src[start + length - 1] == '\r') length--;
        return Mk(TokenKind.LineComment, start, length, sLine, sCol);
    }

    Token LexBlockComment()
    {
        int start = pos, sLine = line, sCol = col;
        Advance(); Advance();
        while (!AtEnd)
        {
            if (src[pos] == '*' && Peek(1) == '/') {
                Advance(); Advance();
                return Mk(TokenKind.BlockComment, start, pos - start, sLine, sCol);
            }
            Advance();
        }
        Report(sLine, sCol, "unterminated block comment");
        return Mk(TokenKind.Error, start, pos - start, sLine, sCol);
    }

    static readonly Dictionary<string, TokenKind> Keywords = new()
    {
        { "true",     TokenKind.TrueLit    },
        { "false",    TokenKind.FalseLit   },
        { "nullptr",  TokenKind.NullptrLit },

        { "void",     TokenKind.KwVoid     },
        { "char",     TokenKind.KwChar     },
        { "short",    TokenKind.KwShort    },
        { "int",      TokenKind.KwInt      },
        { "long",     TokenKind.KwLong     },
        { "float",    TokenKind.KwFloat    },
        { "double",   TokenKind.KwDouble   },
        { "signed",   TokenKind.KwSigned   },
        { "unsigned", TokenKind.KwUnsigned },
        { "bool",     TokenKind.KwBool     },

        { "typedef",  TokenKind.KwTypedef  },

        { "struct",   TokenKind.KwStruct   },
        { "union",    TokenKind.KwUnion    },
        { "enum",     TokenKind.KwEnum     },
        { "soa",      TokenKind.KwSoa      },
        { "aos",      TokenKind.KwAos      },

        { "if",       TokenKind.KwIf       },
        { "else",     TokenKind.KwElse     },
        { "while",    TokenKind.KwWhile    },
        { "do",       TokenKind.KwDo       },
        { "for",      TokenKind.KwFor      },
        { "break",    TokenKind.KwBreak    },
        { "continue", TokenKind.KwContinue },
        { "return",   TokenKind.KwReturn   },

        { "sizeof",   TokenKind.KwSizeof   },
    };

    Token LexIdentOrKeyword()
    {
        int start = pos, sLine = line, sCol = col;
        Advance();
        while (!AtEnd && IsAlnum(src[pos])) Advance();

        var lexeme = src.AsSpan(start, pos - start);
        // span -> string alloc is fine at this scale
        var kind = Keywords.TryGetValue(lexeme.ToString(), out var k) ? k : TokenKind.Ident;
        return Mk(kind, start, pos - start, sLine, sCol);
    }

    Token LexNumber()
    {
        int start = pos, sLine = line, sCol = col;

        // hex or binary prefix
        if (src[pos] == '0' && pos + 1 < src.Length)
        {
            char n = src[pos + 1];
            if (n == 'x' || n == 'X')
            {
                Advance(); Advance();
                int hexStart = pos;
                while (!AtEnd && IsHex(src[pos])) Advance();
                if (pos == hexStart)
                {
                    Report(sLine, sCol, "hex literal has no digits after '0x'");
                    return Mk(TokenKind.Error, start, pos - start, sLine, sCol);
                }
                ConsumeIntSuffix();
                return Mk(TokenKind.IntLit, start, pos - start, sLine, sCol);
            }
            if (n == 'b' || n == 'B')
            {
                Advance(); Advance();
                int binStart = pos;
                while (!AtEnd && (src[pos] == '0' || src[pos] == '1')) Advance();
                if (pos == binStart)
                {
                    Report(sLine, sCol, "binary literal has no digits after '0b'");
                    return Mk(TokenKind.Error, start, pos - start, sLine, sCol);
                }
                ConsumeIntSuffix();
                return Mk(TokenKind.IntLit, start, pos - start, sLine, sCol);
            }
        }

        bool isFloat = false;

        if (!AtEnd && src[pos] != '.')
            while (!AtEnd && IsDigit(src[pos])) Advance();

        if (!AtEnd && src[pos] == '.')
        {
            isFloat = true;
            Advance();
            while (!AtEnd && IsDigit(src[pos])) Advance();
        }

        if (!AtEnd && (src[pos] == 'e' || src[pos] == 'E'))
        {
            isFloat = true;
            Advance();
            if (!AtEnd && (src[pos] == '+' || src[pos] == '-')) Advance();
            int expStart = pos;
            while (!AtEnd && IsDigit(src[pos])) Advance();
            if (pos == expStart)
            {
                Report(sLine, sCol, "float exponent has no digits");
                return Mk(TokenKind.Error, start, pos - start, sLine, sCol);
            }
        }

        if (isFloat) { ConsumeFloatSuffix(); return Mk(TokenKind.FloatLit, start, pos - start, sLine, sCol); }
        ConsumeIntSuffix();
        return Mk(TokenKind.IntLit, start, pos - start, sLine, sCol);
    }

    void ConsumeIntSuffix()
    {
        while (!AtEnd)
        {
            char c = src[pos];
            if (c == 'u' || c == 'U' || c == 'l' || c == 'L') Advance();
            else break;
        }
    }

    void ConsumeFloatSuffix()
    {
        if (AtEnd) return;
        char c = src[pos];
        if (c == 'f' || c == 'F' || c == 'l' || c == 'L') Advance();
    }

    bool ScanQuotedBody(char quote)
    {
        while (!AtEnd)
        {
            char c = src[pos];
            if (c == quote)  { Advance(); return true; }
            if (c == '\n')   return false;
            if (c == '\\')
            {
                Advance();
                if (AtEnd) return false;
                char esc = src[pos];
                Advance();
                // eat \r\n as a single line continuation
                if (esc == '\r' && !AtEnd && src[pos] == '\n') Advance();
                continue;
            }
            Advance();
        }
        return false;
    }

    Token LexString()
    {
        int start = pos, sLine = line, sCol = col;
        Advance();
        if (!ScanQuotedBody('"'))
        {
            Report(sLine, sCol, "unterminated string literal");
            return Mk(TokenKind.Error, start, pos - start, sLine, sCol);
        }
        return Mk(TokenKind.StringLit, start, pos - start, sLine, sCol);
    }

    Token LexChar()
    {
        int start = pos, sLine = line, sCol = col;
        Advance();
        if (!ScanQuotedBody('\''))
        {
            Report(sLine, sCol, "unterminated character literal");
            return Mk(TokenKind.Error, start, pos - start, sLine, sCol);
        }
        return Mk(TokenKind.CharLit, start, pos - start, sLine, sCol);
    }

    void SkipHorizontalWs()
    {
        while (!AtEnd)
        {
            char c = src[pos];
            if (c == ' ' || c == '\t' || c == '\r') Advance();
            else break;
        }
    }

    Token LexIncludeOrError()
    {
        int start = pos, sLine = line, sCol = col;

        Advance(); // eat '#'
        SkipHorizontalWs();

        const string kw = "include";

        bool matches =
            src.Length - pos >= kw.Length &&
            string.CompareOrdinal(src, pos, kw, 0, kw.Length) == 0 &&
            !(pos + kw.Length < src.Length && IsAlnum(src[pos + kw.Length]));

        if (!matches)
        {
            Report(sLine, sCol, "only '#include' is allowed; CData has no preprocessor");
            while (!AtEnd && src[pos] != '\n') Advance();
            int len = pos - start;
            if (len > 0 && src[start + len - 1] == '\r') len--;
            return Mk(TokenKind.Error, start, len, sLine, sCol);
        }

        for (int i = 0; i < kw.Length; i++) Advance();
        SkipHorizontalWs();

        if (AtEnd)
        {
            Report(sLine, sCol, "'#include' missing header name");
            return Mk(TokenKind.Error, start, pos - start, sLine, sCol);
        }

        char open = src[pos];
        char close;
        if      (open == '<') close = '>';
        else if (open == '"') close = '"';
        else
        {
            Report(sLine, sCol, "'#include' must be followed by '<...>' or \"...\"");
            while (!AtEnd && src[pos] != '\n') Advance();
            int len = pos - start;
            if (len > 0 && src[start + len - 1] == '\r') len--;
            return Mk(TokenKind.Error, start, len, sLine, sCol);
        }

        Advance();
        while (!AtEnd)
        {
            char c = src[pos];
            if (c == '\n')
            {
                Report(sLine, sCol, "unterminated header name in '#include'");
                int len = pos - start;
                if (len > 0 && src[start + len - 1] == '\r') len--;
                return Mk(TokenKind.Error, start, len, sLine, sCol);
            }
            if (c == close) { Advance(); return Mk(TokenKind.Include, start, pos - start, sLine, sCol); }
            Advance();
        }

        Report(sLine, sCol, "unterminated header name in '#include'");
        return Mk(TokenKind.Error, start, pos - start, sLine, sCol);
    }
}
