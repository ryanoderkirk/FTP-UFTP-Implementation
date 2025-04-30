using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

public class Server
{
    //maintain state of current command running. If read or write in process, the response to an ACK should be sending another block
    enum commandType { none, read, write, readUDP};
    commandType currentCommand = commandType.none;
    int readBlocks = 0;
    FileStream readFileStream = null;

    TCPListener listener = null;

    UdpClient udpDataLine = null;

    public async Task run()
    {
        // sets default directory to /documents/CNProject/Server/
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CNProject/Server";
        if (!Directory.Exists(documentsPath))
        {
            Directory.CreateDirectory(documentsPath);
        }
        Directory.SetCurrentDirectory(documentsPath);

        udpDataLine = new UdpClient(13002);
        listener = new TCPListener("192.168.1.240", 13000, 13001, 
        // Callback Control
        async (string msg) => {

            string[] commandSections = msg.Split(' ', 2);
            if (commandSections.Length > 2)
            {
                await listener.sendControlMessage("Incorrect Command Structure");
                return;
            }
                
            if (commandSections.Length == 1)
            {
                await listener.sendControlMessage(handleControlLine(commandSections[0], "").Result);
                return;
            }
                
            await listener.sendControlMessage(handleControlLine(commandSections[0], commandSections[1]).Result);
            return;
        },
        // Callback Data
        async (byte[] data) => {
            return;
        });

        await listener.listen();
    }

    public async Task<string> handleControlLine(string command, string arguments) {

        if (command == "cd")
        {
            if (arguments == "")
            {
                return "error";
            }
            Directory.SetCurrentDirectory(arguments);
            return Directory.GetCurrentDirectory();
        }

        else if(command == "pwd")
        {
            return Directory.GetCurrentDirectory();
        }

        else if (command == "read")
        {
            if(arguments == "")
            {
                return "error";
            }
            if (File.Exists(arguments)){

                currentCommand = commandType.read;

                //send first block. wait for a received ack before sending next block
                readFileStream = new FileStream(arguments, FileMode.Open);
                byte[] buffer = new byte[256];

                long currentPosition = readFileStream.Position;
                while (readFileStream.Read(buffer, 0, buffer.Length) != 0)
                {
                    currentPosition = readFileStream.Position;
                    await listener.sendDataMessage(buffer);
                }
                long bytesRemaining = readFileStream.Position - currentPosition;
                if(bytesRemaining != 0)
                {
                    byte[] lastBuffer = new byte[bytesRemaining];
                    for(int i = 0; i < bytesRemaining; i++)
                        lastBuffer[i] = buffer[i];
                    await listener.sendDataMessage(lastBuffer);
                }

                currentCommand = commandType.none;
            }
            return "";
        }

        else if (command == "readudp")
        {
            currentCommand = commandType.readUDP;
            readFileStream = new FileStream(arguments, FileMode.Open);
            IPEndPoint clientIP = null;
            udpDataLine.Receive(ref clientIP);
            udpDataLine.Connect(clientIP);
            while (true)
            {
                byte[] buffer = new byte[256];
                long currentPosition = readFileStream.Position;
                int done = readFileStream.Read(buffer, 2, buffer.Length - 2);
                long nextPosition = readFileStream.Position;

                if (done == 0)
                {
                    break;
                }
                //assign 2nd byte of array to hold size of the message. Leave first index alone for now
                buffer[1] = (byte)(nextPosition - currentPosition);
                Console.WriteLine(buffer);
                udpDataLine.Send(buffer, buffer.Length);
            }
            currentCommand = commandType.none;
            readFileStream.Close();
            return "";
        }

        else if (command == "list")
        {
            string[] dirs = Directory.GetDirectories(Directory.GetCurrentDirectory());
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
            if (dirs.Length == 0 && files.Length == 0)
            {
                return "Empty Directory";
            }
            string messageBack = "";
            foreach (string path in dirs)
            {
                messageBack += (path.Substring(Directory.GetCurrentDirectory().Length) + "\\\n");
            }
            foreach (string file in files)
            {
                messageBack += (file.Substring(Directory.GetCurrentDirectory().Length) + "\n");
            }
            return messageBack;
        }


        return "unknown command";
    }

    public static void udpConnectCallback(IAsyncResult passedObj)
    {
        UdpClient udpClient = (UdpClient)passedObj.AsyncState;
    }
}

