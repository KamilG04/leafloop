import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js';

// Item Card Component
const ItemCard = ({ item }) => {
    const photoPath = item.mainPhotoPath || null;

    return (
        <div className="col mb-4">
            <div className="card h-100 shadow-sm">
                <div style={{ height: '200px', overflow: 'hidden' }}>
                    {photoPath ? (
                        <img src={photoPath} className="card-img-top" alt={item.name}
                             style={{ objectFit: 'cover', height: '100%', width: '100%' }} />
                    ) : (
                        <div className="bg-light d-flex align-items-center justify-content-center h-100">
                            <i className="bi bi-image text-secondary" style={{ fontSize: '3rem' }}></i>
                        </div>
                    )}
                </div>
                <div className="card-body d-flex flex-column">
                    <h5 className="card-title text-truncate">{item.name}</h5>
                    <p className="card-text small text-muted flex-grow-1">
                        {item.description?.length > 70 ? item.description.substring(0, 70) + "..." : item.description}
                    </p>
                    <div className="d-flex gap-1 mb-2">
                        {item.isAvailable ? (
                            <span className="badge bg-success">Dostępny</span>
                        ) : (
                            <span className="badge bg-secondary">Niedostępny</span>
                        )}

                        {item.condition === "Used" && (
                            <span className="badge bg-info">Używany</span>
                        )}
                        {item.condition === "New" && (
                            <span className="badge bg-primary">Nowy</span>
                        )}
                        {item.condition === "Damaged" && (
                            <span className="badge bg-warning">Uszkodzony</span>
                        )}
                    </div>
                    <div className="d-flex justify-content-between align-items-center">
                        <small className="text-muted">Kategoria: {item.categoryName || 'Brak'}</small>
                    </div>
                    <a href={`/Items/Details/${item.id}`} className="btn btn-outline-success mt-2">
                        Zobacz szczegóły
                    </a>
                </div>
                <div className="card-footer bg-white">
                    <small className="text-muted">
                        Dodano: {new Date(item.dateAdded).toLocaleDateString()}
                    </small>
                </div>
            </div>
        </div>
    );
};

