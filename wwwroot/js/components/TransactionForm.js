// TransactionForm.js
import React, { useState } from 'react';
import { getAuthHeaders, handleApiResponse } from '../utils/auth.js';

const TransactionForm = ({ itemId, itemName, sellerId }) => {
    const [message, setMessage] = useState('');
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState(null);
    const [success, setSuccess] = useState(false);

    const handleSubmit = async (e) => {
        e.preventDefault();
        setLoading(true);
        setError(null);

        try {
            const response = await fetch('/api/transactions', {
                method: 'POST',
                headers: getAuthHeaders(true),
                body: JSON.stringify({
                    itemId: itemId,
                    initialMessage: message,
                })
            });

            await handleApiResponse(response);
            setSuccess(true);
            setMessage('');
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="card shadow-sm mb-4">
            <div className="card-header bg-light">
                <h5 className="mb-0 fs-5">Start Transaction for: {itemName}</h5>
            </div>
            <div className="card-body">
                {success ? (
                    <div className="alert alert-success">
                        <i className="bi bi-check-circle me-2"></i>
                        Transaction request sent! The owner will be notified.
                    </div>
                ) : (
                    <form onSubmit={handleSubmit} className="transaction-form">
                        {error && (
                            <div className="alert alert-danger">
                                <i className="bi bi-exclamation-triangle me-2"></i>
                                {error}
                            </div>
                        )}

                        <div className="mb-3">
                            <label htmlFor="message" className="form-label">Message to Seller</label>
                            <textarea
                                id="message"
                                className="form-control"
                                rows="3"
                                value={message}
                                onChange={(e) => setMessage(e.target.value)}
                                placeholder="Introduce yourself and explain why you're interested in this item..."
                                required
                            ></textarea>
                        </div>

                        <div className="d-grid gap-2 d-md-flex justify-content-md-end">
                            <button
                                type="submit"
                                className="btn btn-success"
                                disabled={loading}
                            >
                                {loading ? (
                                    <>
                                        <span className="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
                                        Sending...
                                    </>
                                ) : (
                                    <>
                                        <i className="bi bi-send me-2"></i>
                                        Send Request
                                    </>
                                )}
                            </button>
                        </div>
                    </form>
                )}
            </div>
        </div>
    );
};

export default TransactionForm;