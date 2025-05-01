// Pełna ścieżka: wwwroot/js/components/itemList.js (lub .jsx) - POPRAWIONY

// ----- POCZĄTEK PLIKU -----
import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client'; // <<< --- DODANA/POPRAWIONA TA LINIA --- >>>
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js'; // Zaimportuj funkcje pomocnicze

// Komponent karty przedmiotu (bez zmian w stosunku do poprzedniej wersji)
const ItemCard = ({ item }) => {
    const photoPath = item.mainPhotoPath || null;
    const placeholder = (
        <div className="card-img-top bg-light d-flex align-items-center justify-content-center" style={{ height: '200px' }}>
            <i className="bi bi-box-seam text-muted" style={{ fontSize: '3rem' }}></i>
        </div>
    );

    return (
        <div className="col-sm-6 col-md-4 col-lg-3 mb-4">
            <div className="card h-100 shadow-sm">
                {photoPath ? (
                    <img src={photoPath} className="card-img-top" alt={item.name} style={{ height: '200px', objectFit: 'cover' }} />
                ) : (
                    placeholder
                )}
                <div className="card-body d-flex flex-column">
                    <h5 className="card-title">{item.name}</h5>
                    <p className="card-text text-muted small flex-grow-1">
                        {item.description?.substring(0, 70)}{item.description?.length > 70 ? '...' : ''}
                    </p>
                    <div className="mb-2">
                        {/* Zakładamy, że ItemDto ma pole isAvailable typu boolean */}
                        <span className={`badge ${item.isAvailable ? 'bg-success' : 'bg-secondary'}`}>
                            {item.isAvailable ? 'Dostępny' : 'Niedostępny'}
                         </span>
                        {/* Zakładamy, że ItemDto ma pole condition typu string */}
                        <span className="badge bg-info ms-1">{item.condition}</span>
                    </div>
                    <p className="card-text small mb-2">
                        {/* Zakładamy, że ItemDto ma pole categoryName */}
                        Kategoria: {item.categoryName || 'Brak'} <br/>
                        {/* Zakładamy, że ItemDto ma pola expectedValue i isForExchange */}
                        Wartość: {item.expectedValue > 0 ? `${item.expectedValue.toFixed(2)} PLN` : (item.isForExchange ? 'Wymiana' : 'Za darmo')}
                    </p>
                    {/* Link do szczegółów przedmiotu */}
                    <a href={`/Items/Details/${item.id}`} className="btn btn-outline-success mt-auto stretched-link">
                        Zobacz szczegóły
                    </a>
                </div>
                <div className="card-footer text-muted small">
                    {/* Zakładamy, że ItemDto ma pole dateAdded i userName */}
                    Dodano: {new Date(item.dateAdded).toLocaleDateString()} przez {item.userName || 'Anonim'}
                </div>
            </div>
        </div>
    );
};

