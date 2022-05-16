(function () {

    'use strict';

    function expansionController($scope, uSync8DashboardService) {

        var vm = this;
        vm.loading = true;
        ///

        uSync8DashboardService.getAddOnSplash()
            .then(function (result) {
                vm.addons = result.data;
                vm.loading = false;
            });
    }

    angular.module('umbraco')
        .controller('uSyncExpansionController', expansionController);
})();