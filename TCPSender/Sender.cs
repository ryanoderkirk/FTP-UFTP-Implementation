using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class Sender
{
    private TcpClient controlClient;
    private TcpClient dataClient;
    private string ip;
    private int controlPort;
    private int dataPort;
    NetworkStream controlStream;
    NetworkStream dataStream;


    public delegate void dataReceived(string msg);
    dataReceived dataBlockReceived;

    public Sender(string ip, int controlPort, int dataPort, dataReceived dataBlockReceived)
    {
        this.ip = ip;
        this.controlPort = controlPort;
        this.dataPort = dataPort;
        controlClient = new TcpClient(ip, controlPort);
        dataClient = new TcpClient(ip, dataPort);
        controlStream = controlClient.GetStream();
        dataStream = dataClient.GetStream();
        this.dataBlockReceived = dataBlockReceived;
        this.listen();
    }

    public int sendControlMessage(string sendMessage, ref string recieveMessage)
    {
        Byte[] data = System.Text.Encoding.ASCII.GetBytes(sendMessage);
        controlStream.Write(data, 0, data.Length);
        Console.WriteLine("Sent: {0}", sendMessage);
        data = new Byte[256];
        String responseData = String.Empty;

        // Read the first batch of the TcpServer response bytes.
        Int32 bytes = controlStream.Read(data, 0, data.Length);
        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
        Console.WriteLine("Received: {0}", responseData);
        recieveMessage = responseData;
        return 0;
    }
    public int sendDataMessage(string sendMessage, ref string recieveMessage)
    {
        Byte[] data = System.Text.Encoding.ASCII.GetBytes(sendMessage);
        dataStream.Write(data, 0, data.Length);
        Console.WriteLine("Sent: {0}", sendMessage);
        data = new Byte[256];
        String responseData = String.Empty;

        // Read the first batch of the TcpServer response bytes.
        Int32 bytes = dataStream.Read(data, 0, data.Length);
        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
        Console.WriteLine("Received: {0}", responseData);
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

            var dataLineListener = Task.Run(async () =>
            {
                while (true)
                {
                    if(dataStream.DataAvailable)
                    {
                        Byte[] bytes = new Byte[256];
                        String data = null;

                        int i;
                        while ((i = dataStream.Read(bytes, 0, bytes.Length)) != 0)
                        {
                            // Translate data bytes to a ASCII string.
                            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                            //Callback when datablock is received
                            dataBlockReceived(data);
                        }
                    }
                    
                }
            });

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
        return 0;
    }

    private async Task dataNotifier(Task<TcpClient> controlLine)
    {
        // Get a stream object for reading and writing
        NetworkStream controlStream = controlLine.Result.GetStream();
        Byte[] bytes = new Byte[256];
        String data = null;

        int i;
        while ((i = controlStream.Read(bytes, 0, bytes.Length)) != 0)
        {
            // Translate data bytes to a ASCII string.
            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

            //Callback when datablock is received
            dataBlockReceived(data);
        }

         
    }
};