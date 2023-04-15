

class Program
{
    static void Main(string[] args)
    {
        // TODO: add more options for GUI etc
        if(args.Length != 1)
        {
            Console.WriteLine("Usage code-counter <directory>");
            return;
        }

        try
        {
            var counter = new CodeCounter(args[0]);

            counter.printCount();
        } 

        catch(Exception ex)
        {
            Console.WriteLine("Error: {0}\n",ex.Message);
            return;
        }
    }
}