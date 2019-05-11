echo off
title "Installation"
cls
mkdir "C:\Program Files\CSGORichPresence"
copy /Y "%~dp0\bin\Release\CSGO Presence.exe" "C:\Program Files\CSGORichPresence\CSGO Presence.exe"
"%~dp0installutil.exe" "C:\Program Files\CSGORichPresence\CSGO Presence.exe"
net start CSGORichPresence
pause
