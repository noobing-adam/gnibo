using OneOf;

public class Parser
{

    Dictionary<TokenType, string> tm = new Dictionary<TokenType, string>()
    {
        {TokenType.semi, "';'"},
        {TokenType.open_paren, "'('"},
        {TokenType.close_paren, "')'"},
        {TokenType.open_curly, "'{'"},
        {TokenType.close_curly, "'}'"},
        {TokenType.eq, "'='"}
    };

    private List<Token> _tokens;
    private int index = 0;


    public class NodeProg
    {
        public List<NodeStmt> stmts = new List<NodeStmt>();
    }

    public class NodeExprIntLit
    {
        public Token int_lit;
    }


    public class NodeTerm
    {
        public OneOf<NodeTermIntLit, NodeTermIdent, NodeTermParen> var;
    }

    public class NodeTermIntLit
    {
        public Token int_lit;
    }

    public class NodeTermIdent
    {
        public Token ident;
    }

    public class NodeTermParen
    {
        public required NodeExpr expr;
    }

    public class NodeBinExpr
    {
        public OneOf<NodeBinExprAdd?, NodeBinExprMulti?, NodeBinExprSub?, NodeBinExprDiv?> var;
    }

    public class NodeBinExprAdd
    {
        public NodeExpr? lhs;
        public NodeExpr? rhs;
    }

    public class NodeBinExprSub
    {
        public NodeExpr? lhs;
        public NodeExpr? rhs;
    }


    public class NodeBinExprMulti
    {
        public NodeExpr? lhs;
        public NodeExpr? rhs;
    }

    public class NodeBinExprDiv
    {
        public NodeExpr? lhs;
        public NodeExpr? rhs;
    }

    public class NodeStmt
    {
        public OneOf<NodeStmtExit, NodeStmtLet, NodeScope, NodeStmtPrint, NodeStmtIf, NodeStmtAssign, NodeStmtWhile, NodeStmtFor> var;
    }

    public class NodeStmtAssign
    {
        public Token ident;
        public required NodeExpr expr;
    }

    public class NodeStmtExit
    {
        public NodeExpr? expr;
    }

    public class NodeStmtPrint
    {
        public Token str;
    }

    public class NodeStmtLet
    {
        public Token ident;
        public NodeExpr? expr;
    }

    public class NodeStmtIf
    {
        public required NodeCheck expr;
        public required NodeStmt stmt;
        public NodeIfPred? pred;
    }

    public class NodeStmtWhile
    {
        public required NodeCheck expr;
        public required NodeStmt stmt;
    }

    public class NodeStmtFor
    {
        public required NodeStmt let;
        public required NodeCheck check;
        public required NodeStmt assign;
        public required NodeStmt stmt;
    }

    public class NodeIfPred
    {
        public NodeStmtIf? if_;
        public NodeStmt? stmt;
    }

    public class NodeScope
    {
        public List<NodeStmt>? stmts;
    }

    public class NodeExprIdent
    {
        public Token ident;
    }

    public class NodeExpr
    {
        public OneOf<NodeTerm, NodeBinExpr> var;
    }

