import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom';

const UserProfile = ({ userId }) => {
    const [user, setUser] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);

    useEffect(() => {
        const fetchUserData = async () => {
            try {
                const response = await fetch(`/api/users/${userId}`);
                if (!response.ok) {
                    throw new Error('Nie udało się pobrać danych użytkownika');
                }
                const data = await response.json();
                setUser(data);
                setLoading(false);
            } catch (err) {
                setError(err.message);
                setLoading(false);
            }
        };

        fetchUserData();
    }, [userId]);

    if (loading) {
        return (
            <div className="d-flex justify-content-center">
                <div className="spinner-border text-success" role="status">
                    <span className="visually-hidden">Ładowanie...</span>
                </div>
            </div>
        );
    }

    if (error) {
        return (
            <div className="alert alert-danger" role="alert">
                {error}
            </div>
        );
    }

    if (!user) {
        return (
            <div className="alert alert-warning" role="alert">
                Nie znaleziono użytkownika
            </div>
        );
    }

    return (
        <div className="card">
            <div className="card-header bg-success text-white">
                <h5 className="mb-0">Profil użytkownika</h5>
            </div>
            <div className="card-body">
                <div className="row">
                    <div className="col-md-4 text-center">
                        {user.avatarPath ? (
                            <img
                                src={user.avatarPath}
                                alt={`${user.firstName} ${user.lastName}`}
                                className="img-fluid rounded-circle mb-3"
                                style={{ maxWidth: '150px' }}
                            />
                        ) : (
                            <div
                                className="rounded-circle bg-light d-flex align-items-center justify-content-center mx-auto mb-3"
                                style={{ width: '150px', height: '150px' }}
                            >
                <span className="display-4 text-muted">
                  {user.firstName?.charAt(0)}{user.lastName?.charAt(0)}
                </span>
                            </div>
                        )}
                        <div className="badge bg-success mb-2">
                            <i className="bi bi-leaf-fill me-1"></i>
                            EcoScore: {user.ecoScore}
                        </div>
                    </div>
                    <div className="col-md-8">
                        <h4>{user.firstName} {user.lastName}</h4>
                        <p className="text-muted">
                            <i className="bi bi-envelope me-2"></i>{user.email}
                        </p>

                        <p className="mb-1">
                            <small className="text-muted">Dołączył(a): {new Date(user.createdDate).toLocaleDateString()}</small>
                        </p>
                        <p>
                            <small className="text-muted">Ostatnia aktywność: {new Date(user.lastActivity).toLocaleDateString()}</small>
                        </p>

                        {user.address && (
                            <div className="mt-3">
                                <h6>Lokalizacja:</h6>
                                <address>
                                    {user.address.city}, {user.address.province}<br />
                                    {user.address.country}
                                </address>
                            </div>
                        )}
                    </div>
                </div>
            </div>
        </div>
    );
};

// Initialization function
const initUserProfile = () => {
    const container = document.getElementById('react-user-profile');
    if (container) {
        const userId = container.getAttribute('data-user-id');
        ReactDOM.render(<UserProfile userId={userId} />, container);
    }
};

// Initialize when DOM is loaded
document.addEventListener('DOMContentLoaded', initUserProfile);