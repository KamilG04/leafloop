using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Security.Claims; // Potrzebne dla User.FindFirstValue
using LeafLoop.Models;       // Modele encji
using LeafLoop.Services.DTOs; // Twoje DTO
using LeafLoop.Services.Interfaces; // Interfejsy serwisów
using Microsoft.AspNetCore.Authorization; // Dla [Authorize]
using Microsoft.AspNetCore.Http;         // Dla IFormFile i StatusCodes
using Microsoft.AspNetCore.Identity;     // Dla UserManager<User>
using Microsoft.AspNetCore.Mvc;          // Dla [Route], [ApiController], ActionResult itp.
using Microsoft.Extensions.Logging;      // Dla ILogger

namespace LeafLoop.Api
{
    [Route("api/[controller]")] // Trasa bazowa: /api/items
    [ApiController]
    public class ItemsController : ControllerBase // Dziedziczenie z ControllerBase dla API
    {
        // --- Zależności ---
        private readonly IItemService _itemService;
        private readonly IPhotoService _photoService; // Upewnij się, że ten serwis istnieje i jest wstrzykiwany
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ItemsController> _logger;

        // --- Konstruktor (Wstrzykiwanie Zależności) ---
        public ItemsController(
            IItemService itemService,
            IPhotoService photoService, // Upewnij się, że ten serwis istnieje i jest zarejestrowany
            UserManager<User> userManager,
            ILogger<ItemsController> logger)
        {
            _itemService = itemService ?? throw new ArgumentNullException(nameof(itemService));
            _photoService = photoService ?? throw new ArgumentNullException(nameof(photoService)); // Dodaj sprawdzenie null
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // --- Endpointy API ---

        // GET: api/items (Wyszukiwanie/Listowanie wszystkich dostępnych przedmiotów)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetItems([FromQuery] ItemSearchDto searchDto)
        {
            try
            {
                // Użyj serwisu do wyszukiwania/filtrowania
                // Zakładamy, że ItemService.SearchItemsAsync obsługuje wszystkie kryteria z ItemSearchDto
                var items = await _itemService.SearchItemsAsync(searchDto);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas wyszukiwania przedmiotów. Kryteria: {@SearchCriteria}", searchDto);
                // Nie zwracaj szczegółów wyjątku do klienta w produkcji
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas pobierania danych.");
            }
        }

        // GET: api/items/{id} (Pobieranie szczegółów konkretnego przedmiotu)
        // --- POPRAWKA ROUTINGU ---
        [HttpGet("{id:int}")] // Dodano ograniczenie :int, aby uniknąć kolizji z "my"
        public async Task<ActionResult<ItemWithDetailsDto>> GetItem(int id)
        {
            if (id <= 0) {
                 return BadRequest("Nieprawidłowe ID przedmiotu.");
             }
            try
            {
                var item = await _itemService.GetItemWithDetailsAsync(id); // Używamy metody zwracającej DTO ze szczegółami
                if (item == null)
                {
                    return NotFound($"Przedmiot o ID {id} nie został znaleziony.");
                }
                return Ok(item);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania szczegółów przedmiotu. ItemId: {ItemId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas pobierania danych.");
            }
        }

        // GET: api/items/my (Pobieranie przedmiotów ZALOGOWANEGO użytkownika)
        // --- NOWA/POTWIERDZONA AKCJA ---
        [HttpGet("my")]
        [Authorize] // Tylko zalogowani użytkownicy
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetCurrentUserItems()
        {
            try
            {
                // Bezpieczne pobranie ID zalogowanego użytkownika z Claimów
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
                {
                    _logger.LogWarning("GetCurrentUserItems wywołane bez poprawnego identyfikatora użytkownika.");
                    // Zwróć 401 Unauthorized zamiast polegać na [Authorize], który mógłby przepuścić np. zły token
                    return Unauthorized("Nie można zidentyfikować użytkownika.");
                }

                // Użyj serwisu do pobrania przedmiotów dla tego użytkownika
                var items = await _itemService.GetItemsByUserAsync(userId);
                return Ok(items); // Zwraca listę ItemDto
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania przedmiotów dla bieżącego użytkownika (ID z claim: {UserIdClaim})", User.FindFirstValue(ClaimTypes.NameIdentifier));
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas pobierania przedmiotów użytkownika.");
            }
        }

        // GET: api/items/user/{userId} (Pobieranie przedmiotów DOWOLNEGO użytkownika po ID)
        [HttpGet("user/{userId:int}")] // Dodano ograniczenie :int
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetUserItems(int userId)
        {
             if (userId <= 0) {
                 return BadRequest("Nieprawidłowe ID użytkownika.");
             }
            try
            {
                // TODO: Rozważ dodanie sprawdzenia, czy użytkownik o podanym ID istnieje
                var items = await _itemService.GetItemsByUserAsync(userId);
                return Ok(items);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania przedmiotów dla użytkownika. UserId: {UserId}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas pobierania danych.");
            }
        }

        // GET: api/items/category/{categoryId} (Pobieranie przedmiotów z kategorii)
        [HttpGet("category/{categoryId:int}")] // Dodano ograniczenie :int
        public async Task<ActionResult<IEnumerable<ItemDto>>> GetCategoryItems(int categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
             if (categoryId <= 0) {
                 return BadRequest("Nieprawidłowe ID kategorii.");
             }
             if (page <= 0) page = 1;
             if (pageSize <= 0) pageSize = 10;

            try
            {
                // Zakładamy, że ItemService obsługuje paginację lub zwraca wszystko, a my filtrujemy (?)
                // Lepsze byłoby przekazanie paginacji do serwisu/repozytorium
                var items = await _itemService.GetItemsByCategoryAsync(categoryId, page, pageSize);
                return Ok(items);
            }
            catch (KeyNotFoundException ex) // Łap konkretny wyjątek, jeśli serwis go rzuca
            {
                 _logger.LogWarning("Próba pobrania przedmiotów dla nieistniejącej kategorii: {CategoryId}", categoryId);
                 return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania przedmiotów z kategorii. CategoryId: {CategoryId}", categoryId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas pobierania danych.");
            }
        }

        // GET: api/items/{id}/photos (Pobieranie zdjęć dla przedmiotu)
        [HttpGet("{id:int}/photos")] // Dodano ograniczenie :int
        public async Task<ActionResult<IEnumerable<PhotoDto>>> GetItemPhotos(int id)
        {
             if (id <= 0) {
                 return BadRequest("Nieprawidłowe ID przedmiotu.");
             }
            try
            {
                 // Sprawdźmy najpierw czy przedmiot istnieje
                 var itemExists = await _itemService.GetItemByIdAsync(id); // Użyj prostej metody
                 if (itemExists == null)
                 {
                     return NotFound($"Przedmiot o ID {id} nie został znaleziony.");
                 }

                var photos = await _itemService.GetItemPhotosAsync(id);
                return Ok(photos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas pobierania zdjęć przedmiotu. ItemId: {ItemId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas pobierania danych.");
            }
        }

        // POST: api/items (Tworzenie nowego przedmiotu)
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<ItemDto>> CreateItem([FromBody] ItemCreateDto itemDto) // Zmieniono zwracany typ na ItemDto dla spójności
        {
             if (!ModelState.IsValid) // Sprawdź walidację DTO
             {
                 return BadRequest(ModelState);
             }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                 if (user == null) return Unauthorized("Nie można zidentyfikować użytkownika."); // Dodatkowe sprawdzenie

                // Użyj serwisu do dodania przedmiotu, który zwraca ID
                var itemId = await _itemService.AddItemAsync(itemDto, user.Id);

                // Po utworzeniu, pobierz nowo utworzony przedmiot jako DTO, aby go zwrócić
                 var createdItemDto = await _itemService.GetItemByIdAsync(itemId);
                 if (createdItemDto == null) {
                      // Coś poszło bardzo nie tak, jeśli nie możemy pobrać właśnie dodanego itemu
                      _logger.LogError("Nie można było pobrać przedmiotu (ID: {ItemId}) zaraz po jego utworzeniu.", itemId);
                      return StatusCode(StatusCodes.Status500InternalServerError, "Błąd podczas pobierania utworzonego przedmiotu.");
                 }

                // Zwróć 201 Created z lokalizacją i utworzonym obiektem DTO
                return CreatedAtAction(nameof(GetItem), new { id = itemId }, createdItemDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas tworzenia przedmiotu. DTO: {@ItemDto}", itemDto);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas tworzenia przedmiotu.");
            }
        }

        // PUT: api/items/{id} (Aktualizacja przedmiotu)
        [HttpPut("{id:int}")] // Dodano ograniczenie :int
        [Authorize]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] ItemUpdateDto itemDto)
        {
            if (id != itemDto.Id)
            {
                return BadRequest("Niezgodność ID przedmiotu w ścieżce i ciele żądania.");
            }
             if (!ModelState.IsValid)
             {
                 return BadRequest(ModelState);
             }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized("Nie można zidentyfikować użytkownika.");

                // Serwis powinien sprawdzić własność i rzucić wyjątek lub zwrócić status
                await _itemService.UpdateItemAsync(itemDto, user.Id);

                return NoContent(); // Standardowa odpowiedź dla PUT zakończonego sukcesem
            }
            catch (KeyNotFoundException ex)
            {
                 _logger.LogWarning("Próba aktualizacji nieistniejącego przedmiotu. ItemId: {ItemId}", id);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                 _logger.LogWarning("Nieautoryzowana próba aktualizacji przedmiotu. ItemId: {ItemId}, UserId: {UserId}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
                // Zwróć Forbid() zamiast 401, bo użytkownik jest zalogowany, ale nie ma uprawnień do tego zasobu
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas aktualizacji przedmiotu. ItemId: {ItemId}, DTO: {@ItemDto}", id, itemDto);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas aktualizacji przedmiotu.");
            }
        }

