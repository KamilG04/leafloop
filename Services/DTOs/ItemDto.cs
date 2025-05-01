using System;
using System.Collections.Generic;

namespace LeafLoop.Services.DTOs
{
    public class ItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public DateTime DateAdded { get; set; }
        public bool IsAvailable { get; set; }
        public bool IsForExchange { get; set; }
        public decimal ExpectedValue { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; }
        public string MainPhotoPath { get; set; }
    }

    public class ItemWithDetailsDto : ItemDto
    {
        public UserDto User { get; set; }
        public CategoryDto Category { get; set; }
        public List<PhotoDto> Photos { get; set; }
        public List<TagDto> Tags { get; set; }
    }

    public class ItemCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public bool IsForExchange { get; set; }
        public decimal ExpectedValue { get; set; }
        public int CategoryId { get; set; }
        public List<int> TagIds { get; set; }
    }

    public class ItemUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Condition { get; set; }
        public bool IsForExchange { get; set; }
        public decimal ExpectedValue { get; set; }
        public bool IsAvailable { get; set; }
        public int CategoryId { get; set; }
    }

    public class ItemSearchDto
    {
        public string SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public List<int> TagIds { get; set; }
        public string Condition { get; set; }
        public bool? IsForExchange { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public DateTime? AddedAfter { get; set; }
        public int? Page { get; set; }
        public int? PageSize { get; set; }
        public string SortBy { get; set; }
        public bool SortDescending { get; set; }
    }
}
