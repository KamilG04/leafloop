// wwwroot/js/components/MyTransactionsList.js
import React, { useState, useEffect } from 'react';
import ReactDOM from 'react-dom/client';
import ApiService from '../services/api.js';

const MyTransactionsList = () => {
    const [buyingTransactions, setBuyingTransactions] = useState([]);
    const [sellingTransactions, setSellingTransactions] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState(null);
    const [activeTab, setActiveTab] = useState('buying'); // 'buying' or 'selling'

    useEffect(() => {
        fetchTransactions();
    }, []);

    const fetchTransactions = async () => {
        setLoading(true);
        setError(null);

        try {
            // Pobierz transakcje jako kupujący
            const buyingData = await ApiService.get('/api/transactions?asSeller=false');
            setBuyingTransactions(Array.isArray(buyingData) ? buyingData : []);

            // Pobierz transakcje jako sprzedający
            const sellingData = await ApiService.get('/api/transactions?asSeller=true');
            setSellingTransactions(Array.isArray(sellingData) ? sellingData : []);

        } catch (err) {
            console.error('Error fetching transactions:', err);
            setError(err.message || 'Nie udało się załadować transakcji');
        } finally {
            setLoading(false);
        }
    };

    const handleStatusUpdate = async (transactionId, newStatus) => {
        try {
            await ApiService.put(`/api/transactions/${transactionId}/status`, { status: newStatus });
            fetchTransactions(); // Odśwież listę
        } catch (err) {
            console.error('Error updating transaction status:', err);
            alert(`Błąd aktualizacji statusu: ${err.message}`);
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

    if (error) {
        return (
            <div className="alert alert-danger">
                {error}
                <button className="btn btn-sm btn-outline-danger ms-3" onClick={fetchTransactions}>
                    Spróbuj ponownie
                </button>
            </div>
        );
    }

    return (
        <div>
            <ul className="nav nav-tabs mb-4">
                <li className="nav-item">
                    <button
                        className={`nav-link ${activeTab === 'buying' ? 'active' : ''}`}
                        onClick={() => setActiveTab('buying')}
                    >
                        Kupuję ({buyingTransactions.length})
                    </button>
                </li>
                <li className="nav-item">
                    <button
                        className={`nav-link ${activeTab === 'selling' ? 'active' : ''}`}
                        onClick={() => setActiveTab('selling')}
                    >
                        Sprzedaję ({sellingTransactions.length})
                    </button>
                </li>
            </ul>

            {activeTab === 'buying' && (
                <TransactionList
                    transactions={buyingTransactions}
                    role="buyer"
                    onStatusUpdate={handleStatusUpdate}
                />
            )}

            {activeTab === 'selling' && (
                <TransactionList
                    transactions={sellingTransactions}
                    role="seller"
                    onStatusUpdate={handleStatusUpdate}
                />
            )}
        </div>
    );
};

const TransactionList = ({ transactions, role, onStatusUpdate }) => {
    if (transactions.length === 0) {
        return <p className="text-muted">Brak transakcji do wyświetlenia.</p>;
    }

    return (
        <div className="row">
            {transactions.map(transaction => (
                <div key={transaction.id} className="col-md-6 mb-4">
                    <div className="card">
                        <div className="card-body">
                            <h5 className="card-title">{transaction.itemName}</h5>
                            <p className="card-text">
                                <small className="text-muted">
                                    {role === 'buyer' ? `Sprzedawca: ${transaction.sellerName}` : `Kupujący: ${transaction.buyerName}`}
                                </small>
                            </p>
                            <p className="card-text">
                                Status: <span className={`badge bg-${getStatusBadgeColor(transaction.status)}`}>
                                    {transaction.status}
                                </span>
                            </p>
                            <p className="card-text">
                                <small>Data rozpoczęcia: {new Date(transaction.startDate).toLocaleDateString()}</small>
                            </p>
                            <div className="d-flex gap-2">
                                <a href={`/Transactions/Details/${transaction.id}`} className="btn btn-sm btn-primary">
                                    Szczegóły
                                </a>
                                {getActionButtons(transaction, role, onStatusUpdate)}
                            </div>
                        </div>
                    </div>
                </div>
            ))}
        </div>
    );
};

const getStatusBadgeColor = (status) => {
    switch (status) {
        case 'Pending': return 'warning';
        case 'InProgress': return 'info';
        case 'Completed': return 'success';
        case 'Cancelled': return 'danger';
        default: return 'secondary';
    }
};

const getActionButtons = (transaction, role, onStatusUpdate) => {
    const buttons = [];

    if (transaction.status === 'Pending' && role === 'seller') {
        buttons.push(
            <button
                key="accept"
                className="btn btn-sm btn-success"
                onClick={() => onStatusUpdate(transaction.id, 'InProgress')}
            >
                Akceptuj
            </button>
        );
    }

    if (transaction.status === 'Pending' || transaction.status === 'InProgress') {
        buttons.push(
            <button
                key="cancel"
                className="btn btn-sm btn-danger"
                onClick={() => onStatusUpdate(transaction.id, 'Cancelled')}
            >
                Anuluj
            </button>
        );
    }

    return buttons;
};

// Inicjalizacja
const container = document.getElementById('react-transactions-list-container');
if (container) {
    const root = ReactDOM.createRoot(container);
    root.render(<MyTransactionsList />);
}