    public class NodeCheck
    {
        public required NodeExpr lhs;
        public NodeExpr? rhs;
        public string? op;
    }
    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
    }


    public NodeTerm? parse_term()
    {
        if (try_consume(TokenType.int_lit) is Token token)
        {
            return new NodeTerm() { var = new NodeTermIntLit { int_lit = token } };
        }
        else if (try_consume(TokenType.ident) is Token token2)
        {
            return new NodeTerm() { var = new NodeTermIdent { ident = token2 } };
        }
        else if (try_consume(TokenType.open_paren) is Token t2)
        {
            var expr = parse_expr();
            if (expr == null)
            {
                Console.WriteLine("Expected expression inside the parentheses on line " + t2.line);
                Environment.Exit(1);
                return null;
            }
            try_consume_err(TokenType.close_paren);
            NodeTerm term = new NodeTerm() { var = new NodeTermParen() { expr = expr } };
            return term;
        }
        else
        {
            return null;
        }

    }

    NodeExpr? parse_expr(int min_prec = 0)
    {
        NodeTerm? term_lhs = parse_term();
        if (term_lhs == null) return null;
        NodeExpr expr_lhs = new NodeExpr() { var = term_lhs };
        while (true)
        {
            Token? curr_tok = peek();
            if (!curr_tok.HasValue) break;
            if (Tokenizer.bin_prec(curr_tok.Value.type) is int prec)
            {
                if (prec < min_prec) break;
            }
            else
            {
                break;
            }
            Token op = consume();
            int next_min_prec = prec + 1;
            var expr_rhs = parse_expr(next_min_prec);
            if (expr_rhs == null)
            {
                Console.WriteLine("Expected integer or identifier after operator on line " + op.line);
                Environment.Exit(1);
            }
            NodeBinExpr expr = new NodeBinExpr();
            NodeExpr expr_lhs2 = new NodeExpr();
            if (op.type == TokenType.plus)
            {
                NodeBinExprAdd add = new NodeBinExprAdd();
                expr_lhs2.var = expr_lhs.var;
                add.lhs = expr_lhs2;
                add.rhs = expr_rhs;
                expr.var = add;
            }
            else if (op.type == TokenType.star)
            {
                NodeBinExprMulti multi = new NodeBinExprMulti();
                expr_lhs2.var = expr_lhs.var;
                multi.lhs = expr_lhs2;
                multi.rhs = expr_rhs;
                expr.var = multi;
            }
            else if (op.type == TokenType.minus)
            {
                NodeBinExprSub sub = new NodeBinExprSub();
                expr_lhs2.var = expr_lhs.var;
                sub.lhs = expr_lhs2;
                sub.rhs = expr_rhs;
                expr.var = sub;
            }
            else if (op.type == TokenType.fslash)
            {
                NodeBinExprDiv div = new NodeBinExprDiv();
                expr_lhs2.var = expr_lhs.var;
                div.lhs = expr_lhs2;
                div.rhs = expr_rhs;
                expr.var = div;
            }

            expr_lhs.var = expr;

        }
        return expr_lhs;
    }


    public NodeScope? parse_scope()
    {
        if (!try_consume(TokenType.open_curly).HasValue) return null;
        NodeScope scope = new NodeScope() { stmts = new List<NodeStmt>() };
        while (parse_stmt() is var stmt && stmt != null)
        {
            scope.stmts.Add(stmt);
            if (peek() is Token cc && cc.type == TokenType.close_curly) break;
        }
        try_consume_err(TokenType.close_curly);
        return scope;
    }

    public NodeIfPred? parse_if_pred()
    {
        if (try_consume(TokenType.else_) is Token t3)
        {
            if (try_consume(TokenType.if_) != null)
            {
                Token? t2 = try_consume_err(TokenType.open_paren);
                if (parse_check(new NodeExpr()) is var expr && expr != null)
                {
                    Token? t1 = try_consume_err(TokenType.close_paren);
                    if (parse_stmt() is var stmt && stmt != null)
                    {
                        NodeIfPred if_pred = new NodeIfPred() { if_ = new NodeStmtIf() { expr = expr, stmt = stmt, pred = parse_if_pred() } };
                        return if_pred;
                    }
                    else
                    {
                        if (!t1.HasValue) return null;
                        Console.WriteLine("Expected statement or scope on line " + t1.Value.line);
                        Environment.Exit(1);
                        return null;
                    }
                }
                else
                {
                    if (!t2.HasValue) return null;
                    Console.WriteLine("Expected expression inside the parentheses on line " + t2.Value.line);
                    Environment.Exit(1);
                    return null;
                }
            }
            else
            {
                if (parse_stmt() is var stmt && stmt != null)
                {
                    NodeIfPred if_pred = new NodeIfPred() { stmt = stmt };
                    return if_pred;
                }
                else
                {
                    Console.WriteLine("Expected statement or scope on line " + t3.line);
                    Environment.Exit(1);
                    return null;
                }
            }
        }
        return null;
    }


    public NodeCheck? parse_check(NodeExpr expr)
    {
        if (expr.var.Value == null)
        {
            var expre = parse_expr();
            if (expre == null) return null;
            expr = expre;
        }
        if (expr == null) return null;
        NodeCheck check = new NodeCheck() { lhs = expr };
        var op = peek();
        var op2 = peek(1);
        if (op == null || (op.Value.type != TokenType.eq && op.Value.type != TokenType.gt && op.Value.type != TokenType.lt)) return check;
        string oper = op.Value.type.ToString();
        consume();
        if (op2 == null)
        {
            Console.WriteLine("Expected expression at line " + op.Value.line);
            Environment.Exit(1);
            return null;
        }
        if (op2.Value.type == TokenType.eq || op2.Value.type == TokenType.gt || op2.Value.type == TokenType.lt)
        {
            oper += op2.Value.type.ToString();
            consume();
        }
        check.op = oper;
        var expr2 = parse_expr();
        if (expr2 == null) return check;
        check.rhs = expr2;
        return check;
    }

    public NodeStmt? parse_stmt()
    {
        if (peek() is Token token)
        {
            if (token.type == TokenType.exit && peek(1) is Token token2 && token2.type == TokenType.open_paren)
            {
                consume();
                Token t1 = consume();
                if (parse_expr() is var node_expr)
                {
                    if (node_expr == null)
                    {
                        node_expr = new NodeExpr { var = new NodeTerm() { var = new NodeTermIntLit { int_lit = new Token { type = TokenType.int_lit, value = "0", line = t1.line } } } };
                    }
                    try_consume_err(TokenType.close_paren);
                    try_consume_err(TokenType.semi);
                    return new NodeStmt { var = new NodeStmtExit { expr = node_expr } };
                }
                else
                {
                    Console.Error.WriteLine("Invalid Expression on line " + t1.line);
                    Environment.Exit(1);
                    return null;
                }
            }
            else if (token.type == TokenType.let && peek(1) is Token token3 && token3.type == TokenType.ident && peek(2) is Token token4 && token4.type == TokenType.eq)
            {
                consume();
                var stmt_let = new NodeStmtLet() { ident = consume() };
                consume();
                int i = 0;
                while (peek(i) is Token t1 && t1.type != TokenType.semi)
                {
                    if (t1.type == TokenType.ident && stmt_let.ident.value == t1.value)
                    {
                        Console.Error.WriteLine("Identifier " + t1.value + " has a circular definition. on line " + t1.line);
                        Environment.Exit(1);
                        return null;
                    }
                    i++;
                }
                if (parse_expr() is var expr)
                {
                    stmt_let.expr = expr;
                }
                else
                {
                    Console.Error.WriteLine("Invalid Expression on line " + token4.line);
                    Environment.Exit(1);
                }
                try_consume_err(TokenType.semi);
                return new NodeStmt { var = stmt_let };
            }
            else if (token.type == TokenType.open_curly)
            {
                if (parse_scope() is var scope && scope != null)
                {
                    return new NodeStmt { var = scope };
                }
                else
                {
                    Console.Error.WriteLine("Invalid Scope on line " + token.line);
                    Environment.Exit(1);
                    return null;
                }
            }
            else if (token.type == TokenType.close_curly)
            {
                if (peek(-1) is Token cc && cc.type == TokenType.open_curly)
                {
                    Console.Error.WriteLine("Empty Scope on line " + cc.line);
                }
                else
                {
                    Console.Error.WriteLine("Extra Closing Curly on line " + token.line);
                }
                Environment.Exit(1);
                return null;
            }
            else if (token.type == TokenType.print && peek(1) is Token token5 && token5.type == TokenType.open_paren)
            {
                consume();
                consume();
                if (peek() is Token t1 && t1.type == TokenType.string_lit)
                {
                    consume();
                    consume();
                    try_consume_err(TokenType.semi);
                    return new NodeStmt { var = new NodeStmtPrint() { str = t1 } };
                }
                else
                {
                    Console.Error.WriteLine("Print currently only supports strings. Line " + token5.line);
                    Environment.Exit(1);
                    return null;
                }
            }
            else if (try_consume(TokenType.if_) != null)
            {
                Token? t1 = try_consume_err(TokenType.open_paren);
                if (t1 == null) return null;
                if (parse_check(new NodeExpr()) is var expr)
                {
                    if (expr == null)
                    {
                        Console.Error.WriteLine("Invalid Expression on line " + t1.Value.line);
                        Environment.Exit(1);
                        return null;
                    }
                    Token? t2 = try_consume_err(TokenType.close_paren);
                    if (t2 == null) return null;
                    if (parse_stmt() is var stmt)
                    {
                        if (stmt == null)
                        {
                            Console.Error.WriteLine("Invalid Statement on line " + t2.Value.line);
                            Environment.Exit(1);
                            return null;
                        }
                        var if_pred = parse_if_pred();
                        if (if_pred != null)
                        {
                            return new NodeStmt { var = new NodeStmtIf() { expr = expr, stmt = stmt, pred = if_pred } };
                        }
                        else
                        {
                            return new NodeStmt { var = new NodeStmtIf() { expr = expr, stmt = stmt } };
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("Invalid Statement on line " + t2.Value.line);
                        Environment.Exit(1);
                        return null;
                    }
                }
                else
                {
                    Console.Error.WriteLine("Invalid Expression on line " + t1.Value.line);
                    Environment.Exit(1);
                    return null;
                }
            }
            else if (peek() is Token t3 && t3.type == TokenType.else_)
            {
                Console.Error.WriteLine("There is an else with no if attached to it on line " + t3.line);
                Environment.Exit(1);
                return null;
            }
            else if (token.type == TokenType.ident && peek(1) is Token token6 && token6.type == TokenType.eq)
            {
                consume();
                consume();
                if (parse_expr() is var expr && expr != null)
                {
                    try_consume_err(TokenType.semi);
                    return new NodeStmt { var = new NodeStmtAssign() { ident = token, expr = expr } };
                }
                else
                {
                    parse_check(new NodeExpr() { var = new NodeTerm { var = new NodeTermIdent { ident = token } } });
                    Console.Error.WriteLine("Invalid Expression on line " + token6.line);
                    Environment.Exit(1);
                    return null;
                }
            }
            else if (token.type == TokenType.while_)
            {
                consume();
                try_consume_err(TokenType.open_paren);
                var check = parse_check(new NodeExpr());
                if (check == null)
                {
                    Console.Error.WriteLine("Invalid Check in while loop on line " + token.line);
                    Environment.Exit(1);
                    return null;
                }
                try_consume_err(TokenType.close_paren);
                if (parse_stmt() is var stmt && stmt != null)
                {
                    if (!stmt.var.IsT2) Console.WriteLine("Statement is not a scope on line " + token.line + ". This may result in an infinite loop.");
                    return new NodeStmt { var = new NodeStmtWhile() { expr = check, stmt = stmt } };
                }
                return null;
            }
            else if (token.type == TokenType.for_)
            {
                consume();
                try_consume_err(TokenType.open_paren);
                var identstmt = parse_stmt();
                if (identstmt == null)
                {
                    Console.Error.WriteLine("You have to assign a variable for the loop. Line " + token.line);
                    Environment.Exit(1);
                    return null;
                }
                var check = parse_check(new NodeExpr());
                if (check == null)
                {
                    Console.Error.WriteLine("Invalid Check in for loop on line " + token.line);
                    Environment.Exit(1);
                    return null;
                }
                try_consume_err(TokenType.semi);
                var assignstmt = parse_stmt();
                if (assignstmt == null)
                {
                    Console.Error.WriteLine("You have to assign a variable for the loop. Line " + token.line);
                    Environment.Exit(1);
                    return null;
                }
                try_consume_err(TokenType.close_paren);
                var stmt = parse_stmt();
                if (stmt == null)
                {
                    Console.Error.WriteLine("Invalid Statement on line " + token.line);
                    Environment.Exit(1);
                    return null;
                }
                return new NodeStmt { var = new NodeStmtFor() { let = identstmt, check = check, assign = assignstmt, stmt = stmt } };
            }
            else
            {
                Console.WriteLine(peek() is Token a1 ? a1.type : "");
                Console.Error.WriteLine("Invalid Statement on line " + token.line);
                if (peek() is Token t4 && t4.type == TokenType.ident)
                {
                    Console.Error.WriteLine("Possibly a spelling mistake: " + t4.value);
                }
                Environment.Exit(1);
                return null;
            }
        }
        else
        {
            return null;
        }
    }

    public NodeProg? parse_prog()
    {
        NodeProg prog = new NodeProg();
        while (peek() is Token token)
        {
            if (parse_stmt() is var stmt && stmt != null)
            {
                prog.stmts.Add(stmt);
            }
            else
            {
                Console.Error.WriteLine("Invalid Program");
                Environment.Exit(1);
                return null;
            }
        }
        return prog;
    }


    private Token? peek(int offset = 0)
    {
        if (index + offset < _tokens.Count)
        {
            return _tokens[index + offset];
        }
        else
        {
            return null;
        }
    }


    private Token? try_consume_err(TokenType type)
    {
        if (peek() is Token token && token.type == type)
        {
            return consume();
        }
        else
        {
            var a = peek();
            if (a == null) return null;
            Console.WriteLine("Expected " + tm[type] + " on line " + a.Value.line);
            Environment.Exit(1);
            return null;
        }
    }

    private Token? try_consume(TokenType type)
    {
        if (peek() is Token token && token.type == type)
        {
            return consume();
        }
        else
        {
            return null;
        }
    }

    private Token consume()
    {
        return _tokens[index++];
    }

}