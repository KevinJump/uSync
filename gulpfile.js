/// <binding ProjectOpened='default' />
const { series, watch, src, dest } = require('gulp');

const source = 'uSync8.BackOffice/App_Plugins/uSync8/**/*';
const destination = 'uSync8.Site/App_Plugins/uSync8';

function copyAppData() {
    return src(source)
        .pipe(dest(destination));
}

exports.default = function () {
    watch(source, { ignoreInitial: false }, copyAppData);
};


