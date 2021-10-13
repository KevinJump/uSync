angular.module('umbraco.services').factory('lessonsService', function ($http, $q, umbRequestHelper) {
       
    var service = {
            
        getLessons: function (path) {
            var qs = "?path=" + path;
            var url = umbRequestHelper.getApiUrl("lessonsApiBaseUrl", "GetLessons" + qs);
            return umbRequestHelper.resourcePromise($http.get(url), "Failed to get lessons content");
        },

        getLessonSteps: function (path) {
            var qs = "?path=" + path;
            var url = umbRequestHelper.getApiUrl("lessonsApiBaseUrl", "GetLessonSteps" + qs);
            return umbRequestHelper.resourcePromise($http.get(url), "Failed to get lessons content");
        }
    };

    return service;

});