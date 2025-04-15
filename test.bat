@echo OFF

rem batch file will call the sender in a seperate cmd window, then start the listener in this one. This can be used to send message to oneself for testing purposes

start "" "%~dp0TCPSender\bin\Debug\net6.0\TCPClient.exe"
"%~dp0TCPListen\bin\Debug\net6.0\TCPListen.exe"
pause