        // DELETE: api/items/{id} (Usuwanie przedmiotu)
        [HttpDelete("{id:int}")] // Dodano ograniczenie :int
        [Authorize]
        public async Task<IActionResult> DeleteItem(int id)
        {
             if (id <= 0) {
                 return BadRequest("Nieprawidłowe ID przedmiotu.");
             }
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized("Nie można zidentyfikować użytkownika.");

                // Serwis powinien sprawdzić własność i rzucić wyjątek lub zwrócić status
                await _itemService.DeleteItemAsync(id, user.Id);

                return NoContent(); // Standardowa odpowiedź dla DELETE zakończonego sukcesem
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Próba usunięcia nieistniejącego przedmiotu. ItemId: {ItemId}", id);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                 _logger.LogWarning("Nieautoryzowana próba usunięcia przedmiotu. ItemId: {ItemId}, UserId: {UserId}", id, User.FindFirstValue(ClaimTypes.NameIdentifier));
                return Forbid(ex.Message);
            }
             // TODO: Rozważ obsługę błędów związanych z istniejącymi transakcjami dla tego przedmiotu?
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania przedmiotu. ItemId: {ItemId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas usuwania przedmiotu.");
            }
        }

        // --- Operacje na Zdjęciach ---

        // POST: api/items/{id}/photos (Dodawanie zdjęcia do przedmiotu)
       [HttpPost("{id:int}/photos")]
