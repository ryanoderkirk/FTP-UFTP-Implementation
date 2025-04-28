using System;
using System.IO;

public class Server
{
    //maintain state of current command running. If read or write in process, the response to an ACK should be sending another block
    enum commandType { none, read, write};
    commandType currentCommand = commandType.none;
    int readBlocks = 0;
    FileStream readFileStream = null;

    TCPListener listener = null;

    public async Task run()
    {
        // sets default directory to /documents/CNProject/Server/
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CNProject/Server";
        if (!Directory.Exists(documentsPath))
        {
            Directory.CreateDirectory(documentsPath);
        }
        Directory.SetCurrentDirectory(documentsPath);


        listener = new TCPListener("10.185.137.42", 13000, 13001, 
        // Callback Control
        (string msg) => {
            string[] commandSections = msg.Split(' ', 2);
            if (commandSections.Length > 2)
                return "Incorrect Command Structure";
            if (commandSections.Length == 1)
                return handleControlLine(commandSections[0], "").Result;
            return handleControlLine(commandSections[0], commandSections[1]).Result;
        },
        // Callback Data
        (string msg) => {
            if (msg == "1")
                return "10";
            if (msg == "2")
                return "20";

            return "";
        });


        await listener.listen();


    }

    public async Task<string> handleControlLine(string command, string arguments) {

        if (currentCommand == commandType.read)
        {
            if (command == "ack")
            {
                byte[] buffer = new byte[256];
                if(readFileStream.Read(buffer, 0, buffer.Length) == 0)
                {    
                    currentCommand = commandType.none;
                }
                else
                {
                    //NEED TO RESIZE ARRAY
                    //int resizeLength = Array.FindIndex<byte>(buffer, val => val == 0);
                    //if(resizeLength == -1)
                    //    resizeLength = buffer.Length;
                    //Array.Resize<byte>(ref buffer, resizeLength);
                    await listener.sendDataMessage(buffer);
                }

                return "";

                
            }
        }

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
                readFileStream.Read(buffer, 0, buffer.Length);
                Console.WriteLine(buffer.ToString());
                await listener.sendDataMessage(buffer);
            }
            return "Done";
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
}

