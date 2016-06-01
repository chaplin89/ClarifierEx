@echo off
call "%VS140COMNTOOLS%\VsDevCmd.bat"
msbuild .\ConfuserEx\Confuser2.sln /t:Build /p:Configuration=Debug
msbuild .\ConfuserEx\Confuser2.sln /t:Build /p:Configuration=Release
pause