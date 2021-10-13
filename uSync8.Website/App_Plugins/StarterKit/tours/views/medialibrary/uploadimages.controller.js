(function () {
    "use strict";

    function UploadImagesController($scope, editorState, mediaResource) {
        
        var vm = this;
        var element = angular.element($scope.model.currentStep.element);
        var currentNode = editorState.getCurrent();
        var imageCount = 0;

        vm.error = false;
        vm.loading = false;
        
        vm.initNextStep = initNextStep;

        function init() {

            vm.loading = true;
            
            mediaResource.getChildren(currentNode.id)
                .then(function (data) {
                    imageCount = data.totalItems;
                    vm.loading = false;
                });

        }

        function initNextStep() {

            vm.error = false;
            vm.buttonState = "busy";

            // make sure we have uploaded at least one image
            mediaResource.getChildren(currentNode.id)
                .then(function (data) {

                    var children = data;

                    if(children.items && children.totalItems > imageCount) {
                        $scope.model.nextStep();
                    } else {
                        vm.error = true;
                    }

                    vm.buttonState = "init";

                });

        }

        init();

    }

    angular.module("umbraco").controller("Umbraco.Starterkit.Tours.MediaLibrary.UploadImagesController", UploadImagesController);
})();
