using System;
using System.IO;

public class Server
{

    enum commandType { none, read, write};
    commandType currentCommand = commandType.none;
    int readBytes = 0;

    string lastCommand = "";
    bool commandComplete = true;
    string workingFile = "";

    TCPListener listener = null;

    public async Task run()
    {

        listener = new TCPListener("192.168.1.161", 13000, 13001, 
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
        lastCommand = command;
        if (command == "cd")
        {
            if(arguments == "")
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
                FileStream fileStream = new FileStream(arguments, FileMode.Open);
                byte[] buffer = new byte[256];
                int charactersReadIn = 0;
                while ((charactersReadIn = fileStream.Read(buffer, 0, 256)) != 0)
                {
                    Console.WriteLine(buffer.ToString());
                    await listener.sendDataMessage(buffer);
                }
            }
            return "Done";
        }

        else if (command == "list")
        {
            string[] dirs = Directory.GetDirectories(Directory.GetCurrentDirectory());
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
            if (dirs.Length > 0)
            {


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
            return "Empty Directory";
        }


        return "unknown command";
    }
}

