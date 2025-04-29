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
    public dataReceived dataBlockReceived;
    public dataReceived controlBlockReceived;

    public Sender(string ip, int controlPort, int dataPort, dataReceived dataBlockReceived, dataReceived controlBlockReceived)
    {
        this.ip = ip;
        this.controlPort = controlPort;
        this.dataPort = dataPort;
        controlClient = new TcpClient(ip, controlPort);
        dataClient = new TcpClient(ip, dataPort);
        controlStream = controlClient.GetStream();
        dataStream = dataClient.GetStream();
        this.dataBlockReceived = dataBlockReceived;
        this.controlBlockReceived = controlBlockReceived;
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
                for(int i = bytesRead-1; i < data.Length; i++)
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

    public int sendControlMessage(string sendMessage)
    {
        Byte[] data = System.Text.Encoding.ASCII.GetBytes(sendMessage);
        controlStream.Write(data, 0, data.Length);
        data = new Byte[256];
        return 0;
    }

    public async Task<int> sendDataMessage(byte[] data)
    {
        if (dataStream != null && dataStream.CanWrite)
        {
            dataStream.Write(data, 0, data.Length);
        }
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
                    if (dataStream.DataAvailable)
                    {
                        Byte[] data = new Byte[256];

                        dataStream.Read(data, 0, data.Length);
                        dataBlockReceived(data);
                    }
                }
            });

            var controlLineListener = Task.Run(async () =>
            {
                while (true)
                {
                    if (controlStream.DataAvailable)
                    {
                        Byte[] data = new Byte[256];
                        List<Byte> totalMessage = new List<Byte>();
                        int bytesRead;

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
                        controlBlockReceived(totalMessage.ToArray());
                    }
                }
            });

            await dataLineListener;
            await controlLineListener;
        }
        catch (SocketException e)
        {
            Console.WriteLine("SocketException: {0}", e);
        }

        return 0;
    }

};