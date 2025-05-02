// Pełna ścieżka: wwwroot/js/components/itemDetails.js (lub .jsx) - POPRAWIONY

// ----- POCZĄTEK PLIKU -----
import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client'; // <<< --- DODANA/POPRAWIONA TA LINIA --- >>>
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js'; // Zaimportuj funkcje pomocnicze
console.log(">>> itemDetails.js: START PLIKU!");
// Komponent wyświetlający zdjęcie w karuzeli lub jako pojedyncze
const ItemPhotoDisplay = ({ photos, itemName }) => {
    if (!photos || photos.length === 0) {
        return (
            <div className="mb-3 bg-light d-flex align-items-center justify-content-center rounded" style={{ minHeight: '300px', maxHeight: '500px' }}>
                <i className="bi bi-image text-muted" style={{ fontSize: '4rem' }}></i>
            </div>
        );
    }

    if (photos.length === 1) {
        return <img src={photos[0].path} className="img-fluid rounded mb-3" alt={photos[0].fileName || itemName} style={{ maxHeight: '500px', objectFit: 'contain', display: 'block', margin: '0 auto' }} />;
    }

    // Karuzela dla wielu zdjęć
    const carouselId = `itemPhotosCarousel-${Date.now()}`; // Unikalne ID dla karuzeli
    return (
        <div id={carouselId} className="carousel slide mb-3" data-bs-ride="carousel">
            <div className="carousel-indicators">
                {photos.map((_, index) => (
                    <button key={index} type="button" data-bs-target={`#${carouselId}`} data-bs-slide-to={index} className={index === 0 ? 'active' : ''} aria-current={index === 0 ? 'true' : 'false'} aria-label={`Zdjęcie ${index + 1}`}></button>
                ))}
            </div>
            <div className="carousel-inner rounded" style={{ maxHeight: '500px', backgroundColor: '#f8f9fa' }}>
                {photos.map((photo, index) => (
                    <div key={photo.id} className={`carousel-item ${index === 0 ? 'active' : ''}`}>
                        <img src={photo.path} className="d-block w-100" alt={photo.fileName || `Zdjęcie ${index + 1}`} style={{ maxHeight: '500px', objectFit: 'contain'}}/>
                    </div>
                ))}
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
    const [item, setItem] = useState(null); // Typ ItemWithDetailsDto
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [isOwner, setIsOwner] = useState(false); // Czy bieżący użytkownik jest właścicielem

    
    // Funkcja do pobierania ID bieżącego użytkownika
    const getCurrentUserId = () => {
        const token = getAuthHeaders(false)['Authorization'];
        console.log(">>> LOG 1 - Token w getCurrentUserId:", token); // Log 1
        if (!token || !token.startsWith('Bearer ')) {
            console.log(">>> LOG 1a - Brak tokenu Bearer");
            return null;
        }
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            const decodedToken = JSON.parse(jsonPayload);
            console.log(">>> LOG 2 - Zdekodowany token:", decodedToken); // Log 2
            // Sprawdź oba popularne typy claimów dla ID użytkownika
            const nameIdClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            const subClaim = "sub"; // Inny popularny claim ID
            const userIdStr = decodedToken[nameIdClaim] || decodedToken[subClaim]; // Spróbuj obu
            console.log(">>> LOG 3 - Pobrany User ID (string):", userIdStr); // Log 3
            const userIdInt = userIdStr ? parseInt(userIdStr, 10) : null;
            console.log(">>> LOG 3a - Pobrany User ID (int):", userIdInt); // Log 3a
            return userIdInt;
        } catch (e) {
            console.error("Błąd dekodowania tokena JWT w ItemDetails:", e);
            return null;
        }
    };
    useEffect(() => {
        const fetchItemData = async () => {
            if (!itemId || itemId <= 0) {
                setError('Nieprawidłowe ID przedmiotu.');
                setLoading(false);
                return;
            }
            console.log(`>>> Rozpoczęcie fetchItemData dla itemId: ${itemId}`); // Log startu
            setLoading(true);
            setError(null);
            try {
                console.log(">>> Wywołanie API: GET /api/items/" + itemId); // Log API call
                const response = await fetch(`/api/items/${itemId}`, {
                    method: 'GET',
                    headers: getAuthHeaders(false)
                });
                // Użyj handleApiResponse do obsługi błędów i parsowania
                const data = await handleApiResponse(response);
                console.log(">>> LOG 4 - Dane przedmiotu z API (po handleApiResponse):", data); // Log 4

                // Sprawdź, czy dane nie są null/undefined po handleApiResponse
                if (!data) {
                    console.error(">>> BŁĄD: handleApiResponse zwróciło puste dane, mimo że status był OK?");
                    throw new Error("Otrzymano puste dane z API.");
                }

                setItem(data);

                const currentUserId = getCurrentUserId(); // Ta funkcja już loguje (Log 1, 2, 3, 3a)
                console.log(">>> LOG 5 - ID bieżącego użytkownika (z tokenu):", currentUserId); // Log 5

                // Sprawdź ostrożnie, czy data i user istnieją przed dostępem do id
                const ownerIdFromApi = data && data.user ? data.user.id : null;
                console.log(">>> LOG 6 - ID właściciela (z danych API):", ownerIdFromApi); // Log 6

                // Porównanie ID
                if (ownerIdFromApi !== null && currentUserId !== null && ownerIdFromApi === currentUserId) {
                    console.log(">>> LOG 7a - Użytkownik JEST właścicielem."); // Log 7a
                    setIsOwner(true);
                } else {
                    console.log(">>> LOG 7b - Użytkownik NIE JEST właścicielem (lub dane niekompletne/różne ID)."); // Log 7b
                    console.log(`>>> LOG 7c - Szczegóły porównania: ownerIdFromApi=${ownerIdFromApi}, currentUserId=${currentUserId}`); // Dodatkowy log porównania
                    setIsOwner(false);
                }

            } catch (err) {
                console.error(">>> Błąd w fetchItemData lub handleApiResponse:", err); // Log błędu
                setError(err.message);
                setItem(null);
            } finally {
                console.log(">>> Zakończenie fetchItemData, setLoading(false)"); // Log końca
                setLoading(false);
            }
        };
        console.log(`>>> ItemDetails Component: RENDER START dla itemId: ${itemId}`);
        fetchItemData();
    }, [itemId]);
    // --- KONIEC LOGÓW W fetchItemData ---

    // Funkcja do usuwania przedmiotu
    const handleDelete = async () => {
        if (!isOwner || !item) return;
        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${item.name}"? Tej operacji nie można cofnąć.`)) {
            return;
        }
        setLoading(true);
        setError(null);
        try {
            const response = await fetch(`/api/items/${item.id}`, {
                method: 'DELETE',
                headers: getAuthHeaders(false) // Wymagany token do usunięcia
            });
            await handleApiResponse(response); // Oczekujemy sukcesu (np. 204 No Content)
            alert(`Przedmiot "${item.name}" został usunięty.`);
            window.location.href = '/Items'; // Przekieruj na listę po usunięciu
        } catch (err) {
            console.error("Błąd podczas usuwania przedmiotu:", err);
            setError(`Nie udało się usunąć przedmiotu: ${err.message}`);
            setLoading(false); // Zatrzymaj ładowanie tylko przy błędzie
        }
        // setLoading(false) niepotrzebne po przekierowaniu
    };

    // --- Renderowanie ---
    if (loading) {
        // Możesz dodać console.log tutaj, żeby zobaczyć, czy spinner się pokazuje
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

    // console.log("Renderowanie: Wyświetlanie szczegółów przedmiotu", item);
    // Destrukturyzacja i reszta renderowania (bez zmian)
    const { name, description, condition, dateAdded, isAvailable, isForExchange, expectedValue, user, category, photos, tags } = item;
    const categoryName = category ? category.name : 'Brak';
    const userName = user ? `${user.firstName} ${user.lastName}` : 'Nieznany użytkownik';
    const userAvatar = user ? user.avatarPath : null;
    const userFirstName = user ? user.firstName : '?';
    const userLastName = user ? user.lastName : '?';
    const userEcoScore = user ? user.ecoScore : 0;
    const userId = user ? user.id : null;

    return (
        <div className="row">
            {/* Kolumna ze zdjęciami */}
            <div className="col-lg-7 mb-4">
                <ItemPhotoDisplay photos={photos || []} itemName={name} />
            </div>

            {/* Kolumna z informacjami */}
            <div className="col-lg-5">
                <div className="card shadow-sm">
                    <div className="card-header bg-light d-flex justify-content-between align-items-center flex-wrap">
                        <h3 className="mb-0 me-2">{name}</h3>
                        <span className={`badge fs-6 ${isAvailable ? 'bg-success' : 'bg-secondary'}`}>
                             {isAvailable ? 'Dostępny' : 'Niedostępny'}
                          </span>
                    </div>
                    <div className="card-body">
                        <p className="lead" style={{ whiteSpace: 'pre-wrap' }}>{description}</p> {/* Zachowaj białe znaki w opisie */}
                        <hr/>
                        <p><strong>Stan:</strong> {condition}</p>
                        <p><strong>Kategoria:</strong> {categoryName}</p>
                        <p><strong>Wartość/Cel:</strong> {expectedValue > 0 ? `${expectedValue.toFixed(2)} PLN` : (isForExchange ? 'Wymiana' : 'Za darmo')}</p>
                        <p><strong>Data dodania:</strong> {dateAdded ? new Date(dateAdded).toLocaleString('pl-PL') : 'Brak daty'}</p>

                        {/* Tagi */}
                        {tags && tags.length > 0 && (
                            <div className="mb-3">
                                <strong>Tagi:</strong>{' '}
                                {tags.map(tag => (
                                    <span key={tag.id} className="badge bg-info me-1">{tag.name}</span>
                                ))}
                            </div>
                        )}

                        {/* Informacje o użytkowniku */}
                        {user && (
                            <div className="mt-3 pt-3 border-top">
                                <h6>Wystawione przez:</h6>
                                <div className="d-flex align-items-center">
                                    {/* Miniaturka avatara */}
                                    {userAvatar ? (
                                        <img src={userAvatar} alt={userName} className="rounded-circle me-2" style={{ width: '40px', height: '40px', objectFit: 'cover' }} />
                                    ) : (
                                        <div className="rounded-circle bg-secondary text-white d-flex align-items-center justify-content-center me-2" style={{ width: '40px', height: '40px', fontSize: '1rem' }}>
                                            {userFirstName?.charAt(0)}{userLastName?.charAt(0)}
                                        </div>
                                    )}
                                    <div>
                                        {/* Link do profilu użytkownika - UŻYJ POPRAWNEJ ŚCIEŻKI! Np. /Profile/Index/{userId} lub /Users/{userId} */}
                                        <a href={`/Profile/Index/${userId}`} className="fw-bold text-decoration-none">{userName}</a>
                                        <br/>
                                        <small className="text-muted">EcoScore: {userEcoScore}</small>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Przyciski Akcji */}
                        <div className="mt-4 pt-3 border-top d-flex flex-wrap gap-2">
                            {/* Przyciski dla wszystkich (jeśli przedmiot jest dostępny i nie należy do nas) */}
                            {!isOwner && isAvailable && (
                                <button className="btn btn-primary" onClick={() => alert('TODO: Funkcjonalność wiadomości/transakcji.')}>
                                    <i className="bi bi-envelope me-1"></i> Zapytaj / Zaproponuj
                                </button>
                            )}

                            {/* Przyciski dla właściciela */}
                            {isOwner && (
                                <>
                                    {/* Użyj linku do akcji Edit kontrolera MVC, która zwróci widok z formularzem edycji */}
                                    <a href={`/Items/Edit/${item.id}`} className="btn btn-warning">
                                        <i className="bi bi-pencil-square me-1"></i> Edytuj
                                    </a>
                                    <button onClick={handleDelete} className="btn btn-danger" disabled={loading}>
                                        {loading && <span className="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>}
                                        <i className="bi bi-trash me-1"></i>
                                        Usuń
                                    </button>
                                    {/* TODO: Dodać przycisk Oznacz jako sprzedany/wymieniony */}
                                    {/* {isAvailable && <button className="btn btn-secondary ms-auto" onClick={() => alert('TODO: Oznacz jako niedostępny')}>Oznacz jako niedostępny</button>} */}
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
        const root = ReactDOM.createRoot(container); // <<<--- UŻYWA ReactDOM --->>>
        root.render(<StrictMode><ItemDetails itemId={itemId} /></StrictMode>);
    } else {
        container.innerHTML = '<div class="alert alert-danger">Błąd: Nieprawidłowe ID przedmiotu przekazane do komponentu.</div>';
        console.error("Nieprawidłowe ID przedmiotu w data-item-id:", container.getAttribute('data-item-id'));
    }
} else {
    console.error("Nie znaleziono kontenera 'react-item-details-container' do renderowania szczegółów.");
}

