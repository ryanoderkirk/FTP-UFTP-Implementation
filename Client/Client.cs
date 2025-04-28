using System;
using System.IO;
using System.Net.Sockets;

class Client
{
    enum commandType { none, read, write };
    static commandType currentCommand = commandType.none;
    static Sender sender = null;
    static FileStream fileWriter = null;

    static int Main(string[] args)
    {
        // sets default directory to /documents/CNProject/Server/
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/CNProject/Client";
        if (!Directory.Exists(documentsPath))
        {
            Directory.CreateDirectory(documentsPath);
        }
        Directory.SetCurrentDirectory(documentsPath);

        Sender.dataReceived callback = dataMessageHandler;
        

        sender = new Sender("10.185.137.42", 13000, 13001, dataMessageHandler);
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

            if (readIn.Split(" ")[0] == "read")
            {
                currentCommand = commandType.read;
                fileWriter = new FileStream(readIn.Replace("read ", ""), FileMode.Create);
            }

            if (readIn == "exit")
                break;

            sender.sendControlMessage(readIn, ref response);
        }
        return 0;
    }

    static void dataMessageHandler(Byte[] msg)
    {

        if (currentCommand == commandType.read)
        {
            Console.WriteLine("MESSAGE: " + System.Text.Encoding.ASCII.GetString(msg, 0, msg.Length));
            
            //if last packet, resize array before writing all null terminators
            

            //if last byte transmitted is NULL, this is the final transmission. reset currentCommandState to 0
            if (msg[msg.Length - 1] == 0)
            {
                Console.WriteLine("\nDONE\n");
                currentCommand = commandType.none;
                fileWriter.Write(msg, 0, Array.FindIndex<byte>(msg, val => val == 0));
                fileWriter.Close();
                
            }
            else
                fileWriter.Write(msg);

            sender.sendControlMessage("ack");
        }
            
    }
}