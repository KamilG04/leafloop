// Path: wwwroot/js/components/itemDetails.js
import React, { useState, useEffect, useCallback, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js'; // Użyj ApiService
import { getCurrentUserId } from '../utils/auth.js'; // Zachowaj do sprawdzania właściciela

// Komponent do wyświetlania zdjęć (bez zmian)
const ItemPhotoDisplay = ({ photos, itemName }) => {
    if (!photos || photos.length === 0) {
        return (
            <div className="mb-3 bg-light d-flex align-items-center justify-content-center rounded" style={{ minHeight: '300px', maxHeight: '500px' }}>
                <i className="bi bi-image text-muted" style={{ fontSize: '4rem' }}></i>
            </div>
        );
    }

    if (photos.length === 1) {
        // Poprawka: Upewnij się, że ścieżka istnieje przed użyciem
        const photoPath = photos[0]?.path;
        if (!photoPath) return null; // Lub zwróć placeholder, jeśli ścieżka jest pusta

        return <img src={photoPath} className="img-fluid rounded mb-3" alt={photos[0].fileName || itemName} style={{ maxHeight: '500px', objectFit: 'contain', display: 'block', margin: '0 auto' }} />;
    }

    // Karuzela (bez zmian)
    const carouselId = `itemPhotosCarousel-${Date.now()}`;
    return (
        <div id={carouselId} className="carousel slide mb-3" data-bs-ride="carousel">
            <div className="carousel-indicators">
                {photos.map((_, index) => (
                    <button key={index} type="button" data-bs-target={`#${carouselId}`} data-bs-slide-to={index} className={index === 0 ? 'active' : ''} aria-current={index === 0 ? 'true' : 'false'} aria-label={`Zdjęcie ${index + 1}`}></button>
                ))}
            </div>
            <div className="carousel-inner rounded" style={{ maxHeight: '500px', backgroundColor: '#f8f9fa' }}>
                {photos.map((photo, index) => (
                    // Poprawka: Upewnij się, że photo.path istnieje
                    photo?.path ? (
                        <div key={photo.id} className={`carousel-item ${index === 0 ? 'active' : ''}`}>
                            <img src={photo.path} className="d-block w-100" alt={photo.fileName || `Zdjęcie ${index + 1}`} style={{ maxHeight: '500px', objectFit: 'contain'}}/>
                        </div>
                    ) : null // Pomiń, jeśli brakuje ścieżki
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


// Główny komponent ItemDetails
const ItemDetails = ({ itemId }) => {
    const [item, setItem] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [isOwner, setIsOwner] = useState(false);

    // Użyj useCallback dla stabilności referencji funkcji
    const fetchItemData = useCallback(async () => {
        // Walidacja ID na początku
        if (!itemId || itemId <= 0) {
            setError('Nieprawidłowe ID przedmiotu.');
            setLoading(false);
            return;
        }

        setLoading(true);
        setError(null); // Resetuj błąd przy ponownej próbie

        try {
            // Użyj ApiService.get zamiast fetch
            const data = await ApiService.get(`/api/items/${itemId}`);

            // Sprawdź, czy ApiService zwrócił dane (może zwracać null/undefined przy błędzie)
            if (!data) {
                throw new Error("Nie otrzymano danych z API.");
            }

            setItem(data);

            // Sprawdź, czy bieżący użytkownik jest właścicielem
            const currentUserId = getCurrentUserId(); // Użyj funkcji pomocniczej z auth.js
            const ownerId = data.user?.id; // Użyj optional chaining (?.) - dane z Postmana pokazują zagnieżdżony obiekt user

            // Ustaw stan isOwner (konwersja na boolean za pomocą !!)
            setIsOwner(!!(currentUserId && ownerId && currentUserId === ownerId));

        } catch (err) {
            console.error("Błąd podczas pobierania szczegółów przedmiotu:", err);
            // Użyj wiadomości błędu z ApiService lub domyślnej
            setError(err.message || "Nie udało się załadować szczegółów przedmiotu.");
            setItem(null); // Wyczyść dane przedmiotu w razie błędu
        } finally {
            setLoading(false);
        }
    }, [itemId]); // Zależność od itemId

    // Efekt do pobrania danych przy montowaniu komponentu lub zmianie itemId
    useEffect(() => {
        fetchItemData();
    }, [fetchItemData]); // Zależność od memoizowanej funkcji fetchItemData

    // Obsługa usuwania przedmiotu
    const handleDelete = useCallback(async () => {
        // Sprawdzenia bezpieczeństwa
        if (!isOwner || !item?.id) return;

        // Potwierdzenie użytkownika
        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${item.name}"? Tej operacji nie można cofnąć.`)) {
            return;
        }

        setLoading(true); // Pokaż stan ładowania podczas usuwania
        setError(null);

        try {
            // Użyj ApiService.delete
            await ApiService.delete(`/api/items/${item.id}`);

            alert(`Przedmiot "${item.name}" został usunięty.`);
            // Przekieruj użytkownika po usunięciu (np. do listy jego przedmiotów)
            window.location.href = '/Items/MyItems';
        } catch (err) {
            console.error("Błąd podczas usuwania przedmiotu:", err);
            setError(`Nie udało się usunąć przedmiotu: ${err.message || "Nieznany błąd"}`);
            setLoading(false); // Wyłącz ładowanie w razie błędu (nawigacja nie nastąpi)
        }
        // Nie ma finally setLoading(false), bo przy sukcesie następuje przekierowanie
    }, [item, isOwner]); // Zależności dla useCallback

    // --- Renderowanie ---

    // Stan ładowania (pokazuj tylko przy pierwszym ładowaniu lub podczas operacji usuwania)
    if (loading && !item) {
        return (
            <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '200px' }}>
                <div className="spinner-border text-success" role="status">
                    <span className="visually-hidden">Ładowanie...</span>
                </div>
            </div>
        );
    }

    // Stan błędu
    if (error) {
        return (
            <div className="alert alert-danger d-flex flex-column align-items-center" role="alert">
                <span>Błąd: {error}</span>
                {/* Przycisk ponowienia próby */}
                <button className="btn btn-outline-danger btn-sm mt-2" onClick={fetchItemData}>
                    <i className="bi bi-arrow-clockwise me-1"></i> Spróbuj ponownie
                </button>
            </div>
        );
    }

    // Stan "Nie znaleziono" (jeśli ładowanie zakończone, ale brak przedmiotu)
    if (!item) {
        return <div className="alert alert-warning" role="alert">Nie znaleziono przedmiotu o podanym ID lub wystąpił błąd podczas ładowania.</div>;
    }

    // Destrukturyzacja właściwości przedmiotu (upewnij się, że pasują do odpowiedzi API z Postmana)
    const { name, description, condition, dateAdded, isAvailable, isForExchange, expectedValue, user, category, photos, tags } = item;
    const categoryName = category?.name || 'Brak'; // Optional chaining
    const userName = user ? `${user.firstName} ${user.lastName}` : 'Nieznany użytkownik';
    const userAvatar = user?.avatarPath || null; // Załóżmy, że UserDto ma avatarPath
    const userFirstName = user?.firstName || '?';
    const userLastName = user?.lastName || '?';
    const userEcoScore = user?.ecoScore ?? 0; // Nullish coalescing dla wartości domyślnej 0
    const userId = user?.id || null;

    // Renderowanie widoku szczegółów
    return (
        <div className="row">
            {/* Kolumna ze zdjęciami */}
            <div className="col-lg-7 mb-4">
                <ItemPhotoDisplay photos={photos || []} itemName={name} />
            </div>

            {/* Kolumna z informacjami */}
            <div className="col-lg-5">
                <div className="card shadow-sm">
                    {/* Nagłówek karty */}
                    <div className="card-header bg-light d-flex justify-content-between align-items-center flex-wrap">
                        <h3 className="mb-0 me-2">{name}</h3>
                        <span className={`badge fs-6 ${isAvailable ? 'bg-success' : 'bg-secondary'}`}>
                            {isAvailable ? 'Dostępny' : 'Niedostępny'}
                        </span>
                    </div>
                    {/* Ciało karty */}
                    <div className="card-body">
                        <p className="lead" style={{ whiteSpace: 'pre-wrap' }}>{description || 'Brak opisu.'}</p>
                        <hr/>
                        <p><strong>Stan:</strong> {condition || 'Nieokreślony'}</p>
                        <p><strong>Kategoria:</strong> {categoryName}</p>
                        <p><strong>Wartość/Cel:</strong> {expectedValue > 0 ? `${expectedValue.toFixed(2)} PLN` : (isForExchange ? 'Wymiana' : 'Za darmo')}</p>
                        <p><strong>Data dodania:</strong> {dateAdded ? new Date(dateAdded).toLocaleString('pl-PL') : 'Brak daty'}</p>

                        {/* Tagi */}
                        {tags && tags.length > 0 && (
                            <div className="mb-3">
                                <strong>Tagi:</strong>{' '}
                                {tags.map(tag => (
                                    // Upewnij się, że TagDto ma 'id' i 'name'
                                    <span key={tag.id} className="badge bg-info me-1">{tag.name}</span>
                                ))}
                            </div>
                        )}

                        {/* Informacje o użytkowniku */}
                        {user && userId && ( // Renderuj tylko jeśli jest user i jego ID
                            <div className="mt-3 pt-3 border-top">
                                <h6>Wystawione przez:</h6>
                                <div className="d-flex align-items-center">
                                    {/* Awatar z fallbackiem */}
                                    {userAvatar ? (
                                        <img src={userAvatar} alt={userName} className="rounded-circle me-2" style={{ width: '40px', height: '40px', objectFit: 'cover' }} onError={(e) => { e.target.style.display='none'; e.target.nextSibling.style.display='flex'; }}/>
                                    ) : null}
                                    <div className={`rounded-circle bg-secondary text-white d-flex align-items-center justify-content-center me-2 ${userAvatar ? 'd-none' : ''}`} style={{ width: '40px', height: '40px', fontSize: '1rem', display: userAvatar ? 'none' : 'flex' }}>
                                        {userFirstName?.charAt(0)}{userLastName?.charAt(0)}
                                    </div>
                                    <div>
                                        {/* Link do profilu */}
                                        <a href={`/Profile/Index/${userId}`} className="fw-bold text-decoration-none">{userName}</a>
                                        <br/>
                                        <small className="text-muted">EcoScore: {userEcoScore}</small>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Przyciski akcji */}
                        <div className="mt-4 pt-3 border-top d-flex flex-wrap gap-2">
                            {/* Przycisk dla nie-właścicieli */}
                            {!isOwner && isAvailable && (
                                <button className="btn btn-primary" onClick={() => alert('TODO: Funkcjonalność wiadomości/transakcji.')}>
                                    <i className="bi bi-envelope me-1"></i> Zapytaj / Zaproponuj
                                </button>
                            )}

                            {/* Przyciski dla właściciela */}
                            {isOwner && (
                                <>
                                    <a href={`/Items/Edit/${item.id}`} className="btn btn-warning">
                                        <i className="bi bi-pencil-square me-1"></i> Edytuj
                                    </a>
                                    {/* Przycisk usuwania z obsługą stanu ładowania */}
                                    <button onClick={handleDelete} className="btn btn-danger" disabled={loading}>
                                        {loading && <span className="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>}
                                        <i className="bi bi-trash me-1"></i> Usuń
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


// Inicjalizacja komponentu (bez zmian)
document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('react-item-details-container');
    if (container) {
        const itemId = parseInt(container.getAttribute('data-item-id'), 10);
        // Poprawka: Sprawdzaj poprawność itemId przed renderowaniem
        if (!isNaN(itemId) && itemId > 0) {
            try {
                const root = ReactDOM.createRoot(container);
                root.render(<StrictMode><ItemDetails itemId={itemId} /></StrictMode>);
            } catch (error) {
                console.error("Błąd podczas renderowania komponentu ItemDetails:", error);
                container.innerHTML = `<div class="alert alert-danger">Błąd inicjalizacji komponentu: ${error.message}</div>`;
            }
        } else {
            // Wyświetl błąd, jeśli ID jest nieprawidłowe
            container.innerHTML = '<div class="alert alert-danger">Nieprawidłowe ID przedmiotu przekazane w atrybucie data.</div>';
            console.error("Invalid item ID found in data-item-id attribute:", container.getAttribute('data-item-id'));
        }
    } else {
        // Ostrzeżenie, jeśli kontener nie zostanie znaleziony
        console.warn("Nie znaleziono kontenera '#react-item-details-container'.");
    }
});