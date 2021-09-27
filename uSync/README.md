# About 

Keep your Umbraco settings in sync - uSync in an Umbraco package that takes the bits of Umbraco that are stored in a database and moves them to disk, so you can source control, copy and move your Umbraco site between computers and servers.

![Importing with uSync](https://raw.githubusercontent.com/KevinJump/uSync/v9/main/screenshots/importing.gif)

uSync will read/write 
- Document Types,
- Media Types,
- Data Types
- Macros
- Member Types
- Templates

With content syncing enabled it will also do : 
- Content
- Media
- Dictionary Items
- Languages
- Domains


## Config 

for v9 everything minimal config should be required. 

### Turn of content syncing 
If you only want to sync settings update the appsettings.json file:

setup uSync so it will only save changes to "Settings" items.

```
"uSync" : {
    "Settings: {
          "ExportOnSave": "Settings",
    }
}
```