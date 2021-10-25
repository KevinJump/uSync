/// <binding ProjectOpened='default' />
const { watch, src, dest } = require('gulp');
var concat = require('gulp-concat');
var uglify = require('gulp-uglify');
var sourcemaps = require('gulp-sourcemaps');
const cleanCSS = require('gulp-clean-css');
const transformManifest = require('./gulp-transformManifest');

const sourceFolders = [
    'uSync.BackOffice.Assets/App_Plugins/'
];

const version = "901";
const pluginName = 'uSync';
const destination = 'uSync.Site/App_Plugins/';
const minDest = destination + pluginName + '/';

function copy(path, baseFolder) {
    return src([path, '!**/*.js', '!**/*.css', '!**/*.manifest'], { base: baseFolder, allowEmpty: true })
        .pipe(dest(destination));
}

function minifyJs(path) {

    return src(path, { base: path })
        .pipe(sourcemaps.init())
            .pipe(concat('usync.' + version + '.min.js'))
            .pipe(uglify({ mangle: false }))
        .pipe(sourcemaps.write('.'))
        .pipe(dest(minDest));
}


function minifyCss(path) {
    return src(path, { base: path })
        .pipe(cleanCSS({ compatibility: 'ie8'}))
        .pipe(concat('usync.' + version + '.min.css'))
        .pipe(dest(minDest));
}

function time() {
    return '[' + new Date().toISOString().slice(11, -5) + ']';
}

function updateManifest(manifest) {

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
        let jsfiles = sourceFolder + '**/*.js';
        let cssFiles = sourceFolder + '**/*.css';

        minifyJs(jsfiles);
        minifyCss(cssFiles);
        updateManifest(sourceFolder + pluginName +  "/package.manifest");
        
        watch(source, { ignoreInitial: false })
            .on('change', function (path, stats) {
                console.log(time(), path, sourceFolder, 'changed');
                copy(path, sourceFolder);
                minifyjs(jsfiles);
                minifyCss(cssFiles);
            })
            .on('add', function (path, stats) {
                console.log(time(), path, sourceFolder, 'added');
                copy(path, sourceFolder);
            });
    });

};


