// Ścieżka: wwwroot/js/components/itemDetails.js
// (Wersja z przyciskiem "Chcę to!" i informacją o transakcjach dla właściciela)

import React, { useState, useEffect, useCallback, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js';
import { getCurrentUserId } from '../utils/auth.js';
// Import TransactionForm NIE jest już potrzebny

// Komponent do wyświetlania zdjęć (bez zmian od ostatniej działającej wersji)
const ItemPhotoDisplay = ({ photos, itemName }) => {
    if (!photos || photos.length === 0) {
        return (
            <div className="mb-3 bg-light d-flex align-items-center justify-content-center rounded" style={{ minHeight: '300px', maxHeight: '500px' }}>
                <i className="bi bi-image text-muted" style={{ fontSize: '4rem' }}></i>
            </div>
        );
    }
    const defaultPlaceholder = ApiService.getImageUrl(null);

    if (photos.length === 1) {
        const photoPath = ApiService.getImageUrl(photos[0]?.path);
        return <img src={photoPath} className="img-fluid rounded mb-3" alt={photos[0]?.fileName || itemName || 'Item image'} style={{ maxHeight: '500px', objectFit: 'contain', display: 'block', margin: '0 auto' }} onError={(e) => { if (e.target.src !== defaultPlaceholder) e.target.src = defaultPlaceholder; }}/>;
    }

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
                    const photoPath = ApiService.getImageUrl(photo?.path);
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
ItemPhotoDisplay.displayName = 'ItemPhotoDisplay';


// Główny komponent ItemDetails
const ItemDetails = ({ itemId }) => {
    // Istniejące stany
    const [item, setItem] = useState(null);
    const [loading, setLoading] = useState(true);
    const [deleting, setDeleting] = useState(false); // Stan usuwania
    const [error, setError] = useState(null); // Błędy ładowania LUB operacji
    const [isOwner, setIsOwner] = useState(false);

    // Nowe stany dla inicjacji transakcji
    const [initiatingTransaction, setInitiatingTransaction] = useState(false);
    const [transactionInitiated, setTransactionInitiated] = useState(false); // Czy już zainicjowano?

    // Funkcja do pobierania danych przedmiotu (zakładamy, że API zwraca teraz 'pendingTransactions')
    const fetchItemData = useCallback(async () => {
        if (!itemId || itemId <= 0) {
            setError('Nieprawidłowe ID przedmiotu.');
            setLoading(false);
            return;
        }
        setLoading(true);
        setError(null);
        setDeleting(false);
        setInitiatingTransaction(false); // Resetuj stany akcji
        setTransactionInitiated(false);
        console.log(`ItemDetails: Fetching data for item ID: ${itemId}`);

        try {
            const data = await ApiService.get(`/api/items/${itemId}`);
            if (!data) throw new Error("Nie znaleziono przedmiotu lub otrzymano pustą odpowiedź.");

            console.log("ItemDetails: Received item data:", data);
            setItem(data);

            const currentUserId = getCurrentUserId();
            const ownerId = data.user?.id;
            setIsOwner(!!(currentUserId && ownerId && currentUserId === ownerId));

            // TODO: Sprawdź, czy bieżący użytkownik (jeśli nie jest właścicielem)
            // już zainicjował transakcję dla tego przedmiotu. Wymaga to informacji
            // z API w odpowiedzi /api/items/{id} lub osobnego zapytania.
            // Jeśli tak, ustaw setTransactionInitiated(true);
            // Przykład: if (!isOwner && data.currentUserTransactionStatus === 'Initiated') setTransactionInitiated(true);

        } catch (err) {
            console.error("ItemDetails: Error fetching item data:", err);
            setError(err.message || "Nie udało się załadować szczegółów przedmiotu.");
            setItem(null);
        } finally {
            setLoading(false);
        }
    }, [itemId]);

    useEffect(() => {
        fetchItemData();
    }, [fetchItemData]);

    // Funkcja do usuwania przedmiotu (bez zmian)
    const handleDelete = useCallback(async () => {
        if (!isOwner || !item?.id) return;
        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${item.name || 'Bez nazwy'}"? Tej operacji nie można cofnąć.`)) {
            return;
        }
        setDeleting(true);
        setError(null);
        try {
            await ApiService.delete(`/api/items/${item.id}`);
            alert(`Przedmiot "${item.name || 'Bez nazwy'}" został usunięty.`);
            window.location.href = '/Items/MyItems';
        } catch (err) {
            console.error("ItemDetails: Error deleting item:", err);
            setError(`Nie udało się usunąć przedmiotu: ${err.message || "Nieznany błąd"}`);
            setDeleting(false);
        }
    }, [item, isOwner]);

    // Funkcja inicjowania transakcji przez kupującego
    const handleInitiateTransaction = useCallback(async () => {
        // Podstawowe zabezpieczenia
        if (!item?.id || isOwner || !item?.isAvailable || transactionInitiated) return;

        if (!window.confirm('Czy na pewno chcesz rozpocząć transakcję dla tego przedmiotu? Skontaktuj się z właścicielem przez widok transakcji.')) {
            return;
        }

        setInitiatingTransaction(true);
        setError(null); // Wyczyść błędy z innych akcji

        try {
            // Przygotuj dane dla DTO (dostosuj 'type' jeśli potrzeba)
            const transactionData = {
                itemId: item.id,
                // Zakładamy, że backend ustali typ na podstawie item.isForExchange lub ma domyślny
                // type: item.isForExchange ? TransactionType.Exchange : TransactionType.Sale // Przykładowa logika (wymaga TransactionType enum/const)
                offer: null // Na razie nie wysyłamy oferty/wiadomości
            };
            console.log("ItemDetails: Initiating transaction with data:", transactionData);

            // Wywołaj API (zakładamy, że ApiService.post zwraca 'data' z odpowiedzi)
            const result = await ApiService.post('/api/transactions', transactionData);

            console.log("ItemDetails: Transaction initiated response:", result);
            setTransactionInitiated(true); // Ustaw flagę sukcesu
            // Zamiast alertu, stan `transactionInitiated` zmieni widok przycisku na komunikat sukcesu
            // alert('Transakcja została zainicjowana! Sprawdź zakładkę "Moje Transakcje" aby zobaczyć szczegóły i wysłać wiadomość.');

        } catch (err) {
            console.error('ItemDetails: Error initiating transaction:', err);
            // Ustaw błąd, który zostanie wyświetlony w komponencie
            setError(`Nie udało się zainicjować transakcji: ${err.message}`);
            setTransactionInitiated(false); // Upewnij się, że stan sukcesu jest false
        } finally {
            setInitiatingTransaction(false); // Zakończ stan ładowania przycisku
        }
    }, [item, isOwner]); // Zależności useCallback

    // --- Renderowanie ---

    if (loading) {
        return (
            <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '300px' }}>
                <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}>
                    <span className="visually-hidden">Ładowanie szczegółów...</span>
                </div>
            </div>
        );
    }

    if (error && !item) { // Błąd krytyczny - nie załadowano itemu
        return (
            <div className="alert alert-danger d-flex flex-column align-items-center" role="alert">
                <span>Błąd: {error}</span>
                <button className="btn btn-outline-danger btn-sm mt-2" onClick={fetchItemData}>
                    <i className="bi bi-arrow-clockwise me-1"></i> Spróbuj ponownie załadować
                </button>
            </div>
        );
    }

    if (!item) {
        return <div className="alert alert-warning" role="alert">Nie znaleziono przedmiotu o podanym ID.</div>;
    }

    // Destrukturyzacja danych przedmiotu
    // Zakładamy, że API zwraca teraz pole 'pendingTransactions' (może być null lub pusta tablica)
    const { id, name, description, condition, dateAdded, isAvailable, isForExchange, expectedValue, user, category, photos, tags, pendingTransactions } = item;
    const categoryName = category?.name || 'Brak';
    const userName = user ? `${user.firstName || ''} ${user.lastName || ''}`.trim() : 'Nieznany użytkownik';
    const userAvatar = user?.avatarPath || null;
    const ownerUserId = user?.id || null; // ID właściciela przedmiotu
    const defaultAvatar = ApiService.getImageUrl(null); // Ścieżka do placeholdera

    return (
        <div className="row">
            {/* Kolumna Zdjęcia */}
            <div className="col-lg-7 mb-4">
                <ItemPhotoDisplay photos={photos || []} itemName={name} />
            </div>

            {/* Kolumna Informacje */}
            <div className="col-lg-5">
                <div className="card shadow-sm">
                    {/* Nagłówek Karty */}
                    <div className="card-header bg-light d-flex justify-content-between align-items-center flex-wrap">
                        <h3 className="mb-0 me-2">{name}</h3>
                        <span className={`badge fs-6 ${isAvailable ? 'bg-success' : 'bg-secondary'}`}>
                            {isAvailable ? 'Dostępny' : 'Niedostępny'}
                        </span>
                    </div>
                    <div className="card-body">
                        {/* Wyświetlaj błąd operacji (usuwania LUB inicjacji transakcji) */}
                        {error && <div className="alert alert-danger">{error}</div>}

                        {/* Dane Przedmiotu */}
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

                        {/* Informacje o właścicielu */}
                        {user && ownerUserId && (
                            <div className="mt-3 pt-3 border-top">
                                <h6>Wystawione przez:</h6>
                                <div className="d-flex align-items-center">
                                    <img
                                        src={ApiService.getImageUrl(userAvatar)}
                                        alt={userName}
                                        className="rounded-circle me-2"
                                        style={{ width: '40px', height: '40px', objectFit: 'cover' }}
                                        onError={(e) => { if (e.target.src !== defaultAvatar) e.target.src = defaultAvatar; }}
                                    />
                                    <div>
                                        <a href={`/Profile/Index/${ownerUserId}`} className="fw-bold text-decoration-none">{userName}</a>
                                        <br/>
                                        <small className="text-muted">EcoScore: {user.ecoScore ?? 0}</small>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Przyciski Akcji i Informacje o Transakcjach */}
                        <div className="mt-4 pt-3 border-top">
                            {isOwner ? (
                                // --- Widok Właściciela ---
                                <>
                                    {/* Info o oczekujących transakcjach */}
                                    {pendingTransactions && pendingTransactions.length > 0 && (
                                        <div className="alert alert-info mb-3 small">
                                            <i className="bi bi-info-circle-fill me-2"></i>
                                            Masz {pendingTransactions.length} oczekującą(e) propozycję(e) transakcji dla tego przedmiotu.
                                            <a href="/Transactions" className="alert-link ms-1">Sprawdź Moje Transakcje</a>.
                                            {/* Można by tu wyświetlić imiona kupujących, jeśli DTO je zawiera */}
                                        </div>
                                    )}
                                    {/* Przyciski Edytuj/Usuń */}
                                    <div className="d-flex flex-wrap gap-2">
                                        <a href={`/Items/Edit/${id}`} className="btn btn-warning">
                                            <i className="bi bi-pencil-square me-1"></i> Edytuj
                                        </a>
                                        <button onClick={handleDelete} className="btn btn-danger" disabled={deleting}>
                                            {deleting ? (
                                                <><span className="spinner-border spinner-border-sm me-1"></span>Usuwanie...</>
                                            ) : (
                                                <><i className="bi bi-trash me-1"></i> Usuń</>
                                            )}
                                        </button>
                                    </div>
                                </>
                            ) : (
                                // --- Widok Kupującego ---
                                isAvailable ? (
                                    transactionInitiated ? (
                                        // Komunikat po udanej inicjacji
                                        <div className="alert alert-success">
                                            <i className="bi bi-check-circle-fill me-1"></i>
                                            Transakcja rozpoczęta! Sprawdź <a href="/Transactions" className="alert-link">Moje Transakcje</a>, aby wysłać wiadomość i kontynuować.
                                        </div>
                                    ) : (
                                        // Przycisk inicjacji transakcji
                                        <button
                                            className="btn btn-primary w-100" // Zmieniono kolor na primary
                                            onClick={handleInitiateTransaction}
                                            disabled={initiatingTransaction}
                                        >
                                            {initiatingTransaction ? (
                                                <><span className="spinner-border spinner-border-sm me-2"></span>Inicjowanie...</>
                                            ) : (
                                                <><i className="bi bi-send-plus-fill me-1"></i> Chcę to! (Rozpocznij transakcję)</> // Zmieniono ikonę/tekst
                                            )}
                                        </button>
                                    )
                                ) : (
                                    // Przedmiot niedostępny
                                    <div className="alert alert-secondary">Przedmiot jest aktualnie niedostępny.</div>
                                )
                            )}
                        </div>
                    </div> {/* Koniec card-body */}
                </div> {/* Koniec card */}
            </div> {/* Koniec col-lg-5 */}
        </div> // Koniec row
    );
};
ItemDetails.displayName = 'ItemDetails';

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