(function () {
    'use strict';

    function changeDialogController($scope, assetsService) {

        var vm = this;
        vm.item = $scope.model.item;

        var jsdiff = 'lib/jsdiff/diff.min.js';

        assetsService.loadJs(jsdiff, $scope).then(function () {
            calcDiffs();
        });

        vm.close = close;
        vm.getTypeName = getTypeName;
        vm.pageTitle = pageTitle;

        function close() {
            if ($scope.model.close) {
                $scope.model.close();
            }
        }

        function getTypeName(typeName) {
            return typeName.substring(typeName.lastIndexOf('.') + 1);
        }

        function pageTitle() {
            return vm.item.change + ' ' + getTypeName(vm.item.itemType) + ' ' + vm.item.name;
        }

        function calcDiffs() {

            vm.item.details.forEach(function (detail, index) {


                let oldValueDiff = detail.oldValue === null ? "" : detail.oldValue;
                let newValueDiff = detail.newValue === null ? "" : detail.newValue;

                if (detail.oldValueJson instanceof Object) {
                    oldValueDiff = JSON.stringify(detail.oldValue, null, 1);
                }

                if (detail.newValueJson instanceof Object) {
                    newValueDiff = JSON.stringify(detail.newValue, null, 1);
                }

                detail.diff = JsDiff.diffWords(oldValueDiff, newValueDiff );
            });
        }
    }

    angular.module('umbraco')
        .controller('uSyncChangeDialogController', changeDialogController);
})();