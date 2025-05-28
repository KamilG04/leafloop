// EventsPage.js
import React, { StrictMode } from 'react';
import ReactDOM from 'react-dom/client';
import EventsList from './EventsList.js';

console.log('EventsPage: Starting initialization...');

const eventsContainer = document.getElementById('react-events-page');
if (eventsContainer) {
    console.log('EventsPage: Container found, creating React root...');

    // Usunięto próbę pobrania currentUserId tutaj,
    // zakładając, że EventsList lub globalny kontekst auth sobie z tym radzi.
    // Jeśli EventsList potrzebuje ID z data-atrybutu, przywróć poprzednią logikę,
    // ale upewnij się, że serwer poprawnie ustawia ten atrybut.
    // Na podstawie logów "EventsList: Current user ID: 7", wydaje się, że EventsList ma swoje źródło.

    const root = ReactDOM.createRoot(eventsContainer);
    root.render(
        <StrictMode>
            {/* Jeśli EventsList sam pobiera ID, nie trzeba go tu przekazywać. */}
            {/* Jeśli jednak EventsList oczekuje 'currentUserId' jako prop,
                a poprzednio było 'null', to trzeba znaleźć, skąd EventsList bierze '7'
                i upewnić się, że ten mechanizm jest stabilny. */}
            <EventsList />
            {/* LUB, jeśli EventsList oczekuje go jako prop, ale my go tu nie mamy:
            <EventsList currentUserId={eventsContainer.dataset.currentUserId || null} />
            Wtedy musisz naprawić ustawianie data-current-user-id po stronie serwera.
            Jednak logi wskazują, że EventsList i tak znajduje '7'.
            */}
        </StrictMode>
    );
    console.log('Events page initialized');
} else {
    console.warn('Events container not found - component will not be rendered');
}