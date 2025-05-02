// Pełna ścieżka: Services/Mappings/MappingProfile.cs (KOMPLETNY I POPRAWIONY)

using System;
using System.Linq;
using AutoMapper;
using LeafLoop.Models; // Upewnij się, że using jest poprawny dla Modeli
using LeafLoop.Services.DTOs; // Upewnij się, że using jest poprawny dla DTO
using System.Collections.Generic;
using LeafLoop.ViewModels.Profile; // Potrzebne dla List<> itp.

// Jeśli używasz DTO z podfolderów, dodaj odpowiednie usingi:
// using LeafLoop.Services.DTOs.Auth;
// using LeafLoop.Services.DTOs.Preferences;

namespace LeafLoop.Services.Mappings // Upewnij się, że namespace jest poprawny
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- Funkcja pomocnicza do formatowania ścieżek obrazków/plików ---
            // Dodaje '/' na początku, usuwa istniejący jeśli jest, zwraca null dla pustej/null ścieżki.
            // Zakłada, że ścieżki wskazują na pliki w wwwroot.
            Func<string, string> formatImagePath = (path) =>
                !string.IsNullOrEmpty(path) ? "/" + path.TrimStart('/') : null;

            // --- User mappings ---
            CreateMap<User, UserDto>()
                 // Formatuj AvatarPath
                 .ForMember(dest => dest.AvatarPath, opt => opt.MapFrom(src => formatImagePath(src.AvatarPath)));

            // Mapowanie dla UserWithDetailsDto - IGNORUJE pola obliczane w UserService
            CreateMap<User, UserWithDetailsDto>()
                .IncludeBase<User, UserDto>() // Dziedziczy mapowania z UserDto (w tym AvatarPath)
                // Mapuj adres - zadziała, jeśli UserService ładuje User z adresem (np. GetUserWithAddressAsync)
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address)) // Wymaga mapowania Address -> AddressDto
                // IGNORUJ pola obliczane/ładowane ręcznie w UserService
                .ForMember(dest => dest.CompletedTransactionsCount, opt => opt.Ignore())
                .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
                .ForMember(dest => dest.Badges, opt => opt.Ignore());

            // Mapowanie DTO rejestracji na encję User (hasło jest obsługiwane przez UserManager)
             CreateMap<UserRegistrationDto, User>()
                 .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email)); // Ustaw UserName na Email przy rejestracji

            // Mapowanie DTO aktualizacji na encję User (aktualizuje tylko wybrane pola)
            CreateMap<UserUpdateDto, User>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore()) // Ignoruj ID, żeby nie nadpisać istniejącego
                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)); // Aktualizuj tylko jeśli wartość w DTO nie jest null? (Opcjonalne)

            CreateMap<UserWithDetailsDto, ProfileViewModel>()
                // AutoMapper powinien automatycznie zmapować pasujące nazwy właściwości:
                // UserId, FirstName, LastName, Email, AvatarPath, EcoScore, CreatedDate, LastActivity,
                // Address, AverageRating, Badges
                // Ignorujemy pola ViewModelu, które wypełniamy ręcznie w kontrolerze:
                .ForMember(dest => dest.RecentItems, opt => opt.Ignore())
                .ForMember(dest => dest.TotalItemsCount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalTransactionsCount, opt => opt.Ignore());
            // --- Address mappings ---
            CreateMap<Address, AddressDto>();
            CreateMap<AddressDto, Address>();

            // --- Item mappings ---
            CreateMap<Item, ItemDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}" : null))
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                // Poprawione mapowanie dla MainPhotoPath
                .ForMember(dest => dest.MainPhotoPath, opt => opt.MapFrom(src =>
                    formatImagePath(src.Photos != null && src.Photos.Any() ? src.Photos.OrderBy(p => p.Id).FirstOrDefault().Path : null) // Dodano ?. dla bezpieczeństwa
                ));

            // Mapowanie ItemWithDetailsDto dziedziczy z ItemDto
             CreateMap<Item, ItemWithDetailsDto>()
                 .IncludeBase<Item, ItemDto>()
                 // Upewnij się, że zapytanie ładujące Item do tego DTO zawiera User, Category, Photos, Tags
                 .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User)) // Wymaga User->UserDto
                 .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category)) // Wymaga Category->CategoryDto
                 .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos)) // Wymaga Photo->PhotoDto
                 .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags != null ? src.Tags.Select(it => it.Tag) : new List<Tag>())); // Wymaga Tag->TagDto

            CreateMap<ItemCreateDto, Item>(); // Mapowanie przy tworzeniu
            CreateMap<ItemUpdateDto, Item>(); // Mapowanie przy aktualizacji

            // --- Category mappings ---
            CreateMap<Category, CategoryDto>()
                // Zakładamy, że Items jest ładowane, gdy potrzebujemy ItemsCount
                .ForMember(dest => dest.ItemsCount, opt => opt.MapFrom(src => src.Items != null ? src.Items.Count : 0))
                // Formatuj IconPath
                .ForMember(dest => dest.IconPath, opt => opt.MapFrom(src => formatImagePath(src.IconPath)));
            CreateMap<CategoryCreateDto, Category>();
            CreateMap<CategoryUpdateDto, Category>();

            // --- Tag mappings ---
            CreateMap<Tag, TagDto>()
                // Zakładamy, że Items jest ładowane, gdy potrzebujemy ItemsCount
                .ForMember(dest => dest.ItemsCount, opt => opt.MapFrom(src => src.Items != null ? src.Items.Count : 0));
            CreateMap<TagCreateDto, Tag>();
            CreateMap<TagUpdateDto, Tag>();

            // --- Transaction mappings ---
            CreateMap<Transaction, TransactionDto>()
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.Seller != null ? $"{src.Seller.FirstName} {src.Seller.LastName}" : null))
                .ForMember(dest => dest.BuyerName, opt => opt.MapFrom(src => src.Buyer != null ? $"{src.Buyer.FirstName} {src.Buyer.LastName}" : null))
                .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => src.Item != null ? src.Item.Name : null))
                // Poprawione mapowanie dla ItemPhotoPath
                .ForMember(dest => dest.ItemPhotoPath, opt => opt.MapFrom(src =>
                     formatImagePath(src.Item != null && src.Item.Photos != null && src.Item.Photos.Any() ? src.Item.Photos.OrderBy(p => p.Id).FirstOrDefault().Path : null) // Dodano ?.
                 ));

            // TransactionWithDetailsDto dziedziczy z TransactionDto
             CreateMap<Transaction, TransactionWithDetailsDto>()
                 .IncludeBase<Transaction, TransactionDto>()
                 // Upewnij się, że zapytanie ładujące Transaction do tego DTO zawiera Seller, Buyer, Item, Messages, Ratings
                 .ForMember(dest => dest.Seller, opt => opt.MapFrom(src => src.Seller))     // Wymaga User->UserDto
                 .ForMember(dest => dest.Buyer, opt => opt.MapFrom(src => src.Buyer))       // Wymaga User->UserDto
                 .ForMember(dest => dest.Item, opt => opt.MapFrom(src => src.Item))         // Wymaga Item->ItemDto
                 .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.Messages)) // Wymaga Message->MessageDto
                 .ForMember(dest => dest.Ratings, opt => opt.MapFrom(src => src.Ratings));  // Wymaga Rating->RatingDto

            CreateMap<TransactionCreateDto, Transaction>();

            // --- Message mappings ---
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender != null ? $"{src.Sender.FirstName} {src.Sender.LastName}" : null))
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.Receiver != null ? $"{src.Receiver.FirstName} {src.Receiver.LastName}" : null));
            CreateMap<MessageCreateDto, Message>();

            // --- Notification mappings ---
            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationCreateDto, Notification>();

            // --- Event mappings ---
             CreateMap<Event, EventDto>()
                 // Zakładamy, że Participants jest ładowane, gdy potrzebujemy CurrentParticipantsCount
                 .ForMember(dest => dest.CurrentParticipantsCount, opt => opt.MapFrom(src => src.Participants != null ? src.Participants.Count : 0));
                 // TODO: Sprawdź/Dodaj mapowanie dla OrganizerName (zależne od OrganizerType i struktury Event)
             CreateMap<Event, EventWithDetailsDto>(); // TODO: Sprawdź/Dodaj mapowanie dla Address, Participants
             CreateMap<EventCreateDto, Event>();
             CreateMap<EventUpdateDto, Event>();

            // --- Photo mappings ---
            CreateMap<Photo, PhotoDto>()
                 // Poprawione mapowanie dla Path
                 .ForMember(dest => dest.Path, opt => opt.MapFrom(src => formatImagePath(src.Path)));
            CreateMap<PhotoCreateDto, Photo>(); // OK

            // --- Rating mappings ---
            CreateMap<Rating, RatingDto>()
                .ForMember(dest => dest.RaterName, opt => opt.MapFrom(src => src.Rater != null ? $"{src.Rater.FirstName} {src.Rater.LastName}" : null));
                 // TODO: Sprawdź/Dodaj mapowanie dla RatedEntityName (zależne od RatedEntityType)
            CreateMap<RatingCreateDto, Rating>();
            CreateMap<RatingUpdateDto, Rating>();

            // --- Badge mappings ---
            CreateMap<Badge, BadgeDto>()
                 // Formatuj IconPath
                 .ForMember(dest => dest.IconPath, opt => opt.MapFrom(src => formatImagePath(src.IconPath)));
            CreateMap<BadgeCreateDto, Badge>();
            CreateMap<BadgeUpdateDto, Badge>();

            // --- Mapowania dla innych DTO/encji ---
            CreateMap<Comment, CommentDto>(); // Dodaj mapowanie dla UserName
            CreateMap<CommentCreateDto, Comment>();
            CreateMap<CommentUpdateDto, Comment>();

            CreateMap<Company, CompanyDto>() // Dodaj mapowanie dla LogoPath
                 .ForMember(dest => dest.LogoPath, opt => opt.MapFrom(src => formatImagePath(src.LogoPath)));
            CreateMap<Company, CompanyWithDetailsDto>(); // Dziedziczy LogoPath, wymaga Address->AddressDto
            CreateMap<CompanyRegistrationDto, Company>();
            CreateMap<CompanyUpdateDto, Company>();

            CreateMap<Report, ReportDto>(); // Dodaj mapowanie dla ReporterName
            CreateMap<ReportCreateDto, Report>();

            CreateMap<SavedSearch, SavedSearchDto>();
            CreateMap<SavedSearchCreateDto, SavedSearch>();
            CreateMap<SavedSearchUpdateDto, SavedSearch>();

            CreateMap<Subscription, SubscriptionDto>(); // Dodaj mapowanie dla ContentName
            CreateMap<SubscriptionCreateDto, Subscription>();

            // Dodaj mapowania dla DTO z Auth i Preferences, jeśli są potrzebne do mapowania encji
             // np. CreateMap<UserPreferences, PreferencesData>();
             //     CreateMap<PreferencesData, UserPreferences>();

        }
    }
}