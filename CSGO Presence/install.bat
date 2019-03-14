echo off
title "Installation"
cls
"%~dp0installutil.exe" "%~dp0\bin\Release\CSGO Presence.exe"
net start CSGORichPresence
pause