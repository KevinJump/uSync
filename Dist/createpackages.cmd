@ECHO OFF

CALL UmbPackage package.xml %1
CALL "c:\Program Files\7-Zip\7z.exe" a .\%1\uSync_%1.zip .\%1\uSync_%1\*.* 
CALL RD .\%1\uSync_%1\ /s /q

CALL UmbPackage package.ContentEdition.xml %1
CALL "c:\Program Files\7-Zip\7z.exe" a .\%1\uSync.ContentEdition_%1.zip .\%1\uSync.ContentEdition_%1\*.* 
CALL RD .\%1\uSync.ContentEdition_%1\ /s /q
