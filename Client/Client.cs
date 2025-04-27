using System;
using System.IO;
using System.Net.Sockets;

class Client
{
    static int Main(string[] args)
    {
        Sender.dataReceived callback = dataMessageHandler;
        
        Sender sender = new Sender("192.168.1.161", 13000, 13001, dataMessageHandler);
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
            sender.sendControlMessage(readIn, ref response);
        }
        return 0;
    }

    static void dataMessageHandler(byte[] msg)
    {
        Console.WriteLine(msg);
    }
}