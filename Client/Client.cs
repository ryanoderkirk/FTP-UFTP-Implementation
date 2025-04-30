using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

class Client
{
    enum commandType { none, read, write , readUDP};
    static commandType currentCommand = commandType.none;
    static Sender sender = null;
    static FileStream fileWriter = null;

    static int Main(string[] args)
    {
        // sets default directory to /documents/CNProject/Server/
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CNProject/Client";
        if (!Directory.Exists(documentsPath))
        {
            Directory.CreateDirectory(documentsPath);
        }
        Directory.SetCurrentDirectory(documentsPath);

        Sender.dataReceived dataCallback = dataMessageHandler;
        Sender.dataReceived controlCallback = controlMessageHandler;
        sender = new Sender("10.185.137.42", 13000, 13001, dataMessageHandler, controlMessageHandler);
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("10.185.137.42"),13002);
        UdpClient udpClient = new UdpClient();
        sender.listen();

        string response = " ";
        string? readIn = "";
            
        while(true)
        {
            readIn = Console.ReadLine();
            if (readIn == null)
            {
                break;
            }
            readIn.TrimEnd('\n');

            if (readIn.Split(" ")[0] == "read")
            {
                currentCommand = commandType.read;
                fileWriter = new FileStream(readIn.Replace("read ", ""), FileMode.Create);
            }

            if(readIn.Split(" ")[0] == "readudp")
            {
                currentCommand = commandType.readUDP;
            }

            if (readIn == "exit")
                break;
            
            sender.sendControlMessage(readIn);

            if (currentCommand == commandType.readUDP)
            {
                udpClient.Connect(serverEndPoint);
                udpClient.Send(new byte[8]);
                fileWriter = new FileStream(readIn.Replace("read ", ""), FileMode.Create);
                while (true)
                {
                    int bytesReceived;
                    if((bytesReceived = udpClient.Available) > 0)
                    {
                        Byte[] msg = new Byte[bytesReceived];
                        msg = udpClient.Receive(ref serverEndPoint);
                        if (msg.Length == 8)
                        {
                                break;
                        }
                        fileWriter.Write(msg, 0, msg.Length);
                    }
                }
                currentCommand = commandType.none;
                fileWriter.Close();
                udpClient.Close();
                udpClient = new UdpClient();
            }
        }
        return 0;
    }

    static void dataMessageHandler(Byte[] msg)
    {

        if (currentCommand == commandType.read)
        {
            //if last byte transmitted is NULL, this is the final transmission. reset currentCommandState to 0
            if (msg[1] < 254)
            {
                currentCommand = commandType.none;
                fileWriter.Write(msg, 2, msg[1]);
                fileWriter.Close();
                
            }
            else
                fileWriter.Write(msg, 2, msg[1]);

            sender.sendControlMessage("ack");
        }
            
    }

    static void controlMessageHandler(Byte[] msg)
    {
        Console.WriteLine(System.Text.Encoding.ASCII.GetString(msg, 0, msg.Length));
    }
}