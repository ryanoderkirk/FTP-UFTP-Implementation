using System;
using System.Net.Sockets;
using System.Text;

public class Sender
{
    private TcpClient controlClient;
    private TcpClient dataClient;

    
    public Sender(string ip, int portControl, int portData)
    {
        controlClient = new TcpClient(ip, portControl);
        dataClient = new TcpClient(ip, portData);
    }

    public int sendControlMessage(string sendMessage, ref string recieveMessage)
    {
        NetworkStream stream = controlClient.GetStream();
        Byte[] data = System.Text.Encoding.ASCII.GetBytes(sendMessage);
        stream.Write(data, 0, data.Length);
        Console.WriteLine("Sent: {0}", sendMessage);
        data = new Byte[256];
        String responseData = String.Empty;

        // Read the first batch of the TcpServer response bytes.
        Int32 bytes = stream.Read(data, 0, data.Length);
        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
        Console.WriteLine("Received: {0}", responseData);
        recieveMessage = responseData;
        return 0;
    }
    public int sendDataMessage(string sendMessage, ref string recieveMessage)
    {
        NetworkStream stream = dataClient.GetStream();
        Byte[] data = System.Text.Encoding.ASCII.GetBytes(sendMessage);
        stream.Write(data, 0, data.Length);
        Console.WriteLine("Sent: {0}", sendMessage);
        data = new Byte[256];
        String responseData = String.Empty;

        // Read the first batch of the TcpServer response bytes.
        Int32 bytes = stream.Read(data, 0, data.Length);
        responseData = System.Text.Encoding.ASCII.GetString(data, 0, bytes);
        Console.WriteLine("Received: {0}", responseData);
        recieveMessage = responseData;
        return 0;
    }

};