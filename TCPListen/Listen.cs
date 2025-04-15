using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class TCPListener
{
    public TCPListener(string ip, int controlPort, int dataPort, ReadAndRespondCallback callbackControl, ReadAndRespondCallback callbackData)
    {
        this.ip = ip;
        this.controlPort = controlPort;
        this.dataPort = dataPort;
        readAndRespondControl = callbackControl;
        readAndRespondData = callbackData;
    }

    private string ip;
    int controlPort;
    int dataPort;

    public delegate string ReadAndRespondCallback(string command);
    ReadAndRespondCallback readAndRespondControl;
    ReadAndRespondCallback readAndRespondData;

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
        NetworkStream stream = controlLine.Result.GetStream();

        int i;

        // Loop to receive all the data sent by the client.
        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
        {
            // Translate data bytes to a ASCII string.
            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
            Console.WriteLine("Received: {0}", data);

            // Process the data sent by the client.
            string responseMessage = readAndRespondControl(data);

            byte[] msg = System.Text.Encoding.ASCII.GetBytes(responseMessage);

            // Send back a response.
            stream.Write(msg, 0, msg.Length);
            Console.WriteLine("Sent: {0}", responseMessage);
        }
    }

    private async Task dataHandlerAsync(Task<TcpClient> controlLine)
    {
        Console.WriteLine("Connected data!");

        Byte[] bytes = new Byte[256];
        String data = null;


        // Get a stream object for reading and writing
        NetworkStream stream = controlLine.Result.GetStream();

        int i;

        // Loop to receive all the data sent by the client.
        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
        {
            // Translate data bytes to a ASCII string.
            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
            Console.WriteLine("Received: {0}", data);

            // Process the data sent by the client.
            string responseMessage = readAndRespondControl(data);

            byte[] msg = System.Text.Encoding.ASCII.GetBytes(responseMessage);

            // Send back a response.
            stream.Write(msg, 0, msg.Length);
            Console.WriteLine("Sent: {0}", responseMessage);
        }
    }

}