// Główny komponent listy
const ItemList = () => {
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [searchTerm, setSearchTerm] = useState('');
    const [selectedCategoryId, setSelectedCategoryId] = useState('');
    const [categories, setCategories] = useState([]);
    const [loadingCategories, setLoadingCategories] = useState(true); // Stan ładowania kategorii

    // Funkcja pobierająca przedmioty z API
    const fetchItems = async (currentSearchTerm = searchTerm, currentCategoryId = selectedCategoryId) => {
        setLoading(true);
        setError(null);
        try {
            const searchParams = new URLSearchParams();
            if (currentSearchTerm) searchParams.append('SearchTerm', currentSearchTerm);
            if (currentCategoryId) searchParams.append('CategoryId', currentCategoryId);
            searchParams.append('PageSize', '24'); // Przykładowy limit przedmiotów

            const url = `/api/items?${searchParams.toString()}`;
            console.log("Fetching items from:", url); // Logowanie URL zapytania

            const response = await fetch(url, {
                method: 'GET',
                headers: getAuthHeaders(false) // GET może być publiczny lub wymagać tokena
            });
            const data = await handleApiResponse(response); // Użyj funkcji obsługi odpowiedzi
            setItems(data || []); // Ustaw przedmioty lub pustą tablicę
        } catch (err) {
            console.error("Błąd pobierania listy przedmiotów:", err);
            setError(err.message);
            setItems([]); // Wyczyść w razie błędu
        } finally {
            setLoading(false);
        }
    };

    // Pobieranie kategorii dla filtra (tylko raz przy montowaniu)
    useEffect(() => {
        const fetchCategories = async () => {
            console.log("Fetching categories for filter...");
            setLoadingCategories(true); // Ustaw ładowanie kategorii
            try {
                const response = await fetch('/api/categories', { headers: getAuthHeaders(false) });
                const data = await handleApiResponse(response);
                setCategories(data || []);
            } catch (err) {
                console.warn("Nie udało się pobrać kategorii dla filtra:", err.message);
                // Można ustawić stan błędu dla kategorii lub zostawić pustą listę
            } finally {
                setLoadingCategories(false); // Zakończ ładowanie kategorii
            }
        };
        fetchCategories();
        // Pierwsze pobranie przedmiotów po zamontowaniu komponentu
        fetchItems();
    }, []); // Pusta tablica zależności - uruchom tylko raz

    // Obsługa wysłania formularza wyszukiwania
    const handleSearchSubmit = (e) => {
        e.preventDefault();
        // Wywołaj fetchItems z aktualnymi wartościami stanu (searchTerm, selectedCategoryId)
        fetchItems(searchTerm, selectedCategoryId);
    }

    // --- Renderowanie JSX ---
    return (
        <div>
            {/* Formularz Wyszukiwania/Filtrowania */}
            <form onSubmit={handleSearchSubmit} className="row g-3 mb-4 p-3 border rounded bg-light shadow-sm">
                <div className="col-md-5">
                    <label htmlFor="searchTerm" className="form-label visually-hidden">Szukaj</label>
                    <input type="text" className="form-control" id="searchTerm" placeholder="Szukaj w nazwie lub opisie..."
                           value={searchTerm} onChange={e => setSearchTerm(e.target.value)} />
                </div>
                <div className="col-md-5">
                    <label htmlFor="categoryFilter" className="form-label visually-hidden">Kategoria</label>
                    <select className="form-select" id="categoryFilter" value={selectedCategoryId}
                            onChange={e => setSelectedCategoryId(e.target.value)} disabled={loadingCategories}> {/* Wyłącz podczas ładowania kategorii */}
                        <option value="">{loadingCategories ? "Ładowanie kat..." : "Wszystkie kategorie"}</option>
                        {/* Zakładamy, że CategoryDto ma pola 'id', 'name', 'itemsCount' */}
                        {categories.map(cat => (
                            <option key={cat.id} value={cat.id}>{cat.name} ({cat.itemsCount})</option>
                        ))}
                    </select>
                </div>
                <div className="col-md-2 d-grid">
                    <button type="submit" className="btn btn-primary" disabled={loading}>
                        {loading ? <span className="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> : 'Szukaj'}
                    </button>
                </div>
            </form>

            {/* Wyświetlanie błędów lub stanu ładowania przedmiotów */}
            {loading && (
                <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '200px' }}>
                    <div className="spinner-border text-success" role="status"><span className="visually-hidden">Ładowanie...</span></div>
                </div>
            )}
            {/* Wyświetl błąd tylko jeśli nie trwa ładowanie */}
            {error && !loading && <div className="alert alert-danger" role="alert">Błąd podczas ładowania przedmiotów: {error}</div>}

            {/* Lista Przedmiotów (renderuj tylko jeśli nie ma błędu i nie trwa ładowanie) */}
            {!loading && !error && (
                <div className="row">
                    {items.length === 0 ? (
                        <div className="col-12"><p className="text-center text-muted mt-4">Nie znaleziono przedmiotów spełniających kryteria.</p></div>
                    ) : (
                        items.map(item => <ItemCard key={item.id} item={item} />)
                    )}
                </div>
            )}
        </div>
    );
};

// --- Renderowanie Komponentu ---
const container = document.getElementById('react-item-list-container');
if (container) {
    const root = ReactDOM.createRoot(container); // <<<--- UŻYWA ReactDOM --->>>
    root.render(<StrictMode><ItemList /></StrictMode>);
} else {
    console.error("Nie znaleziono kontenera 'react-item-list-container' do renderowania listy.");
}

