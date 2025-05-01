namespace LeafLoop.Services.DTOs
{
    public class BadgeDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public string RequirementCondition { get; set; }
    }

    public class BadgeCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public string RequirementCondition { get; set; }
    }

    public class BadgeUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public string RequirementCondition { get; set; }
    }
}
