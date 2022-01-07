angular.module("umbraco.directives").directive("dtgeBindHtmlCompile", ["$compile", function ($compile) {
    return {
        restrict: "A",
        link: function (scope, element, attrs) {
            scope.$watch(function () {
                return scope.$eval(attrs.dtgeBindHtmlCompile);
            }, function (value) {
                element.html(value);
                $compile(element.contents())(scope);
            });
        }
    };
}]);