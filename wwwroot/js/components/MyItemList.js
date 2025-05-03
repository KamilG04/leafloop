import React, { useState, useEffect, useCallback } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js'; // Importuj ApiService

// Pamięć podręczna (cache) - bez zmian
const itemsCache = {
    data: null,
    timestamp: null,
    maxAge: 60000 // 1 minuta (w milisekundach)
};

// Komponent karty przedmiotu - teraz używa ApiService.getImageUrl
const MyItemCard = React.memo(({ item, onDelete }) => {
    // Użyj ApiService.getImageUrl do uzyskania poprawnej ścieżki LUB placeholdera
    const photoPath = ApiService.getImageUrl(item.mainPhotoPath);

    // Obsługa błędu ładowania obrazka - próbuje załadować placeholder
    const handleImageError = (e) => {
        const placeholder = ApiService.getImageUrl(null); // Pobierz ścieżkę placeholdera
        // Ustaw placeholder tylko jeśli bieżący src nie jest już placeholderem
        if (e.target.src !== placeholder) {
            e.target.src = placeholder;
        }
    };

    return (
        <div className="col-sm-6 col-md-4 col-lg-3 mb-4">
            <div className="card h-100 shadow-sm">
                <img
                    src={photoPath} // Użyj ścieżki z ApiService.getImageUrl
                    className="card-img-top"
                    alt={item.name || 'Przedmiot'} // Dodaj fallback dla alt
                    style={{ height: '180px', objectFit: 'cover' }} // Stała wysokość i dopasowanie
                    onError={handleImageError} // Obsługa błędów ładowania
                    loading="lazy" // Leniwe ładowanie obrazków
                />
                <div className="card-body d-flex flex-column">
                    {/* Użyj text-truncate dla długich nazw */}
                    <h5 className="card-title text-truncate" title={item.name}>{item.name || 'Bez nazwy'}</h5>
                    <div className="d-flex justify-content-between align-items-center mb-2">
                        {/* Badge dostępności */}
                        <span className={`badge bg-${item.isAvailable ? 'success' : 'secondary'}`}>
                            {item.isAvailable ? 'Dostępny' : 'Niedostępny'}
                        </span>
                        {/* Badge stanu */}
                        <span className="badge bg-info">{item.condition || 'Nieokreślony'}</span>
                    </div>
                    <p className="card-text small text-muted">
                        {/* Kategoria */}
                        Kategoria: {item.categoryName || 'Brak'}
                    </p>
                    {/* Link do podglądu - używa mt-auto, aby przykleić do dołu */}
                    <a href={`/Items/Details/${item.id}`} className="btn btn-sm btn-outline-secondary mt-auto mb-2">
                        Podgląd
                    </a>
                </div>
                {/* Stopka karty z przyciskami */}
                <div className="card-footer bg-light d-flex justify-content-between">
                    {/* Link do edycji */}
                    <a href={`/Items/Edit/${item.id}`} className="btn btn-sm btn-warning">
                        <i className="bi bi-pencil-square me-1"></i> Edytuj
                    </a>
                    {/* Przycisk usuwania */}
                    <button onClick={() => onDelete(item.id, item.name)} className="btn btn-sm btn-danger">
                        <i className="bi bi-trash me-1"></i> Usuń
                    </button>
                </div>
            </div>
        </div>
    );
});
MyItemCard.displayName = 'MyItemCard'; // Dodanie displayName dla React DevTools

