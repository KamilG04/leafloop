namespace LeafLoop.Services.DTOs
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
        public int ItemsCount { get; set; }
    }

    public class CategoryCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
    }

    public class CategoryUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string IconPath { get; set; }
    }
}
