angular.module("umbraco")
    .controller("Bergmania.OpenStreetMap.Controller", ["$scope", "$element",  function ($scope, $element) {
        const vm = this;

        vm.currentMarker = null;

        function onInit(){

            const initValue = $scope.model.value || $scope.model.config.defaultPosition || { marker : { latitude: 54.975556, longitude : -1.621667}, "boundingBox":{"southWestCorner":{"latitude":54.970495269313204,"longitude":-1.6278648376464846},"northEastCorner":{"latitude":54.97911600936982,"longitude":-1.609625816345215}}, zoom: 16};
            const tileLayer = $scope.model.config.tileLayer || 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png';
            const tileLayerOptions = { attribution:$scope.model.config.tileLayerAttribution };

            vm.map = L.map($element.find("[data-openstreetmap]")[0])
                .fitBounds(L.latLngBounds(L.latLng(initValue.boundingBox.southWestCorner.latitude, initValue.boundingBox.southWestCorner.longitude),
                    L.latLng(initValue.boundingBox.northEastCorner.latitude, initValue.boundingBox.northEastCorner.longitude)));

            L.tileLayer(tileLayer, tileLayerOptions).addTo(vm.map);

            vm.map.on('click', onMapClick);
            vm.map.on('moveend', updateModel);
            vm.map.on('zoomend', updateModel);
            vm.map.on('contextmenu', clearMarker);

            if(initValue.marker){
                vm.currentMarker = L.marker(L.latLng(initValue.marker.latitude, initValue.marker.longitude), {draggable:true,}).addTo(vm.map);
            }
        }
        function clearMarker() {
            if(vm.currentMarker){
                vm.currentMarker.remove(vm.map);
                vm.currentMarker = null;
            }

            updateModel();
        }

        function onMapClick(e) {
            clearMarker();

            vm.map.setView(e.latlng);
            vm.currentMarker = L.marker(e.latlng, {draggable:true,}).addTo(vm.map);

            updateModel(e);
        }

        function updateModel(){

            $scope.model.value = {};

            $scope.model.value.zoom = vm.map.getZoom();

            if(!$scope.model.value.boundingBox){
                $scope.model.value.boundingBox = {};
            }
            if(!$scope.model.value.boundingBox.southWestCorner){
                $scope.model.value.boundingBox.southWestCorner = {};
            }
            if(!$scope.model.value.boundingBox.northEastCorner){
                $scope.model.value.boundingBox.northEastCorner = {};
            }

            const northEastCorner = vm.map.getBounds().getNorthEast();
            const southWestCorner = vm.map.getBounds().getSouthWest();

            $scope.model.value.boundingBox.northEastCorner.latitude = northEastCorner.lat;
            $scope.model.value.boundingBox.northEastCorner.longitude = northEastCorner.lng;
            $scope.model.value.boundingBox.southWestCorner.latitude = southWestCorner.lat;
            $scope.model.value.boundingBox.southWestCorner.longitude =southWestCorner.lng;

            if(vm.currentMarker){
                const marker = vm.currentMarker.getLatLng();

                if(!$scope.model.value.marker){
                    $scope.model.value.marker = {};
                }
                $scope.model.value.marker.latitude = marker.lat;
                $scope.model.value.marker.longitude = marker.lng;

            }else{
                $scope.model.value.marker = null;
            }


        }
        onInit();
    }]);