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
            var umbType = typeName.substring(0, typeName.indexOf(','));
            return umbType.substring(umbType.lastIndexOf('.') + 1);
        }

        function pageTitle() {
            return vm.item.Change + ' ' + getTypeName(vm.item.ItemType) + ' ' + vm.item.Name;
        }

        function calcDiffs() {

            vm.item.Details.forEach(function (detail, index) {


                let oldValueDiff = detail.OldValue === null ? "" : detail.OldValue;
                let newValueDiff = detail.NewValue === null ? "" : detail.NewValue;

                if (detail.oldValueJson instanceof Object) {
                    oldValueDiff = JSON.stringify(detail.OldValue, null, 1);
                }

                if (detail.newValueJson instanceof Object) {
                    newValueDiff = JSON.stringify(detail.NewValue, null, 1);
                }

                detail.diff = JsDiff.diffWords(oldValueDiff, newValueDiff );
            });
        }
    }

    angular.module('umbraco')
        .controller('uSyncChangeDialogController', changeDialogController);
})();