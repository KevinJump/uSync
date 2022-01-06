const through = require('through2');

function transformManifest(opts, file) {

    const manifest = JSON.parse(file.contents.toString());

    let folder = opts.folder;
    let name = opts.name;
    let version = opts.version
    
    manifest.version = version;
    manifest.bundleOptions = "Independent";
    manifest.javascript = ["~/App_Plugins/" + folder  + "/" + name + "." + version + ".min.js"];
    manifest.css = ["~/App_Plugins/" + folder  + "/" + name + "." + version + ".min.css"]

    const stringContents = JSON.stringify(manifest, null, 2);
    const newBuffer = Buffer.alloc(stringContents.length, stringContents);
    file.contents = newBuffer;
    return file;
}

/////////

module.exports = function (opts) {
    return through.obj(function (file, encoding, callback) {
       callback(null, transformManifest(opts, file)) 
    });
}

