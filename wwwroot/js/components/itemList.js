import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';

// Item Card Component
// Improved ItemCard component for better mobile experience
const ItemCard = ({ item }) => {
    const photoPath = item.mainPhotoPath || null;

    return (
        <div className="col mb-3 mb-md-4">
            <div className="card h-100 shadow-sm">
                <div style={{ height: "180px", overflow: "hidden" }}>
                    {photoPath ? (
                        <img
                            src={photoPath}
                            className="card-img-top"
                            alt={item.name}
                            style={{ objectFit: 'cover', height: '100%', width: '100%' }}
                        />
                    ) : (
                        <div className="bg-light d-flex align-items-center justify-content-center h-100">
                            <i className="bi bi-image text-secondary" style={{ fontSize: '2rem' }}></i>
                        </div>
                    )}
                </div>
                <div className="card-body d-flex flex-column p-3">
                    <h5 className="card-title fs-6 fs-md-5 text-truncate">{item.name}</h5>
                    <p className="card-text small text-muted mb-2 d-none d-sm-block">
                        {item.description?.length > 60 ? item.description.substring(0, 60) + "..." : item.description}
                    </p>
                    {/* Rest of the card content */}
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

    // Fetch items
    const fetchItems = async () => {
        setLoading(true);
        try {
            // Use the existing controller endpoint instead of an API
            const response = await fetch(`/Items/GetItems?page=${currentPage}&pageSize=${itemsPerPage}`);

            if (!response.ok) {
                throw new Error(`Server error (status: ${response.status})`);
            }

            const data = await response.json();

            // Check if data is in the expected format
            if (!data.items || !Array.isArray(data.items)) {
                console.warn("Unexpected data format:", data);
                // Fallback: If data itself is an array, use it directly
                if (Array.isArray(data)) {
                    setItems(data);
                    // Estimate total pages if not provided
                    setTotalPages(Math.ceil(data.length / itemsPerPage) || 1);
                } else {
                    setItems([]);
                    setTotalPages(1);
                }
            } else {
                // Use the data as expected
                setItems(data.items);
                setTotalPages(data.totalPages || 1);
            }
        } catch (err) {
            console.error("Error fetching items:", err);
            setError(err.message || "Failed to load items");
            setItems([]);
        } finally {
            setLoading(false);
        }
    };

    // Initial data loading
    useEffect(() => {
        fetchItems();
    }, [currentPage]);

    // Handle page change
    const handlePageChange = (page) => {
        if (page >= 1 && page <= totalPages) {
            setCurrentPage(page);
            // Page change will trigger a new fetchItems via the useEffect
        }
    };

    return (
        <div className="container mt-4">
            <div className="d-flex justify-content-between align-items-center mb-3">
                <h1>Przeglądaj Przedmioty</h1>
                <a href="/Items/Create" className="btn btn-success">
                    <i className="bi bi-plus-circle me-1"></i> Dodaj nowy przedmiot
                </a>
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

            {/* Empty state */}
            {!loading && !error && items.length === 0 && (
                <div className="alert alert-info" role="alert">
                    <i className="bi bi-info-circle me-2"></i>
                    Brak przedmiotów do wyświetlenia.
                </div>
            )}

            {/* Items grid */}
            {!loading && !error && items.length > 0 && (
                // Improved responsive layout
                <div className="row row-cols-1 row-cols-sm-2 row-cols-md-3 row-cols-lg-4 g-3 g-md-4 mb-4">
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

                        {[...Array(totalPages).keys()].map(i => (
                            <li key={i + 1} className={`page-item ${currentPage === i + 1 ? 'active' : ''}`}>
                                <button
                                    className="page-link"
                                    onClick={() => handlePageChange(i + 1)}
                                >
                                    {i + 1}
                                </button>
                            </li>
                        ))}

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