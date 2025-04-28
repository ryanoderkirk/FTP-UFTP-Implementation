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


    public delegate void dataReceived(Byte[] msg);
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
                for(int i = bytesRead-1; i < data.Length; i++)
                {
                    data[i] = 0;
                }
            }
            totalMessage.AddRange(data);

        }
        // Read the first batch of the TcpServer response bytes.
        responseData = System.Text.Encoding.ASCII.GetString(totalMessage.ToArray(), 0, totalMessage.Count);
        Console.WriteLine("Received: {0}", responseData);
        recieveMessage = responseData;
        return 0;
    }
    public int sendControlMessage(string sendMessage)
    {
        Byte[] data = System.Text.Encoding.ASCII.GetBytes(sendMessage);
        controlStream.Write(data, 0, data.Length);
        Console.WriteLine("Sent: {0}", sendMessage);
        data = new Byte[256];
        return 0;
    }

    public int sendDataMessage(string sendMessage, ref string recieveMessage)
    {
        Byte[] data = System.Text.Encoding.ASCII.GetBytes(sendMessage);
        dataStream.Write(data, 0, data.Length);
        Console.WriteLine("Sent: {0}", sendMessage);
        data = new Byte[256];
        String responseData = String.Empty;
        List<Byte> totalMessage = new List<Byte>();
        int bytesRead = 0;

        dataStream.Read(data, 0, data.Length);
        totalMessage.AddRange(data);
        while (dataStream.DataAvailable)
        {
            bytesRead = dataStream.Read(data, 0, data.Length);
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
        Console.WriteLine("Received: {0}", responseData);
        recieveMessage = responseData;
        return 0;
    }


    public async Task<int> listen()
    {
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
                            dataBlockReceived(bytes);
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
            dataBlockReceived(bytes);
        }

         
    }
};