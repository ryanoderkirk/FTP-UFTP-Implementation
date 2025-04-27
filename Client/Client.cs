using System;
using System.IO;
using System.Net.Sockets;

class Client
{
    enum commandType { none, read, write };
    static commandType currentCommand = commandType.none;
    static Sender sender = null;

    static int Main(string[] args)
    {
        

        Sender.dataReceived callback = dataMessageHandler;
        

        sender = new Sender("192.168.0.128", 13000, 13001, dataMessageHandler);
        string response = " ";
        string? readIn = "";
//        sender.sendControlMessage("pwd newDir",ref response);
//        sender.sendControlMessage("cd ..", ref response);
//        sender.sendControlMessage("read AFile", ref response);
//        sender.sendControlMessage("list UselessArg", ref response);
        while(readIn != "exit\n")
        {
            readIn = Console.ReadLine();
            if (readIn == null)
            {
                break;
            }
            readIn.TrimEnd('\n');
            Console.Write(readIn);
            if (readIn.Split(" ")[0] == "read")
                currentCommand = commandType.read;
            sender.sendControlMessage(readIn, ref response);
        }
        return 0;
    }

    static void dataMessageHandler(Byte[] msg)
    {

        if (currentCommand == commandType.read)
        {
            Console.WriteLine("MESSAGE: " + System.Text.Encoding.ASCII.GetString(msg, 0, msg.Length));

            //if last byte transmitted is NULL, this is the final transmission. reset currentCommandState to 0
            if (msg[msg.Length - 1] == 0)
                currentCommand = commandType.none;
            sender.sendControlMessage("ack");
        }
            
    }
}