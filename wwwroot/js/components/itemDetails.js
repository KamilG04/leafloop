// Ścieżka: wwwroot/js/components/itemDetails.js
// (Wersja z ApiService, ale BEZ zintegrowanego TransactionForm)

import React, { useState, useEffect, useCallback, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js'; // Użyj ApiService
import { getCurrentUserId } from '../utils/auth.js'; // Do sprawdzania właściciela

// Komponent do wyświetlania zdjęć (z poprawkami dla ApiService.getImageUrl)
const ItemPhotoDisplay = ({ photos, itemName }) => {
    if (!photos || photos.length === 0) {
        return (
            <div className="mb-3 bg-light d-flex align-items-center justify-content-center rounded" style={{ minHeight: '300px', maxHeight: '500px' }}>
                <i className="bi bi-image text-muted" style={{ fontSize: '4rem' }}></i>
            </div>
        );
    }
    // Użyj helpera dla spójności i obsługi placeholdera
    const defaultPlaceholder = ApiService.getImageUrl(null);

    if (photos.length === 1) {
        const photoPath = ApiService.getImageUrl(photos[0]?.path); // Użyj helpera
        return <img src={photoPath} className="img-fluid rounded mb-3" alt={photos[0]?.fileName || itemName || 'Item image'} style={{ maxHeight: '500px', objectFit: 'contain', display: 'block', margin: '0 auto' }} onError={(e) => { if (e.target.src !== defaultPlaceholder) e.target.src = defaultPlaceholder; }}/>;
    }

    // Karuzela
    const carouselId = `itemPhotosCarousel-${Date.now()}`;
    return (
        <div id={carouselId} className="carousel slide mb-3" data-bs-ride="carousel">
            <div className="carousel-indicators">
                {photos.map((_, index) => (
                    <button key={`indicator-${index}`} type="button" data-bs-target={`#${carouselId}`} data-bs-slide-to={index} className={index === 0 ? 'active' : ''} aria-current={index === 0 ? 'true' : 'false'} aria-label={`Zdjęcie ${index + 1}`}></button>
                ))}
            </div>
            <div className="carousel-inner rounded" style={{ maxHeight: '500px', backgroundColor: '#f8f9fa' }}>
                {photos.map((photo, index) => {
                    const photoPath = ApiService.getImageUrl(photo?.path); // Użyj helpera
                    // Dodaj fallback na ID, jeśli photo.id jest puste/null
                    const key = photo?.id ? `photo-${photo.id}` : `item-${index}`;
                    return (
                        <div key={key} className={`carousel-item ${index === 0 ? 'active' : ''}`}>
                            <img src={photoPath} className="d-block w-100" alt={photo?.fileName || `Zdjęcie ${index + 1}`} style={{ maxHeight: '500px', objectFit: 'contain'}} onError={(e) => { if (e.target.src !== defaultPlaceholder) e.target.src = defaultPlaceholder; }}/>
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
ItemPhotoDisplay.displayName = 'ItemPhotoDisplay'; // Nazwa dla DevTools


// Główny komponent ItemDetails
const ItemDetails = ({ itemId }) => {
    const [item, setItem] = useState(null);
    const [loading, setLoading] = useState(true); // Ładowanie danych przedmiotu
    const [deleting, setDeleting] = useState(false); // Stan dla procesu usuwania
    const [error, setError] = useState(null);
    const [isOwner, setIsOwner] = useState(false);

    // Funkcja do pobierania danych przedmiotu
    const fetchItemData = useCallback(async () => {
        if (!itemId || itemId <= 0) {
            setError('Nieprawidłowe ID przedmiotu.');
            setLoading(false);
            return;
        }
        setLoading(true);
        setError(null);
        setDeleting(false); // Resetuj stan usuwania przy nowym ładowaniu
        console.log(`ItemDetails: Fetching data for item ID: ${itemId}`);

        try {
            const data = await ApiService.get(`/api/items/${itemId}`);
            if (!data) throw new Error("Nie znaleziono przedmiotu lub otrzymano pustą odpowiedź.");

            console.log("ItemDetails: Received item data:", data);
            setItem(data);

            // Sprawdź właściciela
            const currentUserId = getCurrentUserId();
            const ownerId = data.user?.id;
            setIsOwner(!!(currentUserId && ownerId && currentUserId === ownerId));

        } catch (err) {
            console.error("ItemDetails: Error fetching item data:", err);
            setError(err.message || "Nie udało się załadować szczegółów przedmiotu.");
            setItem(null);
        } finally {
            setLoading(false);
        }
    }, [itemId]);

    // Efekt do pobrania danych
    useEffect(() => {
        fetchItemData();
    }, [fetchItemData]);

    // Funkcja do usuwania przedmiotu
    const handleDelete = useCallback(async () => {
        if (!isOwner || !item?.id) return;
        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${item.name || 'Bez nazwy'}"? Tej operacji nie można cofnąć.`)) {
            return;
        }

        setDeleting(true);
        setError(null); // Wyczyść poprzednie błędy przed próbą usunięcia

        try {
            await ApiService.delete(`/api/items/${item.id}`);
            alert(`Przedmiot "${item.name || 'Bez nazwy'}" został usunięty.`);
            window.location.href = '/Items/MyItems'; // Przekieruj
        } catch (err) {
            console.error("ItemDetails: Error deleting item:", err);
            setError(`Nie udało się usunąć przedmiotu: ${err.message || "Nieznany błąd"}`);
            setDeleting(false); // Wyłącz stan usuwania TYLKO w razie błędu
        }
        // Przy sukcesie następuje przekierowanie, więc nie resetujemy deleting
    }, [item, isOwner]);

    // --- Renderowanie ---

    // Stan ładowania początkowego
    if (loading) { // Uproszczone - pokazuj spinner, dopóki item nie zostanie załadowany
        return (
            <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '300px' }}>
                <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}>
                    <span className="visually-hidden">Ładowanie szczegółów...</span>
                </div>
            </div>
        );
    }

    // Stan błędu ładowania (jeśli po załadowaniu item jest nadal null)
    if (error && !item) {
        return (
            <div className="alert alert-danger d-flex flex-column align-items-center" role="alert">
                <span>Błąd: {error}</span>
                <button className="btn btn-outline-danger btn-sm mt-2" onClick={fetchItemData}>
                    <i className="bi bi-arrow-clockwise me-1"></i> Spróbuj ponownie załadować
                </button>
            </div>
        );
    }

    // Stan nieznalezionego przedmiotu (jeśli nie ma błędu, ale item jest null)
    if (!item) {
        return <div className="alert alert-warning" role="alert">Nie znaleziono przedmiotu o podanym ID.</div>;
    }

    // Destrukturyzacja danych przedmiotu (po sprawdzeniu, że item nie jest null)
    const { id, name, description, condition, dateAdded, isAvailable, isForExchange, expectedValue, user, category, photos, tags } = item;
    const categoryName = category?.name || 'Brak';
    const userName = user ? `${user.firstName || ''} ${user.lastName || ''}`.trim() : 'Nieznany użytkownik';
    const userAvatar = user?.avatarPath || null;
    const userFirstName = user?.firstName || '?';
    const userLastName = user?.lastName || '?';
    const userEcoScore = user?.ecoScore ?? 0;
    const userId = user?.id || null; // ID właściciela przedmiotu

    // Domyślny awatar
    const defaultAvatar = ApiService.getImageUrl(null);

    return (
        <div className="row">
            {/* Kolumna Zdjęcia */}
            <div className="col-lg-7 mb-4">
                <ItemPhotoDisplay photos={photos || []} itemName={name} />
            </div>

            {/* Kolumna Informacje */}
            <div className="col-lg-5">
                <div className="card shadow-sm">
                    <div className="card-header bg-light d-flex justify-content-between align-items-center flex-wrap">
                        <h3 className="mb-0 me-2">{name}</h3>
                        <span className={`badge fs-6 ${isAvailable ? 'bg-success' : 'bg-secondary'}`}>
                            {isAvailable ? 'Dostępny' : 'Niedostępny'}
                        </span>
                    </div>
                    <div className="card-body">
                        {/* Wyświetlaj błąd usuwania, jeśli wystąpił */}
                        {error && deleting && <div className="alert alert-danger mt-2">{error}</div>}

                        <p className="lead" style={{ whiteSpace: 'pre-wrap' }}>{description || 'Brak opisu.'}</p>
                        <hr/>
                        <p><strong>Stan:</strong> {condition || 'Nieokreślony'}</p>
                        <p><strong>Kategoria:</strong> {categoryName}</p>
                        <p><strong>Wartość/Cel:</strong> {expectedValue > 0 ? `${expectedValue.toFixed(2)} PLN` : (isForExchange ? 'Wymiana' : 'Za darmo')}</p>
                        <p><strong>Data dodania:</strong> {dateAdded ? new Date(dateAdded).toLocaleString('pl-PL', { dateStyle: 'medium', timeStyle: 'short' }) : 'Brak daty'}</p>

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
                        {user && userId && (
                            <div className="mt-3 pt-3 border-top">
                                <h6>Wystawione przez:</h6>
                                <div className="d-flex align-items-center">
                                    <img
                                        src={ApiService.getImageUrl(userAvatar)} // Użyj helpera
                                        alt={userName}
                                        className="rounded-circle me-2"
                                        style={{ width: '40px', height: '40px', objectFit: 'cover' }}
                                        onError={(e) => { if (e.target.src !== defaultAvatar) e.target.src = defaultAvatar; }} // Fallback
                                    />
                                    <div>
                                        {/* Link do profilu właściciela */}
                                        <a href={`/Profile/Index/${userId}`} className="fw-bold text-decoration-none">{userName}</a>
                                        <br/>
                                        <small className="text-muted">EcoScore: {userEcoScore}</small>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Przyciski Akcji */}
                        <div className="mt-4 pt-3 border-top d-flex flex-wrap gap-2">
                            {/* Przycisk "Zapytaj" dla nie-właścicieli */}
                            {!isOwner && isAvailable && (
                                <button className="btn btn-primary" onClick={() => alert('Funkcjonalność "Zapytaj / Zaproponuj" zostanie zastąpiona przez formularz transakcji poniżej (lub zintegrowana).')}>
                                    <i className="bi bi-envelope me-1"></i> Zapytaj / Zaproponuj
                                </button>
                                // Tutaj normalnie byłby TransactionForm, ale na razie go pomijamy
                            )}

                            {/* Przyciski Edytuj/Usuń dla właściciela */}
                            {isOwner && (
                                <>
                                    <a href={`/Items/Edit/${id}`} className="btn btn-warning">
                                        <i className="bi bi-pencil-square me-1"></i> Edytuj
                                    </a>
                                    <button onClick={handleDelete} className="btn btn-danger" disabled={deleting}>
                                        {deleting ? (
                                            <><span className="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>Usuwanie...</>
                                        ) : (
                                            <><i className="bi bi-trash me-1"></i> Usuń</>
                                        )}
                                    </button>
                                </>
                            )}
                        </div>
                    </div>{/* Koniec card-body */}
                </div>{/* Koniec card */}

                {/* -------------------------------------------------------------
                    Miejsce, gdzie normalnie dodalibyśmy TransactionForm:

                    {!isOwner && isAvailable && (
                        <div className="mt-4"> 
                            <TransactionForm itemId={id} itemName={name} />
                        </div>
                    )}
                    -------------------------------------------------------------
                */}

            </div> {/* Koniec col-lg-5 */}
        </div> // Koniec row
    );
};
ItemDetails.displayName = 'ItemDetails'; // Nazwa dla DevTools


// --- Inicjalizacja Komponentu (bez zmian) ---
document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('react-item-details-container');
    if (container) {
        const itemIdString = container.getAttribute('data-item-id');
        const itemId = parseInt(itemIdString, 10);
        if (!isNaN(itemId) && itemId > 0) {
            try {
                const root = ReactDOM.createRoot(container);
                root.render(<StrictMode><ItemDetails itemId={itemId} /></StrictMode>);
                console.log(`ItemDetails component initialized for ItemID: ${itemId}`);
            } catch (error) {
                console.error("Error rendering ItemDetails component:", error);
                container.innerHTML = `<div class="alert alert-danger">Error initializing component: ${error.message}</div>`;
            }
        } else {
            console.error(`ItemDetails: Invalid or missing item ID in data-item-id attribute. Value found: "${itemIdString}". Parsed as: ${itemId}.`);
            container.innerHTML = '<div class="alert alert-danger">Błąd krytyczny: Nie można załadować szczegółów. Brak poprawnego ID przedmiotu w atrybucie HTML.</div>';
        }
    } else {
        console.warn("Container element '#react-item-details-container' not found on this page.");
    }
});