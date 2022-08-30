using cslox;

var cslox = new CsLox();
switch (args.Length)
{
    case 0:
        cslox.Repl();
        return 0;
    case 1:
        cslox.RunFile(args[0]);
        return 0;
    default:
        Console.Error.WriteLine("Usage: cslox [path]");
        return 64;
}