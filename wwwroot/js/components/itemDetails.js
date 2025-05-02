// Pełna ścieżka: wwwroot/js/components/itemDetails.js

import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
// === DODANO getResponseData DO IMPORTU ===
import { getAuthHeaders, handleApiResponse, getResponseData, getCurrentUserId } from '../utils/auth.js';
// ========================================
console.log(">>> itemDetails.js: START PLIKU!");

// Komponent ItemPhotoDisplay (bez zmian)
const ItemPhotoDisplay = ({ photos, itemName }) => {
    // ... (kod komponentu bez zmian) ...
    if (!photos || photos.length === 0) {
        return (
            <div className="mb-3 bg-light d-flex align-items-center justify-content-center rounded" style={{ minHeight: '300px', maxHeight: '500px' }}>
                <i className="bi bi-image text-muted" style={{ fontSize: '4rem' }}></i>
            </div>
        );
    }

    if (photos.length === 1) {
        // Użyj poprawnej ścieżki z PhotoDto
        const photoPath = photos[0].path ? (photos[0].path.startsWith('/') || photos[0].path.startsWith('http') ? photos[0].path : `/${photos[0].path}`) : '/img/default-item-photo.png';
        return <img src={photoPath} className="img-fluid rounded mb-3" alt={photos[0].fileName || itemName} style={{ maxHeight: '500px', objectFit: 'contain', display: 'block', margin: '0 auto' }} onError={(e) => { e.target.src = '/img/default-item-photo.png'; }} />;
    }

    const carouselId = `itemPhotosCarousel-${Date.now()}`;
    return (
        <div id={carouselId} className="carousel slide mb-3" data-bs-ride="carousel">
            <div className="carousel-indicators">
                {photos.map((_, index) => (
                    <button key={index} type="button" data-bs-target={`#${carouselId}`} data-bs-slide-to={index} className={index === 0 ? 'active' : ''} aria-current={index === 0 ? 'true' : 'false'} aria-label={`Zdjęcie ${index + 1}`}></button>
                ))}
            </div>
            <div className="carousel-inner rounded" style={{ maxHeight: '500px', backgroundColor: '#f8f9fa' }}>
                {photos.map((photo, index) => {
                    const photoPath = photo.path ? (photo.path.startsWith('/') || photo.path.startsWith('http') ? photo.path : `/${photo.path}`) : '/img/default-item-photo.png';
                    return (
                        <div key={photo.id || index} className={`carousel-item ${index === 0 ? 'active' : ''}`}>
                            <img src={photoPath} className="d-block w-100" alt={photo.fileName || `Zdjęcie ${index + 1}`} style={{ maxHeight: '500px', objectFit: 'contain'}} onError={(e) => { e.target.src = '/img/default-item-photo.png'; }}/>
                        </div>
                    );
                })}
            </div>
            <button className="carousel-control-prev" type="button" data-bs-target={`#${carouselId}`} data-bs-slide="prev">
                <span className="carousel-control-prev-icon" aria-hidden="true" style={{filter: 'invert(0.5) grayscale(100)'}}></span>
                <span className="visually-hidden">Poprzedni</span>
            </button>
            <button className="carousel-control-next" type="button" data-bs-target={`#${carouselId}`} data-bs-slide="next">
                <span className="carousel-control-next-icon" aria-hidden="true" style={{filter: 'invert(0.5) grayscale(100)'}}></span>
                <span className="visually-hidden">Następny</span>
            </button>
        </div>
    );
};


