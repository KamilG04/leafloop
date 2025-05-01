using System;
using System.Collections.Generic;

namespace LeafLoop.Services.DTOs
{
    public class SavedSearchDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public List<int> TagIds { get; set; }
        public string Condition { get; set; }
        public bool? IsForExchange { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class SavedSearchCreateDto
    {
        public string Name { get; set; }
        public string SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public List<int> TagIds { get; set; }
        public string Condition { get; set; }
        public bool? IsForExchange { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public int UserId { get; set; }
    }

    public class SavedSearchUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public List<int> TagIds { get; set; }
        public string Condition { get; set; }
        public bool? IsForExchange { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
    }
}
