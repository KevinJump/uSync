(function () {
    'use strict';

    function dashboardController(
        $scope, $timeout, navigationService, notificationsService, uSync8DashboardService) {

        var vm = this;

        vm.page = {
            title: 'uSync 9.Core',
            description: '9.x',
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

        uSync8DashboardService.getAddOns()
            .then(function (result) {
                vm.addOns = result.data.addOns;
                vm.addOns.forEach(function (value, key) {
                    if (value.view !== '') {
                        vm.page.navigation.splice(vm.page.navigation.length - 2, 0,
                            {
                                'name': value.displayName,
                                'alias': value.alias,
                                'icon': value.icon,
                                'view': value.view
                            });
                    }
                });
            });
    }

    angular.module('umbraco')
        .controller('uSyncSettingsDashboardController', dashboardController);
})();