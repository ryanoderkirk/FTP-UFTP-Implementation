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
        listener = new TCPListener("192.168.1.161", 13000, 13001, 
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
                long previousPosition = readFileStream.Position;
                long bytesRead = 0;
                while (readFileStream.Read(buffer, 0, buffer.Length) != 0)
                {
                    previousPosition = currentPosition;
                    currentPosition = readFileStream.Position;
                    bytesRead = currentPosition - previousPosition;

                    if(bytesRead < 256)
                    {
                        break;
                    }
                    else
                        await listener.sendDataMessage(buffer);
                }
                readFileStream.Close();

                if (bytesRead != 0)
                {
                    byte[] lastBuffer = new byte[bytesRead + 8];
                    for(int i = 0; i < bytesRead; i++)
                        lastBuffer[i] = buffer[i];
                    //append  01010101 to end of message to identiy EOF
                    for(int i = 0; i < 8; i++)
                    {
                        lastBuffer[bytesRead + i] = (byte)(i % 2);
                    }
                    await listener.sendDataMessage(lastBuffer);
                }
                else
                {
                    byte[] endOfFileMsg = new byte[8];
                    endOfFileMsg[0] = 0; endOfFileMsg[1] = 1; endOfFileMsg[2] = 0; endOfFileMsg[3] = 1;
                    endOfFileMsg[4] = 0; endOfFileMsg[5] = 1; endOfFileMsg[6] = 0; endOfFileMsg[7] = 1;
                    await listener.sendDataMessage(endOfFileMsg);
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

