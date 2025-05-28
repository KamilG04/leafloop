// Full Path: Services/Mappings/MappingProfile.cs (COMPLETE AND REVISED)

using System;
using System.Linq;
using AutoMapper;
using LeafLoop.Models;       // Ensure this using is correct for your Models
using LeafLoop.Services.DTOs; // Ensure this using is correct for your DTOs
using System.Collections.Generic;
using LeafLoop.ViewModels.Profile; // Required for ProfileViewModel, etc.

namespace LeafLoop.Services.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // --- Helper function for formatting image/file paths ---
            // Adds '/' at the beginning, removes existing if present, returns null for empty/null path.
            // Assumes paths point to files in wwwroot.
            Func<string, string> formatImagePath = (path) =>
                !string.IsNullOrEmpty(path) ? "/" + path.TrimStart('/') : null;

            // --- User mappings ---
            CreateMap<User, UserDto>()
                 .ForMember(dest => dest.AvatarPath, opt => opt.MapFrom(src => formatImagePath(src.AvatarPath)))
                 .ForMember(dest => dest.SearchRadius, opt => opt.MapFrom(src => src.SearchRadius)); // <<< --- DODANE MAPOWANIE --- >>>

            CreateMap<User, UserWithDetailsDto>()
                .IncludeBase<User, UserDto>() // Inherits mappings from UserDto (including AvatarPath and SearchRadius)
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address)) 
                .ForMember(dest => dest.CompletedTransactionsCount, opt => opt.Ignore())
                .ForMember(dest => dest.AverageRating, opt => opt.Ignore())
                .ForMember(dest => dest.Badges, opt => opt.Ignore());

            CreateMap<UserRegistrationDto, User>()
                 .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));

            CreateMap<UserUpdateDto, User>()
                 .ForMember(dest => dest.Id, opt => opt.Ignore()) 
                 .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
                 // If UserUpdateDto gets SearchRadius, ensure it's mapped here if needed:
                 // .ForMember(dest => dest.SearchRadius, opt => opt.Condition(src => src.SearchRadius.HasValue)) // Only map if DTO value is not null


            CreateMap<UserWithDetailsDto, ProfileViewModel>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.RecentItems, opt => opt.Ignore())
                .ForMember(dest => dest.TotalItemsCount, opt => opt.Ignore())
                .ForMember(dest => dest.TotalTransactionsCount, opt => opt.Ignore());

            // --- Address mappings ---
            CreateMap<Address, AddressDto>();
            CreateMap<AddressDto, Address>()
                .ForMember(dest => dest.Id, opt => opt.Ignore());

            // --- Item mappings ---
            CreateMap<Item, ItemDto>()
                .ForMember(dest => dest.MainPhotoPath,
                    opt => opt.MapFrom(src =>
                        formatImagePath(src.Photos != null && src.Photos.Any()
                            ? src.Photos.OrderBy(p => p.Id).FirstOrDefault().Path
                            : null)))
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null))
                .ForMember(dest => dest.UserName,
                    opt => opt.MapFrom(src => src.User != null
                        ? $"{src.User.FirstName} {src.User.LastName}".Trim()
                        : null));

            CreateMap<Item, ItemSummaryDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src =>
                    src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : "N/A"))
                .ForMember(dest => dest.MainPhotoPath, opt => opt.MapFrom(src =>
                    formatImagePath(src.Photos != null && src.Photos.Any() ? src.Photos.OrderBy(p => p.Id).FirstOrDefault().Path : null)))
                .ForMember(dest => dest.City, opt => opt.MapFrom(src =>
                    src.User != null && src.User.Address != null ? src.User.Address.City : null))
                .ForMember(dest => dest.Country, opt => opt.MapFrom(src =>
                    src.User != null && src.User.Address != null ? src.User.Address.Country : null))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src =>
                    src.User != null && src.User.Address != null ? src.User.Address.Latitude : (decimal?)null))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src =>
                    src.User != null && src.User.Address != null ? src.User.Address.Longitude : (decimal?)null))
                // CategoryName should be added to ItemSummaryDto for consistency with ItemDto if needed by UI
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : null));


            CreateMap<Transaction, TransactionBasicDto>()
                .ForMember(dest => dest.BuyerName, opt => opt.MapFrom(src =>
                    src.Buyer != null ? $"{src.Buyer.FirstName} {src.Buyer.LastName}".Trim() : null));

            CreateMap<Item, ItemWithDetailsDto>()
                .IncludeBase<Item, ItemDto>() 
                .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User)) 
                .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category)) 
                .ForMember(dest => dest.Photos, opt => opt.MapFrom(src => src.Photos)) 
                .ForMember(dest => dest.Tags, opt => opt.MapFrom(src => src.Tags != null ? src.Tags.Select(it => it.Tag) : new List<Tag>())) 
                .ForMember(dest => dest.PendingTransactions, opt => opt.MapFrom(src =>
                    src.Transactions != null
                        ? src.Transactions.Where(t => t.Status == TransactionStatus.Pending || t.Status == TransactionStatus.InProgress)
                        : new List<Transaction>())); 

            CreateMap<ItemCreateDto, Item>();
            CreateMap<ItemUpdateDto, Item>();

            // --- Category mappings ---
            CreateMap<Category, CategoryDto>()
                .ForMember(dest => dest.ItemsCount, opt => opt.MapFrom(src => src.Items != null ? src.Items.Count : 0))
                .ForMember(dest => dest.IconPath, opt => opt.MapFrom(src => formatImagePath(src.IconPath)));
            CreateMap<CategoryCreateDto, Category>();
            CreateMap<CategoryUpdateDto, Category>();

            // --- Admin mappings ---
            CreateMap<AdminLog, AdminLogDto>()
                .ForMember(dest => dest.AdminUserName,
                    opt => opt.MapFrom(src => src.AdminUser != null ?
                        $"{src.AdminUser.FirstName} {src.AdminUser.LastName}".Trim() : "Unknown"));

            CreateMap<User, UserManagementDto>()
                .ForMember(dest => dest.Roles, opt => opt.Ignore());

            // --- Tag mappings ---
            CreateMap<Tag, TagDto>()
                .ForMember(dest => dest.ItemsCount, opt => opt.MapFrom(src => src.Items != null ? src.Items.Count : 0));
            CreateMap<TagCreateDto, Tag>();
            CreateMap<TagUpdateDto, Tag>();

            // --- Transaction mappings ---
            CreateMap<Transaction, TransactionDto>()
                .ForMember(dest => dest.SellerName, opt => opt.MapFrom(src => src.Seller != null ? $"{src.Seller.FirstName} {src.Seller.LastName}".Trim() : null))
                .ForMember(dest => dest.BuyerName, opt => opt.MapFrom(src => src.Buyer != null ? $"{src.Buyer.FirstName} {src.Buyer.LastName}".Trim() : null))
                .ForMember(dest => dest.ItemName, opt => opt.MapFrom(src => src.Item != null ? src.Item.Name : null))
                .ForMember(dest => dest.ItemPhotoPath, opt => opt.MapFrom(src =>
                    formatImagePath(src.Item != null && src.Item.Photos != null && src.Item.Photos.Any() ? src.Item.Photos.OrderBy(p => p.Id).FirstOrDefault().Path : null)))
                .ForMember(dest => dest.BuyerConfirmed, opt => opt.MapFrom(src => src.BuyerConfirmed))
                .ForMember(dest => dest.SellerConfirmed, opt => opt.MapFrom(src => src.SellerConfirmed));

             CreateMap<Transaction, TransactionWithDetailsDto>()
                 .IncludeBase<Transaction, TransactionDto>()
                 .ForMember(dest => dest.Seller, opt => opt.MapFrom(src => src.Seller))
                 .ForMember(dest => dest.Buyer, opt => opt.MapFrom(src => src.Buyer))
                 .ForMember(dest => dest.Item, opt => opt.MapFrom(src => src.Item))
                 .ForMember(dest => dest.Messages, opt => opt.MapFrom(src => src.Messages))
                 .ForMember(dest => dest.Ratings, opt => opt.MapFrom(src => src.Ratings));

            CreateMap<TransactionCreateDto, Transaction>();

            // --- Message mappings ---
            CreateMap<Message, MessageDto>()
                .ForMember(dest => dest.SenderName, opt => opt.MapFrom(src => src.Sender != null ? $"{src.Sender.FirstName} {src.Sender.LastName}".Trim() : null))
                .ForMember(dest => dest.ReceiverName, opt => opt.MapFrom(src => src.Receiver != null ? $"{src.Receiver.FirstName} {src.Receiver.LastName}".Trim() : null));
            CreateMap<MessageCreateDto, Message>();

            // --- Notification mappings ---
            CreateMap<Notification, NotificationDto>();
            CreateMap<NotificationCreateDto, Notification>();

            // --- Event mappings --
            CreateMap<Event, EventDto>()
                .ForMember(dest => dest.CurrentParticipantsCount, opt => opt.MapFrom(src => src.Participants != null ? src.Participants.Count : 0))
                
                // .ForMember(dest => dest.OrganizerName, opt => opt.MapFrom(src => src.ActualOrganizerNamePropertyIfExistsOnEventEntity))
                ; // Pozostawiamy bez mapowania OrganizerName tutaj, serwis to uzupełni

            CreateMap<Event, EventWithDetailsDto>()
                .IncludeBase<Event, EventDto>() // Dziedziczy mapowanie CurrentParticipantsCount
                .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address))
                // Zakładamy, że Event.Participants to ICollection<EventParticipant>, a EventParticipant.User to encja User
                // Chcemy zmapować listę encji User (z Event.Participants) na listę UserDto
                .ForMember(dest => dest.Participants, opt => opt.MapFrom(src =>
                    src.Participants != null
                        ? src.Participants.Select(ep => ep.User) // Wybiera encje User z EventParticipant
                        : new List<User>())) // AutoMapper użyje istniejącego mapowania User -> UserDto
                ;


             CreateMap<EventCreateDto, Event>();
             CreateMap<EventUpdateDto, Event>();

            // --- Photo mappings ---
            CreateMap<Photo, PhotoDto>()
                 .ForMember(dest => dest.Path, opt => opt.MapFrom(src => formatImagePath(src.Path)));
            CreateMap<PhotoCreateDto, Photo>();

            // --- Rating mappings ---
            CreateMap<Rating, RatingDto>()
                .ForMember(dest => dest.RaterName, opt => opt.MapFrom(src => src.Rater != null ? $"{src.Rater.FirstName} {src.Rater.LastName}".Trim() : null))
                .ForMember(dest => dest.RatedEntityName, opt => opt.Ignore()); // TODO: Implement RatedEntityName
            CreateMap<RatingCreateDto, Rating>();
            CreateMap<RatingUpdateDto, Rating>();

            // --- Badge mappings ---
            CreateMap<Badge, BadgeDto>()
                 .ForMember(dest => dest.IconPath, opt => opt.MapFrom(src => formatImagePath(src.IconPath)));
            CreateMap<BadgeCreateDto, Badge>();
            CreateMap<BadgeUpdateDto, Badge>();

            // --- Mappings for other DTOs/entities ---
            CreateMap<Comment, CommentDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? $"{src.User.FirstName} {src.User.LastName}".Trim() : null));
            CreateMap<CommentCreateDto, Comment>();
            CreateMap<CommentUpdateDto, Comment>();

            CreateMap<Company, CompanyDto>()
                 .ForMember(dest => dest.LogoPath, opt => opt.MapFrom(src => formatImagePath(src.LogoPath)));
            
            CreateMap<Company, CompanyWithDetailsDto>()
                 .ForMember(dest => dest.Address, opt => opt.MapFrom(src => src.Address)); 
            
            CreateMap<CompanyRegistrationDto, Company>();
            CreateMap<CompanyUpdateDto, Company>();

            CreateMap<Report, ReportDto>()
                 .ForMember(dest => dest.ReporterName, opt => opt.Ignore()); // TODO: Map from src.Reporter
            CreateMap<ReportCreateDto, Report>();

            CreateMap<SavedSearch, SavedSearchDto>();
            CreateMap<SavedSearchCreateDto, SavedSearch>();
            CreateMap<SavedSearchUpdateDto, SavedSearch>();

            CreateMap<Subscription, SubscriptionDto>()
                 .ForMember(dest => dest.ContentName, opt => opt.Ignore()); // TODO: Map based on ContentType and ContentId
            CreateMap<SubscriptionCreateDto, Subscription>();
        }
    }
}