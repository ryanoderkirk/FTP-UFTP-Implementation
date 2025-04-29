using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class TCPListener
{
    public TCPListener(string ip, int controlPort, int dataPort, controlReadCallback callbackControl, dataReadCallback callbackData)
    {
        this.ip = ip;
        this.controlPort = controlPort;
        this.dataPort = dataPort;
        readControl = callbackControl;
        readData = callbackData;
    }

    public enum communicationType {server, client };
    private string ip;
    int controlPort;
    int dataPort;
    private TcpClient controlClient = null;
    private TcpClient dataClient = null;
    NetworkStream controlStream = null;
    NetworkStream dataStream = null;

    public delegate void dataReadCallback(byte[] command);
    dataReadCallback readData;

    public delegate void controlReadCallback(string command);
    controlReadCallback readControl;

    public async Task<int> sendDataMessage(byte[] data)
    {
        if(dataStream != null && dataStream.CanWrite)
        {
            dataStream.Write(data, 0, data.Length);
        }
        return 0;
    }

    public async Task<int> sendControlMessage(string message)
    {
        byte[] data = System.Text.Encoding.ASCII.GetBytes(message);
        if (dataStream != null && dataStream.CanWrite)
        {
            Console.WriteLine("Sending: " + message);
            Console.WriteLine(message.Length);
            controlStream.Write(data, 0, data.Length);
        }
        return 0;
    }

    public int sendControlMessage(string sendMessage, ref string recieveMessage)
    {
        Byte[] data = System.Text.Encoding.ASCII.GetBytes(sendMessage);
        controlStream.Write(data, 0, data.Length);
        data = new Byte[256];
        String responseData = String.Empty;
        List<Byte> totalMessage = new List<Byte>();
        int bytesRead = 0;

        controlStream.Read(data, 0, data.Length);
        totalMessage.AddRange(data);
        while (controlStream.DataAvailable)
        {
            bytesRead = controlStream.Read(data, 0, data.Length);
            //if bytes read is less than a full message, pad the rest with 0's
            if (bytesRead < 256)
            {
                for (int i = bytesRead - 1; i < data.Length; i++)
                {
                    data[i] = 0;
                }
            }
            totalMessage.AddRange(data);

        }
        // Read the first batch of the TcpServer response bytes.
        responseData = System.Text.Encoding.ASCII.GetString(totalMessage.ToArray(), 0, totalMessage.Count);
        recieveMessage = responseData;
        return 0;
    }

    public async Task<int> listen()
    {
        TcpListener controlLine = null;
        TcpListener dataLine = null;
        try
        {
            IPAddress localAddr = IPAddress.Parse(ip);            

            controlLine = new TcpListener(localAddr, controlPort);
            dataLine = new TcpListener(localAddr, dataPort);


            // Start listening for client requests.
            controlLine.Start();
            dataLine.Start();

            var controlLineListener = Task.Run( async () =>
            {
                while (true)
                {
                    Task<Task> c = controlLine.AcceptTcpClientAsync().ContinueWith(controlHandlerAsync);
                    await c;
                }
            });

            var dataLineListener = Task.Run(async () =>
            {
                while (true)
                {
                    Task<Task> d = dataLine.AcceptTcpClientAsync().ContinueWith(dataHandlerAsync);
                    await d;
                }
            });

            await controlLineListener;
            await dataLineListener;
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }
        finally
        {
            controlLine.Stop();
            dataLine.Stop();
        }

        Console.WriteLine("\nHit enter to continue...");
        Console.Read();

        return 0;
    }

    private async Task controlHandlerAsync(Task<TcpClient> controlLine)
    {
        Console.WriteLine("Connected control!");

        Byte[] bytes = new Byte[256];
        String data = null;

        // Get a stream object for reading and writing
        controlStream = controlLine.Result.GetStream();

        int i;

        // Loop to receive all the data sent by the client.
        while ((i = controlStream.Read(bytes, 0, bytes.Length)) != 0)
        {
            // Translate data bytes to a ASCII string.
            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
            Console.WriteLine("Received: {0}", data);

            // Process the data sent by the client.
            readControl(data);
        }
    }

    private async Task dataHandlerAsync(Task<TcpClient> controlLine)
    {
        Console.WriteLine("Connected data!");

        Byte[] bytes = new Byte[256];
        String data = null;


        // Get a stream object for reading and writing
        dataStream = controlLine.Result.GetStream();

        int i;

        // Loop to receive all the data sent by the client.
        while ((i = dataStream.Read(bytes, 0, bytes.Length)) != 0)
        {
            // Translate data bytes to a ASCII string.
            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
            Console.WriteLine("Received: {0}", data);

            // Process the data sent by the client.
            readData(bytes);
        }
    }

}