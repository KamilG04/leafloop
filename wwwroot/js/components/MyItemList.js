import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js';

const MyItemCard = ({ item, onDelete }) => (
    <div className="col-sm-6 col-md-4 col-lg-3 mb-4">
        <div className="card h-100 shadow-sm">
            <img
                src={item.mainPhotoPath || '/img/placeholder-item.png'}
                className="card-img-top"
                alt={item.name}
                style={{ height: '180px', objectFit: 'cover' }}
                onError={(e) => { e.target.src = '/img/placeholder-item.png'; }}
            />
            <div className="card-body d-flex flex-column">
                <h5 className="card-title text-truncate">{item.name}</h5>
                <div className="d-flex justify-content-between align-items-center mb-2">
                    <span className={`badge bg-${item.isAvailable ? 'success' : 'secondary'}`}>
                        {item.isAvailable ? 'Dostępny' : 'Niedostępny'}
                    </span>
                    <span className="badge bg-info">{item.condition}</span>
                </div>
                <p className="card-text small text-muted">
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

const MyItemList = () => {
    const [items, setItems] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    const fetchItems = async () => {
        setLoading(true);
        try {
            const data = await ApiService.get('/api/items/my');
            setItems(Array.isArray(data) ? data : []);
            setError(null);
        } catch (err) {
            setError(err.message);
            setItems([]);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        fetchItems();
    }, []);

    const handleDelete = async (itemId, itemName) => {
        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${itemName}"?`)) {
            return;
        }

        try {
            await ApiService.delete(`/api/items/${itemId}`);
            alert(`Przedmiot "${itemName}" został usunięty.`);
            fetchItems(); // Reload the list
        } catch (err) {
            alert(`Nie udało się usunąć przedmiotu: ${err.message}`);
        }
    };

    if (loading) {
        return (
            <div className="d-flex justify-content-center py-5">
                <div className="spinner-border text-success" role="status">
                    <span className="visually-hidden">Ładowanie...</span>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="alert alert-danger">
                {error}
                <div className="mt-3">
                    <button className="btn btn-outline-danger btn-sm" onClick={fetchItems}>
                        <i className="bi bi-arrow-clockwise me-1"></i> Spróbuj ponownie
                    </button>
                </div>
            </div>
        );
    }

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

    return (
        <div>
            <div className="row">
                {items.map(item => (
                    <MyItemCard key={item.id} item={item} onDelete={handleDelete} />
                ))}
            </div>
            <div className="mt-4 text-center">
                <a href="/Items/Create" className="btn btn-success">
                    <i className="bi bi-plus-circle me-1"></i> Dodaj nowy przedmiot
                </a>
            </div>
        </div>
    );
};

// Initialize component
const container = document.getElementById('react-my-item-list-container');
if (container) {
    const root = ReactDOM.createRoot(container);
    root.render(<MyItemList />);
}