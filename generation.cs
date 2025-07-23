using System.Text;

class Generator
{
    public Generator(Parser.NodeProg root)
    {
        _root = root;
    }


    public string gen_string_hex(string input)
    {

        byte[] bytes = Encoding.UTF8.GetBytes(input);

        ulong result = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            result |= (ulong)bytes[i] << (8 * i);
        }

        return $"0x{result:X}";
    }


    public (string, int) gen_string(string input)
    {
        string miniout = "";
        if (input.Length < 8)
        {
            miniout += "    mov rax, " + gen_string_hex(input) + "\n";
            miniout += "    push rax\n";

            return (miniout, 8);
        }
        else
        {
            byte[] bytes = Encoding.UTF8.GetBytes(input);
            int len = bytes.Length;
            int fullChunks = len / 8;
            int remainder = len % 8;

            if (len < 8)
            {
                miniout += "    mov rax, " + gen_string_hex(input) + "\n";
                miniout += "    push rax\n";
                return (miniout, 8);
            }

            if (remainder > 0)
            {
                byte[] lastPart = new byte[remainder];
                Array.Copy(bytes, fullChunks * 8, lastPart, 0, remainder);
                miniout += "    mov rax, " + gen_string_hex(Encoding.UTF8.GetString(lastPart)) + "\n";
                miniout += "    push rax\n";
            }

            for (int i = fullChunks - 1; i >= 0; i--)
            {
                byte[] part = new byte[8];
                Array.Copy(bytes, i * 8, part, 0, 8);
                miniout += "    mov rax, " + gen_string_hex(Encoding.UTF8.GetString(part)) + "\n";
                miniout += "    push rax\n";
            }

            return (miniout, (fullChunks + (remainder > 0 ? 1 : 0)) * 8);
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
                miniout += push("rax");
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
        else if (term.var.TryPickT3(out var termFnCall, out _))
        {
            if (termFnCall.args != null)
                foreach (var arg in termFnCall.args)
                    miniout += gen_expr(arg);
            stack_size++;
            miniout += "    call " + termFnCall.name.value + "\n";
            if (termFnCall.args != null)
                foreach (var arg in termFnCall.args)
                    miniout += pop("rbx");
            miniout += push("rax");
            stack_size--;
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
            miniout += "    cqo\n";
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
        bool donethat = false;
        foreach (Parser.NodeStmt stmt2 in scope.stmts)
        {
            if (stmt2 is Parser.NodeStmtReturn)
            {
                donethat = true;
                miniout += gen_stmt(stmt2);
                break;
            }
            miniout += gen_stmt(stmt2);
        }
        if(!donethat) miniout += end_scope();
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

    public string gen_checks(List<Parser.NodeCheck> checks, string label, string start = "not", bool isNegated = false)
    {
        string miniout = "";
        for (int i = 0; i < checks.Count; i++)
        {
            Parser.NodeCheck check = checks[i];
            if (i == 0 || check.type == "and")
            {
                miniout += gen_check(check, label, isNegated, "and", start);
            }
            else
            {
                miniout += gen_check(check, label, isNegated, "or", start);
                miniout += start + ":\n";
            }
        }
    return miniout;
    }

    public string gen_check(Parser.NodeCheck check, string label, bool isNegated = false, string type = "and", string start = "")
    {
        string miniout = "";
        if (check.rhs == null)
        {
            miniout += gen_expr(check.lhs);
            miniout += pop("rax");
            miniout += "    test rax, rax\n";
            if (type == "and")
                miniout += (isNegated ? "    jnz " : "    jz ") + label + "\n";
            else
                miniout += (isNegated ? "    jz " : "    jnz ") + start + "\n";
        }
        else
        {
            miniout += gen_expr(check.lhs);
            miniout += gen_expr(check.rhs);
            miniout += pop("rbx");
            miniout += pop("rax");
            miniout += "    cmp rax, rbx\n";

            var jumpMap = new Dictionary<string, (string normal, string negated)>
            {
            { "eqeq", ("jne", "je") },
            { "gteq", ("jl", "jge") },
            { "lteq", ("jg", "jle") },
            { "lt",   ("jge", "jl") },
            { "gt",   ("jle", "jg") }
            };

            if (jumpMap.TryGetValue(check.op!, out var jumps))
            {
                if (type == "and")
                    miniout += "    " + (isNegated ? jumps.negated : jumps.normal) + " " + label + "\n";
                else
                    miniout += "    " + (isNegated ? jumps.normal : jumps.negated) + " " + start + "\n";
            }
        }
        return miniout;
    }


    public string gen_if_pred(Parser.NodeIfPred pred, string end_label)
    {
        string miniout = "";
        if (pred.if_ != null)
        {
            string label = create_label();
            string strt = create_label();
            miniout += gen_checks(pred.if_.checks, label, strt);
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

        if (stmt is Parser.NodeStmtExit exit)
        {
            if (exit.expr == null) exit.expr = new Parser.NodeExpr() { var = new Parser.NodeTerm { var = new Parser.NodeTermIntLit { int_lit = new Token() { type = TokenType.int_lit, value = "0" } } } };
            miniout += gen_expr(exit.expr);
            miniout += "    mov rax, 60\n";
            miniout += pop("rdi");
            miniout += "    syscall\n";
            return miniout;
        }
        else if (stmt is Parser.NodeStmtLet let)
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
        else if (stmt is Parser.NodeScope scope)
        {
            miniout += gen_scope(scope);
            return miniout;
        }
        else if (stmt is Parser.NodeStmtPrint print)
        {
            foreach (var string_lit in print.str.expr)
            {
                if (string_lit.TryPickT0(out var lit_string, out _))
                {
                    if (lit_string.value == null) return miniout;
                    var (text, integer) = gen_string(lit_string.value);
                    miniout += text;
                    miniout += "    mov rax, 1\n";
                    miniout += "    mov rdi, 1\n";
                    miniout += "    mov rsi, rsp\n";
                    miniout += "    mov rdx, " + Encoding.UTF8.GetByteCount(lit_string.value) + "\n";
                    miniout += "    syscall\n";
                    miniout += "    add rsp, " + integer + "\n";
                }
                else if (string_lit.TryPickT1(out var expr, out _))
                {
                    if (expr == null) return miniout;
                    miniout += gen_expr(expr);
                    miniout += pop("rdi");
                    miniout += "    lea rsi, [buffer]\n";
                    miniout += "    call int_to_string\n";
                    miniout += "    mov rdx, rax\n";
                    miniout += "    mov rax, 1\n";
                    miniout += "    mov rdi, 1\n";
                    miniout += "    mov rsi, buffer\n";
                    miniout += "    syscall\n";
                }
            }
                var (text2, integer2) = gen_string("\n");
                miniout += text2;
                miniout += "    mov rax, 1\n";
                miniout += "    mov rdi, 1\n";
                miniout += "    mov rsi, rsp\n";
                miniout += "    mov rdx, 1\n";
                miniout += "    syscall\n";
                miniout += "    add rsp, " + integer2 + "\n";
/*            if (print.str.TryPickT0(out var str, out _))
            {
                if (str.value == null) return miniout;
                var (text, integer) = gen_string(str.value);
                miniout += text;
                miniout += "    mov rax, 1\n";
                miniout += "    mov rdi, 1\n";
                miniout += "    mov rsi, rsp\n";
                miniout += "    mov rdx, " + (str.value.Length + 1) + "\n";
                miniout += "    syscall\n";
                miniout += "    add rsp, " + integer + "\n";
            }
            else if (print.str.TryPickT1(out var expr, out _))
            {
                if (expr == null) return miniout;
                miniout += gen_expr(expr);
                miniout += pop("rdi");
                miniout += "    lea rsi, [buffer]\n";
                miniout += "    call int_to_string\n";
                miniout += "    mov rdx, rax\n";
                miniout += "    mov rax, 1\n";
                miniout += "    mov rdi, 1\n";
                miniout += "    mov rsi, buffer\n";
                miniout += "    syscall\n";
            }
            else if (print.str.TryPickT2(out var exprtostr, out _))
            {
                miniout += gen_expr(exprtostr.expr);
                miniout += pop("rdi");
                miniout += "    lea rsi, [buffer]\n";
                miniout += "    call int_to_string\n";
                miniout += "    mov rdx, rax\n";
                miniout += "    mov rax, 1\n";
                miniout += "    mov rdi, 1\n";
                miniout += "    mov rsi, buffer\n";
                miniout += "    syscall\n";
            }
*/
            return miniout;
        }
        else if (stmt is Parser.NodeStmtIf if_)
        {
            string label = create_label();
            string strt = create_label();
            miniout += gen_checks(if_.checks, label, strt);
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
        else if (stmt is Parser.NodeStmtAssign assign)
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
        else if (stmt is Parser.NodeStmtWhile while_)
        {
            string label = create_label();
            string ifnot = create_label();
            string strt = create_label();

            miniout += ifnot + ":\n";
            miniout += gen_checks(while_.checks, label);
            miniout += gen_stmt(while_.stmt);
            miniout += gen_checks(while_.checks, ifnot, strt, true);
            miniout += label + ":\n";
            return miniout;
        }
        else if (stmt is Parser.NodeStmtFor for_)
        {
            string label = create_label();
            string ifnot = create_label();
            string strt = create_label();

            miniout += gen_stmt(for_.let);
            miniout += ifnot + ":\n";
            miniout += gen_checks(for_.checks, label);
            miniout += gen_stmt(for_.stmt);
            miniout += gen_stmt(for_.assign);
            miniout += gen_checks(for_.checks, ifnot, strt, true);
            scopes.Add(vars.Count - 1);
            miniout += end_scope();
            miniout += label + ":\n";
            return miniout;
        }
        else if (stmt is Parser.NodeStmtFunc func)
        {
            funcs.Add(new Tuple<List<Token?>, Token, Parser.NodeStmt>(func.args, func.name, func.stmt));
            return miniout;
        }
        else if (stmt is Parser.NodeStmtReturn ret)
        {
            if (ret.infunc == false)
            {
                Console.Error.WriteLine("Cannot return outside of function");
                Environment.Exit(1);
            }
            if (ret.expr == null)
            {
                miniout += end_scope();
                miniout += "    mov rax, 0\n";
                miniout += "    ret\n";
                return miniout;
            }
            miniout += gen_expr(ret.expr);
            miniout += pop("rax");
            miniout += end_scope();
            miniout += "    ret\n";
            return miniout;
        }
        else if (stmt is Parser.NodeStmtCall call)
        {
            if (call.expr != null)
            {
                miniout += gen_expr(call.expr);
                miniout += pop("rax");
            }
            miniout += "    call " + call.fname.value + "\n";
            return miniout;
        }
        else
        {
            Console.Error.WriteLine("Invalid Statement");
            Environment.Exit(1);
            return "This Shouldn't Happen";
        }

    }


    public string gen_function(Tuple<List<Token?>, Token, Parser.NodeStmt> func)
    {
        if(func.Item2.value == null) return "";
        string miniout = "";
        for (int i = 0; i < func.Item1.Count; i++)
        {
            var t = func.Item1[i];
            if (!t.HasValue) continue;
            if(t.Value.value == null) continue;
            vars.Add(new Var { name = t.Value.value, stack_loc = stack_size-func.Item1.Count+i-1 });
        }
        miniout += func.Item2.value + ":\n";
        miniout += gen_stmt(func.Item3);
        return miniout;
    }

    public string gen_prog()
    {
        string output = "section .bss\nbuffer: resb 20\n\nsection .text\nglobal _start\n_start:\n";
        foreach (Parser.NodeStmt stmt in _root.stmts)
        {
            output += gen_stmt(stmt);
        }

        output += "    mov rax, 60\n    mov rdi, 0\n    syscall\n";
        foreach (Tuple<List<Token?>, Token, Parser.NodeStmt> func in funcs)
        {
            stack_size += 1;
            output += gen_function(func);
            foreach (Token? t in func.Item1)
            {
                if (!t.HasValue) continue;
                Var? v = vars.Find(a => a.name == t.Value.value);
                if (v != null)
                {
                    vars.Remove(v);
                }
            }
            stack_size -= 1;

        }
        output += """    
int_to_string:
    ; Eğer sayı 0 ise direkt '0' ve '\n' yaz
    cmp rdi, 0
    jne convert
    mov byte [rsi], '0'
    mov byte [rsi+2], 0      ; null terminator
    mov rax, 2               ; toplam uzunluk: 3 (0 + \n + null)
    ret

convert:
    xor rcx, rcx          ; sayaç
    mov rbx, rdi          ; sayıyı kopyala

loop:
    mov rdx, 0
    mov rax, rbx
    mov rdi, 10
    div rdi               ; rax = rbx/10, rdx = rbx%10
    add dl, '0'
    mov [rsi + rcx], dl
    inc rcx
    mov rbx, rax
    cmp rbx, 0
    jne loop

    mov byte [rsi + rcx], 0

    ; Ters çevir
    mov rdi, rcx
    dec rdi
    xor rbx, rbx

reverse_loop:
    cmp rbx, rdi
    jge done
    mov al, [rsi + rbx]
    mov dl, [rsi + rdi]
    mov [rsi + rbx], dl
    mov [rsi + rdi], al
    inc rbx
    dec rdi
    jmp reverse_loop

done:
    mov byte [rsi + rcx], 0
    mov rax, rcx
    ret
""";
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
    List<Tuple<List<Token?>, Token, Parser.NodeStmt>> funcs = new List<Tuple<List<Token?>, Token, Parser.NodeStmt>>();
    List<int> scopes = new List<int>();
    int label_count = 0;
}