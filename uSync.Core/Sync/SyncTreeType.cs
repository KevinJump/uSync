namespace uSync.Core.Sync
{
    /// <summary>
    ///  The type of tree item this is.
    /// </summary>
    /// <remarks>
    ///  in general things are 'settings' unless they 
    ///  are special. 
    ///  
    ///  content and media have node based permissions
    ///  so have to be handled slightly diffrently when 
    ///  we work out if a user has permission to see them
    ///  
    ///  when something doesn't have node based permissions
    ///  we can treat it as a 'settings' tree and show the 
    ///  menus when they are valid.
    /// </remarks>
    public enum SyncTreeType
    {
        /// <summary>
        ///  This item doesn't have options on its tree menu
        /// </summary>
        None, 

        /// <summary>
        ///  Sending settings (e.g doctypes, datatypes, non-content things)
        /// </summary>
        Settings,

        /// <summary>
        ///  sending content 
        /// </summary>
        Content,

        /// <summary>
        ///  sending media 
        /// </summary>
        Media, 

        /// <summary>
        ///  sending files
        /// </summary>
        File, 
    }

}
