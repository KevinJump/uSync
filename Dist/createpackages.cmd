@ECHO OFF

ECHO Packaging uSync Umbraco Package
CALL UmbPack pack .\dist\package.xml -o .\dist\%1 -v %1

ECHO Packaging uSync.ContentEditon Umbraco Package
CALL UmbPack pack .\dist\Package.ContentEdition.xml -o .\dist\%1 -v %1