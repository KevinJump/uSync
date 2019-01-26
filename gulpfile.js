/// <binding ProjectOpened='default' />
const { watch, src, dest } = require('gulp');

const sourceFolder = 'uSync8.BackOffice/App_Plugins/uSync8/';

const source = sourceFolder + '**/*';
const destination = 'uSync8.Site/App_Plugins/uSync8';


function copy(path) {

    return src(path, { base: sourceFolder })
        .pipe(dest(destination));
}

function time() {
    return '[' + new Date().toISOString().slice(11, -5) + ']';
}

exports.default = function () {
    watch(source, { ignoreInitial: false })
        .on('change', function (path, stats) {
            console.log(time(), path, 'changed');
            copy(path);
        })
        .on('add', function (path, stats) {
            console.log(time(), path, 'added');
            copy(path);
        });
};
    

