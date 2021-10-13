(function () {
    "use strict";

    function ValidateTextController($scope) {
        
        var vm = this;
        var element = angular.element($scope.model.currentStep.element);
        var validateText = $scope.model.currentStep.customProperties.validateText ? $scope.model.currentStep.customProperties.validateText : "";

        vm.error = false;
        
        vm.initNextStep = initNextStep;

        function initNextStep() {
            if(element.val() === validateText) {
                $scope.model.nextStep();
            } else {
                vm.error = true;
            }
        }

    }

    angular.module("umbraco").controller("Umbraco.Starterkit.Tours.ValidateTextController", ValidateTextController);
})();
