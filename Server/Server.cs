using System;
using System.IO;

class Server
{
    static void Main(string[] args)
    {
        TCPListener listener = new TCPListener((string msg) => {
            if (msg == "1")
                return "1";
            if (msg == "2")
                return "2";

            return "";
        });

        listener.listen();

    }
}

