
       __                   _____      _                           
 _   _/ _\_   _ _ __   ___ /__   \_ __(_) __ _  __ _  ___ _ __ ___ 
| | | \ \| | | | '_ \ / __|  / /\/ '__| |/ _` |/ _` |/ _ \ '__/ __|
| |_| |\ \ |_| | | | | (__ _/ /  | |  | | (_| | (_| |  __/ |  \__ \
 \__,_\__/\__, |_| |_|\___(_)/   |_|  |_|\__, |\__, |\___|_|  |___/
          |___/                          |___/ |___/               


  Thanks for downloading uSync.triggers. 

  This will add an endpoint to your site that you can call without
  being authenticated, that can start uSync import or export 
  processes. 

  -----

  n.b : this will not work until you have a uSync.Triggers value
  in your web.config 
  
  <add key="uSync.Triggers" value="True"/>
  
  -----

  With this set you can call import or export, via post or get

  You need to supply a valid umbraco username/password 
  the umbraco user must have access to the settings section 
  (so can see the uSync tree)


  {siteUrl}/umbraco/usync/trigger/import

  Valid Actions: 
      import: /umbraco/usync/trigger/import
      export: /umbraco/usync/trigger/export

  Additional options (for all actions)

     Group  : Handler group to use (e.g settings / content )
     
     Set    : Handler set from the usync8.config file to use

     Folder : location on disk you want to import/export 

         Note: 
             Folder will only work inside usyncfolder unless
             you add 
             <add key="uSync.TriggerFolderLimits" value="false"/>
             to your web.config. 


  CURL Examples: 

    // import default options 

    curl -u user:password http://my.site/umbraco/usync/triggers/import

    // import only settings 

    curl -X POST -d 'group=settings' -u user:password http://my.site/umbraco/usync/triggers/import

    -----
 
    For extra security consider restricting /umbraco/usync by IP
    in your web.config
 
    -----
