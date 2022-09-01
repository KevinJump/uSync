(function () {
    'use strict';

    function importDialogController($scope, Upload, notificationsService) {

        var vm = this;

        vm.close = close;
        vm.submit = submit;

        vm.errors = [];
        vm.uploaded = false; 
        vm.success = false; 

        vm.buttonState = 'init';
        vm.file = null;
        vm.handleFiles = handleFiles;
        vm.upload = upload;

        ////////

        function handleFiles(files, event) {
            if (files && files.length > 0) {
                vm.file = files[0];
            }
        }

        function upload(file) {
            vm.buttonState = 'busy';

            Upload.upload({
                url: Umbraco.Sys.ServerVariables.uSync.uSyncService + 'UploadImport',
                fields: {
                    clean: vm.cleanImport
                },
                file: file
            }).success(function (data, status, headers, config) {
                vm.uploaded = true;
                vm.success = data.success;

                if (data.success) {
                    vm.buttonState = 'success';
                }
                else {
                    vm.buttonState = 'error';
                    vm.errors = data.errors; 
                }
            }).error(function (event, status, headers, config) {
                vm.uploaded = true;
                vm.success = false; 
                vm.buttonState = 'error';
                notificationsService.error('error', 'Failed to upload '
                    + status + ' ' + event.ExceptionMessage);

                vm.errors.push('Zip file upload error ' + 
                    '[' + status + '] ' +
                    event.ExceptionMessage);
            });
        }


        ////////

        function close() {
            if ($scope.model.close) {
                $scope.model.close();
            }
        }

        function submit(action) {
            if ($scope.model.submit) {
                $scope.model.submit(action);
            }
        }

    }

    angular.module('umbraco')
        .controller('uSyncImportDialogController', importDialogController);
})();