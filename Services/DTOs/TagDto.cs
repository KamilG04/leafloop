namespace LeafLoop.Services.DTOs
{
    public class TagDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsSystem { get; set; }
        public int ItemsCount { get; set; }
    }

    public class TagCreateDto
    {
        public string Name { get; set; }
        public bool IsSystem { get; set; }
    }

    public class TagUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsSystem { get; set; }
    }
}
