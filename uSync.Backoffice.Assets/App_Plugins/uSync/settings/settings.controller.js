(function () {
    'use strict';

    function settingsController($scope,
        uSync8DashboardService,
        overlayService,
        notificationsService) {

        var vm = this;
        vm.working = false; 
        vm.loading = true;
        vm.readonly = true;

        vm.docslink = "https://docs.jumoo.co.uk/uSync/v9/settings/";

        vm.umbracoVersion = Umbraco.Sys.ServerVariables.application.version;

        vm.saveSettings = saveSettings;
        vm.openAppSettingsOverlay = openAppSettingsOverlay;

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
                    getHandlerSetSettings(vm.settings.defaultSet);
                });
        }

        function getHandlerSetSettings(setname) {

            uSync8DashboardService.getHandlerSetSettings(setname)
                .then(function (result) {
                    vm.handlerSet = result.data;
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



        function openAppSettingsOverlay() {

            var appSetting = {
                "uSync": {
                    "Settings": toPascal(vm.settings),
                    "Sets": {
                        "Default": toPascal(vm.handlerSet)
                    }
                }
            };

            var options = {
                view: Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/uSync/settings/settings.overlay.html',
                title: 'appsettings.json snipped',
                content: JSON.stringify(appSetting, null, 4),
                docslink: vm.docslink,
                disableBackdropClick: true,
                disableEscKey: true,
                hideSubmitButton: true,
                submit: function () {
                    overlayService.close();
                }
            };

            overlayService.confirm(options);

        }


        function toPascal(o) {
            var newO, origKey, newKey, value
            if (o instanceof Array) {
                return o.map(function (value) {
                    if (typeof value === "object") {
                        value = toCamel(value)
                    }
                    return value
                })
            } else {
                newO = {}
                for (origKey in o) {
                    if (o.hasOwnProperty(origKey)) {
                        newKey = (origKey.charAt(0).toUpperCase() + origKey.slice(1) || origKey).toString()
                        value = o[origKey]
                        if (value instanceof Array || (value !== null && value.constructor === Object)) {
                            value = toPascal(value)
                        }
                        newO[newKey] = value
                    }
                }
            }
            return newO
        }



    }

    angular.module('umbraco')
        .controller('uSyncSettingsController', settingsController);
})();