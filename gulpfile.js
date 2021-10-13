/// <binding ProjectOpened='default' />
const { watch, src, dest } = require('gulp');

const sourceFolders = [
    'uSync8.BackOffice/App_Plugins/',
    'uSync8.HistoryView/App_Plugins/'];

const destination = 'uSync8.Website/App_Plugins/';


function copy(path, baseFolder) {

    return src(path, { base: baseFolder })
        .pipe(dest(destination));
}

function time() {
    return '[' + new Date().toISOString().slice(11, -5) + ']';
}

exports.default = function () {

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


