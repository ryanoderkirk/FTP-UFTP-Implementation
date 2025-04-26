using System;
using System.IO;
using System.Net.Sockets;

class Client
{
    static int Main(string[] args)
    {
        Sender sender = new Sender("192.168.1.240", 13000, 13001);
        string response = " ";
        string? readIn = "";
//        sender.sendControlMessage("pwd newDir",ref response);
//        sender.sendControlMessage("cd ..", ref response);
//        sender.sendControlMessage("read AFile", ref response);
//        sender.sendControlMessage("list UselessArg", ref response);
        while(true)
        {
            readIn = Console.ReadLine();
            if (readIn == null)
            {
                break;
            }
            readIn.TrimEnd('\n');
            Console.Write(readIn);
            if (readIn == "exit")
                break;
            sender.sendControlMessage(readIn, ref response);
        }
        return 0;
    }
}