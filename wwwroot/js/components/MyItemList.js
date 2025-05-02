// Pełna ścieżka: wwwroot/js/components/MyItemList.js
import React, { useState, useEffect, StrictMode, useCallback } from 'react';
import ReactDOM from 'react-dom/client';
// Używamy standardowych helperów
import { getAuthHeaders, handleApiResponse, getResponseData } from '../utils/auth.js';

// Komponent MyItemCard (bez zmian)
const MyItemCard = ({ item, onDelete }) => {
    const photoPath = item.mainPhotoPath || '/img/placeholder-item.png'; // Dodano fallback
    const placeholder = (
        <div className="card-img-top bg-light d-flex align-items-center justify-content-center" style={{ height: '180px' }}>
            <i className="bi bi-box-seam text-muted" style={{ fontSize: '2.5rem' }}></i>
        </div>
    );
    const getStatusBadge = (isAvailable) => {
        return isAvailable
            ? <span className="badge bg-success">Dostępny</span>
            : <span className="badge bg-secondary">Niedostępny</span>;
    };

    return (
        <div className="col-sm-6 col-md-4 col-lg-3 mb-4">
            <div className="card h-100 shadow-sm">
                <img src={photoPath} className="card-img-top" alt={item.name} style={{ height: '180px', objectFit: 'cover' }} onError={(e) => { e.target.src = '/img/placeholder-item.png'; }}/>
                <div className="card-body d-flex flex-column pb-2">
                    <h5 className="card-title text-truncate">{item.name}</h5>
                    <div className="d-flex justify-content-between align-items-center mb-2">
                        {getStatusBadge(item.isAvailable)}
                        <span className="badge bg-info">{item.condition}</span>
                    </div>
                    <p className="card-text small text-muted flex-grow-1 mb-2">
                        Kategoria: {item.categoryName || 'Brak'}
                    </p>
                    <a href={`/Items/Details/${item.id}`} className="btn btn-sm btn-outline-secondary mt-auto mb-2">
                        Podgląd
                    </a>
                </div>
                <div className="card-footer bg-light d-flex justify-content-between">
                    <a href={`/Items/Edit/${item.id}`} className="btn btn-sm btn-warning">
                        <i className="bi bi-pencil-square me-1"></i> Edytuj
                    </a>
                    <button onClick={() => onDelete(item.id, item.name)} className="btn btn-sm btn-danger">
                        <i className="bi bi-trash me-1"></i> Usuń
                    </button>
                </div>
            </div>
        </div>
    );
};


