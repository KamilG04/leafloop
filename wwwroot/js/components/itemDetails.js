// Pełna ścieżka: wwwroot/js/components/itemDetails.js (lub .jsx) - POPRAWIONY

// ----- POCZĄTEK PLIKU -----
import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client'; // <<< --- DODANA/POPRAWIONA TA LINIA --- >>>
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js'; // Zaimportuj funkcje pomocnicze

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
        const token = getAuthHeaders(false)['Authorization']; // Pobierz tylko nagłówek Auth
        if (!token || !token.startsWith('Bearer ')) return null;
        try {
            const base64Url = token.split('.')[1];
            const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
            const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
                return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
            }).join(''));
            const decodedToken = JSON.parse(jsonPayload);
            const userIdClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
            const userIdStr = decodedToken[userIdClaim];
            return userIdStr ? parseInt(userIdStr, 10) : null;
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
            setLoading(true);
            setError(null);
            try {
                const response = await fetch(`/api/items/${itemId}`, {
                    method: 'GET',
                    headers: getAuthHeaders(false) // Zakładamy, że API zwraca 404 jeśli nie ma itemu, lub 401/403 jeśli jest chronione
                });
                const data = await handleApiResponse(response); // Oczekujemy ItemWithDetailsDto
                setItem(data);

                // Sprawdź, czy bieżący użytkownik jest właścicielem (porównaj ID)
                const currentUserId = getCurrentUserId();
                // DTO ItemWithDetailsDto zawiera obiekt 'user' z polem 'id'
                if (data && data.user && currentUserId && data.user.id === currentUserId) {
                    setIsOwner(true);
                } else {
                    setIsOwner(false);
                }

            } catch (err) {
                console.error("Błąd pobierania danych przedmiotu:", err);
                setError(err.message);
                setItem(null); // Wyczyść item w razie błędu
            } finally {
                setLoading(false);
            }
        };
        fetchItemData();
    }, [itemId]); // Zależność od itemId

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
        return <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '200px' }}><div className="spinner-border text-success" role="status"><span className="visually-hidden">Ładowanie...</span></div></div>;
    }
    if (error) { return <div className="alert alert-danger" role="alert">Błąd: {error}</div>; }
    // Jeśli nie ma błędu, ale item jest null (np. API zwróciło 404 i handleApiResponse rzuciło błąd przechwycony wyżej)
    if (!item) { return <div className="alert alert-warning" role="alert">Nie znaleziono przedmiotu o podanym ID lub wystąpił błąd podczas ładowania.</div>; }

    // Destrukturyzacja dla łatwiejszego dostępu (z ItemWithDetailsDto)
    const { name, description, condition, dateAdded, isAvailable, isForExchange, expectedValue, user, category, photos, tags } = item;

    // Bezpieczne sprawdzanie istnienia zagnieżdżonych obiektów
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