[Authorize]
public async Task<ActionResult<PhotoDto>> UploadPhoto(int id, IFormFile photo) // Zwraca PhotoDto
{
    if (id <= 0) return BadRequest("Nieprawidłowe ID przedmiotu.");
    // Podstawowa walidacja pliku (rozmiar, typ itp.)
    if (photo == null || photo.Length == 0) return BadRequest("Nie przesłano pliku zdjęcia.");
    if (photo.Length > 5 * 1024 * 1024) return BadRequest("Plik zdjęcia jest zbyt duży (maksymalnie 5MB).");
    var allowedContentTypes = new[] { "image/jpeg", "image/png", "image/webp" };
    if (!allowedContentTypes.Contains(photo.ContentType.ToLowerInvariant())) return BadRequest("Niedozwolony typ pliku zdjęcia (dozwolone: JPG, PNG, WEBP).");

    try
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized("Nie można zidentyfikować użytkownika.");

        // Sprawdź, czy użytkownik jest właścicielem przedmiotu
        if (!await _itemService.IsItemOwnerAsync(id, user.Id))
        {
            return Forbid("Nie jesteś właścicielem tego przedmiotu.");
        }

        int photoId; // Zmienna na ID zwrócone przez AddPhotoAsync
        using (var stream = photo.OpenReadStream())
        {
            // 1. Upload fizyczny pliku
            var photoPath = await _photoService.UploadPhotoAsync(stream, photo.FileName, photo.ContentType);

            // 2. Przygotuj DTO do zapisu metadanych
            var photoCreateDto = new PhotoCreateDto
            {
                Path = photoPath,
                FileName = Path.GetFileName(photo.FileName), // Użyj bezpiecznej nazwy pliku
                FileSize = photo.Length,
                ItemId = id
            };
            // 3. Zapisz metadane i pobierz ID (serwis zwraca int)
            photoId = await _photoService.AddPhotoAsync(photoCreateDto, user.Id);
        } // Koniec using dla stream

        // --- WAŻNA CZĘŚĆ ---
        // 4. Pobierz pełne DTO nowo dodanego zdjęcia używając jego ID
        var createdPhotoDto = await _photoService.GetPhotoByIdAsync(photoId);
        if (createdPhotoDto == null)
        {
            // To nie powinno się zdarzyć, ale lepiej obsłużyć
            _logger.LogError("Nie można było pobrać PhotoDto (ID: {PhotoId}) zaraz po jego dodaniu do przedmiotu {ItemId}", photoId, id);
            return StatusCode(StatusCodes.Status500InternalServerError, "Błąd podczas pobierania informacji o dodanym zdjęciu.");
        }
        // 5. Zwróć kod 201 Created wraz z obiektem PhotoDto
        return StatusCode(StatusCodes.Status201Created, createdPhotoDto);
        // Alternatywnie, jeśli masz akcję GetPhoto(int itemId, int photoId):
        // return CreatedAtAction(nameof(GetPhoto), new { itemId = id, photoId = createdPhotoDto.Id }, createdPhotoDto);

    }
    catch (UnauthorizedAccessException ex) { return Forbid(ex.Message); }
    catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Błąd podczas przesyłania zdjęcia dla przedmiotu. ItemId: {ItemId}", id);
        return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas przesyłania zdjęcia.");
    }
}
        // DELETE: api/items/{itemId}/photos/{photoId} (Usuwanie zdjęcia)
        [HttpDelete("{itemId:int}/photos/{photoId:int}")] // Dodano ograniczenia :int
        [Authorize]
        public async Task<IActionResult> DeletePhoto(int itemId, int photoId)
        {
            if (itemId <= 0 || photoId <= 0) return BadRequest("Nieprawidłowe ID.");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized("Nie można zidentyfikować użytkownika.");

                // Sprawdź, czy użytkownik jest właścicielem przedmiotu
                // (Alternatywnie, IPhotoService.DeletePhotoAsync mógłby to sprawdzać wewnętrznie)
                if (!await _itemService.IsItemOwnerAsync(itemId, user.Id))
                {
                     _logger.LogWarning("Nieautoryzowana próba usunięcia zdjęcia z przedmiotu. ItemId: {ItemId}, PhotoId: {PhotoId}, UserId: {UserId}", itemId, photoId, user.Id);
                    return Forbid("Nie jesteś właścicielem tego przedmiotu.");
                }

                // Użyj serwisu do usunięcia zdjęcia (z dysku i z bazy)
                // Zakładamy, że rzuci KeyNotFoundException jeśli zdjęcie nie istnieje
                await _photoService.DeletePhotoAsync(photoId, user.Id); // Przekazujemy userId do weryfikacji w serwisie? Lub nie, jeśli sprawdziliśmy wyżej.

                return NoContent(); // Sukces
            }
            catch (KeyNotFoundException ex)
            {
                 _logger.LogWarning("Próba usunięcia nieistniejącego zdjęcia. PhotoId: {PhotoId}", photoId);
                return NotFound(ex.Message);
            }
             catch (UnauthorizedAccessException ex) // Jeśli serwis zdjęć by to rzucał
             {
                 return Forbid(ex.Message);
             }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania zdjęcia. PhotoId: {PhotoId}, ItemId: {ItemId}", photoId, itemId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas usuwania zdjęcia.");
            }
        }

        // --- Operacje na Statusie i Tagach ---

        // POST: api/items/{id}/mark-sold (Oznaczanie jako niedostępny/sprzedany)
        [HttpPost("{id:int}/mark-sold")] // Zmieniono na mark-unavailable? Lub dodano alias
        [Route("{id:int}/mark-unavailable")] // Można dodać alias trasy
        [Authorize]
        public async Task<IActionResult> MarkItemAsUnavailable(int id) // Zmieniono nazwę dla jasności
        {
            if (id <= 0) return BadRequest("Nieprawidłowe ID przedmiotu.");

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized("Nie można zidentyfikować użytkownika.");

                // Użyj serwisu do oznaczenia jako niedostępny
                // Serwis powinien sprawdzić własność
                await _itemService.MarkItemAsSoldAsync(id, user.Id); // Używamy metody z serwisu

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas oznaczania przedmiotu jako niedostępny. ItemId: {ItemId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas aktualizacji statusu przedmiotu.");
            }
        }

        // POST: api/items/{itemId}/tags/{tagId} (Dodawanie tagu do przedmiotu)
        [HttpPost("{itemId:int}/tags/{tagId:int}")]
        [Authorize]
        public async Task<IActionResult> AddTagToItem(int itemId, int tagId)
        {
             if (itemId <= 0 || tagId <= 0) return BadRequest("Nieprawidłowe ID.");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized("Nie można zidentyfikować użytkownika.");

                // Serwis powinien sprawdzać własność przedmiotu
                await _itemService.AddTagToItemAsync(itemId, tagId, user.Id);

                return NoContent(); // Lub Ok() jeśli chcesz coś zwrócić
            }
            catch (KeyNotFoundException ex) // Jeśli przedmiot lub tag nie istnieje
            {
                 return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex) // Jeśli użytkownik nie jest właścicielem
            {
                return Forbid(ex.Message);
            }
             catch (InvalidOperationException ex) // Np. jeśli tag jest już dodany
             {
                 return Conflict(ex.Message); // Zwróć 409 Conflict
             }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas dodawania tagu do przedmiotu. ItemId: {ItemId}, TagId: {TagId}", itemId, tagId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas dodawania tagu.");
            }
        }

        // DELETE: api/items/{itemId}/tags/{tagId} (Usuwanie tagu z przedmiotu)
        [HttpDelete("{itemId:int}/tags/{tagId:int}")]
        [Authorize]
        public async Task<IActionResult> RemoveTagFromItem(int itemId, int tagId)
        {
            if (itemId <= 0 || tagId <= 0) return BadRequest("Nieprawidłowe ID.");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null) return Unauthorized("Nie można zidentyfikować użytkownika.");

                // Serwis powinien sprawdzać własność przedmiotu
                await _itemService.RemoveTagFromItemAsync(itemId, tagId, user.Id);

                return NoContent();
            }
            catch (KeyNotFoundException ex) // Jeśli przedmiot, tag lub powiązanie nie istnieje
            {
                 return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex) // Jeśli użytkownik nie jest właścicielem
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd podczas usuwania tagu z przedmiotu. ItemId: {ItemId}, TagId: {TagId}", itemId, tagId);
                return StatusCode(StatusCodes.Status500InternalServerError, "Wystąpił błąd podczas usuwania tagu.");
            }
        }

        // --- Prywatna metoda pomocnicza (jeśli potrzebna) ---
        // private async Task<bool> CheckItemOwnership(int itemId) { ... }
    }
}