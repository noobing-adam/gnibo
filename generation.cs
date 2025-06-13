using System.Text;

class Generator
{
    public Generator(Parser.NodeProg root)
    {
        _root = root;
    }


    public string gen_string_hex(string input)
    {

        byte[] bytes = Encoding.ASCII.GetBytes(input);

        ulong result = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            result |= (ulong)bytes[i] << (8 * i);
        }

        return $"0x{result:X}";
    }


    public string gen_string(string input)
    {
        input += "\n";
        string miniout = "";
        if (input.Length < 8)
        {
            miniout += "    mov rax, " + gen_string_hex(input) + "\n";
            miniout += "    push rax\n";
            miniout += "    mov rax, 1\n";
            miniout += "    mov rdi, 1\n";
            miniout += "    mov rsi, rsp\n";
            miniout += "    mov rdx, " + input.Length + "\n";
            miniout += "    syscall\n";
            miniout += "    add rsp, 8\n";
            return miniout;
        }
        else
        {

            int len = input.Length;
            int fullChunks = len / 8;
            int remainder = len % 8;

            if (remainder > 0)
            {
                string lastPart = input.Substring(fullChunks * 8, remainder);
                miniout += "    mov rax, " + gen_string_hex(lastPart) + "\n";
                miniout += "    push rax\n";
            }

            for (int i = fullChunks - 1; i >= 0; i--)
            {
                string part = input.Substring(i * 8, 8);
                miniout += "    mov rax, " + gen_string_hex(part) + "\n";
                miniout += "    push rax\n";
            }


            miniout += "    mov rax, 1\n";
            miniout += "    mov rdi, 1\n";
            miniout += "    mov rsi, rsp\n";
            miniout += "    mov rdx, " + len + "\n";
            miniout += "    syscall\n";
            miniout += "    add rsp, " + ((fullChunks + (remainder > 0 ? 1 : 0)) * 8) + "\n";
            return miniout;
        }
    }


    public string gen_term(Parser.NodeTerm term)
    {
        string miniout = "";
        if (term.var.TryPickT0(out var term_int_lit, out _))
        {
            miniout += "    mov rax, " + term_int_lit.int_lit.value + "\n";
            miniout += push("rax");
        }
        else if (term.var.TryPickT1(out var term_ident, out _))
        {
            if (term_ident.ident.value == null) return miniout;
            if (vars.Find(x => x.name == term_ident.ident.value) is Var ident)
            {
                miniout += "    mov rax, QWORD [rsp + " + (stack_size - ident.stack_loc - 1) * 8 + "]\n";
                miniout += push("QWORD [rsp + " + (stack_size - ident.stack_loc - 1) * 8 + "]");
            }
            else
            {
                Console.WriteLine("Identifier " + term_ident.ident.value + " not declared");
                Environment.Exit(1);
            }
            return miniout;
        }
        else if (term.var.TryPickT2(out var term_parent, out _))
        {
            miniout += gen_expr(term_parent.expr);
        }
        return miniout;
    }


    public string gen_bin_expr(Parser.NodeBinExpr expr)
    {
        string miniout = "";
        if (expr.var.TryPickT0(out var add, out _))
        {
            if (add == null || add.rhs == null || add.lhs == null) return miniout;
            miniout += gen_expr(add.rhs);
            miniout += gen_expr(add.lhs);
            miniout += pop("rax");
            miniout += pop("rbx");
            miniout += "    add rax, rbx\n";
            miniout += push("rax");
        }
        else if (expr.var.TryPickT1(out var mul, out _))
        {
            if (mul == null || mul.rhs == null || mul.lhs == null) return miniout;
            miniout += gen_expr(mul.rhs);
            miniout += gen_expr(mul.lhs);
            miniout += pop("rax");
            miniout += pop("rbx");
            miniout += "    mul rbx\n";
            miniout += push("rax");
        }
        else if (expr.var.TryPickT2(out var sub, out _))
        {
            if (sub == null || sub.rhs == null || sub.lhs == null) return miniout;
            miniout += gen_expr(sub.rhs);
            miniout += gen_expr(sub.lhs);
            miniout += pop("rax");
            miniout += pop("rbx");
            miniout += "    sub rax, rbx\n";
            miniout += push("rax");
        }
        else if (expr.var.TryPickT3(out var div, out _))
        {
            if (div == null || div.rhs == null || div.lhs == null) return miniout;
            miniout += gen_expr(div.rhs);
            miniout += gen_expr(div.lhs);
            miniout += pop("rax");
            miniout += pop("rbx");
            miniout += "    div rbx\n";
            miniout += push("rax");
        }
        return miniout;
    }


    public string gen_expr(Parser.NodeExpr expr)
    {
        string miniout = "";
        if (expr.var.TryPickT0(out var term, out _))
        {
            miniout += gen_term(term);
        }
        else if (expr.var.TryPickT1(out var binexpr, out _))
        {
            miniout += gen_bin_expr(binexpr);
        }
        return miniout;
    }

    public string gen_scope(Parser.NodeScope scope)
    {
        string miniout = "";
        if (scope.stmts == null)
        {
            return miniout;
        }
        begin_scope();
        foreach (Parser.NodeStmt stmt2 in scope.stmts)
        {
            miniout += gen_stmt(stmt2);
        }
        miniout += end_scope();
        return miniout;
    }


    public void begin_scope()
    {
        scopes.Add(vars.Count);
    }

    public string end_scope()
    {
        string miniout = "";
        int pop_count = vars.Count - scopes[scopes.Count - 1];
        miniout += "    add rsp, " + pop_count * 8 + "\n";
        stack_size -= pop_count;
        for (int i = 0; i < pop_count; i++) vars.RemoveAt(vars.Count - 1);
        scopes.RemoveAt(scopes.Count - 1);
        return miniout;
    }

    public string gen_check(Parser.NodeCheck check, string label)
    {
        string miniout = "";
        if (check.rhs == null)
        {
            miniout += gen_expr(check.lhs);
            miniout += pop("rax");
            miniout += "    test rax, rax\n";
            miniout += "    jz " + label + "\n";
            return miniout;
        }
        miniout += gen_expr(check.lhs);
        miniout += gen_expr(check.rhs);
        miniout += pop("rbx");
        miniout += pop("rax");
        miniout += "    cmp rax, rbx\n";
        if (check.op == "eqeq")
        {
            miniout += "    jne " + label + "\n";
        }
        else if (check.op == "gteq")
        {
            miniout += "    jl " + label + "\n";
        }
        else if (check.op == "lteq")
        {
            miniout += "    jg " + label + "\n";
        }
        else if (check.op == "lt")
        {
            miniout += "    jge " + label + "\n";
        }
        else if (check.op == "gt")
        {
            miniout += "    jle " + label + "\n";
        }
            return miniout;       
    }


    public string gen_if_pred(Parser.NodeIfPred pred, string end_label)
    {
        string miniout = "";
        if (pred.if_ != null)
        {
            string label = create_label();
            miniout += gen_check(pred.if_.expr, label);
            miniout += gen_stmt(pred.if_.stmt);
            if (pred.if_.pred != null)
            {
                miniout += "    jmp " + end_label + "\n";
                miniout += label + ":\n";
                miniout += gen_if_pred(pred.if_.pred, end_label);
                miniout += end_label + ":\n";
            }
            else
            {
                miniout += label + ":\n";
            }
        }
        else
        {
            if (pred.stmt == null) return miniout;
            miniout += gen_stmt(pred.stmt);
        }
        return miniout;
    }

    public string gen_stmt(Parser.NodeStmt stmt)
    {
        string miniout = "";

        if (stmt.var.TryPickT0(out var exit, out _))
        {
            if (exit.expr == null) exit.expr = new Parser.NodeExpr() { var = new Parser.NodeTerm { var = new Parser.NodeTermIntLit { int_lit = new Token() { type = TokenType.int_lit, value = "0" } } } };
            miniout += gen_expr(exit.expr);
            miniout += "    mov rax, 60\n";
            miniout += pop("rdi");
            miniout += "    syscall\n";
            return miniout;
        }
        else if (stmt.var.TryPickT1(out var let, out _))
        {
            if (let.ident.value == null) return miniout;

            if (vars.Find(x => x.name == let.ident.value) != null)
            {
                Console.WriteLine("Identifier " + let.ident.value + " already declared");
                Environment.Exit(1);
                return miniout;
            }
            if (let.expr == null) return miniout;
            vars.Add(new Var() { name = let.ident.value, stack_loc = stack_size });
            miniout += gen_expr(let.expr);
            return miniout;
        }
        else if (stmt.var.TryPickT2(out var scope, out _))
        {
            miniout += gen_scope(scope);
            return miniout;
        }
        else if (stmt.var.TryPickT3(out var print, out _))
        {
            if (print == null || print.str.value == null) return miniout;
            miniout += gen_string(print.str.value);
            return miniout;
        }
        else if (stmt.var.TryPickT4(out var if_, out _))
        {
            string label = create_label();
            miniout += gen_check(if_.expr, label);
            miniout += gen_stmt(if_.stmt);
            if (if_.pred != null)
            {
                string end_label = create_label();
                miniout += "    jmp " + end_label + "\n";
                miniout += label + ":\n";
                miniout += gen_if_pred(if_.pred, end_label);
                miniout += end_label + ":\n";
            }
            else
            {
                miniout += label + ":\n";
            }
            return miniout;
        }
        else if (stmt.var.TryPickT5(out var assign, out _))
        {
            var ident = vars.Find(x => x.name == assign.ident.value);
            if (ident != null)
            {
                miniout += gen_expr(assign.expr);
                miniout += pop("rax");
                miniout += "    mov [rsp + " + (stack_size - ident.stack_loc - 1) * 8 + "], rax\n";
                return miniout;
            }
            else
            {
                Console.WriteLine("Identifier " + assign.ident.value + " not declared");
                Environment.Exit(1);
                return null;
            }
        }
        else
        {
            Console.Error.WriteLine("Invalid Statement");
            Environment.Exit(1);
            return "This Shouldn't Happen";
        }

    }


    public string gen_prog()
    {
        string output = "global _start\n_start:\n";
        //        output += "    mov rax, 0x0A6F6C6C6548\n    push rax\n    mov rax, 1\n    mov rdi, 1\n    mov rsi, rsp\n    mov rdx, 6\n    syscall\n";
        foreach (Parser.NodeStmt stmt in _root.stmts)
        {
            output += gen_stmt(stmt);
        }

        output += "    mov rax, 60\n    mov rdi, 0\n    syscall";
        return output;
    }



    private string push(string reg)
    {
        stack_size++;
        return "    push " + reg + "\n";
    }

    private string pop(string reg)
    {
        stack_size--;
        return "    pop " + reg + "\n";
    }

    private string create_label()
    {
        return "label" + label_count++;
    }

    Parser.NodeProg _root;
    int stack_size = 0;
    class Var
    {
        public required string name;
        public int stack_loc = 0;
    }
    List<Var> vars = new List<Var>();
    List<int> scopes = new List<int>();
    int label_count = 0;
}