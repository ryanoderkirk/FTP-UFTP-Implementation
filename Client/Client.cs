using System;
using System.IO;
using System.Net.Sockets;

class Server
{
    static int Main(string[] args)
    {
        Sender sender = new Sender("10.185.137.42", 13000, 13001);
        return 0;
    }
}