
public class Tokenizer
{
    private string _src;
    private int index = 0;

    public Tokenizer(string src)
    {
        _src = src;
    }

    public List<Token> tokenize()
    {

        var tokens = new List<Token>();
        string buf = "";
        while (peek() is char c)
        {
            if (char.IsLetter(c) || c == '_')
            {
                buf += consume();
                while (peek() is char d && (char.IsLetterOrDigit(d) || d == '_'))
                {
                    buf += consume();
                }

                if (buf == "exit")
                {
                    tokens.Add(new Token() { type = TokenType.exit });
                }
                else if (buf == "let")
                {
                    tokens.Add(new Token() { type = TokenType.let });
                }
                else if (buf == "print")
                {
                    tokens.Add(new Token() { type = TokenType.print });
                }
                else if (buf == "if")
                {
                    tokens.Add(new Token() { type = TokenType.if_ });
                }
                else if (buf == "else")
                {
                    tokens.Add(new Token() { type = TokenType.else_ });
                }
                else
                    tokens.Add(new Token() { type = TokenType.ident, value = buf });
                buf = "";
            }
            else if (char.IsDigit(c))
            {
                buf += consume();
                while (peek() is char e && char.IsDigit(e))
                {
                    buf += consume();
                }
                tokens.Add(new Token() { type = TokenType.int_lit, value = buf });
                buf = "";
            }
            else if (c == ';')
            {
                tokens.Add(new Token() { type = TokenType.semi });
                consume();
            }
            else if (c == '(')
            {
                tokens.Add(new Token() { type = TokenType.open_paren });
                consume();
            }
            else if (c == ')')
            {
                tokens.Add(new Token() { type = TokenType.close_paren });
                consume();
            }
            else if (c == '=')
            {
                tokens.Add(new Token() { type = TokenType.eq });
                consume();
            }
            else if (c == '+')
            {
                tokens.Add(new Token() { type = TokenType.plus });
                consume();
            }
            else if (c == '-')
            {
                tokens.Add(new Token() { type = TokenType.minus });
                consume();
            }
            else if (c == '*')
            {
                tokens.Add(new Token() { type = TokenType.star });
                consume();
            }
            else if (c == '/')
            {
                consume();
                var d = peek();
                if (d == '/')
                {
                    consume();
                    while (peek() is char e && e != '\n')
                    {
                        consume();
                    }
                }
                else if (d == '*')
                {
                    consume();
                    while (peek() is char e && !(e == '*' && peek(1) is char z && z == '/'))
                    {
                        consume();
                    }
                    if (peek() is char x && x == '*' && peek(1) is char y && y == '/')
                    {
                        consume();
                        consume();
                    }
                    else
                    {
                        Console.WriteLine("Unterminated comment (add */ at the end)");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    tokens.Add(new Token() { type = TokenType.fslash });
                    consume();
                }
            }
            else if (c == '{')
            {
                tokens.Add(new Token() { type = TokenType.open_curly });
                consume();
            }
            else if (c == '}')
            {
                tokens.Add(new Token() { type = TokenType.close_curly });
                consume();
            }
            else if (char.IsWhiteSpace(c))
            {
                consume();
            }
            else if (c == '"')
            {
                consume();
                while (peek() is char d && d != '"')
                {
                    buf += consume();
                }
                consume();
                tokens.Add(new Token() { type = TokenType.string_lit, value = buf });
                buf = "";
            }
            else
            {
                Console.WriteLine("Unknown character: " + c);
                consume();
            }
        }

        return tokens;
    }


    private char? peek(int offset = 0)
    {
        if (index + offset < _src.Length)
        {
            return _src[index + offset];
        }
        else
        {
            return null;
        }
    }

    private char consume()
    {
        return _src[index++];
    }

    public static int? bin_prec(TokenType type)
    {
        if (type == TokenType.plus || type == TokenType.minus) return 0;
        else if (type == TokenType.star || type == TokenType.fslash) return 1;
        else return null;
    }
}
