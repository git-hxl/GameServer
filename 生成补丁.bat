@echo off

MD HotDlls

xcopy HotLibrary\bin\Release\net6.0\HotLibrary.dll HotDlls/s /y /r

set path=%date:~0,4%%date:~5,2%%date:~8,2%_%time:~0,2%%time:~3,2%%time:~6,2%

ren HotDlls %path%

pause