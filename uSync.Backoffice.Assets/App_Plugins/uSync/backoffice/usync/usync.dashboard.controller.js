(function () {
    'use strict';

    function dashboardController($controller,
        $scope, $timeout, navigationService, eventsService, uSync8DashboardService) {

        var vm = this;

        var _settingsFolder = Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/uSync/settings';

        vm.selectNavigationItem = function (item) {
            eventsService.emit('usync-dashboard.tab.change', item);
        }

        vm.page = {
            title: 'uSync',
            description: '...',
            navigation: [ ]
        };

        var uSyncSettings = Umbraco.Sys.ServerVariables.uSync;

        if (!uSyncSettings.disabledDashboard) {
            vm.page.navigation.push({
                'name': 'uSync',
                'alias': 'uSync',
                'icon': 'icon-infinity',
                'view': _settingsFolder + '/default.html',
                'active': true
            });
        }

        vm.page.navigation.push({
            'name': 'Settings',
            'alias': 'settings',
            'icon': 'icon-settings',
            'view': _settingsFolder + '/settings.html',
        });


        $timeout(function () {
            navigationService.syncTree({ tree: "uSync", path: "-1" });
        });

        uSync8DashboardService.getAddOns()
            .then(function (result) {

                vm.version = 'v' + result.data.version;
                if (result.data.addOnString.length > 0) {
                    vm.version += ' + ' + result.data.addOnString;
                }

                vm.page.description = vm.version;
                vm.addOns = result.data.addOns;

                var insertOffset = 1;
                if (vm.version.indexOf('Complete') == -1) {
                     insertOffset = 2;
                     vm.page.navigation.push(
                         {
                             'name': 'Add ons',
                             'alias': 'expansion',
                             'icon': 'icon-box',
                             'view': _settingsFolder + '/expansion.html'
                         });
                }

                vm.addOns.forEach(function (value, key) {
                    if (value.view !== '') {
                        vm.page.navigation.splice(vm.page.navigation.length - insertOffset, 0,
                            {
                                'name': value.displayName,
                                'alias': value.alias,
                                'icon': value.icon,
                                'view': value.view
                            });
                    }
                });

                vm.page.navigation[0].active = true;
            });
    }

    angular.module('umbraco')
        .controller('uSyncSettingsDashboardController', dashboardController);
})();