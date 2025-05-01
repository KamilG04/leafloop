using System;
using LeafLoop.Models;

namespace LeafLoop.Services.DTOs
{
    public class CompanyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string NIP { get; set; }
        public string REGON { get; set; }
        public string Description { get; set; }
        public string LogoPath { get; set; }
        public DateTime JoinDate { get; set; }
        public VerificationStatus VerificationStatus { get; set; }
    }

    public class CompanyWithDetailsDto : CompanyDto
    {
        public AddressDto Address { get; set; }
        public double AverageRating { get; set; }
    }

    public class CompanyRegistrationDto
    {
        public string Name { get; set; }
        public string NIP { get; set; }
        public string REGON { get; set; }
        public string Description { get; set; }
        public AddressDto Address { get; set; }
    }

    public class CompanyUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string LogoPath { get; set; }
    }
}
