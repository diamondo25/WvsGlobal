Patcher.exe in this directory is the fixed version. It will download from ftp://patch.mapleglobal.com in an passive connection.
The patches include:
- Fix URLs to point to patch.mapleglobal.com
- Fix active connection issue (set 0x08000000 in flags of InternetConnectURL)

Version.info has the following format:
```
0x{CRC of Patcher.exe}
{ list of supported versions, where the version will be used for {version}to{current version}.patch filename }
```

For V.2 this is:
```
0x741C5CE3
00002
```


The CRC should be calculated through dropping the patcher on the PatchCreator exe.