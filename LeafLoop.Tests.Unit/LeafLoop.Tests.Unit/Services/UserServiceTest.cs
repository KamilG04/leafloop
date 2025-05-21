using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Repositories.Interfaces;
using LeafLoop.Services; 
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace LeafLoop.Tests.Unit.Services
{
    public class UserServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<IAddressRepository> _mockAddressRepository;
        private readonly Mock<ITransactionRepository> _mockTransactionRepository;
        private readonly Mock<UserManager<User>> _mockUserManager;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IRatingService> _mockRatingService;
        private readonly ILogger<UserService> _logger;
        private readonly UserService _userService;

        public UserServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockAddressRepository = new Mock<IAddressRepository>();
            _mockTransactionRepository = new Mock<ITransactionRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockRatingService = new Mock<IRatingService>();
            _logger = NullLogger<UserService>.Instance;

            // UserManager requires IUserStore, so we mock that minimally.
            var mockUserStore = new Mock<IUserStore<User>>();
            _mockUserManager = new Mock<UserManager<User>>(
                mockUserStore.Object, null, null, null, null, null, null, null, null);

            // Setup UnitOfWork mocks to return specific repository mocks
            _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Items).Returns(_mockItemRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Addresses).Returns(_mockAddressRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Transactions).Returns(_mockTransactionRepository.Object);
            
            _userService = new UserService(
                _mockUnitOfWork.Object,
                _mockUserManager.Object,
                _mockMapper.Object,
                _mockRatingService.Object,
                _logger
            );
        }

        private User CreateTestUser(int id = 1, string email = "test@example.com", string firstName = "Test", string lastName = "User")
        {
            return new User { Id = id, Email = email, UserName = email, FirstName = firstName, LastName = lastName, SearchRadius = 10 };
        }

        // --- Tests for GetUserByIdAsync ---
        [Fact]
        public async Task GetUserByIdAsync_UserExists_ShouldReturnMappedUserDto()
        {
            // Arrange: Mock repository returns a user, mapper converts to DTO.
            var userId = 1;
            var userEntity = CreateTestUser(userId);
            var expectedDto = new UserDto { Id = userId, Email = userEntity.Email };

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(userEntity);
            _mockMapper.Setup(mapper => mapper.Map<UserDto>(userEntity)).Returns(expectedDto);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Id, result.Id);
            _mockUserRepository.Verify(repo => repo.GetByIdAsync(userId), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<UserDto>(userEntity), Times.Once);
        }

        [Fact]
        public async Task GetUserByIdAsync_UserDoesNotExist_ShouldReturnNull()
        {
            // Arrange
            var userId = 99;
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync((User)null);
            _mockMapper.Setup(mapper => mapper.Map<UserDto>(null)).Returns((UserDto)null);


            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.Null(result);
        }

      
        [Fact]
        public async Task GetUserWithDetailsAsync_UserNotFound_ShouldReturnNull()
        {
            // Arrange
            var userId = 99;
            _mockUserRepository.Setup(repo => repo.GetUserWithAddressAsync(userId)).ReturnsAsync((User)null);

            // Act
            var result = await _userService.GetUserWithDetailsAsync(userId);

            // Assert
            Assert.Null(result);
        }

        // --- Tests for RegisterUserAsync ---
        [Fact]
        public async Task RegisterUserAsync_SuccessfulRegistration_ShouldReturnNewUserId()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto { Email = "new@example.com", Password = "Password123!", FirstName = "New", LastName = "User" };
            var mappedUser = new User { Email = "new@example.com", UserName = "new@example.com", FirstName = "New", LastName = "User" };
            
            _mockMapper.Setup(m => m.Map<User>(registrationDto)).Returns(mappedUser);
            _mockUserManager.Setup(um => um.CreateAsync(mappedUser, registrationDto.Password))
                .ReturnsAsync(IdentityResult.Success)
                .Callback<User, string>((u,p) => u.Id = 123); // Simulate UserManager setting ID

            // Act
            var newUserId = await _userService.RegisterUserAsync(registrationDto);

            // Assert
            Assert.Equal(123, newUserId);
            Assert.True(mappedUser.IsActive);
            Assert.Equal(registrationDto.Email, mappedUser.UserName);
            _mockUserManager.Verify(um => um.CreateAsync(mappedUser, registrationDto.Password), Times.Once);
        }

        [Fact]
        public async Task RegisterUserAsync_UserManagerFails_ShouldThrowApplicationException()
        {
            // Arrange
            var registrationDto = new UserRegistrationDto { Email = "fail@example.com", Password = "Password123!" };
            var mappedUser = new User { Email = "fail@example.com" };
            _mockMapper.Setup(m => m.Map<User>(registrationDto)).Returns(mappedUser);
            _mockUserManager.Setup(um => um.CreateAsync(mappedUser, registrationDto.Password))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "Test failure" }));

            // Act & Assert
            var ex = await Assert.ThrowsAsync<ApplicationException>(() => _userService.RegisterUserAsync(registrationDto));
            Assert.Contains("Test failure", ex.Message);
        }


        // --- Tests for UpdateUserProfileAsync ---
        [Fact]
        public async Task UpdateUserProfileAsync_UserExists_ShouldUpdateAndSaveChanges()
        {
            // Arrange
            var userUpdateDto = new UserUpdateDto { Id = 1, FirstName = "UpdatedFirst" };
            var userEntity = CreateTestUser(1, "original@example.com", "OriginalFirst", "OriginalLast");
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userUpdateDto.Id)).ReturnsAsync(userEntity);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _userService.UpdateUserProfileAsync(userUpdateDto);

            // Assert
            _mockMapper.Verify(m => m.Map(userUpdateDto, userEntity), Times.Once); // Verify mapper was called to update entity
            _mockUserRepository.Verify(repo => repo.Update(userEntity), Times.Once); // Verify repo.Update was called
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
            Assert.True(userEntity.LastActivity > DateTime.UtcNow.AddMinutes(-1)); // Check LastActivity was updated
        }

        [Fact]
        public async Task UpdateUserProfileAsync_UserNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var userUpdateDto = new UserUpdateDto { Id = 99 };
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userUpdateDto.Id)).ReturnsAsync((User)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _userService.UpdateUserProfileAsync(userUpdateDto));
        }

        // --- Tests for UpdateUserLocationAsync ---
        [Fact]
        public async Task UpdateUserLocationAsync_UserExists_NewAddress_ShouldCreateAddressAndUpdateUser()
        {
            // Arrange
            var userId = 1;
            var locationData = new LocationUpdateDto { Latitude = 10, Longitude = 20, SearchRadius = 15, LocationName = "NewCity, NewCountry" };
            var userEntity = CreateTestUser(userId); // User initially has no address
            userEntity.Address = null; 
            var newAddressEntity = new Address(); // What mapper would produce for Address

            _mockUserRepository.Setup(repo => repo.GetUserWithAddressAsync(userId)).ReturnsAsync(userEntity);
            // Assume mapper for AddressDto to Address is not used directly here, as Address is created new.
            // If LocationUpdateDto contained an AddressDto, then mapper for that would be used.
            // The service logic creates 'new Address()' and sets props.
            
            _mockUnitOfWork.Setup(uow => uow.Users.Update(userEntity)); // User entity will be updated
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _userService.UpdateUserLocationAsync(userId, locationData);

            // Assert
            Assert.NotNull(userEntity.Address); // Address object should be created and assigned
            Assert.Equal(locationData.Latitude, userEntity.Address.Latitude);
            Assert.Equal(locationData.Longitude, userEntity.Address.Longitude);
            Assert.Equal(locationData.SearchRadius, userEntity.SearchRadius);
            Assert.Equal("NewCity", userEntity.Address.City); // From LocationName parsing
            Assert.Equal("NewCountry", userEntity.Address.Country);
            _mockUnitOfWork.Verify(uow => uow.Users.Update(userEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }
        
        [Fact]
        public async Task UpdateUserLocationAsync_UserExists_ExistingAddress_ShouldUpdateAddressAndUser()
        {
            // Arrange
            var userId = 1;
            var locationData = new LocationUpdateDto { Latitude = 12, Longitude = 22, SearchRadius = 25, LocationName = "UpdatedCity" };
            var existingAddress = new Address { Id = 5, City = "OldCity", Latitude = 10, Longitude = 20 };
            var userEntity = CreateTestUser(userId);
            userEntity.Address = existingAddress; // User has an existing address
            userEntity.AddressId = existingAddress.Id;

            _mockUserRepository.Setup(repo => repo.GetUserWithAddressAsync(userId)).ReturnsAsync(userEntity);
            _mockUnitOfWork.Setup(uow => uow.Users.Update(userEntity));
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _userService.UpdateUserLocationAsync(userId, locationData);

            // Assert
            Assert.NotNull(userEntity.Address);
            Assert.Equal(locationData.Latitude, userEntity.Address.Latitude);
            Assert.Equal(locationData.Longitude, userEntity.Address.Longitude);
            Assert.Equal("UpdatedCity", userEntity.Address.City); // City updated from LocationName
            Assert.Equal(locationData.SearchRadius, userEntity.SearchRadius); // User's search radius updated
            _mockUnitOfWork.Verify(uow => uow.Users.Update(userEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }


        // --- Tests for ChangeUserPasswordAsync ---
        [Fact]
        public async Task ChangeUserPasswordAsync_ValidRequest_ShouldReturnTrue()
        {
            // Arrange
            var userId = 1;
            var user = CreateTestUser(userId);
            _mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.ChangePasswordAsync(user, "OldPassword", "NewPassword1"))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _userService.ChangeUserPasswordAsync(userId, "OldPassword", "NewPassword1");

            // Assert
            Assert.True(result);
            _mockUserManager.Verify(um => um.ChangePasswordAsync(user, "OldPassword", "NewPassword1"), Times.Once);
        }

        [Fact]
        public async Task ChangeUserPasswordAsync_UserNotFound_ShouldReturnFalse()
        {
            // Arrange
            var userId = 99;
            _mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString())).ReturnsAsync((User)null);

            // Act
            var result = await _userService.ChangeUserPasswordAsync(userId, "OldPassword", "NewPassword1");

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ChangeUserPasswordAsync_ChangePasswordFails_ShouldReturnFalse()
        {
            // Arrange
            var userId = 1;
            var user = CreateTestUser(userId);
            _mockUserManager.Setup(um => um.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
            _mockUserManager.Setup(um => um.ChangePasswordAsync(user, "WrongOldPassword", "NewPassword1"))
                .ReturnsAsync(IdentityResult.Failed());

            // Act
            var result = await _userService.ChangeUserPasswordAsync(userId, "WrongOldPassword", "NewPassword1");

            // Assert
            Assert.False(result);
        }
        
        // --- Tests for DeactivateUserAsync ---
        [Fact]
        public async Task DeactivateUserAsync_UserExistsAndIsActive_ShouldDeactivateAndReturnTrue()
        {
            // Arrange
            var userId = 1;
            var userEntity = CreateTestUser(userId);
            userEntity.IsActive = true;

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(userEntity);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            var result = await _userService.DeactivateUserAsync(userId);

            // Assert
            Assert.True(result);
            Assert.False(userEntity.IsActive);
            _mockUserRepository.Verify(repo => repo.Update(userEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeactivateUserAsync_UserAlreadyInactive_ShouldReturnTrue()
        {
            // Arrange
            var userId = 1;
            var userEntity = CreateTestUser(userId);
            userEntity.IsActive = false; // Already inactive

            _mockUserRepository.Setup(repo => repo.GetByIdAsync(userId)).ReturnsAsync(userEntity);

            // Act
            var result = await _userService.DeactivateUserAsync(userId);

            // Assert
            Assert.True(result); // Still true as the desired state (inactive) is met
            _mockUserRepository.Verify(repo => repo.Update(It.IsAny<User>()), Times.Never); // No update needed
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never); // No save needed
        }


        
    }
}