echo off
title "Installation"
cls
copy /Y "%~dp0\bin\Release\CSGO Presence.exe" "C:\Program Files\CSGORichPresence\CSGO Presence.exe"
"%~dp0installutil.exe" "C:\Program Files\CSGORichPresence\CSGO Presence.exe"
net start CSGORichPresence
pause
