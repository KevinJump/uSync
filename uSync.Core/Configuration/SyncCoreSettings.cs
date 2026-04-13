using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace uSync.Core.Configuration;

/// <summary>
///  Core behavior settings, 
/// </summary>
/// <remarks>
///  these are used sparingly, and change the default behaviors in value mappers
///  or other core bits of the uSync code base. 
///  
///  The changes apply to all setups, (eg. all sets, core, publisher, exporter, etc).
///  they are not things you can tweak depending on what you are doing.
/// </remarks>
public class SyncCoreSettings
{
    /// <summary>
    ///  Update an Image Urls, that contain a hmac query value. 
    /// </summary>
    /// <remarks>
    ///  by default (v17.3+) each site will have its own HMAC value that 
    ///  is appended to images to stop DOS attacks and the like. 
    ///  
    ///  The recommendation is all your sites share their HMAC seceret so
    ///  the key is the same on all sites, but if you don't do that then 
    ///  you will get missing images. This value lets you turn that mapping 
    ///  on, so uSync will map the HMAC keys for you.  
    /// </remarks>
    [DefaultValue(false)]
    public bool UpdateHMACUrls { get; set; } = false;
}
