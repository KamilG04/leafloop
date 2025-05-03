using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using LeafLoop.Models;

namespace LeafLoop.Services.DTOs
{
    public class EventDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ParticipantsLimit { get; set; }
        public int CurrentParticipantsCount { get; set; }
        public int OrganizerId { get; set; }
        public string OrganizerName { get; set; }
        public OrganizerType OrganizerType { get; set; }
    }

    public class EventWithDetailsDto : EventDto
    {
        public AddressDto Address { get; set; }
        public List<UserDto> Participants { get; set; }
    }

    public class EventCreateDto
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ParticipantsLimit { get; set; }
        public AddressDto Address { get; set; }
        public int OrganizerId { get; set; }
        public OrganizerType OrganizerType { get; set; }
    }

    public class EventUpdateDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int ParticipantsLimit { get; set; }
    }
  

    // === DEFINICJA DTO ===
    public class ParticipationStatusUpdateDto
    {
        [Required(ErrorMessage = "Participation status is required.")] // Walidacja
        public ParticipationStatus Status { get; set; }
    }

    // Przykład definicji enum OrganizerType (jeśli nie masz go gdzie indziej)
 
}
