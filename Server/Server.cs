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
            if (commandSections.Length != 2)
                return "Incorrect Command Structure";
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
        if (command == "cd") {
            return command;
        }
        if (command == "read") {
            workingFile = arguments;
            return command;
        }


        return "unknown command";
    }
}

