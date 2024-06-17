@echo off
MD  ServerRuntime

xcopy MasterServer\bin\Release\net6.0\*.* ServerRuntime/s /y /r
xcopy GameServer\bin\Release\net6.0\*.* ServerRuntime/s /y /r

xcopy HotLibrary\bin\Release\net6.0\HotLibrary.dll ServerRuntime\HotDlls/s /y /r
pause