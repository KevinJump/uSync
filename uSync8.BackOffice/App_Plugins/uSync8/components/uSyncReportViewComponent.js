(function () {
    'use strict';

    var uSyncReportViewComponent = {
        templateUrl: Umbraco.Sys.ServerVariables.application.applicationPath + 'App_Plugins/uSync8/Components/uSyncReportView.html',
        bindings: {
            action: '<',
            results: '<'
        },
        controllerAs: 'vm',
        controller: uSyncReportViewController
    };

    function uSyncReportViewController($scope, editorService) {

        var vm = this;

        vm.showChange = showChange;
        vm.getTypeName = getTypeName;
        vm.countChanges = countChanges;
        vm.openDetail = openDetail;
        vm.showAll = false; 

        /////////

        function showChange(change) {
            return vm.showAll || (change !== 'NoChange' && change !== 'Removed');
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
                view: "/App_Plugins/uSync8/changeDialog.html",
                close: function () {
                    editorService.close();
                }
            };
            editorService.open(options);
        }

    }

    angular.module('umbraco')
        .component('usyncReportView', uSyncReportViewComponent);
})();