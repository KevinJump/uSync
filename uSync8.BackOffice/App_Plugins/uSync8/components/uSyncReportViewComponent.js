﻿(function () {
    'use strict';

    var uSyncReportViewComponent = {
        templateUrl: Umbraco.Sys.ServerVariables.application.applicationPath + 'App_Plugins/uSync8/Components/uSyncReportView.html',
        bindings: {
            action: '<',
            results: '<',
            hideAction: '<',
            hideLink: '<',
            showAll: '<'
        },
        controllerAs: 'vm',
        controller: uSyncReportViewController
    };

    function uSyncReportViewController($scope, editorService, uSync8DashboardService) {

        var vm = this;

        vm.showChange = showChange;
        vm.getIcon = getIcon;
        vm.getTypeName = getTypeName;
        vm.countChanges = countChanges;
        vm.openDetail = openDetail;
        vm.showAll = vm.showAll || false;

        vm.$onInit = function () {
            vm.hideLink = vm.hideLink ? true : false;
            vm.hideAction = vm.hideAction ? true : false;
        };


        vm.apply = apply;
        vm.status = status;

        /////////

        function showChange(change) {
            return vm.showAll || (change !== 'NoChange' && change !== 'Removed');
        }

        function getIcon(change) {
            switch (change) {
                case 'NoChange':
                    return 'icon-check color-grey';
                case 'Update':
                    return 'icon-check color-orange';
                case 'Delete':
                    return 'icon-delete color-red';
                case 'Import':
                    return 'icon-check color-green';
                case 'Export':
                    return 'icon-check color-green';
                default:
                    return 'icon-flag color-red';
            }
        }

        function getTypeName(typeName) {
            var umbType = typeName.substring(0, typeName.indexOf(','));
            return umbType.substring(umbType.lastIndexOf('.') + 1);
        }

        function countChanges(changes) {
            var count = 0;
            angular.forEach(changes, function (val, key) {
                if (val.Change !== 'NoChange') {
                    count++;
                }
            });

            return count;
        }

        function openDetail(item) {

            var options = {
                item: item,
                title: 'uSync Change',
                view: Umbraco.Sys.ServerVariables.application.applicationPath + "App_Plugins/uSync8/changeDialog.html",
                close: function () {
                    editorService.close();
                }
            };
            editorService.open(options);
        }

        function apply(item) {

            // do some application thing (apply just one item)
            item.applyState = 'busy';
            uSync8DashboardService.importItem(item)
                .then(function (result) {
                    console.log(result.data);
                    item.applyState = 'success';
                }, function (error) {
                    console.log(error);
                    item.applyState = 'error';
                });
        }

        function status(item) {
            if (item.applyState === undefined) return 'init';
            return item.applyState;
        }

    }

    angular.module('umbraco')
        .component('usyncReportView', uSyncReportViewComponent);
})();