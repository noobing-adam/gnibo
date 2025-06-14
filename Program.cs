using System.Diagnostics;


public enum TokenType
{
    exit,
    print,
    int_lit,
    string_lit,
    semi,
    ident,
    open_paren,
    close_paren,
    let,
    eq,
    gt,
    lt,
    plus,
    minus,
    star,
    fslash,
    open_curly,
    close_curly,
    if_,
    else_,
    while_,
    for_,
    and,
    or
}

public struct Token
{
    public TokenType type;
    public string? value;
    public int line;
}


class Gnibo
{

    static void Main(string[] args)
    {
        if (args.Length != 1)
        {
            Console.WriteLine("Incorrect usage. Usage: gnibo <input.nob>");
            Environment.Exit(1);
        }

        string filePath = args[0];

        string content = File.ReadAllText(filePath);


        Tokenizer tokenizer = new Tokenizer(content);
        var tokens = tokenizer.tokenize();

        Parser parser = new Parser(tokens);
        var tree = parser.parse_prog();

        if (tree == null)
        {
            Console.Error.WriteLine("No Statement Found");
            Environment.Exit(1);
        }

        Generator generator = new Generator(tree);
        var output = generator.gen_prog();
        File.WriteAllText("out.asm", output);

        Process process = new Process();
        process.StartInfo.FileName = "ubuntu";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardInput = true;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.CreateNoWindow = true;

        process.Start();

        var stdin = process.StandardInput;

        stdin.Write("cd \"/mnt/c/Users/Aliha/Desktop/My Projects/I don't think i have the knowledge to do this/mycompiler\"\n");
        stdin.Write("nasm -f elf64 out.asm -o out.o\n");
        stdin.Write("ld out.o -o out\n");
        stdin.Write("./out\n");
        stdin.Write("echo $?");

        stdin.Close();

        string outpute = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();

        process.WaitForExit();
        Console.WriteLine(outpute);
        Console.WriteLine(error);


        // dotnet run -- main.nob


        // umarım daha lazım olmaz ama önlem:
        // cd "/mnt/c/Users/Aliha/Desktop/My Projects/I don't think i have the knowledge to do this/mycompiler"
        // nasm -felf64 out.asm -o out.o && ld out.o -o out; ./out; echo $?
    }
}
