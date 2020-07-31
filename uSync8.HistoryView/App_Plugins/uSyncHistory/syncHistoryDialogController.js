(function () {
    'use strict';

    function historyDialogController($scope, $filter) {

        var vm = this;
        vm.close = close;

        vm.history = $scope.model.history;

        vm.title = vm.history.Action + ' History ';
        vm.description = 'by : ' + vm.history.Username + ' @ ' + $filter('date')(vm.history.When, 'medium');

        /////////////

        function close() {
            if ($scope.model.close) {
                $scope.model.close();
            }
        }

    }

    angular.module('umbraco')
        .controller('uSyncHistoryDialogController', historyDialogController);

})();