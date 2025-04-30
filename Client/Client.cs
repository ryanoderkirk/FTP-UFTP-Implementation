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
        sender = new Sender("10.185.45.229", 13000, 13001, dataMessageHandler, controlMessageHandler);
        IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.240"),13002);
        UdpClient udpClient = new UdpClient();
        udpClient.Connect(serverEndPoint);
        udpClient.Send(new Byte[8]);
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
                fileWriter = new FileStream(readIn.Replace("read ", ""), FileMode.Create);
                while (true)
                {
                    Byte[] msg = new Byte[256];
                    msg = udpClient.Receive(ref serverEndPoint);
                    fileWriter.Write(msg, 2, msg[1]);
                    if (msg[1] < 254)
                    {
                        currentCommand = commandType.none;
                        fileWriter.Close();
                        break;
                    }
                }
                fileWriter.Close();
            }
        }
        return 0;
    }

    static void dataMessageHandler(Byte[] msg)
    {

        if (currentCommand == commandType.read)
        {
            //while(true)
            //{
                //check for EOF code
                if (msg[msg.Length - 8] == 0 && msg[msg.Length - 7] == 1 && msg[msg.Length - 6] == 0 && msg[msg.Length - 5] == 1 &&
                    msg[msg.Length - 4] == 0 && msg[msg.Length - 3] == 1 && msg[msg.Length - 2] == 0 && msg[msg.Length - 1] == 1)
                {
                    fileWriter.Write(msg, 0, msg.Length - 8);
                    currentCommand = commandType.none;
                    fileWriter.Close();
                    return;
                    //break;
                }
                else
                    fileWriter.Write(msg, 0, msg.Length);
            //}
            

        }
            
    }

    static void controlMessageHandler(Byte[] msg)
    {
        Console.WriteLine(System.Text.Encoding.ASCII.GetString(msg, 0, msg.Length));
    }
}