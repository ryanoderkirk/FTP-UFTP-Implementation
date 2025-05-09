@echo OFF

rem batch file will call the sender in a seperate cmd window, then start the listener in this one. This can be used to send message to oneself for testing purposes

start "" "%~dp0Client\bin\Debug\net6.0\Client.exe"
"%~dp0Server\bin\Debug\net6.0\Server.exe"
pause
