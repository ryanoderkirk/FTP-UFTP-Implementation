This project implements FTP protocol with a client server architecture.

USAGE:

Server.cs and Client.cs both have const strings name IPDADDRESS at the top of their files. Both of these need to be updated to the server's IP address. The project can then be built.

The Server should be launched first, which will wait for a connection from the client. Once the client is launched, an alert will appear to confirm that the connection has been made. The client can send commands: pwd, cd, list, read "file.txt", write "file.txt", readudp "file.txt", writeudp "file.txt"
