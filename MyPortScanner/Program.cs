using System.Net.Sockets;

class PortScanner
{
    static async Task Main(string[] args)
    {
        while (true)
        {
            int openedPortsCount = 0;

            Console.Write("Input IP or domain: ");
            string host = Console.ReadLine()!;

            Console.Write("How many ports you want to scan: ");
            int count = int.Parse(Console.ReadLine()!);

            Console.WriteLine($"\nScanning {count} port(s) on {host}\n");

            Task[] tasks = new Task[count];

            for (int port = 0; port < count; port++)
            {
                int currentPort = port;
                tasks[port] = ScanPortAsync(host, currentPort, () => Interlocked.Increment(ref openedPortsCount));
            }

            await Task.WhenAll(tasks);

            Console.WriteLine($"\nFound {openedPortsCount} open port(s).");
        }
    }

    static async Task ScanPortAsync(string host, int port, Action increment)
    {
        using TcpClient client = new TcpClient();
        try
        {
            await client.ConnectAsync(host, port);
            Console.WriteLine($"[OPENED] Port {port}");
            increment();
        }
        catch
        {
        }
    }
}