// Główny komponent listy przedmiotów
const MyItemList = () => {
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // Funkcja do pobierania przedmiotów z API
    const fetchItems = useCallback(async (forceRefresh = false) => {
        const now = Date.now();

        // Sprawdź cache (bez zmian)
        if (!forceRefresh && itemsCache.data && itemsCache.timestamp &&
            (now - itemsCache.timestamp) < itemsCache.maxAge) {
            setItems(itemsCache.data);
            setLoading(false);
            setError(null); // Upewnij się, że błąd jest czyszczony przy użyciu cache
            // console.log('Using cached items'); // Opcjonalny log
            return;
        }

        setLoading(true);
        setError(null); // Resetuj błąd przed nowym zapytaniem
        // console.log('Fetching items from API...'); // Opcjonalny log

        try {
            // Użyj ApiService do pobrania danych
            const data = await ApiService.get('/api/items/my');
            // ApiService zwraca już pole 'data' z odpowiedzi API
            // Upewnij się, że wynik jest tablicą
            const itemsArray = Array.isArray(data) ? data : [];

            // console.log('Received items:', itemsArray); // Opcjonalny log

            // Zapisz do cache (bez zmian)
            itemsCache.data = itemsArray;
            itemsCache.timestamp = now;

            setItems(itemsArray); // Ustaw stan

        } catch (err) {
            console.error("Błąd podczas pobierania moich przedmiotów:", err);
            setError(err.message || 'Nie udało się załadować Twoich przedmiotów.'); // Ustaw stan błędu
            setItems([]); // Wyczyść przedmioty w razie błędu
            // Wyczyść cache w razie błędu
            itemsCache.data = null;
            itemsCache.timestamp = null;
        } finally {
            setLoading(false); // Zakończ ładowanie
            // console.log('Fetching finished.'); // Opcjonalny log
        }
    }, []); // Pusta tablica zależności - funkcja nie zmienia się

    // Efekt do pobrania danych przy pierwszym renderowaniu
    useEffect(() => {
        fetchItems();
    }, [fetchItems]); // Zależność od fetchItems (które jest memoizowane przez useCallback)

    // Funkcja do obsługi usuwania przedmiotu
    const handleDelete = useCallback(async (itemId, itemName) => {
        // Potwierdzenie (bez zmian)
        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${itemName || 'Bez nazwy'}"?`)) {
            return;
        }

        // Nie ustawiamy tutaj loading, aby uniknąć migotania całej listy
        // Można by dodać stan ładowania dla konkretnej karty, ale to komplikuje

        try {
            // Użyj ApiService do usunięcia
            await ApiService.delete(`/api/items/${itemId}`);

            // Usuń element z lokalnego stanu zamiast przeładowywać wszystko
            setItems(prevItems => prevItems.filter(item => item.id !== itemId));

            // Wyczyść cache po udanej modyfikacji
            itemsCache.data = null;
            itemsCache.timestamp = null;

            // Poinformuj użytkownika (opcjonalnie)
            // alert(`Przedmiot "${itemName}" został usunięty.`);

        } catch (err) {
            console.error(`Nie udało się usunąć przedmiotu (ID: ${itemId}):`, err);
            // Poinformuj użytkownika o błędzie
            alert(`Nie udało się usunąć przedmiotu: ${err.message || 'Nieznany błąd'}`);
            // Nie modyfikujemy stanu `items` w razie błędu
        }
    }, []); // Pusta tablica zależności, bo modyfikuje stan przez funkcję zwrotną

    // --- Renderowanie ---

    // Stan ładowania
    if (loading) {
        return (
            <div className="d-flex justify-content-center py-5">
                <div className="spinner-border text-success" role="status">
                    <span className="visually-hidden">Ładowanie...</span>
                </div>
            </div>
        );
    }

    // Stan błędu
    if (error) {
        return (
            <div className="alert alert-danger d-flex flex-column align-items-center">
                <span>{error}</span>
                {/* Przycisk ponowienia */}
                <button className="btn btn-outline-danger btn-sm mt-2" onClick={() => fetchItems(true)}> {/* forceRefresh=true */}
                    <i className="bi bi-arrow-clockwise me-1"></i> Spróbuj ponownie
                </button>
            </div>
        );
    }

    // Stan "brak przedmiotów"
    if (items.length === 0) {
        return (
            <div className="text-center py-5">
                <p className="lead text-muted">Nie dodałeś jeszcze żadnych przedmiotów.</p>
                <a href="/Items/Create" className="btn btn-lg btn-success">
                    <i className="bi bi-plus-circle me-1"></i> Dodaj pierwszy przedmiot
                </a>
            </div>
        );
    }

    // Renderowanie listy przedmiotów
    return (
        <div>
            {/* Grid dla kart */}
            <div className="row">
                {/* Mapowanie tablicy przedmiotów na komponenty MyItemCard */}
                {items.map(item => (
                    <MyItemCard key={item.id} item={item} onDelete={handleDelete} />
                ))}
            </div>
            {/* Przycisk dodawania nowego przedmiotu na dole */}
            <div className="mt-4 text-center">
                <a href="/Items/Create" className="btn btn-success">
                    <i className="bi bi-plus-circle me-1"></i> Dodaj nowy przedmiot
                </a>
            </div>
        </div>
    );
};

// Inicjalizacja komponentu React w kontenerze HTML (bez zmian)
const container = document.getElementById('react-my-item-list-container');
if (container) {
    const root = ReactDOM.createRoot(container);
    root.render(<MyItemList />); // Użyj StrictMode jeśli chcesz dodatkowych sprawdzeń w trybie deweloperskim: <React.StrictMode><MyItemList /></React.StrictMode>
} else {
    console.warn("Nie znaleziono kontenera '#react-my-item-list-container'.");
}