// Main Item List Component
const ItemList = () => {
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    // Pagination state
    const [currentPage, setCurrentPage] = useState(1);
    const [itemsPerPage] = useState(8);
    const [totalPages, setTotalPages] = useState(1);
    const [totalItems, setTotalItems] = useState(0);

    // Search state
    const [searchTerm, setSearchTerm] = useState('');
    const [categoryId, setCategoryId] = useState('');
    const [condition, setCondition] = useState('');
    const [categories, setCategories] = useState([]);
    const [loadingCategories, setLoadingCategories] = useState(true);

    // Load categories for filter
    useEffect(() => {
        const fetchCategories = async () => {
            try {
                const response = await fetch('/api/categories');
                const result = await handleApiResponse(response);

                if (result && result.success && Array.isArray(result.data)) {
                    setCategories(result.data);
                } else {
                    console.warn("Unexpected categories response format:", result);
                    setCategories([]);
                }
            } catch (err) {
                console.error("Error fetching categories:", err);
                setCategories([]);
            } finally {
                setLoadingCategories(false);
            }
        };

        fetchCategories();
    }, []);

    // Fetch items with standardized API response
    const fetchItems = async () => {
        setLoading(true);
        try {
            // Build query params
            const params = new URLSearchParams({
                page: currentPage,
                pageSize: itemsPerPage
            });

            if (searchTerm) params.append('searchTerm', searchTerm);
            if (categoryId) params.append('categoryId', categoryId);
            if (condition) params.append('condition', condition);

            const response = await fetch(`/api/items?${params.toString()}`, {
                headers: getAuthHeaders(false)
            });

            const result = await handleApiResponse(response);

            // Check if result has the standardized format
            if (result && result.success) {
                setItems(result.data || []);
                setTotalPages(result.totalPages || 1);
                setTotalItems(result.totalItems || 0);
            } else {
                // If not standardized, handle the old format or error
                console.warn("Unexpected API response format:", result);
                setItems([]);
                setTotalPages(1);
                setTotalItems(0);
            }
        } catch (err) {
            console.error("Error fetching items:", err);
            setError(err.message || "Failed to load items");
            setItems([]);
        } finally {
            setLoading(false);
        }
    };

    // Initial data loading and when search params change
    useEffect(() => {
        fetchItems();
    }, [currentPage, searchTerm, categoryId, condition]);

    // Handle page change
    const handlePageChange = (page) => {
        if (page >= 1 && page <= totalPages) {
            setCurrentPage(page);
        }
    };

    // Handle search
    const handleSearch = (e) => {
        e.preventDefault();
        // Reset to first page when searching
        setCurrentPage(1);
        // fetchItems will be triggered by the useEffect
    };

    // Clear filters
    const clearFilters = () => {
        setSearchTerm('');
        setCategoryId('');
        setCondition('');
        setCurrentPage(1);
    };

    return (
        <div className="container mt-4">
            <div className="d-flex justify-content-between align-items-center mb-3">
                <h1>Przeglądaj Przedmioty</h1>
                <a href="/Items/Create" className="btn btn-success">
                    <i className="bi bi-plus-circle me-1"></i> Dodaj nowy przedmiot
                </a>
            </div>

            {/* Search and filters */}
            <div className="card mb-4">
                <div className="card-body">
                    <form onSubmit={handleSearch}>
                        <div className="row g-3">
                            <div className="col-md-4">
                                <div className="input-group">
                                    <input
                                        type="text"
                                        className="form-control"
                                        placeholder="Szukaj przedmiotów..."
                                        value={searchTerm}
                                        onChange={(e) => setSearchTerm(e.target.value)}
                                    />
                                </div>
                            </div>
                            <div className="col-md-3">
                                <select
                                    className="form-select"
                                    value={categoryId}
                                    onChange={(e) => setCategoryId(e.target.value)}
                                    disabled={loadingCategories}
                                >
                                    <option value="">Wszystkie kategorie</option>
                                    {categories.map(cat => (
                                        <option key={cat.id} value={cat.id}>{cat.name}</option>
                                    ))}
                                </select>
                            </div>
                            <div className="col-md-3">
                                <select
                                    className="form-select"
                                    value={condition}
                                    onChange={(e) => setCondition(e.target.value)}
                                >
                                    <option value="">Dowolny stan</option>
                                    <option value="New">Nowy</option>
                                    <option value="LikeNew">Jak nowy</option>
                                    <option value="Used">Używany</option>
                                    <option value="Damaged">Uszkodzony</option>
                                </select>
                            </div>
                            <div className="col-md-2 d-flex gap-2">
                                <button type="submit" className="btn btn-primary flex-grow-1">
                                    <i className="bi bi-search me-1"></i> Szukaj
                                </button>
                                <button type="button" className="btn btn-outline-secondary" onClick={clearFilters}>
                                    <i className="bi bi-x-circle"></i>
                                </button>
                            </div>
                        </div>
                    </form>
                </div>
            </div>

            {/* Loading indicator */}
            {loading && (
                <div className="d-flex justify-content-center my-5">
                    <div className="spinner-border text-success" role="status">
                        <span className="visually-hidden">Ładowanie...</span>
                    </div>
                </div>
            )}

            {/* Error message */}
            {error && !loading && (
                <div className="alert alert-danger" role="alert">
                    <i className="bi bi-exclamation-triangle me-2"></i>
                    {error}
                </div>
            )}

            {/* Results summary */}
            {!loading && !error && (
                <div className="mb-3">
                    <p className="text-muted">
                        Znaleziono {totalItems} przedmiotów
                        {searchTerm && <span> dla zapytania: <strong>"{searchTerm}"</strong></span>}
                        {categoryId && <span>, kategoria: <strong>{categories.find(c => c.id == categoryId)?.name}</strong></span>}
                        {condition && <span>, stan: <strong>{
                            condition === 'New' ? 'Nowy' :
                                condition === 'LikeNew' ? 'Jak nowy' :
                                    condition === 'Used' ? 'Używany' :
                                        condition === 'Damaged' ? 'Uszkodzony' : condition
                        }</strong></span>}
                    </p>
                </div>
            )}

            {/* Empty state */}
            {!loading && !error && items.length === 0 && (
                <div className="alert alert-info" role="alert">
                    <i className="bi bi-info-circle me-2"></i>
                    Brak przedmiotów do wyświetlenia.
                </div>
            )}

            {/* Items grid */}
            {!loading && !error && items.length > 0 && (
                <div className="row row-cols-1 row-cols-md-2 row-cols-lg-4 g-4 mb-4">
                    {items.map(item => (
                        <ItemCard key={item.id} item={item} />
                    ))}
                </div>
            )}

            {/* Pagination */}
            {totalPages > 1 && (
                <nav aria-label="Page navigation">
                    <ul className="pagination justify-content-center">
                        <li className={`page-item ${currentPage === 1 ? 'disabled' : ''}`}>
                            <button
                                className="page-link"
                                onClick={() => handlePageChange(currentPage - 1)}
                                disabled={currentPage === 1}
                            >
                                <span aria-hidden="true">&laquo;</span>
                            </button>
                        </li>

                        {Array.from({ length: totalPages }, (_, i) => i + 1)
                            .filter(page =>
                                page === 1 ||
                                page === totalPages ||
                                (page >= currentPage - 1 && page <= currentPage + 1))
                            .reduce((acc, page, idx, array) => {
                                if (idx > 0 && array[idx - 1] !== page - 1) {
                                    acc.push(
                                        <li key={`ellipsis-${page}`} className="page-item disabled">
                                            <span className="page-link">...</span>
                                        </li>
                                    );
                                }
                                acc.push(
                                    <li key={page} className={`page-item ${currentPage === page ? 'active' : ''}`}>
                                        <button
                                            className="page-link"
                                            onClick={() => handlePageChange(page)}
                                        >
                                            {page}
                                        </button>
                                    </li>
                                );
                                return acc;
                            }, [])
                        }

                        <li className={`page-item ${currentPage === totalPages ? 'disabled' : ''}`}>
                            <button
                                className="page-link"
                                onClick={() => handlePageChange(currentPage + 1)}
                                disabled={currentPage === totalPages}
                            >
                                <span aria-hidden="true">&raquo;</span>
                            </button>
                        </li>
                    </ul>
                </nav>
            )}
        </div>
    );
};

// Render the component
const container = document.getElementById('react-item-list-container');
if (container) {
    try {
        const root = ReactDOM.createRoot(container);
        root.render(
            <StrictMode>
                <ItemList />
            </StrictMode>
        );
    } catch (error) {
        console.error("Error rendering React component:", error);
        container.innerHTML = `
            <div class="alert alert-danger">
                <p><strong>Error rendering the component:</strong> ${error.message}</p>
                <p>Please check the console for more details.</p>
            </div>
        `;
    }
} else {
    console.error("Container 'react-item-list-container' not found");
}