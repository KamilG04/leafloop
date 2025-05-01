// Pełna ścieżka: wwwroot/js/components/MyItemList.js (lub .jsx)

import React, { useState, useEffect, StrictMode, useCallback } from 'react';
import ReactDOM from 'react-dom/client'; // Upewnij się, że ten import jest obecny!
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js'; // Zaimportuj funkcje pomocnicze

// --- Komponent Karty dla "Moich Przedmiotów" ---
// Podobny do ItemCard z itemList.js, ale z przyciskami Edytuj/Usuń
const MyItemCard = ({ item, onDelete }) => {
    const photoPath = item.mainPhotoPath || null; // Użyj pola z ItemDto
    const placeholder = (
        <div className="card-img-top bg-light d-flex align-items-center justify-content-center" style={{ height: '180px' }}>
            <i className="bi bi-box-seam text-muted" style={{ fontSize: '2.5rem' }}></i>
        </div>
    );

    // Mały helper do formatowania statusu
    const getStatusBadge = (isAvailable) => {
        return isAvailable
            ? <span className="badge bg-success">Dostępny</span>
            : <span className="badge bg-secondary">Niedostępny</span>;
    };

    return (
        <div className="col-sm-6 col-md-4 col-lg-3 mb-4">
            <div className="card h-100 shadow-sm">
                {photoPath ? (
                    <img src={photoPath} className="card-img-top" alt={item.name} style={{ height: '180px', objectFit: 'cover' }} />
                ) : (
                    placeholder
                )}
                <div className="card-body d-flex flex-column pb-2"> {/* Mniejszy padding dolny */}
                    <h5 className="card-title">{item.name}</h5>
                    <div className="d-flex justify-content-between align-items-center mb-2">
                        {getStatusBadge(item.isAvailable)}
                        <span className="badge bg-info">{item.condition}</span>
                    </div>
                    <p className="card-text small text-muted flex-grow-1 mb-2">
                        Kategoria: {item.categoryName || 'Brak'}
                    </p>
                    {/* Link do szczegółów - zawsze przydatny */}
                    <a href={`/Items/Details/${item.id}`} className="btn btn-sm btn-outline-secondary mt-auto mb-2">
                        Podgląd
                    </a>
                </div>
                {/* Stopka z akcjami tylko dla właściciela */}
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


// --- Główny Komponent Listy "Moich Przedmiotów" ---
const MyItemList = () => {
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [actionError, setActionError] = useState(null); // Osobny błąd dla akcji usuwania

    // Użyj useCallback, aby uniknąć ponownego tworzenia funkcji przy każdym renderze
    const fetchMyItems = useCallback(async () => {
        setLoading(true);
        setError(null);
        setActionError(null); // Czyść błędy akcji przy odświeżaniu
        try {
            // Endpoint API - upewnij się, że istnieje i jest poprawny
            const url = '/api/items/my'; // Zakładamy, że to endpoint dla przedmiotów zalogowanego użytkownika

            console.log("Fetching MY items from:", url);

            const response = await fetch(url, {
                method: 'GET',
                headers: getAuthHeaders(false) // Wymaga tokena!
            });
            const data = await handleApiResponse(response);
            setItems(data || []);
        } catch (err) {
            console.error("Błąd pobierania moich przedmiotów:", err);
            setError(err.message); // Ustaw główny błąd ładowania
            setItems([]); // Wyczyść w razie błędu
        } finally {
            setLoading(false);
        }
    }, []); // Pusta tablica zależności - funkcja nie zmienia się

    // Pobierz dane przy pierwszym renderowaniu
    useEffect(() => {
        fetchMyItems();
    }, [fetchMyItems]); // Wywołaj fetchMyItems

    // Funkcja obsługująca usuwanie przedmiotu
    const handleDeleteItem = useCallback(async (itemId, itemName) => {
        setActionError(null); // Wyczyść poprzedni błąd akcji
        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${itemName}"? Tej operacji nie można cofnąć.`)) {
            return;
        }

        // Można ustawić stan ładowania dla konkretnego itemu, ale dla uproszczenia ustawimy globalny (?)
        // Na razie nie ustawiamy globalnego loading, żeby nie blokować całej listy
        console.log(`Attempting to delete item ${itemId}`);
        try {
            const response = await fetch(`/api/items/${itemId}`, {
                method: 'DELETE',
                headers: getAuthHeaders(false) // Wymaga tokena
            });
            await handleApiResponse(response); // Oczekujemy sukcesu (np. 204 No Content)

            // Jeśli sukces, odśwież listę przedmiotów LUB usuń element ze stanu lokalnie
            alert(`Przedmiot "${itemName}" został usunięty.`);
            // Odświeżenie przez ponowne pobranie danych:
            fetchMyItems();
            // Alternatywa: usunięcie lokalne (szybsze dla UI)
            // setItems(prevItems => prevItems.filter(item => item.id !== itemId));

        } catch (err) {
            console.error(`Błąd podczas usuwania przedmiotu ${itemId}:`, err);
            setActionError(`Nie udało się usunąć przedmiotu "${itemName}": ${err.message}`);
            // Można pokazać błąd przy konkretnym elemencie lub jako alert globalny
        }
    }, [fetchMyItems]); // Zależność od fetchMyItems, aby mieć pewność, że jest aktualna

    // --- Renderowanie Komponentu ---

    // Stan ładowania
    if (loading) {
        return (
            <div className="d-flex justify-content-center align-items-center py-5">
                <div className="spinner-border text-success" role="status" style={{ width: '3rem', height: '3rem' }}>
                    <span className="visually-hidden">Ładowanie...</span>
                </div>
            </div>
        );
    }

    // Stan błędu ładowania
    if (error) {
        return <div className="alert alert-danger" role="alert">Błąd podczas ładowania Twoich przedmiotów: {error}</div>;
    }

    // Główny widok listy
    return (
        <div>
            {/* Wyświetlanie błędów akcji (np. usuwania) */}
            {actionError && <div className="alert alert-warning" role="alert">Wystąpił błąd akcji: {actionError}</div>}

            {/* Sprawdzenie, czy użytkownik ma przedmioty */}
            {items.length === 0 ? (
                <div className="text-center py-5">
                    <p className="lead text-muted">Nie dodałeś jeszcze żadnych przedmiotów.</p>
                    <a href="/Items/Create" className="btn btn-lg btn-success">
                        <i className="bi bi-plus-circle me-1"></i> Dodaj pierwszy przedmiot
                    </a>
                </div>
            ) : (
                // Wyświetlanie siatki z przedmiotami
                <div className="row">
                    {items.map(item => (
                        // Przekazujemy funkcję do usuwania jako prop
                        <MyItemCard key={item.id} item={item} onDelete={handleDeleteItem} />
                    ))}
                </div>
            )}
        </div>
    );
};

// --- Renderowanie Komponentu do Kontenera w Widoku Razor ---
const container = document.getElementById('react-my-item-list-container');
if (container) {
    const root = ReactDOM.createRoot(container);
    root.render(<StrictMode><MyItemList /></StrictMode>);
} else {
    // Ten błąd nie powinien się pojawić, jeśli ID diva w MyItems.cshtml jest poprawne
    console.error("Nie znaleziono kontenera 'react-my-item-list-container' do renderowania listy moich przedmiotów.");
}

