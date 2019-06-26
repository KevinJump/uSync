(function () {
    'use strict';

    var uSyncProgressViewComponent = {
        templateUrl: Umbraco.Sys.ServerVariables.application.applicationPath + 'App_Plugins/uSync8/Components/uSyncProgressView.html',
        bindings: {
            status: '<',
            update: '<',
            hideLabels: '<'
        },
        controllerAs: 'vm',
        controller: uSyncProgressViewController
    };

    function uSyncProgressViewController() {
        var vm = this;

        vm.calcPercentage = calcPercentage;

        function calcPercentage(status) {
            if (status !== undefined) {
                return (100 * status.Count) / status.Total;
            }
            return 1;
        }
    }

    angular.module('umbraco')
        .component('usyncProgressView', uSyncProgressViewComponent);
})();