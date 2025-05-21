import React, { useState, useEffect, useRef } from 'react';
import ApiService from '../services/api.js';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

import markerIcon2x from 'leaflet/dist/images/marker-icon-2x.png';
import markerIcon from 'leaflet/dist/images/marker-icon.png';
import markerShadow from 'leaflet/dist/images/marker-shadow.png';
delete L.Icon.Default.prototype._getIconUrl;

L.Icon.Default.mergeOptions({
    iconRetinaUrl: markerIcon2x,
    iconUrl: markerIcon,
    shadowUrl: markerShadow,});

const LocationPicker = ({ initialLocation, onLocationChange, readOnly = false, userId }) => {
   
    console.log('[LocationPicker] Otrzymane propsy:', { initialLocation, readOnly, userId });

    const [location, setLocation] = useState(() => {
        const defaultLat = 52.237049; // Warszawa
        const defaultLng = 21.017532;
        const defaultRadius = 10;

        // Sprawdź, czy initialLocation i jego współrzędne są zdefiniowane i są liczbami
        const lat = typeof initialLocation?.latitude === 'number' ? initialLocation.latitude : defaultLat;
        const lng = typeof initialLocation?.longitude === 'number' ? initialLocation.longitude : defaultLng;
        const radius = typeof initialLocation?.searchRadius === 'number' ? initialLocation.searchRadius : defaultRadius;
        const name = initialLocation?.locationName || '';

        const initialState = {
            latitude: lat,
            longitude: lng,
            searchRadius: radius,
            locationName: name
        };
        console.log('[LocationPicker] Ustawiono początkowy stan wewnętrzny (initialLocation prop był:', initialLocation, '):', initialState);
        return initialState;
    });

    const [mapInstance, setMapInstance] = useState(null);
    const [marker, setMarker] = useState(null);
    const [circle, setCircle] = useState(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(false);
    const mapNodeRef = useRef(null);
    const mapId = useRef(`map-container-${userId || Math.random().toString(36).substring(7)}`);

    // ... (useEffect i funkcje jak w poprzedniej wersji z obszernym logowaniem) ...
    // Efekt do jednorazowej inicjalizacji mapy
    useEffect(() => {
        console.log('[LocationPicker] Efekt Inicjalizacji Mapy: Uruchomiono. Stan mapInstance:', mapInstance, 'Wartości location w momencie inicjalizacji:', location);
        let leafletMap = null;

        if (mapNodeRef.current && !mapInstance) {
            if (typeof location.latitude !== 'number' || typeof location.longitude !== 'number') {
                console.error('[LocationPicker] Efekt Inicjalizacji Mapy: Nieprawidłowe współrzędne w stanie `location` do inicjalizacji mapy. Lat:', location.latitude, 'Lng:', location.longitude);
                setError("Nie można zainicjalizować mapy: brak poprawnych współrzędnych początkowych.");
                return;
            }
            console.log('[LocationPicker] Efekt Inicjalizacji Mapy: mapNodeRef.current jest dostępne. Próba inicjalizacji mapy na elemencie ID:', mapNodeRef.current.id);
            try {
                leafletMap = L.map(mapNodeRef.current).setView(
                    [location.latitude, location.longitude],
                    13
                );
                console.log('[LocationPicker] Efekt Inicjalizacji Mapy: L.map() pomyślne.');

                L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                    attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors'
                }).addTo(leafletMap);
                console.log('[LocationPicker] Efekt Inicjalizacji Mapy: Dodano TileLayer.');

                const newMarker = L.marker(
                    [location.latitude, location.longitude],
                    { draggable: !readOnly }
                ).addTo(leafletMap);
                console.log('[LocationPicker] Efekt Inicjalizacji Mapy: Dodano marker. Draggable:', !readOnly);

                const newCircle = L.circle(
                    [location.latitude, location.longitude],
                    {
                        radius: location.searchRadius * 1000,
                        color: 'green',
                        fillColor: '#3d9970',
                        fillOpacity: 0.2,
                        interactive: !readOnly
                    }
                ).addTo(leafletMap);
                console.log('[LocationPicker] Efekt Inicjalizacji Mapy: Dodano okrąg. Promień (m):', location.searchRadius * 1000);

                if (!readOnly) {
                    console.log('[LocationPicker] Efekt Inicjalizacji Mapy: Ustawianie nasłuchiwaczy zdarzeń mapy (dragend, click).');
                    const handleLocationUpdateFromMap = async (latlng) => {
                        console.log('[LocationPicker] Zdarzenie Mapy: handleLocationUpdateFromMap wywołane. Nowe LatLng:', latlng);
                        if (!newMarker || !newCircle) {
                            console.error('[LocationPicker] Zdarzenie Mapy: Marker lub Okrąg jest null. Nie można zaktualizować.');
                            return;
                        }
                        newMarker.setLatLng(latlng);
                        newCircle.setLatLng(latlng);
                        leafletMap.panTo(latlng);
                        try {
                            const fetchedLocationName = await reverseGeocode(latlng.lat, latlng.lng);
                            const newLocationData = {
                                latitude: latlng.lat,
                                longitude: latlng.lng,
                                searchRadius: location.searchRadius, // Zachowaj obecny promień ze stanu
                                locationName: fetchedLocationName
                            };
                            console.log('[LocationPicker] Zdarzenie Mapy: Wywołanie setLocation z:', newLocationData);
                            setLocation(newLocationData);
                            if (onLocationChange) {
                                console.log('[LocationPicker] Zdarzenie Mapy: Wywołanie onLocationChange prop.');
                                onLocationChange(newLocationData);
                            }
                        } catch (geoError) {
                            console.error("[LocationPicker] Zdarzenie Mapy: Błąd podczas geokodowania:", geoError);
                            const newLocationDataWithoutName = {
                                latitude: latlng.lat,
                                longitude: latlng.lng,
                                searchRadius: location.searchRadius, // Zachowaj obecny promień
                                locationName: location.locationName // Zachowaj starą nazwę lub ustaw pustą
                            };
                            setLocation(newLocationDataWithoutName);
                            if (onLocationChange) onLocationChange(newLocationDataWithoutName);
                            setError("Nie udało się pobrać nazwy lokalizacji po interakcji z mapą.");
                            setTimeout(() => setError(null), 3000);
                        }
                    };
                    newMarker.on('dragend', (e) => handleLocationUpdateFromMap(newMarker.getLatLng()));
                    leafletMap.on('click', (e) => handleLocationUpdateFromMap(e.latlng));
                }

                setMapInstance(leafletMap);
                setMarker(newMarker);
                setCircle(newCircle);
                console.log('[LocationPicker] Efekt Inicjalizacji Mapy: Instancje Mapy, Markera, Okręgu ustawione w stanie.');

            } catch (err) {
                console.error("[LocationPicker] Efekt Inicjalizacji Mapy: KRYTYCZNY błąd podczas inicjalizacji mapy:", err);
                setError("Nie udało się zainicjalizować mapy. " + err.message);
            }
        } else {
            if (!mapNodeRef.current) console.warn('[LocationPicker] Efekt Inicjalizacji Mapy: mapNodeRef.current jest wciąż null.');
            if (mapInstance) console.log('[LocationPicker] Efekt Inicjalizacji Mapy: Mapa już zainicjalizowana. Pomijanie.');
        }

        return () => {
            if (leafletMap) {
                console.log('[LocationPicker] Efekt Inicjalizacji Mapy: Czyszczenie - usuwanie instancji mapy.');
                leafletMap.remove();
            }
        };
    }, []); // Pusta tablica zależności: uruchom raz przy montowaniu.

    useEffect(() => {
        console.log('[LocationPicker] Efekt readOnly: Uruchomiono. readOnly:', readOnly, 'Stan markera:', marker, 'mapInstance:', mapInstance);
        if (marker && mapInstance) { // Upewnij się, że mapInstance też istnieje
            try {
                if (readOnly) {
                    if (marker.dragging && marker.dragging.enabled()) {
                        console.log('[LocationPicker] Efekt readOnly: Wyłączanie przeciągania markera.');
                        marker.dragging.disable();
                    }
                } else {
                    if (marker.dragging && !marker.dragging.enabled()) {
                        console.log('[LocationPicker] Efekt readOnly: Włączanie przeciągania markera.');
                        marker.dragging.enable();
                    } else if (!marker.dragging) {
                        // Jeśli marker został stworzony bez opcji draggable, a teraz readOnly jest false
                        console.warn('[LocationPicker] Efekt readOnly: Właściwość "dragging" markera nie była włączona przy inicjalizacji, a readOnly jest false. Przeciąganie może nie działać.');
                    }
                }
            } catch (e) {
                console.error('[LocationPicker] Efekt readOnly: Błąd podczas zmiany stanu przeciągania markera:', e)
            }
        }
    }, [readOnly, marker, mapInstance]);

    useEffect(() => {
        console.log('[LocationPicker] Efekt Promienia Wyszukiwania: Uruchomiono. location.searchRadius:', location.searchRadius, 'Stan okręgu:', circle);
        if (circle && typeof location.searchRadius === 'number' && location.searchRadius > 0) {
            const newRadiusMeters = location.searchRadius * 1000;
            console.log('[LocationPicker] Efekt Promienia Wyszukiwania: Aktualizacja promienia okręgu (m):', newRadiusMeters);
            circle.setRadius(newRadiusMeters);
        }
    }, [location.searchRadius, circle]);

    useEffect(() => {
        console.log('[LocationPicker] Efekt initialLocation Prop: Uruchomiono. Prop initialLocation:', initialLocation, 'Obecny stan location:', location);

        const newLat = initialLocation?.latitude;
        const newLng = initialLocation?.longitude;
        const newRadius = initialLocation?.searchRadius;
        const newName = initialLocation?.locationName;

        // Sprawdź, czy initialLocation ma sensowne wartości do aktualizacji
        // i czy różni się od obecnego stanu wewnętrznego, aby uniknąć pętli
        let shouldUpdate = false;
        if (typeof newLat === 'number' && newLat !== location.latitude) shouldUpdate = true;
        if (typeof newLng === 'number' && newLng !== location.longitude) shouldUpdate = true;
        if (typeof newRadius === 'number' && newRadius !== location.searchRadius) shouldUpdate = true;
        if (typeof newName === 'string' && newName !== location.locationName) shouldUpdate = true;


        if (shouldUpdate) {
            console.log('[LocationPicker] Efekt initialLocation Prop: initialLocation różni się od bieżącego stanu. Aktualizacja mapy i stanu wewnętrznego.');

            const updatedInternalLocation = {
                latitude: typeof newLat === 'number' ? newLat : location.latitude,
                longitude: typeof newLng === 'number' ? newLng : location.longitude,
                searchRadius: typeof newRadius === 'number' ? newRadius : location.searchRadius,
                locationName: typeof newName === 'string' ? newName : location.locationName,
            };

            if (mapInstance && marker && circle) {
                console.log('[LocationPicker] Efekt initialLocation Prop: Aktualizacja wizualizacji mapy.');
                const currentZoom = mapInstance.getZoom();
                mapInstance.setView([updatedInternalLocation.latitude, updatedInternalLocation.longitude], currentZoom);
                marker.setLatLng([updatedInternalLocation.latitude, updatedInternalLocation.longitude]);
                circle.setLatLng([updatedInternalLocation.latitude, updatedInternalLocation.longitude]);
                if(typeof updatedInternalLocation.searchRadius === 'number') {
                    circle.setRadius(updatedInternalLocation.searchRadius * 1000);
                }
            } else {
                console.warn('[LocationPicker] Efekt initialLocation Prop: Obiekty mapy (mapInstance, marker, circle) niegotowe podczas próby aktualizacji z initialLocation.');
            }

            console.log('[LocationPicker] Efekt initialLocation Prop: Wywołanie setLocation z:', updatedInternalLocation);
            setLocation(updatedInternalLocation);
        } else {
            console.log('[LocationPicker] Efekt initialLocation Prop: initialLocation nie wymaga aktualizacji stanu wewnętrznego lub mapy.');
        }
    }, [initialLocation]); // Zależność tylko od initialLocation prop


    const reverseGeocode = async (lat, lng) => { /* ... jak w poprzedniej wersji z logami ... */
        console.log('[LocationPicker] reverseGeocode: Wywołano z Lat:', lat, 'Lng:', lng);
        try {
            const apiUrl = `https://nominatim.openstreetmap.org/reverse?format=json&lat=${lat}&lon=${lng}&zoom=18&addressdetails=1&accept-language=pl`; // Zmieniono na pl
            console.log('[LocationPicker] reverseGeocode: Zapytanie do URL:', apiUrl);
            const response = await fetch(apiUrl);
            if (!response.ok) {
                console.error('[LocationPicker] reverseGeocode: Żądanie Nominatim nie powiodło się. Status:', response.status);
                throw new Error(`Żądanie Nominatim nie powiodło się, status ${response.status}`);
            }
            const data = await response.json();
            console.log('[LocationPicker] reverseGeocode: Otrzymano dane z Nominatim:', data);

            if (data && data.display_name) {
                let simpleName = '';
                if (data.address) {
                    const city = data.address.city || data.address.town || data.address.village || data.address.hamlet || data.address.county;
                    const country = data.address.country;
                    if (city && country) simpleName = `${city}, ${country}`;
                    else if (city) simpleName = city;
                    else if (country) simpleName = country;
                }
                const finalName = simpleName || data.display_name.split(',').slice(0, 3).join(', ');
                console.log('[LocationPicker] reverseGeocode: Sparsowana nazwa lokalizacji:', finalName);
                return finalName;
            }
            console.warn('[LocationPicker] reverseGeocode: display_name nie znaleziono w odpowiedzi Nominatim.');
            return 'Nie znaleziono nazwy lokalizacji';
        } catch (err) {
            console.error("[LocationPicker] reverseGeocode: Błąd podczas geokodowania odwrotnego:", err);
            throw err; // Rzuć błąd dalej, aby można go było obsłużyć
        }
    };
    const handleRadiusChange = (e) => { /* ... jak w poprzedniej wersji z logami ... */
        const newRadius = parseFloat(e.target.value);
        console.log('[LocationPicker] handleRadiusChange: Nowa wartość promienia z inputa:', newRadius);
        if (!isNaN(newRadius) && newRadius >= 1 && newRadius <= 200) {
            setError(null);
            const newLocationData = { ...location, searchRadius: newRadius };
            console.log('[LocationPicker] handleRadiusChange: Wywołanie setLocation z:', newLocationData);
            setLocation(newLocationData);
            if (onLocationChange) {
                console.log('[LocationPicker] handleRadiusChange: Wywołanie onLocationChange prop.');
                onLocationChange(newLocationData);
            }
        } else {
            console.warn('[LocationPicker] handleRadiusChange: Nieprawidłowa wartość promienia:', newRadius);
            setError("Promień musi być pomiędzy 1 a 200 km.");
        }
    };
    const handleGetCurrentLocation = () => { /* ... jak w poprzedniej wersji z logami ... */
        console.log('[LocationPicker] handleGetCurrentLocation: Kliknięto.');
        if (navigator.geolocation) {
            setLoading(true);
            setError(null);
            navigator.geolocation.getCurrentPosition(
                async (position) => {
                    const { latitude, longitude } = position.coords;
                    console.log('[LocationPicker] handleGetCurrentLocation: Geolokalizacja pomyślna. Współrzędne:', { latitude, longitude });

                    if (mapInstance && marker && circle) {
                        console.log('[LocationPicker] handleGetCurrentLocation: Aktualizacja wizualizacji mapy.');
                        mapInstance.setView([latitude, longitude], 13);
                        marker.setLatLng([latitude, longitude]);
                        circle.setLatLng([latitude, longitude]);
                    } else {
                        console.warn('[LocationPicker] handleGetCurrentLocation: mapInstance, marker lub circle niedostępne do aktualizacji wizualnej.');
                    }

                    try {
                        const fetchedLocationName = await reverseGeocode(latitude, longitude);
                        const newLocationData = {
                            latitude,
                            longitude,
                            searchRadius: location.searchRadius, // Zachowaj obecny promień
                            locationName: fetchedLocationName
                        };
                        console.log('[LocationPicker] handleGetCurrentLocation: Wywołanie setLocation z:', newLocationData);
                        setLocation(newLocationData);
                        if (onLocationChange) {
                            console.log('[LocationPicker] handleGetCurrentLocation: Wywołanie onLocationChange prop.');
                            onLocationChange(newLocationData);
                        }
                    } catch (geoError) {
                        console.error('[LocationPicker] handleGetCurrentLocation: Błąd podczas geokodowania:', geoError);
                        setError(geoError.message || "Nie udało się pobrać nazwy dla bieżącej pozycji.");
                    } finally {
                        setLoading(false);
                    }
                },
                (geoError) => {
                    console.error("[LocationPicker] handleGetCurrentLocation: Geolokalizacja nie powiodła się.", geoError);
                    let message = "Nie udało się pobrać Twojej bieżącej lokalizacji.";
                    if (geoError.code === 1) message = "Odmówiono pozwolenia na geolokalizację. Włącz je w ustawieniach przeglądarki.";
                    else if (geoError.code === 2) message = "Informacja o lokalizacji jest niedostępna.";
                    else if (geoError.code === 3) message = "Przekroczono czas żądania lokalizacji.";
                    setError(message);
                    setLoading(false);
                },
                { timeout: 10000, enableHighAccuracy: true }
            );
        } else {
            console.warn('[LocationPicker] handleGetCurrentLocation: Geolokalizacja nie jest wspierana przez przeglądarkę.');
            setError("Geolokalizacja nie jest wspierana przez Twoją przeglądarkę.");
        }
    };
    const handleSaveLocation = async () => { /* ... jak w poprzedniej wersji z logami ... */
        console.log('[LocationPicker] handleSaveLocation: Kliknięto. Bieżący stan lokalizacji:', location);
        if (!userId) {
            console.warn('[LocationPicker] handleSaveLocation: ID użytkownika nie jest zdefiniowane.');
            setError("ID użytkownika nie jest zdefiniowane. Nie można zapisać lokalizacji.");
            return;
        }
        if (readOnly) {
            console.log('[LocationPicker] handleSaveLocation: W trybie readOnly, zapis anulowany.');
            return;
        }

        setLoading(true);
        setError(null);
        setSuccess(false);

        try {
            const locationDataPayload = {
                latitude: location.latitude,
                longitude: location.longitude,
                searchRadius: location.searchRadius,
                locationName: location.locationName
            };
            console.log('[LocationPicker] handleSaveLocation: Wysyłanie do API:', locationDataPayload);
            await ApiService.put(`/api/users/${userId}/location`, locationDataPayload);
            console.log('[LocationPicker] handleSaveLocation: Wywołanie API pomyślne.');
            setSuccess(true);
            if (onLocationChange) {
                console.log('[LocationPicker] handleSaveLocation: Wywołanie onLocationChange prop z finalnie zapisaną lokalizacją.');
                onLocationChange(location); // Przekaż zaktualizowany stan 'location'
            }
            setTimeout(() => setSuccess(false), 3000);
        } catch (err) {
            console.error("[LocationPicker] handleSaveLocation: Błąd podczas zapisywania lokalizacji:", err);
            const errorMessage = err.response?.data?.message || err.message || "Nie udało się zapisać lokalizacji. Spróbuj ponownie później.";
            setError(errorMessage);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="location-picker card shadow-sm">
            <div className="card-header d-flex justify-content-between align-items-center">
                <span>Ustawienia Lokalizacji {location.locationName && `(${location.locationName})`}</span>
                {error && !error.toLowerCase().includes("promień") && <button onClick={() => { console.log('[LocationPicker] Ręczne czyszczenie ogólnego błędu.'); setError(null); }} className="btn-close btn-sm" aria-label="Zamknij błąd"></button>}
            </div>
            <div className="card-body">
                <div
                    ref={mapNodeRef}
                    id={mapId.current}
                    style={{ height: '350px', width: '100%', marginBottom: '1rem', backgroundColor: '#e9ecef' }}
                    aria-label="Mapa do wyboru lokalizacji"
                >
                    {/* Ten błąd jest specyficzny dla inicjalizacji, inne błędy będą poniżej */}
                    {!mapInstance && error && error.includes("Could not initialize") &&
                        <div className="alert alert-danger d-flex align-items-center justify-content-center h-100">{error}</div>
                    }
                </div>

                {!readOnly && (
                    <div className="mb-3">
                        <div className="row g-2 mb-2 align-items-center">
                            <div className="col-md-7 col-lg-8">
                                <label htmlFor={`radius-input-${mapId.current}`} className="form-label mb-0 me-2">
                                    Promień wyszukiwania: <strong>{location.searchRadius} km</strong>
                                </label>
                                <input
                                    type="range"
                                    id={`radius-input-${mapId.current}`}
                                    className="form-range"
                                    value={location.searchRadius}
                                    onChange={handleRadiusChange}
                                    min="1"
                                    max="200"
                                    step="1"
                                    disabled={loading || readOnly}
                                    aria-label="Promień wyszukiwania w kilometrach"
                                />
                            </div>
                            <div className="col-md-5 col-lg-4 d-flex align-items-end">
                                <button
                                    type="button"
                                    className="btn btn-outline-secondary w-100"
                                    onClick={handleGetCurrentLocation}
                                    disabled={loading || readOnly}
                                    title="Użyj Twojej bieżącej lokalizacji geograficznej"
                                >
                                    <i className="bi bi-geo-fill me-1"></i>
                                    {loading && (document.activeElement?.onclick === handleGetCurrentLocation || (document.activeElement?.closest('button')?.onclick === handleGetCurrentLocation)) ? 'Lokalizowanie...' : 'Moja Lokalizacja'}
                                </button>
                            </div>
                        </div>
                        {error && error.toLowerCase().includes("promień") && <div className="text-danger small mt-1 mb-2">{error}</div>}
                    </div>
                )}

                <div className="mb-3">
                    {location.locationName ? (
                        <p className="text-muted small mb-0">
                            <i className="bi bi-pin-map me-1"></i>Wybrana lokalizacja: <strong>{location.locationName}</strong>
                        </p>
                    ) : (
                        (typeof location.latitude === 'number' && typeof location.longitude === 'number') && // Pokaż tylko jeśli są współrzędne
                        <p className="text-muted small mb-1">
                            Punkt: Lat: {location.latitude.toFixed(5)}, Lng: {location.longitude.toFixed(5)}
                        </p>
                    )}
                </div>

                {!readOnly && userId && (
                    <button
                        type="button"
                        className="btn btn-success w-100"
                        onClick={handleSaveLocation}
                        disabled={loading || readOnly}
                    >
                        {loading && (document.activeElement?.onclick === handleSaveLocation || (document.activeElement?.closest('button')?.onclick === handleSaveLocation)) ? (
                            <>
                                <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span>
                                <span className="ms-1">Zapisywanie...</span>
                            </>
                        ) : (
                            <>
                                <i className="bi bi-check-circle me-1"></i> Zapisz Lokalizację
                            </>
                        )}
                    </button>
                )}
                {/* Ogólne komunikaty zwrotne (nie dotyczące walidacji promienia) */}
                {error && !error.toLowerCase().includes("promień") && !error.includes("Could not initialize") && !loading &&
                    <div className="alert alert-danger mt-3 py-2">{error}</div>
                }
                {success && <div className="alert alert-success mt-3 py-2">Lokalizacja zapisana pomyślnie!</div>}
            </div>
        </div>
    );
};

export default LocationPicker;