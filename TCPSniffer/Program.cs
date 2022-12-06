using Microsoft.Extensions.Configuration;
using Serilog;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TCPSniffer.Model;

class Program
{

    private static readonly object _lock = new object();
    private static readonly Dictionary<int, TcpClient> list_clients = new Dictionary<int, TcpClient>();
    private static List<string> lista_camaras_videolost = new List<string>();
    private static List<string> temporal = new List<string>();
    private static bool flag = false;


    static void Main(string[] args)
    {

        var config = new ConfigurationBuilder()
                     .SetBasePath(Directory.GetCurrentDirectory())
                     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();
        var section = config.GetSection(nameof(Configuration));
        var ClientConfig = section.Get<Configuration>();

        string customTemplate = "{Timestamp:dd/MM/yy HH:mm:ss.fff}\t[{Level:u3}]\t{Message}{NewLine}{Exception}";
        Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.Console(outputTemplate: customTemplate)
               .CreateLogger();

        int count = 1;
        IPAddress address = IPAddress.Parse(ClientConfig.Path);
        TcpListener ServerSocket = new TcpListener(IPAddress.Any, ClientConfig.Port);
        ServerSocket.Start();
        Log.Information("Server ready for listen!!! {0}:{1}", ClientConfig.Path, ClientConfig.Port);

        while (true)
        {
            TcpClient client = ServerSocket.AcceptTcpClient();
            lock (_lock) list_clients.Add(count, client);
            Log.Information("Client connected!!");
            Thread t = new Thread(handle_clients);
            t.Start(count);
            count++;
        }
    }

    public static void handle_clients(object o)
    {
        int id = (int)o;
        TcpClient client;

        lock (_lock) client = list_clients[id];

        while (true)
        {
            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[10000];
            int byte_count = stream.Read(buffer, 0, buffer.Length);

            string data = Encoding.ASCII.GetString(buffer, 0, byte_count);
            Console.WriteLine("\n");
            Log.Information("Recive Data: {0}", data);
            Console.WriteLine("\n");

        }

        lock (_lock) list_clients.Remove(id);
        client.Client.Shutdown(SocketShutdown.Both);
        client.Close();
        Log.Information("Client Disconnect!!");
        Console.WriteLine("\n");

    }

    public static string ByteToHex(byte[] comByte, bool removeZero = false, bool removeSpace = false)
    {
        string returnValue = "";

        try
        {
            if ((comByte != null))
            {
                StringBuilder builder = new StringBuilder(comByte.Length * 3);
                //loop through each byte in the array
                foreach (byte data in comByte)
                {
                    if (removeZero == true)
                    {
                        if (data != 0)
                        {
                            builder.Append(Convert.ToString(data, 16).PadLeft(2, '0').PadRight(3, ' '));
                        }
                    }
                    else
                    {
                        builder.Append(Convert.ToString(data, 16).PadLeft(2, '0').PadRight(3, ' '));
                    }
                    //convert the byte to a string and add to the stringbuilder
                }
                //return the converted value
                returnValue = builder.ToString().ToUpper();
                if (removeSpace)
                    returnValue = returnValue.Replace(" ", "");
            }
        }
        catch (Exception)
        {
            //Log.Error($"Error Exception:{ex.ToString()}");
        }

        return returnValue;

    }



    public static void broadcast(string write)
    {
        lock (_lock)
        {
            foreach (TcpClient c in list_clients.Values)
            {
                Console.WriteLine("\n");
                Log.Information("Writing " + write);

                NetworkStream stream = c.GetStream();
                byte[] buffer = Encoding.ASCII.GetBytes(write);
                stream.Write(buffer, 0, buffer.Length);
            }
        }
    }




}