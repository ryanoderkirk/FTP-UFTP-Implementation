using System;
using System.IO;

class Server
{
    static async Task Main(string[] args)
    {
        TCPListener listener = new TCPListener((string msg) => {
            if (msg == "1")
                return "1";
            if (msg == "2")
                return "2";

            return "";
        },
        (string msg) => {
            if (msg == "1")
                return "10";
            if (msg == "2")
                return "20";

            return "";
        });

        await listener.listen();


    }
}

