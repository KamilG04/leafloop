// Ścieżka: wwwroot/js/components/userLocationMap.js
import React, { useState, useMemo, useEffect } from 'react';
import LocationPicker from './locationPicker.js';

const UserLocationMap = ({ address, userSearchRadius, userId, isCurrentUser = false }) => {
    console.log('[UserLocationMap] Props otrzymane:', { address, userSearchRadius, userId, isCurrentUser });

    const [isEditing, setIsEditing] = useState(false);

    // Sprawdź, czy props.address jest zdefiniowane i czy zawiera liczbowe współrzędne
    const hasCoordinates = useMemo(() => {
        const result = address && typeof address.latitude === 'number' && typeof address.longitude === 'number';
        console.log('[UserLocationMap] Obliczanie hasCoordinates. Adres prop:', address, 'Wynik:', result);
        return result;
    }, [address]);

    const initialLocationForPicker = useMemo(() => {
        console.log('[UserLocationMap] useMemo dla initialLocationForPicker. Adres:', address, 'UserSearchRadius:', userSearchRadius, 'HasCoordinates:', hasCoordinates);
        let computedLoc;
        if (hasCoordinates) {
            computedLoc = {
                latitude: address.latitude,
                longitude: address.longitude,
                searchRadius: userSearchRadius || 10, // Użyj userSearchRadius, jeśli dostępne
                locationName: address.city ? (address.country ? `${address.city}, ${address.country}` : address.city) : (address.locationName || '')
            };
            console.log('[UserLocationMap] useMemo: obliczona lokalizacja ze współrzędnymi:', computedLoc);
        } else {
            // Jeśli nie ma współrzędnych, LocationPicker użyje swoich domyślnych (np. Warszawa)
            // ale przekażmy searchRadius i pustą nazwę
            computedLoc = {
                latitude: null,
                longitude: null,
                searchRadius: userSearchRadius || 10,
                locationName: ''
            };
            console.log('[UserLocationMap] useMemo: obliczona lokalizacja z PUSTYMI współrzędnymi:', computedLoc);
        }
        return computedLoc;
    }, [address, userSearchRadius, hasCoordinates]);

    // Logowanie finalnej wartości przekazywanej do LocationPicker
    useEffect(() => {
        console.log('[UserLocationMap] Wartość initialLocationForPicker PRZED renderowaniem LocationPicker:', initialLocationForPicker);
    }, [initialLocationForPicker]);


    const handleLocationChange = (newLocation) => {
        console.log("[UserLocationMap] LocationPicker zgłosił zmianę/zapis:", newLocation);
        // Na razie tylko logujemy. Użytkownik manualnie kończy edycję.
        // Jeśli LocationPicker miałby callback onSaveSuccess, tutaj moglibyśmy np. ustawić setIsEditing(false)
        // lub powiadomić komponent nadrzędny (UserProfile) o potrzebie odświeżenia danych.
    };

    // Jeśli nie ma adresu (ani jego współrzędnych) i nie jest to profil bieżącego użytkownika, nic nie pokazuj.
    if (!address && !isCurrentUser) { // Zmieniono warunek, aby sprawdzał istnienie 'address'
        console.log('[UserLocationMap] Brak adresu i nie jest to bieżący użytkownik. Renderowanie null.');
        return null;
    }

    // Jeśli nie ma współrzędnych (nawet jeśli obiekt address istnieje), a jest to bieżący użytkownik, pokaż opcję ustawienia lokalizacji.
    if (!hasCoordinates && isCurrentUser) {
        console.log('[UserLocationMap] Brak współrzędnych, ale to bieżący użytkownik. Pokazywanie opcji ustawienia lokalizacji. isEditing:', isEditing);
        return (
            <div className="mt-3 pt-3 border-top">
                <h5 className="mb-3">
                    <i className="bi bi-map me-2 text-success"></i>
                    Moja Lokalizacja
                </h5>
                {isEditing ? (
                    <>
                        <p className="text-muted mb-3">
                            Ustaw swoją lokalizację i preferowany promień wyszukiwania.
                            Dzięki temu inni użytkownicy będą wiedzieli, gdzie szukasz przedmiotów,
                            a Ty będziesz widzieć oferty z Twojej okolicy.
                        </p>
                        <LocationPicker
                            initialLocation={initialLocationForPicker} // Przekażemy obiekt z null lat/lng, LocationPicker użyje domyślnych
                            userId={userId}
                            onLocationChange={handleLocationChange}
                            readOnly={false}
                        />
                        <button
                            className="btn btn-sm btn-outline-secondary mt-3"
                            onClick={() => { console.log("[UserLocationMap] Anulowano dodawanie nowej lokalizacji."); setIsEditing(false);}}
                        >
                            <i className="bi bi-x-circle me-1"></i>
                            Anuluj
                        </button>
                    </>
                ) : (
                    <div className="text-center py-4 border rounded bg-light">
                        <i className="bi bi-geo-alt text-muted fs-1"></i>
                        <p className="mt-2 mb-3">Nie ustawiłeś jeszcze swojej lokalizacji.</p>
                        <button
                            className="btn btn-success"
                            onClick={() => { console.log("[UserLocationMap] Kliknięto 'Ustaw Lokalizację'."); setIsEditing(true); }}
                        >
                            <i className="bi bi-map me-1"></i>
                            Ustaw Lokalizację
                        </button>
                    </div>
                )}
            </div>
        );
    }

    // Jeśli są współrzędne (lub jest to bieżący użytkownik, który może je ustawić, ale zaczął od pustego stanu i wszedł w edycję)
    // Ten warunek jest teraz obsługiwany przez powyższe, więc tu zakładamy, że albo są koordynaty,
    // albo jesteśmy w trybie edycji dla bieżącego użytkownika, który właśnie je ustawia.
    // Jeśli `initialLocationForPicker` nie jest `undefined` (co nie powinno się zdarzyć), renderuj LocationPicker.
    if (typeof initialLocationForPicker === 'undefined') {
        console.error("[UserLocationMap] KRYTYCZNY BŁĄD: initialLocationForPicker jest undefined tuż przed renderowaniem LocationPicker!");
        return <div className="alert alert-danger">Błąd wewnętrzny: Nie można przygotować danych mapy.</div>;
    }

    console.log('[UserLocationMap] Renderowanie LocationPicker z initialLocation:', initialLocationForPicker, 'isEditing:', isEditing);
    return (
        <div className="mt-3 pt-3 border-top">
            <div className="d-flex justify-content-between align-items-center mb-3">
                <h5 className="mb-0">
                    <i className="bi bi-map me-2 text-success"></i>
                    {isCurrentUser ? 'Moja Lokalizacja' : 'Lokalizacja'}
                </h5>
                {isCurrentUser && hasCoordinates && ( // Pokaż przycisk Edytuj tylko jeśli są już jakieś koordynaty do edycji
                    <button
                        className="btn btn-sm btn-outline-success"
                        onClick={() => { console.log("[UserLocationMap] Zmieniono tryb edycji na:", !isEditing); setIsEditing(!isEditing);}}
                    >
                        <i className={`bi bi-${isEditing ? 'eye' : 'pencil'} me-1`}></i>
                        {isEditing ? 'Tryb Podglądu' : 'Edytuj'}
                    </button>
                )}
            </div>
            <LocationPicker
                initialLocation={initialLocationForPicker}
                userId={userId}
                onLocationChange={handleLocationChange}
                readOnly={!isEditing && hasCoordinates} // Mapa jest readOnly jeśli nie edytujemy ORAZ jeśli są koordynaty (czyli nie jesteśmy w trybie "pierwszego ustawiania")
            />
        </div>
    );
};

export default UserLocationMap;