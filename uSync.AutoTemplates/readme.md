# uSync.AutoTemplates

> **Current Status: Experimental**
>
> AutoTemplates is currently a uSync experiment, you can try it, and we would love feedback, but really make sure you have backups, etc, before you use it.
> 
> You need to get uSync from our [nightly feed](../nightly.md) to get the nuget package.

Template folder watcher, that keeps your template folder in Sync with Umbraco's Templates view. 

If you create or edit .cshtml files in the view folder outside of Umbraco AutoTemplates will attempt to workout how the file should fit into the templates. 


## How it works

**Note: Only works when Umbraco:Hosting:Mode is debug**

1. AutoTemplates watches the ~/views folder in your Umbraco site, 

2. Tries to find the Umbraco template in the DB for the file, if it doesn't exist it creates the template entry. 

3. Reads the file and reads the value of the Layout variable, to workout what the master template should be - and put the template in umbraco in the right place 

- also scans the folder at startup for when you change things while Umbraco isn't running. 

## Settings 

By default - AutoTemplates is disabled, you have to turn it on in the 
`appsettings.config` file: 

### Enable AutoTemplates (default: false)

```json
"uSync": {
    "AutoTemplates": {
        "Enabled": true
    }
}
```

### Delete missing templates (default: false)
When enabled - AutoTemplates will delete Templates from the Umbraco database
when it cannot find a file in the ~/views folder for that template. 

this is off by default, changing settings such as compile razor views can 
cause these files to be missing from disk, so if you have made changes like
that you should keep this value turned off. 


```json
"uSync": {
    "AutoTemplates": {
        "Enabled": true
        "Delete": true
    }
}
```


