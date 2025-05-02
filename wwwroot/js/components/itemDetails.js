// Path: wwwroot/js/components/itemDetails.js
import React, { useState, useEffect, StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js';

// Component for displaying item photos (carousel or single image)
const ItemPhotoDisplay = ({ photos, itemName }) => {
    if (!photos || photos.length === 0) {
        return (
            <div className="mb-3 bg-light d-flex align-items-center justify-content-center rounded" style={{ minHeight: '300px', maxHeight: '500px' }}>
                <i className="bi bi-image text-muted" style={{ fontSize: '4rem' }}></i>
            </div>
        );
    }

    if (photos.length === 1) {
        return <img src={photos[0].path} className="img-fluid rounded mb-3" alt={photos[0].fileName || itemName} style={{ maxHeight: '500px', objectFit: 'contain', display: 'block', margin: '0 auto' }} />;
    }

    // Carousel for multiple photos
    const carouselId = `itemPhotosCarousel-${Date.now()}`;
    return (
        <div id={carouselId} className="carousel slide mb-3" data-bs-ride="carousel">
            <div className="carousel-indicators">
                {photos.map((_, index) => (
                    <button key={index} type="button" data-bs-target={`#${carouselId}`} data-bs-slide-to={index} className={index === 0 ? 'active' : ''} aria-current={index === 0 ? 'true' : 'false'} aria-label={`Zdjęcie ${index + 1}`}></button>
                ))}
            </div>
            <div className="carousel-inner rounded" style={{ maxHeight: '500px', backgroundColor: '#f8f9fa' }}>
                {photos.map((photo, index) => (
                    <div key={photo.id} className={`carousel-item ${index === 0 ? 'active' : ''}`}>
                        <img src={photo.path} className="d-block w-100" alt={photo.fileName || `Zdjęcie ${index + 1}`} style={{ maxHeight: '500px', objectFit: 'contain'}}/>
                    </div>
                ))}
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

// Function to get current user ID from JWT token
const getCurrentUserId = () => {
    const token = getAuthHeaders(false)['Authorization'];
    if (!token || !token.startsWith('Bearer ')) {
        return null;
    }

    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(atob(base64).split('').map(function(c) {
            return '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2);
        }).join(''));

        const decodedToken = JSON.parse(jsonPayload);

        // Check for both common claim types
        const nameIdClaim = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
        const subClaim = "sub";

        const userIdStr = decodedToken[nameIdClaim] || decodedToken[subClaim];
        if (!userIdStr) return null;

        return parseInt(userIdStr, 10);
    } catch (e) {
        console.error("Error decoding JWT token:", e);
        return null;
    }
};

// Main item details component
const ItemDetails = ({ itemId }) => {
    const [item, setItem] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [isOwner, setIsOwner] = useState(false);

    useEffect(() => {
        const fetchItemData = async () => {
            if (!itemId || itemId <= 0) {
                setError('Invalid item ID');
                setLoading(false);
                return;
            }

            try {
                // Get item details from API
                const response = await fetch(`/api/items/${itemId}`, {
                    headers: getAuthHeaders(false)
                });

                const data = await handleApiResponse(response);

                if (!data) {
                    throw new Error("Received empty data from API");
                }

                setItem(data);

                // Check if current user is the owner
                const currentUserId = getCurrentUserId();
                const ownerId = data.user?.id;

                if (currentUserId && ownerId && currentUserId === ownerId) {
                    setIsOwner(true);
                }
            } catch (err) {
                console.error("Error fetching item details:", err);
                setError(err.message);
            } finally {
                setLoading(false);
            }
        };

        fetchItemData();
    }, [itemId]);

    // Handle item deletion
    const handleDelete = async () => {
        if (!isOwner || !item) return;

        if (!window.confirm(`Czy na pewno chcesz usunąć przedmiot "${item.name}"? Tej operacji nie można cofnąć.`)) {
            return;
        }

        setLoading(true);
        setError(null);

        try {
            const response = await fetch(`/api/items/${item.id}`, {
                method: 'DELETE',
                headers: getAuthHeaders(false)
            });

            await handleApiResponse(response);

            alert(`Przedmiot "${item.name}" został usunięty.`);
            window.location.href = '/Items';
        } catch (err) {
            console.error("Error deleting item:", err);
            setError(`Failed to delete item: ${err.message}`);
            setLoading(false);
        }
    };

    // Render loading state
    if (loading) {
        return (
            <div className="d-flex justify-content-center align-items-center" style={{ minHeight: '200px' }}>
                <div className="spinner-border text-success" role="status">
                    <span className="visually-hidden">Ładowanie...</span>
                </div>
            </div>
        );
    }

    // Render error state
    if (error) {
        return <div className="alert alert-danger" role="alert">Błąd: {error}</div>;
    }

    // Render "not found" state
    if (!item) {
        return <div className="alert alert-warning" role="alert">Nie znaleziono przedmiotu o podanym ID.</div>;
    }

    // Destructure item properties
    const { name, description, condition, dateAdded, isAvailable, isForExchange, expectedValue, user, category, photos, tags } = item;
    const categoryName = category?.name || 'Brak';
    const userName = user ? `${user.firstName} ${user.lastName}` : 'Nieznany użytkownik';
    const userAvatar = user?.avatarPath || null;
    const userFirstName = user?.firstName || '?';
    const userLastName = user?.lastName || '?';
    const userEcoScore = user?.ecoScore || 0;
    const userId = user?.id || null;

    return (
        <div className="row">
            {/* Photos column */}
            <div className="col-lg-7 mb-4">
                <ItemPhotoDisplay photos={photos || []} itemName={name} />
            </div>

            {/* Info column */}
            <div className="col-lg-5">
                <div className="card shadow-sm">
                    <div className="card-header bg-light d-flex justify-content-between align-items-center flex-wrap">
                        <h3 className="mb-0 me-2">{name}</h3>
                        <span className={`badge fs-6 ${isAvailable ? 'bg-success' : 'bg-secondary'}`}>
                            {isAvailable ? 'Dostępny' : 'Niedostępny'}
                        </span>
                    </div>
                    <div className="card-body">
                        <p className="lead" style={{ whiteSpace: 'pre-wrap' }}>{description}</p>
                        <hr/>
                        <p><strong>Stan:</strong> {condition}</p>
                        <p><strong>Kategoria:</strong> {categoryName}</p>
                        <p><strong>Wartość/Cel:</strong> {expectedValue > 0 ? `${expectedValue.toFixed(2)} PLN` : (isForExchange ? 'Wymiana' : 'Za darmo')}</p>
                        <p><strong>Data dodania:</strong> {dateAdded ? new Date(dateAdded).toLocaleString('pl-PL') : 'Brak daty'}</p>

                        {/* Tags */}
                        {tags && tags.length > 0 && (
                            <div className="mb-3">
                                <strong>Tagi:</strong>{' '}
                                {tags.map(tag => (
                                    <span key={tag.id} className="badge bg-info me-1">{tag.name}</span>
                                ))}
                            </div>
                        )}

                        {/* User info */}
                        {user && (
                            <div className="mt-3 pt-3 border-top">
                                <h6>Wystawione przez:</h6>
                                <div className="d-flex align-items-center">
                                    {userAvatar ? (
                                        <img src={userAvatar} alt={userName} className="rounded-circle me-2" style={{ width: '40px', height: '40px', objectFit: 'cover' }} />
                                    ) : (
                                        <div className="rounded-circle bg-secondary text-white d-flex align-items-center justify-content-center me-2" style={{ width: '40px', height: '40px', fontSize: '1rem' }}>
                                            {userFirstName?.charAt(0)}{userLastName?.charAt(0)}
                                        </div>
                                    )}
                                    <div>
                                        <a href={`/Profile/Index/${userId}`} className="fw-bold text-decoration-none">{userName}</a>
                                        <br/>
                                        <small className="text-muted">EcoScore: {userEcoScore}</small>
                                    </div>
                                </div>
                            </div>
                        )}

                        {/* Action buttons */}
                        <div className="mt-4 pt-3 border-top d-flex flex-wrap gap-2">
                            {!isOwner && isAvailable && (
                                <button className="btn btn-primary" onClick={() => alert('TODO: Funkcjonalność wiadomości/transakcji.')}>
                                    <i className="bi bi-envelope me-1"></i> Zapytaj / Zaproponuj
                                </button>
                            )}

                            {isOwner && (
                                <>
                                    <a href={`/Items/Edit/${item.id}`} className="btn btn-warning">
                                        <i className="bi bi-pencil-square me-1"></i> Edytuj
                                    </a>
                                    <button onClick={handleDelete} className="btn btn-danger" disabled={loading}>
                                        {loading && <span className="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>}
                                        <i className="bi bi-trash me-1"></i> Usuń
                                    </button>
                                </>
                            )}
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
};

// Initialize the component when DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    const container = document.getElementById('react-item-details-container');
    if (container) {
        const itemId = parseInt(container.getAttribute('data-item-id'), 10);
        if (!isNaN(itemId) && itemId > 0) {
            try {
                const root = ReactDOM.createRoot(container);
                root.render(<StrictMode><ItemDetails itemId={itemId} /></StrictMode>);
            } catch (error) {
                console.error("Error rendering ItemDetails component:", error);
                container.innerHTML = `<div class="alert alert-danger">Error initializing component: ${error.message}</div>`;
            }
        } else {
            container.innerHTML = '<div class="alert alert-danger">Invalid item ID</div>';
        }
    }
});