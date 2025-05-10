using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

class PortScanner
{
    static async Task Main(string[] args)
    {
        while (true)
        {
            int n = 0;

            Console.WriteLine(
            "1.Check all IP addresses in the local network.\n" +
            "2.Scans all open ports by IP address\n" +
            "3.Check my ports\n\n" +
            "Choose option:");
            switch (n = int.Parse(Console.ReadLine()!))
            {
                case 1:
                    await NetworkScanner();
                    Console.WriteLine();
                    break;
                case 2:
                    await CheckPorts();
                    Console.WriteLine();
                    break;
                case 3:
                    await CheckMyPorts();
                    Console.WriteLine();
                    break;
                default:
                    Console.WriteLine("Invalid choice.");
                    Console.WriteLine();
                    break;
            }
        }

        async Task CheckPorts()
        {
            int openedPortsCount = 0;

            Console.Write("Input IP or domain: ");
            string host = Console.ReadLine()!;

            Console.Write("Input number of ports (or use '*' for check all): ");
            string? input = Console.ReadLine();

            int count;
            if (input == "*")
            {
                count = 65535;
            }
            else if (!int.TryParse(input, out count) || count < 1 || count > 65535)
            {
                Console.WriteLine("Invalid number. Please enter a value between 1 and 65535 or '*'.");
                return;
            }


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

        async Task ScanPortAsync(string host, int port, Action increment)
        {
            using TcpClient client = new TcpClient();
            try
            {
                var banner = await GetBannerAsync(host, port);
                await client.ConnectAsync(host, port);
                Console.WriteLine($"[OPENED] Port {port}\t" + $"Banner:{banner}");
                increment();
            }
            catch
            {
            }
        }

        async Task NetworkScanner()
        {
            string? localIp = GetLocalIPAddress();

            if (localIp == null)
            {
                Console.WriteLine("Unable to determine local IP address.");
                return;
            }

            string subnet = localIp.Substring(0, localIp.LastIndexOf('.') + 1);

            Console.WriteLine($"\nScanning network: {subnet}0/24 ...\n");

            Task[] tasks = new Task[254];

            for (int i = 1; i < 255; i++)
            {
                string ip = subnet + i;
                tasks[i - 1] = PingAndPrintAsync(ip);
            }

            await Task.WhenAll(tasks);
        }

        async Task PingAndPrintAsync(string ip)
        {
            using Ping ping = new();
            try
            {
                PingReply reply = await ping.SendPingAsync(ip, 200);
                if (reply.Status == IPStatus.Success)
                {
                    Console.WriteLine($"[ACTIVE] {ip}");
                }
            }
            catch
            {
            }
        }

        async Task<string> GetBannerAsync(string ip, int port)
        {
            try
            {
                using TcpClient client = new TcpClient();
                var connectTask = client.ConnectAsync(ip, port);
                if (await Task.WhenAny(connectTask, Task.Delay(1000)) != connectTask)
                    return "";

                using NetworkStream stream = client.GetStream();

                byte[] data = Encoding.ASCII.GetBytes("HEAD / HTTP/1.0\r\n\r\n");
                await stream.WriteAsync(data, 0, data.Length);

                byte[] buffer = new byte[1024];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

                string banner = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                if (!string.IsNullOrWhiteSpace(banner))
                    return banner;

                return "Banner not found";
            }
            catch
            {
                return "Error";
            }
        }

        string? GetLocalIPAddress()
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    var ipProps = ni.GetIPProperties();

                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(addr.Address))
                        {
                            return addr.Address.ToString();
                        }
                    }
                }
            }

            return null;
        }

        async Task CheckMyPorts()
        {
            string? host = GetLocalIPAddress();
            if (host == null)
            {
                Console.WriteLine("Unable to determine local IP address.");
                return;
            }

            Console.Write("Input number of ports to check (or '*' for all): ");
            string? input = Console.ReadLine();

            int count;
            if (input == "*")
                count = 65535;
            else if (!int.TryParse(input, out count) || count < 1 || count > 65535)
            {
                Console.WriteLine("Invalid number. Please enter a value between 1 and 65535 or '*'.");
                return;
            }

            Console.WriteLine($"\nScanning {count} port(s) on your local IP ({host})\n");

            int openedPortsCount = 0;
            Task[] tasks = new Task[count];

            for (int port = 0; port < count; port++)
            {
                int currentPort = port;
                tasks[port] = ScanPortAsync(host, currentPort, () => Interlocked.Increment(ref openedPortsCount));
            }

            await Task.WhenAll(tasks);
        }
    }
}