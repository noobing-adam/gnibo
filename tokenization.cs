
using System.ComponentModel;

public class Tokenizer
{
    private string _src;
    private int index = 0;
    private int line = 1;

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
                    tokens.Add(new Token() { type = TokenType.exit, line = line });
                }
                else if (buf == "let")
                {
                    tokens.Add(new Token() { type = TokenType.let, line = line });
                }
                else if (buf == "string")
                {
                    tokens.Add(new Token() { type = TokenType.string_, line = line });
                }
                else if (buf == "print")
                {
                    tokens.Add(new Token() { type = TokenType.print, line = line });
                }
                else if (buf == "if")
                {
                    tokens.Add(new Token() { type = TokenType.if_, line = line });
                }
                else if (buf == "else")
                {
                    tokens.Add(new Token() { type = TokenType.else_, line = line });
                }
                else if (buf == "while")
                {
                    tokens.Add(new Token() { type = TokenType.while_, line = line });
                }
                else if (buf == "for")
                {
                    tokens.Add(new Token() { type = TokenType.for_, line = line });
                }
                else if (buf == "fn")
                {
                    tokens.Add(new Token() { type = TokenType.fn, line = line });
                }
                else if (buf == "return")
                {
                    tokens.Add(new Token() { type = TokenType.return_, line = line });
                }
                else if (buf == "toString")
                {
                    tokens.Add(new Token() { type = TokenType.toString, line = line });
                }
                else if (buf == "break")
                {
                    tokens.Add(new Token() { type = TokenType.break_, line = line });
                }
                else if (buf == "continue")
                {
                    tokens.Add(new Token() { type = TokenType.continue_, line = line });
                }
                else if (peek() is char c2 && c2 == '(')
                {
                    tokens.Add(new Token() { type = TokenType.fname, value = buf, line = line });
                }
                else
                    tokens.Add(new Token() { type = TokenType.ident, value = buf, line = line });
                buf = "";
            }
            else if (char.IsDigit(c))
            {
                buf += consume();
                while (peek() is char e && char.IsDigit(e))
                {
                    buf += consume();
                }
                tokens.Add(new Token() { type = TokenType.int_lit, value = buf, line = line });
                buf = "";
            }
            else if (c == ';')
            {
                tokens.Add(new Token() { type = TokenType.semi, line = line });
                consume();
            }
            else if (c == ',')
            {
                tokens.Add(new Token() { type = TokenType.comma, line = line });
                consume();
            }
            else if (c == '.')
            {
                tokens.Add(new Token() { type = TokenType.dot, line = line });
                consume();
            }
            else if (c == '(')
            {
                tokens.Add(new Token() { type = TokenType.open_paren, line = line });
                consume();
            }
            else if (c == ')')
            {
                tokens.Add(new Token() { type = TokenType.close_paren, line = line });
                consume();
            }
            else if (c == '=')
            {
                tokens.Add(new Token() { type = TokenType.eq, line = line });
                consume();
            }
            else if (c == '>')
            {
                tokens.Add(new Token() { type = TokenType.gt, line = line });
                consume();
            }
            else if (c == '<')
            {
                tokens.Add(new Token() { type = TokenType.lt, line = line });
                consume();
            }
            else if (c == '+')
            {
                tokens.Add(new Token() { type = TokenType.plus, line = line });
                consume();
            }
            else if (c == '-')
            {
                tokens.Add(new Token() { type = TokenType.minus, line = line });
                consume();
            }
            else if (c == '*')
            {
                tokens.Add(new Token() { type = TokenType.star, line = line });
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
                    int start_line = line;
                    while (peek() is char e && !(e == '*' && peek(1) is char z && z == '/'))
                    {
                        if (e == '\n') line++;
                        consume();
                    }
                    if (peek() is char x && x == '*' && peek(1) is char y && y == '/')
                    {
                        consume();
                        consume();
                    }
                    else
                    {
                        Console.WriteLine("Unterminated comment (add */ at line " + start_line + ")");
                        Environment.Exit(1);
                    }
                }
                else
                {
                    tokens.Add(new Token() { type = TokenType.fslash, line = line });
                }
            }
            else if (c == '{')
            {
                tokens.Add(new Token() { type = TokenType.open_curly, line = line });
                consume();
            }
            else if (c == '}')
            {
                tokens.Add(new Token() { type = TokenType.close_curly, line = line });
                consume();
            }
            else if (c == '\n')
            {
                line++;
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
                    if (d == '\n') line++;
                }
                consume();
                tokens.Add(new Token() { type = TokenType.string_lit, value = buf, line = line });
                buf = "";
            }
            else if (c == '&' && peek() is char c1 && c1 == '&')
            {
                tokens.Add(new Token() { type = TokenType.and, line = line });
                consume();
                consume();
            }
            else if (c == '|' && peek() is char c2 && c2 == '|')
            {
                tokens.Add(new Token() { type = TokenType.or, line = line });
                consume();
                consume();
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