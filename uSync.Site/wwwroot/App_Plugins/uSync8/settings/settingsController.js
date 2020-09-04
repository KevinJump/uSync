(function () {
    'use strict';

    function settingsController($scope,
        uSync8DashboardService,
        notificationsService) {

        var vm = this;
        vm.working = false; 
        vm.loading = true; 

        vm.umbracoVersion = Umbraco.Sys.ServerVariables.application.version;

        vm.saveSettings = saveSettings;

        init();

        ///////////

        function init() {
            getSettings();
        }

        ///////////
        function getSettings() {

            uSync8DashboardService.getSettings()
                .then(function (result) {
                    vm.settings = result.data;
                    vm.loading = false;
                });
        }
        

        function saveSettings() {
            vm.working = false;
            uSync8DashboardService.saveSettings(vm.settings)
                .then(function (result) {
                    vm.working = false;
                    notificationsService.success('Saved', 'Settings updated');
                }, function (error) {
                    notificationsService.error('Saving', error.data.Message);
                });
        }

    }

    angular.module('umbraco')
        .controller('uSyncSettingsController', settingsController);
})();