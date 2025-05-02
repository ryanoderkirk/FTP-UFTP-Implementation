using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

public class Server
{
    //maintain state of current command running. If read or write in process, the response to an ACK should be sending another block
    enum commandType { none, read, write, readUDP, writeUDP};
    commandType currentCommand = commandType.none;
    int readBlocks = 0;
    FileStream readFileStream = null;
    FileStream writeFileStream = null;

    TCPListener listener = null;

    UdpClient udpDataLine = null;

    public struct writeCallbackObj
    {
        public Server svr;
        public UdpClient udpClient;
        public FileStream writeStream;
    }

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
        listener = new TCPListener("10.185.137.42", 13000, 13001, 
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
            if (currentCommand == commandType.write)
            {
            //check for EOF code
                if(data.Length > 7)
                {
                    if (data[data.Length - 8] == 0 && data[data.Length - 7] == 1 && data[data.Length - 6] == 0 && data[data.Length - 5] == 1 &&
                    data[data.Length - 4] == 0 && data[data.Length - 3] == 1 && data[data.Length - 2] == 0 && data[data.Length - 1] == 1)
                    {
                        writeFileStream.Write(data, 0, data.Length - 8);
                        currentCommand = commandType.none;
                        Console.WriteLine("Done Reading");
                        writeFileStream.Close();
                        return;
                        //break;
                    }
                    else
                        writeFileStream.Write(data, 0, data.Length);
                }
                else
                    writeFileStream.Write(data, 0, data.Length);
            }
            
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
        else if(command == "write")
        {
            currentCommand = commandType.write;
            writeFileStream = new FileStream("written" + arguments, FileMode.Create);
            if(writeFileStream.CanWrite)
            {
                return "CTS " + arguments;
            }
            else
                Console.WriteLine("FILESTREAM NOT WRITEABLE");
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
                int done = readFileStream.Read(buffer, 0, buffer.Length);
                long nextPosition = readFileStream.Position;

                if (done == 0)
                {
                    udpDataLine.Send(new byte[8], 8);
                    break;
                }
                Console.WriteLine(buffer.Length);
                udpDataLine.Send(buffer, (int)(nextPosition-currentPosition));
            }
            currentCommand = commandType.none;
            readFileStream.Close();
            udpDataLine.Close();
            udpDataLine = new UdpClient(13002);
            return "";
        }

        else if (command == "writeudp")
        {
            string outFile = "writeUDP" + arguments;
            writeFileStream = new FileStream(outFile, FileMode.Create);

            writeCallbackObj passedObj = new writeCallbackObj();
            passedObj.writeStream = writeFileStream;
            passedObj.udpClient = udpDataLine;
            passedObj.svr = this;

            IPEndPoint clientIP = null;
            udpDataLine.Receive(ref clientIP);
            udpDataLine.Connect(clientIP);

            udpDataLine.BeginReceive(udpWriteCallback,passedObj);

            string messageBack = "CTS ";
            messageBack += arguments;

            return messageBack;
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

    public static void udpWriteCallback(IAsyncResult passedObj)
    {
        Server svr = ((writeCallbackObj)(passedObj.AsyncState)).svr;
        FileStream writeStream = ((writeCallbackObj)(passedObj.AsyncState)).writeStream;
        UdpClient udpClient = ((writeCallbackObj)(passedObj.AsyncState)).udpClient;

        IPEndPoint temp = null;
        while (true)
        {
            int bytesReceived;
            if ((bytesReceived = udpClient.Available) > 0)
            {
                Byte[] msg = new Byte[bytesReceived];
                msg = udpClient.Receive(ref temp);
                Console.WriteLine(msg.Length);
                if (msg.Length != 256)
                {
                    if (msg.Length != 8)
                    {
                        writeStream.Write(msg,0,msg.Length);
                        continue;
                    }
                    break;
                }
                writeStream.Write(msg, 0, msg.Length);
            }
        }
        svr.currentCommand = commandType.none;
        writeStream.Close();
        svr.udpDataLine.Close();
        svr.udpDataLine = new UdpClient(13002);
        Console.WriteLine("DONE");
    }
}

