@ECHO OFF

ECHO Packaging uSync Umbraco Package
CALL UmbPack pack package.xml -o .\%1 -v %1

ECHO Packaging uSync.ContentEditon Umbraco Package
CALL UmbPack pack Package.ContentEdition.xml -o .\%1 -v %1