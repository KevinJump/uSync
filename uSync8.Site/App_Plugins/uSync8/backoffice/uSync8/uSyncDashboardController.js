(function () {
    'use strict';

    function dashboardController(
        $scope, $timeout, navigationService, notificationsService, uSync8DashboardService) {

        var vm = this;

        vm.page = {
            title: 'uSync 8',
            description: '8.1.x',
            navigation: [
                {
                    'name': 'uSync',
                    'alias': 'uSync',
                    'icon': 'icon-infinity',
                    'view': Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/usync8/settings/default.html',
                    'active': true
                },
                {
                    'name': 'Settings',
                    'alias': 'settings',
                    'icon': 'icon-settings',
                    'view': Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/uSync8/settings/settings.html'
                },
                {
                    'name': 'Add ons',
                    'alias': 'expansion',
                    'icon': 'icon-box',
                    'view': Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/usync8/settings/expansion.html'
                } 
            ]
        };

        $timeout(function () {
            navigationService.syncTree({ tree: "uSync8", path: "-1" });
        });
    }

    angular.module('umbraco')
        .controller('uSyncSettingsDashboardController', dashboardController);
})();