// Główny Komponent Listy "Moich Przedmiotów"
const MyItemList = () => {
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionError, setActionError] = useState(null);

    const fetchMyItems = useCallback(async () => {
        setLoading(true);
        setError(null);
        setActionError(null);
        try {
            const url = '/api/items/my';
            const headers = getAuthHeaders(false); // Pobierz nagłówki z tokenem

            // === LOGOWANIE WYSYŁANYCH NAGŁÓWKÓW ===
            console.log("MyItemList: Wysyłanie żądania do", url);
            console.log("MyItemList: Nagłówki:", headers);
            if (!headers['Authorization']) {
                console.warn("MyItemList: Brak nagłówka Authorization! Token nie został znaleziony?");
            }
            // =====================================

            const response = await fetch(url, { method: 'GET', headers: headers });

            // Sprawdź content-type przed handleApiResponse
            const contentType = response.headers.get("content-type");
            if (contentType && !contentType.includes("application/json") && response.status !== 204) {
                console.error("MyItemList: Otrzymano odpowiedź inną niż JSON. Status:", response.status);
                // handleApiResponse powinien obsłużyć 401/403 i przekierować
                if (response.status !== 401 && response.status !== 403) {
                    throw new Error(`Otrzymano nieoczekiwany format odpowiedzi (${contentType || 'brak'}) z serwera.`);
                }
            }

            const apiResult = await handleApiResponse(response); // Obsługa błędów i parsowanie
            console.log("MyItemList: Surowy wynik z API (po handleApiResponse):", apiResult);

            // === UŻYCIE getResponseData ===
            const itemsData = getResponseData(apiResult); // Wyciągnij dane z pola 'data'
            console.log("MyItemList: Dane przedmiotów (po getResponseData):", itemsData);
            // =============================

            setItems(Array.isArray(itemsData) ? itemsData : []); // Ustaw stan poprawnymi danymi

        } catch (err) {
            console.error("MyItemList: Błąd podczas pobierania przedmiotów:", err);
            setError(err.message || "Wystąpił błąd podczas ładowania Twoich przedmiotów.");
            // handleApiResponse powinien przekierować przy 401
            setItems([]);
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        fetchMyItems();
    }, [fetchMyItems]);

    const handleDeleteItem = useCallback(async (itemId, itemName) => {
        // ... (logika usuwania bez zmian, używa getAuthHeaders) ...
        setActionError(null);
        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${itemName}"? Tej operacji nie można cofnąć.`)) {
            return;
        }
        console.log(`Attempting to delete item ${itemId}`);
        try {
            const response = await fetch(`/api/items/${itemId}`, {
                method: 'DELETE',
                headers: getAuthHeaders(false) // Wymaga tokena
            });
            await handleApiResponse(response);
            alert(`Przedmiot "${itemName}" został usunięty.`);
            fetchMyItems(); // Odśwież listę
        } catch (err) {
            console.error(`Błąd podczas usuwania przedmiotu ${itemId}:`, err);
            setActionError(`Nie udało się usunąć przedmiotu "${itemName}": ${err.message}`);
        }
    }, [fetchMyItems]);

    // --- Renderowanie ---
    if (loading) {
        return ( /* ... spinner ... */
            <div className="d-flex justify-content-center align-items-center py-5">
                <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}>
                    <span className="visually-hidden">Ładowanie...</span>
                </div>
            </div>
        );
    }

    if (error) {
        return ( /* ... komunikat błędu ... */
            <div className="alert alert-danger" role="alert">
                <i className="bi bi-exclamation-triangle-fill me-2"></i>
                Błąd podczas ładowania Twoich przedmiotów: {error}
                <div className="mt-3">
                    <button className="btn btn-outline-danger btn-sm me-2" onClick={fetchMyItems}>
                        <i className="bi bi-arrow-clockwise me-1"></i> Spróbuj ponownie
                    </button>
                    <a href="/Account/Login" className="btn btn-outline-primary btn-sm">
                        <i className="bi bi-box-arrow-in-right me-1"></i> Zaloguj się
                    </a>
                </div>
            </div>
        );
    }

    return ( /* ... reszta renderowania (lista lub komunikat o braku) ... */
        <div>
            {actionError && (
                <div className="alert alert-warning alert-dismissible fade show" role="alert">
                    <i className="bi bi-exclamation-triangle-fill me-2"></i>
                    {actionError}
                    <button type="button" className="btn-close" onClick={() => setActionError(null)} aria-label="Close"></button>
                </div>
            )}

            {items.length === 0 ? (
                <div className="text-center py-5">
                    <p className="lead text-muted">Nie dodałeś jeszcze żadnych przedmiotów.</p>
                    <a href="/Items/Create" className="btn btn-lg btn-success">
                        <i className="bi bi-plus-circle me-1"></i> Dodaj pierwszy przedmiot
                    </a>
                </div>
            ) : (
                <div className="row">
                    {items.map(item => (
                        <MyItemCard key={item.id} item={item} onDelete={handleDeleteItem} />
                    ))}
                </div>
            )}

            {items.length > 0 && (
                <div className="mt-4 d-flex justify-content-center">
                    <a href="/Items/Create" className="btn btn-success">
                        <i className="bi bi-plus-circle me-1"></i> Dodaj nowy przedmiot
                    </a>
                </div>
            )}
        </div>
    );
};

// Inicjalizacja komponentu (bez zmian)
document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('react-my-item-list-container');
    if (container) {
        try {
            const root = ReactDOM.createRoot(container);
            root.render(<StrictMode><MyItemList /></StrictMode>);
        } catch (error) {
            console.error("Error rendering MyItemList component:", error);
            container.innerHTML = `<div class="alert alert-danger">Error initializing component: ${error.message}</div>`;
        }
    }
});
