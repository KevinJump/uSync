(function () {
    'use strict';

    function changeDialogController($scope) {

        var vm = this;
        vm.item = $scope.model.item;


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
    }

    angular.module('umbraco')
        .controller('uSyncChangeDialogController', changeDialogController);
})();