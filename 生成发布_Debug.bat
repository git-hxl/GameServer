@echo off
MD  ServerRuntime

xcopy MasterServer\bin\Debug\net6.0\*.* ServerRuntime/s /y /r
xcopy GameServer\bin\Debug\net6.0\*.* ServerRuntime/s /y /r

xcopy HotLibrary\bin\Debug\net6.0\HotLibrary.dll ServerRuntime\HotDlls/s /y /r
pause