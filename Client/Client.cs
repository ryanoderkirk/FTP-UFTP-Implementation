using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

class Client
{
    enum commandType { none, read, write , readUDP, writeUDP};
    static commandType currentCommand = commandType.none;
    static Sender sender = null;
    static FileStream fileWriter = null;
    static FileStream fileReader = null;
    static UdpClient udpClient = null;
    static IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("10.185.137.42"), 13002);

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
        udpClient = new UdpClient();
        sender.listen();

        string response = " ";
        string? readIn = "";
            
        while(true)
        {
            while(currentCommand != commandType.none)
            { }
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

            if (readIn.Split(" ")[0] == "write")
            {
                currentCommand = commandType.write;
            }

            if (readIn.Split(" ")[0] == "readudp")
            {
                currentCommand = commandType.readUDP;
            }

            if(readIn.Split(" ")[0] == "writeudp")
            {
                currentCommand = commandType.writeUDP;
                udpClient.Connect(serverEndPoint);
                udpClient.Send(new byte[8]);
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
            if (msg.Length > 7)
            {
                if (msg[msg.Length - 8] == 0 && msg[msg.Length - 7] == 1 && msg[msg.Length - 6] == 0 && msg[msg.Length - 5] == 1 &&
                msg[msg.Length - 4] == 0 && msg[msg.Length - 3] == 1 && msg[msg.Length - 2] == 0 && msg[msg.Length - 1] == 1)
                {
                    fileWriter.Write(msg, 0, msg.Length - 8);
                    currentCommand = commandType.none;
                    Console.WriteLine("Done Reading");
                    fileWriter.Close();
                    return;
                    //break;
                }
                else
                    fileWriter.Write(msg, 0, msg.Length);
            }
            else
                fileWriter.Write(msg, 0, msg.Length);
        }
            
    }

    static void controlMessageHandler(Byte[] msg)
    {
        int lastIndex = Array.FindLastIndex(msg, b => b != 0);

        Array.Resize(ref msg, lastIndex + 1);

        string response = System.Text.Encoding.ASCII.GetString(msg, 0, msg.Length);

        if (currentCommand == commandType.write)
        {
            string[] command = response.Split(" ", 2);
            Console.WriteLine(command[0]);
            Console.WriteLine(command[1]);
            if (File.Exists(command[1]))
            {
                Console.WriteLine("File Exists!");
            }
            if (command[0] == "CTS")
            {
                FileStream readFileStream = new FileStream(command[1], FileMode.Open);
                byte[] buffer = new byte[256];

                long currentPosition = readFileStream.Position;
                long previousPosition = readFileStream.Position;
                long bytesRead = 0;
                while (readFileStream.Read(buffer, 0, buffer.Length) != 0)
                {
                    previousPosition = currentPosition;
                    currentPosition = readFileStream.Position;
                    bytesRead = currentPosition - previousPosition;

                    if (bytesRead < 256)
                    {
                        break;
                    }
                    else
                        sender.sendDataMessage(buffer).Wait();
                }
                readFileStream.Close();

                if (bytesRead != 0)
                {
                    byte[] lastBuffer = new byte[bytesRead + 8];
                    for (int i = 0; i < bytesRead; i++)
                        lastBuffer[i] = buffer[i];
                    //append  01010101 to end of message to identiy EOF
                    for (int i = 0; i < 8; i++)
                    {
                        lastBuffer[bytesRead + i] = (byte)(i % 2);
                    }
                        sender.sendDataMessage(lastBuffer).Wait();
                }
                else
                {
                    byte[] endOfFileMsg = new byte[8];
                    endOfFileMsg[0] = 0; endOfFileMsg[1] = 1; endOfFileMsg[2] = 0; endOfFileMsg[3] = 1;
                    endOfFileMsg[4] = 0; endOfFileMsg[5] = 1; endOfFileMsg[6] = 0; endOfFileMsg[7] = 1;
                        sender.sendDataMessage(endOfFileMsg).Wait();
                }
                currentCommand = commandType.none;
            }
        }
            if (currentCommand == commandType.writeUDP)
        {
            string[] command = response.Split(" ", 2);
            Console.WriteLine(command[0]);
            Console.WriteLine(command[1]);
            if (File.Exists(command[1]))
            {
                Console.WriteLine("File Exists!");
            }

            if (command[0] == "CTS"){
                fileReader = new FileStream(command[1], FileMode.Open);
                udpClient.Send(new byte[8], 8);
                while (true)
                {
                    byte[] buffer = new byte[256];
                    long currentPosition = fileReader.Position;
                    int done = fileReader.Read(buffer, 0, buffer.Length);
                    long nextPosition = fileReader.Position;

                    if (done == 0)
                    {
                        udpClient.Send(new byte[8], 8);
                        Console.WriteLine("DONE");
                        break;
                    }
                    Console.WriteLine(buffer.Length);
                    udpClient.Send(buffer, (int)(nextPosition - currentPosition));
                }
                currentCommand = commandType.none;
                fileReader.Close();
                udpClient.Close();
                udpClient = new UdpClient();
            }
        }

        Console.WriteLine(System.Text.Encoding.ASCII.GetString(msg, 0, msg.Length));
    }

}