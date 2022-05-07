namespace BITool.Models
{
    public record AdminCreateOrUpdateDto
    {
        public int Id { get; set; } = default!;
        public string UserName { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = " ";
        public string Email { get; set; } = default!;
        public string Password { get; set; } = default!;
    }
}
