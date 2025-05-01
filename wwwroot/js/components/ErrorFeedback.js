// ErrorFeedback.js
import React from 'react';

/**
 * Component for displaying errors consistently across the application
 * @param {Object} props
 * @param {string|null} props.error - Error message to display (null to hide)
 * @param {string} props.type - Error type: 'alert', 'inline', or 'toast'
 * @param {Function} props.onDismiss - Optional callback when error is dismissed
 */
const ErrorFeedback = ({ error, type = 'alert', onDismiss = null }) => {
    if (!error) return null;

    switch (type) {
        case 'inline':
            return (
                <div className="text-danger mt-1 small">
                    <i className="bi bi-exclamation-circle me-1"></i>
                    {error}
                </div>
            );

        case 'toast':
            return (
                <div className="position-fixed top-0 end-0 p-3" style={{ zIndex: 1050 }}>
                    <div className="toast show" role="alert" aria-live="assertive" aria-atomic="true">
                        <div className="toast-header bg-danger text-white">
                            <i className="bi bi-exclamation-triangle me-2"></i>
                            <strong className="me-auto">Error</strong>
                            <button type="button" className="btn-close btn-close-white" onClick={onDismiss}></button>
                        </div>
                        <div className="toast-body">
                            {error}
                        </div>
                    </div>
                </div>
            );

        case 'alert':
        default:
            return (
                <div className="alert alert-danger d-flex align-items-center" role="alert">
                    <i className="bi bi-exclamation-triangle-fill me-2 flex-shrink-0"></i>
                    <div className="flex-grow-1">{error}</div>
                    {onDismiss && (
                        <button type="button" className="btn-close" aria-label="Close" onClick={onDismiss}></button>
                    )}
                </div>
            );
    }
};

export default ErrorFeedback;