const { watch, src, dest } = require('gulp');
const del = require('del');
var concat = require('gulp-concat');
var uglify = require('gulp-uglify');
var sourcemaps = require('gulp-sourcemaps');
const cleanCSS = require('gulp-clean-css');
const transformManifest = require('./gulp-transformManifest');

const sourceFolders = [
    'uSync.BackOffice.Assets/App_Plugins/uSync/'
];

const pluginName = 'uSync';
const destination = 'uSync.Site/App_Plugins/uSync';
const minDest = 'uSync.BackOffice.Assets/wwwroot/';

// fetch command line arguments
const arg = (argList => {

    let arg = {}, a, opt, thisOpt, curOpt;
    for (a = 0; a < argList.length; a++) {
  
      thisOpt = argList[a].trim();
      opt = thisOpt.replace(/^\-+/, '');
  
      if (opt === thisOpt) {
  
        // argument value
        if (curOpt) arg[curOpt] = opt;
        curOpt = null;
  
      }
      else {
  
        // argument name
        curOpt = opt;
        arg[curOpt] = true;
  
      }
  
    }
  
    return arg;
  
  })(process.argv);

  function copy(path, baseFolder) {
    return src(path, { base: baseFolder })
        .pipe(dest(destination));
}

function minifyJs(path, version) {

    return src(path, { base: path })
        .pipe(sourcemaps.init())
            .pipe(concat('usync.' + version + '.min.js'))
            .pipe(uglify({ mangle: false }))
        .pipe(sourcemaps.write('.'))
        .pipe(dest(minDest));
}


function minifyCss(path, version) {
    return src([path, '!**/nonodes.css'], { base: path })
        .pipe(cleanCSS({ compatibility: 'ie8'}))
        .pipe(concat('usync.' + version + '.min.css'))
        .pipe(dest(minDest));
}

function copyRequired(path, baseFolder, destFolder) {
    return src([path, '!**/*.manifest', '!**/*.js', '!**/*.css'], { base: baseFolder })
        .pipe(dest(destFolder));
}

function copySpecific(file, baseFolder, destfolder) {
    return src(file, { base: baseFolder })
        .pipe(dest(destfolder));
}

function time() {
    return '[' + new Date().toISOString().slice(11, -5) + ']';
}

function updateManifest(manifest, version) {

    return src(manifest)
        .pipe(transformManifest({
            folder : pluginName,
            name: pluginName.toLowerCase(),
            version: version
        }))
        .pipe(dest(minDest));

}


////////////
exports.default = function() {
    sourceFolders.forEach(function (sourceFolder) {

        let source = sourceFolder + '**/*';
       
        watch(source, { ignoreInitial: false })
            .on('change', function (path, stats) {
                console.log(time(), path, sourceFolder, 'changed');
                copy(path, sourceFolder);
            })
            .on('add', function (path, stats) {
                console.log(time(), path, sourceFolder, 'added');
                copy(path, sourceFolder);
            });
    });

};

exports.minify = function (cb) {
    
    var version = arg.release;
   
    sourceFolders.forEach(function (sourceFolder) {
        let source = sourceFolder + '**/*';
        let jsfiles = sourceFolder + '**/*.js';
        let cssFiles = sourceFolder + '**/*.css';

        minifyJs(jsfiles, version);
        minifyCss(cssFiles, version);
        updateManifest(sourceFolder + "/package.manifest", version);
        copyRequired(source, sourceFolder, minDest);
        copySpecific(sourceFolder + '**/nonodes.css', sourceFolder, minDest);
    });

    cb();
}

