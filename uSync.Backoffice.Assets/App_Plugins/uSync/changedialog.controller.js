(function () {
    'use strict';

    function changeDialogController($scope,
        assetsService,
        uSync8DashboardService,
        overlayService) {

        var vm = this;
        vm.item = $scope.model.item;

        var jsdiff = 'lib/jsdiff/diff.js';

        assetsService.loadJs(jsdiff, $scope).then(function () {
            calcDiffs();
        });

        vm.close = close;
        vm.getTypeName = getTypeName;
        vm.pageTitle = pageTitle;

        vm.showApply = $scope.model.showApply ?? false;
        vm.apply = apply;

        function close() {
            if ($scope.model.close) {
                $scope.model.close();
            }
        }


        function apply(item) {

            overlayService.open({
                view: Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/uSync/dialogs/apply.html',
                title: 'Apply changes to ' + item.name,
                item: item,
                size: 'small',
                disableBackdropClick: true,
                disableEscKey: true,
                submitButtonLabelKey: 'usync_apply',
                closeButtonLabelKey: 'general_close',
                submit: function (model) {
                    doApply(model.item, function (result) {
                        model.hideSubmitButton = true;
                        model.done = true;
                        model.success = result;
                    });
                },
                close: function () {
                    overlayService.close();
                }
            });
        }

        function doApply(item, cb) {

            // do some application thing (apply just one item)
            item.applyState = 'busy';
            uSync8DashboardService.importItem(item)
                .then(function (result) {
                    item._result = result.data;
                    cb(true);
                }, function (error) {
                    console.error(error);
                    cb(false);
                });
        }

        function getTypeName(typeName) {
            return typeName.substring(typeName.lastIndexOf('.') + 1);
        }

        function pageTitle() {
            return vm.item.change + ' ' + getTypeName(vm.item.itemType) + ' ' + vm.item.name;
        }

        function calcDiffs() {

            vm.item.details.forEach(function (detail, index) {


                let oldValueDiff = detail.oldValue === null ? "" : detail.oldValue;
                let newValueDiff = detail.newValue === null ? "" : detail.newValue;

                if (detail.oldValueJson instanceof Object) {
                    oldValueDiff = JSON.stringify(detail.oldValue, null, 1);
                }

                if (detail.newValueJson instanceof Object) {
                    newValueDiff = JSON.stringify(detail.newValue, null, 1);
                }

                detail.diff = Diff.diffWords(oldValueDiff, newValueDiff);
            });
        }
    }

    angular.module('umbraco')
        .controller('uSyncChangeDialogController', changeDialogController);
})();