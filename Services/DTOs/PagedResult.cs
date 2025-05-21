namespace LeafLoop.Services.DTOs
{
    /// <summary>
    /// Klasa generyczna reprezentująca stronicowany wynik
    /// </summary>
    /// <typeparam name="T">Typ elementów w wynikach</typeparam>
    public class PagedResult<T>
    {
        /// <summary>
        /// Lista elementów na aktualnej stronie
        /// </summary>
        public IEnumerable<T> Items { get; }
        
        /// <summary>
        /// Całkowita liczba dostępnych elementów
        /// </summary>
        public int TotalCount { get; }
        
        /// <summary>
        /// Aktualny numer strony (od 1)
        /// </summary>
        public int PageNumber { get; }
        
        /// <summary>
        /// Rozmiar strony
        /// </summary>
        public int PageSize { get; }
        
        /// <summary>
        /// Całkowita liczba stron
        /// </summary>
        public int TotalPages => (TotalCount + PageSize - 1) / PageSize;
        
        /// <summary>
        /// Czy istnieje poprzednia strona
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;
        
        /// <summary>
        /// Czy istnieje następna strona
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Tworzy nowy stronicowany wynik
        /// </summary>
        /// <param name="items">Elementy na aktualnej stronie</param>
        /// <param name="totalCount">Całkowita liczba dostępnych elementów</param>
        /// <param name="pageNumber">Aktualny numer strony (od 1)</param>
        /// <param name="pageSize">Rozmiar strony</param>
        public PagedResult(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        {
            Items = items;
            TotalCount = totalCount;
            PageNumber = pageNumber;
            PageSize = pageSize;
        }
    }
}