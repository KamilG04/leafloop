import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js';

const ItemCard = ({ item }) => {
    // Use ApiService.getImageUrl for consistent image handling
    const photoPath = ApiService.getImageUrl(item.mainPhotoPath);

    const handleImageError = (e) => {
        const placeholder = ApiService.getImageUrl(null);
        if (e.target.src !== placeholder) {
            e.target.src = placeholder;
        }
    };

    return (
        <div className="col mb-4">
            <div className="card h-100 shadow-sm">
                <div style={{ height: '200px', overflow: 'hidden' }}>
                    <img
                        src={photoPath}
                        className="card-img-top"
                        alt={item.name || 'Przedmiot'}
                        style={{ objectFit: 'cover', height: '100%', width: '100%' }}
                        onError={handleImageError}
                        loading="lazy"
                    />
                </div>
                <div className="card-body d-flex flex-column">
                    <h5 className="card-title text-truncate" title={item.name}>
                        {item.name || 'Bez nazwy'}
                    </h5>
                    <p className="card-text small text-muted flex-grow-1">
                        {item.description ?
                            (item.description.length > 70 ?
                                    `${item.description.substring(0, 70)}...` :
                                    item.description
                            ) :
                            'Brak opisu'
                        }
                    </p>
                    <div className="d-flex gap-1 mb-2">
                        <span className={`badge bg-${item.isAvailable ? 'success' : 'secondary'}`}>
                            {item.isAvailable ? 'Dostępny' : 'Niedostępny'}
                        </span>
                        <span className="badge bg-info">{item.condition || 'Nieokreślony'}</span>
                    </div>
                    <div className="small text-muted mb-2">
                        Kategoria: {item.categoryName || 'Brak'}
                    </div>
                    <a href={`/Items/Details/${item.id}`} className="btn btn-outline-success mt-auto">
                        Zobacz szczegóły
                    </a>
                </div>
            </div>
        </div>
    );
};

