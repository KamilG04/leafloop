using System;
using System.Collections.Generic;

namespace LeafLoop.Services.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime LastActivity { get; set; }
        public int EcoScore { get; set; }
        public string AvatarPath { get; set; }
        public bool IsActive { get; set; }
    }

    public class UserWithDetailsDto : UserDto
    {
        public AddressDto Address { get; set; }
        public double AverageRating { get; set; }
        public int CompletedTransactionsCount { get; set; }
        public List<BadgeDto> Badges { get; set; }
    }

    public class UserRegistrationDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public class UserUpdateDto
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string AvatarPath { get; set; }
    }
}
