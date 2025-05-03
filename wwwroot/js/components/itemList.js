import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js';

const ItemCard = ({ item }) => (
    <div className="col mb-4">
        <div className="card h-100 shadow-sm">
            <div style={{ height: '200px', overflow: 'hidden' }}>
                {item.mainPhotoPath ? (
                    <img
                        src={item.mainPhotoPath}
                        className="card-img-top"
                        alt={item.name}
                        style={{ objectFit: 'cover', height: '100%', width: '100%' }}
                    />
                ) : (
                    <div className="bg-light d-flex align-items-center justify-content-center h-100">
                        <i className="bi bi-image text-secondary" style={{ fontSize: '3rem' }}></i>
                    </div>
                )}
            </div>
            <div className="card-body d-flex flex-column">
                <h5 className="card-title text-truncate">{item.name}</h5>
                <p className="card-text small text-muted flex-grow-1">
                    {item.description?.substring(0, 70)}...
                </p>
                <div className="d-flex gap-1 mb-2">
                    <span className={`badge bg-${item.isAvailable ? 'success' : 'secondary'}`}>
                        {item.isAvailable ? 'Dostępny' : 'Niedostępny'}
                    </span>
                    <span className="badge bg-info">{item.condition}</span>
                </div>
                <a href={`/Items/Details/${item.id}`} className="btn btn-outline-success mt-auto">
                    Zobacz szczegóły
                </a>
            </div>
        </div>
    </div>
);

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

    // Load categories
    useEffect(() => {
        ApiService.get('/api/categories')
            .then(data => setCategories(data || []))
            .catch(err => console.error('Failed to load categories:', err));
    }, []);

    // Load items
    useEffect(() => {
        const loadItems = async () => {
            setLoading(true);
            try {
                const params = new URLSearchParams({
                    page: page.toString(),
                    pageSize: '8',
                    ...(searchTerm && { searchTerm }),
                    ...(categoryId && { categoryId })
                });

                const data = await ApiService.get(`/api/items?${params}`);
                setItems(Array.isArray(data) ? data : []);
                setError(null);
            } catch (err) {
                setError(err.message);
                setItems([]);
            } finally {
                setLoading(false);
            }
        };

        loadItems();
    }, [page, searchTerm, categoryId]);

    const handleSearch = (e) => {
        e.preventDefault();
        setPage(1);
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

            {error && (
                <div className="alert alert-danger">
                    {error}
                </div>
            )}

            {!error && items.length === 0 && (
                <div className="alert alert-info">
                    Brak przedmiotów do wyświetlenia.
                </div>
            )}

            <div className="row row-cols-1 row-cols-md-2 row-cols-lg-4">
                {items.map(item => (
                    <ItemCard key={item.id} item={item} />
                ))}
            </div>
        </div>
    );
};

// Initialize component
const container = document.getElementById('react-item-list-container');
if (container) {
    const root = ReactDOM.createRoot(container);
    root.render(<ItemList />);
}