const ItemList = () => {
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // Search and filter states
    const [searchTerm, setSearchTerm] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [categories, setCategories] = useState([]);
    const [page, setPage] = useState(1);
    const [totalPages, setTotalPages] = useState(1);
    const [totalItems, setTotalItems] = useState(0);

    // Load categories
    useEffect(() => {
        const loadCategories = async () => {
            try {
                const response = await ApiService.get('/api/categories');
                console.log('Categories response:', response);

                // Handle different response structures
                let categoriesData = [];
                if (response && Array.isArray(response)) {
                    categoriesData = response;
                } else if (response && response.data && Array.isArray(response.data)) {
                    categoriesData = response.data;
                }

                setCategories(categoriesData);
            } catch (err) {
                console.error('Failed to load categories:', err);
                setCategories([]);
            }
        };

        loadCategories();
    }, []);

    // Load items
    useEffect(() => {
        const loadItems = async () => {
            setLoading(true);
            setError(null);

            try {
                const params = new URLSearchParams({
                    page: page.toString(),
                    pageSize: '8',
                    ...(searchTerm && { searchTerm }),
                    ...(categoryId && { categoryId })
                });

                console.log('Loading items with params:', params.toString());

                const response = await ApiService.get(`/api/items?${params}`);
                console.log('Items API response:', response);

                // Handle different response structures from your API
                if (response) {
                    // Sprawdź czy to response z paginacją
                    if (response.data && Array.isArray(response.data)) {
                        // Paginated response with wrapper
                        setItems(response.data);
                        setTotalPages(response.totalPages || 1);
                        setTotalItems(response.totalItems || 0);
                    } else if (Array.isArray(response)) {
                        // Direct array response
                        setItems(response);
                        setTotalPages(1);
                        setTotalItems(response.length);
                    } else {
                        // Unknown structure
                        console.warn('Unexpected API response structure:', response);
                        setItems([]);
                        setTotalPages(1);
                        setTotalItems(0);
                    }
                } else {
                    // Empty response
                    setItems([]);
                    setTotalPages(1);
                    setTotalItems(0);
                }

            } catch (err) {
                console.error('Error loading items:', err);
                setError(err.message || 'Nie udało się załadować przedmiotów.');
                setItems([]);
                setTotalPages(1);
                setTotalItems(0);
            } finally {
                setLoading(false);
            }
        };

        loadItems();
    }, [page, searchTerm, categoryId]);

    const handleSearch = (e) => {
        e.preventDefault();
        setPage(1); // Reset to first page when searching
    };

    const goToPage = (newPage) => {
        if (newPage >= 1 && newPage <= totalPages && newPage !== page) {
            setPage(newPage);
        }
    };

    if (loading) {
        return (
            <div className="d-flex justify-content-center my-5">
                <div className="spinner-border text-success" role="status">
                    <span className="visually-hidden">Ładowanie...</span>
                </div>
            </div>
        );
    }

    return (
        <div className="container mt-4">
            <div className="d-flex justify-content-between align-items-center mb-3">
                <h1>Przeglądaj Przedmioty</h1>
                <a href="/Items/Create" className="btn btn-success">
                    <i className="bi bi-plus-circle me-1"></i> Dodaj nowy przedmiot
                </a>
            </div>

            {/* Search form */}
            <div className="card mb-4">
                <div className="card-body">
                    <form onSubmit={handleSearch}>
                        <div className="row g-3">
                            <div className="col-md-6">
                                <input
                                    type="text"
                                    className="form-control"
                                    placeholder="Szukaj przedmiotów..."
                                    value={searchTerm}
                                    onChange={(e) => setSearchTerm(e.target.value)}
                                />
                            </div>
                            <div className="col-md-4">
                                <select
                                    className="form-select"
                                    value={categoryId}
                                    onChange={(e) => setCategoryId(e.target.value)}
                                >
                                    <option value="">Wszystkie kategorie</option>
                                    {categories.map(cat => (
                                        <option key={cat.id} value={cat.id}>{cat.name}</option>
                                    ))}
                                </select>
                            </div>
                            <div className="col-md-2">
                                <button type="submit" className="btn btn-primary w-100">
                                    <i className="bi bi-search"></i> Szukaj
                                </button>
                            </div>
                        </div>
                    </form>
                </div>
            </div>

            {/* Error state */}
            {error && (
                <div className="alert alert-danger">
                    <span>{error}</span>
                    <button
                        className="btn btn-outline-danger btn-sm mt-2 ms-2"
                        onClick={() => window.location.reload()}
                    >
                        <i className="bi bi-arrow-clockwise me-1"></i> Spróbuj ponownie
                    </button>
                </div>
            )}

            {/* No items state */}
            {!error && items.length === 0 && !loading && (
                <div className="alert alert-info text-center">
                    <p className="mb-2">Brak przedmiotów do wyświetlenia.</p>
                    {(searchTerm || categoryId) && (
                        <button
                            className="btn btn-outline-primary btn-sm"
                            onClick={() => {
                                setSearchTerm('');
                                setCategoryId('');
                                setPage(1);
                            }}
                        >
                            Wyczyść filtry
                        </button>
                    )}
                </div>
            )}

            {/* Items grid */}
            {items.length > 0 && (
                <>
                    <div className="row row-cols-1 row-cols-md-2 row-cols-lg-4">
                        {items.map(item => (
                            <ItemCard key={item.id} item={item} />
                        ))}
                    </div>

                    {/* Pagination */}
                    {totalPages > 1 && (
                        <nav aria-label="Nawigacja strona" className="mt-4">
                            <div className="d-flex justify-content-between align-items-center mb-3">
                                <small className="text-muted">
                                    Strona {page} z {totalPages} ({totalItems} przedmiotów)
                                </small>
                            </div>
                            <ul className="pagination justify-content-center">
                                <li className={`page-item ${page === 1 ? 'disabled' : ''}`}>
                                    <button
                                        className="page-link"
                                        onClick={() => goToPage(page - 1)}
                                        disabled={page === 1}
                                    >
                                        Poprzednia
                                    </button>
                                </li>

                                {/* Page numbers */}
                                {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                                    let pageNum;
                                    if (totalPages <= 5) {
                                        pageNum = i + 1;
                                    } else if (page <= 3) {
                                        pageNum = i + 1;
                                    } else if (page >= totalPages - 2) {
                                        pageNum = totalPages - 4 + i;
                                    } else {
                                        pageNum = page - 2 + i;
                                    }

                                    return (
                                        <li key={pageNum} className={`page-item ${page === pageNum ? 'active' : ''}`}>
                                            <button
                                                className="page-link"
                                                onClick={() => goToPage(pageNum)}
                                            >
                                                {pageNum}
                                            </button>
                                        </li>
                                    );
                                })}

                                <li className={`page-item ${page === totalPages ? 'disabled' : ''}`}>
                                    <button
                                        className="page-link"
                                        onClick={() => goToPage(page + 1)}
                                        disabled={page === totalPages}
                                    >
                                        Następna
                                    </button>
                                </li>
                            </ul>
                        </nav>
                    )}
                </>
            )}
        </div>
    );
};

// Initialize component
const container = document.getElementById('react-item-list-container');
if (container) {
    const root = ReactDOM.createRoot(container);
    root.render(<ItemList />);
} else {
    console.error("Nie znaleziono kontenera '#react-item-list-container'. Sprawdź czy widok zawiera odpowiedni element div.");
}