// Główny komponent szczegółów
const ItemDetails = ({ itemId }) => {
    const [item, setItem] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [isOwner, setIsOwner] = useState(false);

    // Funkcja getCurrentUserId (bez zmian - zakładamy, że działa poprawnie)
    // const getCurrentUserId = () => { ... }; // Ta funkcja jest już zdefiniowana w auth.js

    useEffect(() => {
        const fetchItemData = async () => {
            if (!itemId || itemId <= 0) {
                setError('Nieprawidłowe ID przedmiotu.');
                setLoading(false);
                return;
            }
            console.log(`>>> Rozpoczęcie fetchItemData dla itemId: ${itemId}`);
            setLoading(true);
            setError(null);
            try {
                console.log(">>> Wywołanie API: GET /api/items/" + itemId);
                const response = await fetch(`/api/items/${itemId}`, {
                    method: 'GET',
                    headers: getAuthHeaders(false)
                });

                // 1. Użyj handleApiResponse
                const apiResult = await handleApiResponse(response);
                console.log(">>> LOG 4 - Surowy wynik z API (po handleApiResponse):", apiResult);

                // === POPRAWKA: Użyj getResponseData ===
                // 2. Wyciągnij dane z obiektu ApiResponse
                const itemData = getResponseData(apiResult);
                console.log(">>> LOG 4a - Dane przedmiotu (po getResponseData):", itemData);
                // === KONIEC POPRAWKI ===

                // Sprawdź, czy dane przedmiotu istnieją
                if (!itemData) {
                    console.error(">>> BŁĄD: getResponseData zwróciło puste dane.");
                    throw new Error("Otrzymano puste dane przedmiotu z API.");
                }

                // 3. Ustaw stan 'item' poprawnymi danymi
                setItem(itemData); // Teraz item będzie zawierał { id, name, description, user, ... }

                // 4. Sprawdź właściciela (logika powinna teraz działać)
                const currentUserId = getCurrentUserId(); // Pobierz ID zalogowanego użytkownika
                console.log(">>> LOG 5 - ID bieżącego użytkownika (z tokenu):", currentUserId);

                // Sprawdź ostrożnie, czy itemData i user istnieją przed dostępem do id
                const ownerIdFromApi = itemData && itemData.user ? itemData.user.id : null;
                console.log(">>> LOG 6 - ID właściciela (z danych API):", ownerIdFromApi);

                if (ownerIdFromApi !== null && currentUserId !== null && ownerIdFromApi === currentUserId) {
                    console.log(">>> LOG 7a - Użytkownik JEST właścicielem.");
                    setIsOwner(true);
                } else {
                    console.log(">>> LOG 7b - Użytkownik NIE JEST właścicielem.");
                    console.log(`>>> LOG 7c - Szczegóły porównania: ownerIdFromApi=${ownerIdFromApi}, currentUserId=${currentUserId}`);
                    setIsOwner(false);
                }

            } catch (err) {
                console.error(">>> Błąd w fetchItemData:", err);
                setError(err.message);
                setItem(null);
            } finally {
                console.log(">>> Zakończenie fetchItemData, setLoading(false)");
                setLoading(false);
            }
        };
        console.log(`>>> ItemDetails Component: RENDER START dla itemId: ${itemId}`);
        fetchItemData();
    }, [itemId]);


    // Funkcja handleDelete (bez zmian)
    const handleDelete = async () => {
        // ... (kod funkcji bez zmian) ...
        if (!isOwner || !item) return;
        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${item.name}"? Tej operacji nie można cofnąć.`)) {
            return;
        }
        setLoading(true);
        setError(null);
        try {
            const response = await fetch(`/api/items/${item.id}`, {
                method: 'DELETE',
                headers: getAuthHeaders(false)
            });
            await handleApiResponse(response);
            alert(`Przedmiot "${item.name}" został usunięty.`);
            window.location.href = '/Items';
        } catch (err) {
            console.error("Błąd podczas usuwania przedmiotu:", err);
            setError(`Nie udało się usunąć przedmiotu: ${err.message}`);
            setLoading(false);
        }
    };

    // --- Renderowanie ---
    if (loading) {
        console.log("Renderowanie: Stan ładowania (spinner)");
        return <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '200px' }}><div className="spinner-border text-success" role="status"><span className="visually-hidden">Ładowanie...</span></div></div>;
    }
    if (error) {
        console.log(`Renderowanie: Stan błędu: ${error}`);
        return <div className="alert alert-danger" role="alert">Błąd: {error}</div>;
    }
    if (!item) {
        console.log("Renderowanie: Brak danych przedmiotu (po załadowaniu/błędzie)");
        return <div className="alert alert-warning" role="alert">Nie znaleziono przedmiotu o podanym ID lub wystąpił błąd podczas ładowania.</div>;
    }

    // Destrukturyzacja i reszta renderowania (bez zmian)
    const { name, description, condition, dateAdded, isAvailable, isForExchange, expectedValue, user, category, photos, tags } = item;
    const categoryName = category ? category.name : 'Brak';
    const userName = user ? `${user.firstName} ${user.lastName}` : 'Nieznany użytkownik';
    const userAvatar = user ? user.avatarPath : null; // Zakładając, że UserDto ma AvatarPath
    const userFirstName = user ? user.firstName : '?';
    const userLastName = user ? user.lastName : '?';
    const userEcoScore = user ? user.ecoScore : 0; // Zakładając, że UserDto ma EcoScore
    const userId = user ? user.id : null;

    return (
        <div className="row">
            <div className="col-lg-7 mb-4">
                <ItemPhotoDisplay photos={photos || []} itemName={name} />
            </div>
            <div className="col-lg-5">
                <div className="card shadow-sm">
                    <div className="card-header bg-light d-flex justify-content-between align-items-center flex-wrap">
                        <h3 className="mb-0 me-2">{name}</h3>
                        <span className={`badge fs-6 ${isAvailable ? 'bg-success' : 'bg-secondary'}`}>
                             {isAvailable ? 'Dostępny' : 'Niedostępny'}
                          </span>
                    </div>
                    <div className="card-body">
                        <p className="lead" style={{ whiteSpace: 'pre-wrap' }}>{description}</p>
                        <hr/>
                        <p><strong>Stan:</strong> {condition}</p>
                        <p><strong>Kategoria:</strong> {categoryName}</p>
                        <p><strong>Wartość/Cel:</strong> {expectedValue > 0 ? `${expectedValue.toFixed(2)} PLN` : (isForExchange ? 'Wymiana' : 'Za darmo')}</p>
                        <p><strong>Data dodania:</strong> {dateAdded ? new Date(dateAdded).toLocaleString('pl-PL') : 'Brak daty'}</p>
                        {tags && tags.length > 0 && (
                            <div className="mb-3">
                                <strong>Tagi:</strong>{' '}
                                {tags.map(tag => (
                                    <span key={tag.id} className="badge bg-info me-1">{tag.name}</span>
                                ))}
                            </div>
                        )}
                        {user && (
                            <div className="mt-3 pt-3 border-top">
                                <h6>Wystawione przez:</h6>
                                <div className="d-flex align-items-center">
                                    {userAvatar ? (
                                        <img src={userAvatar} alt={userName} className="rounded-circle me-2" style={{ width: '40px', height: '40px', objectFit: 'cover' }} onError={(e) => { e.target.src = '/img/default-avatar.png'; }}/>
                                    ) : (
                                        <div className="rounded-circle bg-secondary text-white d-flex align-items-center justify-content-center me-2" style={{ width: '40px', height: '40px', fontSize: '1rem' }}>
                                            {userFirstName?.charAt(0)}{userLastName?.charAt(0)}
                                        </div>
                                    )}
                                    <div>
                                        <a href={`/Profile/Index/${userId}`} className="fw-bold text-decoration-none">{userName}</a>
                                        <br/>
                                        <small className="text-muted">EcoScore: {userEcoScore}</small>
                                    </div>
                                </div>
                            </div>
                        )}
                        <div className="mt-4 pt-3 border-top d-flex flex-wrap gap-2">
                            {!isOwner && isAvailable && (
                                <button className="btn btn-primary" onClick={() => alert('TODO: Funkcjonalność wiadomości/transakcji.')}>
                                    <i className="bi bi-envelope me-1"></i> Zapytaj / Zaproponuj
                                </button>
                            )}
                            {isOwner && (
                                <>
                                    <a href={`/Items/Edit/${item.id}`} className="btn btn-warning">
                                        <i className="bi bi-pencil-square me-1"></i> Edytuj
                                    </a>
                                    <button onClick={handleDelete} className="btn btn-danger" disabled={loading}>
                                        {loading && <span className="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>}
                                        <i className="bi bi-trash me-1"></i>
                                        Usuń
                                    </button>
                                </>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

// --- Renderowanie Komponentu ---
const container = document.getElementById('react-item-details-container');
if (container) {
    const itemId = parseInt(container.getAttribute('data-item-id'), 10);
    if (!isNaN(itemId) && itemId > 0) {
        const root = ReactDOM.createRoot(container);
        root.render(<StrictMode><ItemDetails itemId={itemId} /></StrictMode>);
    } else {
        container.innerHTML = '<div class="alert alert-danger">Błąd: Nieprawidłowe ID przedmiotu przekazane do komponentu.</div>';
        console.error("Nieprawidłowe ID przedmiotu w data-item-id:", container.getAttribute('data-item-id'));
    }
} else {
    console.error("Nie znaleziono kontenera 'react-item-details-container' do renderowania szczegółów.");
}
