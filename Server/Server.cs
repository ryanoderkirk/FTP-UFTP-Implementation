using System;
using System.IO;

class Server
{

    string lastCommand = "";
    bool commandComplete = true;
    string workingFile = "";

    static async Task Main(string[] args)
    {

        Server server = new Server();
        TCPListener listener = new TCPListener("192.168.1.240", 13000, 13001, 
        // Callback Control
        (string msg) => {
            string[] commandSections = msg.Split(' ', 2);
            if (commandSections.Length > 2)
                return "Incorrect Command Structure";
            if (commandSections.Length == 1)
                return server.handleControlLine(commandSections[0], "");
            return server.handleControlLine(commandSections[0], commandSections[1]);
/*
            if (msg == "1")
                return "1";
            if (msg == "2")
                return "2";

            return "";
*/
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

    public Server() {

    }

    public string handleControlLine(string command, string arguments) {
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
                StreamReader fileStream = new StreamReader(arguments);
                char[] buffer = new char[256];
                int charactersReadIn = 0;
                while ((charactersReadIn = fileStream.ReadBlock(buffer, 0, 256)) != 0)
                {
                    Console.WriteLine(buffer, 0, 256);
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

