// Path: LeafLoop.Tests.Unit/Services/TransactionServiceTests.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using LeafLoop.Models; // For Transaction, Item, User, TransactionStatus, etc.
using LeafLoop.Repositories.Interfaces; // For IUnitOfWork, ITransactionRepository, IItemRepository, IUserRepository
using LeafLoop.Services;              // For TransactionService
using LeafLoop.Services.DTOs;         // For TransactionDto, TransactionCreateDto, ItemDto etc.
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; // For NullLogger
using Moq;
using Xunit;

namespace LeafLoop.Tests.Unit.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<ITransactionRepository> _mockTransactionRepository;
        private readonly Mock<IItemRepository> _mockItemRepository;
        private readonly Mock<IUserRepository> _mockUserRepository; // For EcoScore updates
        private readonly Mock<IMapper> _mockMapper;
        private readonly ILogger<TransactionService> _logger;
        private readonly TransactionService _transactionService;

        public TransactionServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockTransactionRepository = new Mock<ITransactionRepository>();
            _mockItemRepository = new Mock<IItemRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = NullLogger<TransactionService>.Instance;

            // Setup UnitOfWork mocks
            _mockUnitOfWork.Setup(uow => uow.Transactions).Returns(_mockTransactionRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Items).Returns(_mockItemRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Users).Returns(_mockUserRepository.Object);

            // Mock ExecuteInTransactionAsync to simply execute the passed function
            _mockUnitOfWork.Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<Func<Task<int>>>()))
                .Returns<Func<Task<int>>>(async (func) => await func());
            _mockUnitOfWork.Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
                .Returns<Func<Task>>(async (func) => await func());

            _transactionService = new TransactionService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _logger
            );
        }

        // --- Tests for GetTransactionByIdAsync ---
        [Fact]
        public async Task GetTransactionByIdAsync_TransactionExists_ShouldReturnMappedDto()
        {
            // Arrange: Mock repository to return a transaction, mapper to convert.
            var transactionId = 1;
            var transactionEntity = new Transaction { Id = transactionId, ItemId = 1, BuyerId = 2, SellerId = 3 };
            var expectedDto = new TransactionDto { Id = transactionId };

            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(transactionId)).ReturnsAsync(transactionEntity);
            _mockMapper.Setup(mapper => mapper.Map<TransactionDto>(transactionEntity)).Returns(expectedDto);

            // Act
            var result = await _transactionService.GetTransactionByIdAsync(transactionId);

            // Assert: Correct DTO returned, mocks called.
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Id, result.Id);
            _mockTransactionRepository.Verify(repo => repo.GetByIdAsync(transactionId), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<TransactionDto>(transactionEntity), Times.Once);
        }

        [Fact]
        public async Task GetTransactionByIdAsync_TransactionDoesNotExist_ShouldReturnNull()
        {
            // Arrange: Mock repository to return null.
            var transactionId = 99;
            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(transactionId)).ReturnsAsync((Transaction)null);
            _mockMapper.Setup(mapper => mapper.Map<TransactionDto>(null)).Returns((TransactionDto)null);


            // Act
            var result = await _transactionService.GetTransactionByIdAsync(transactionId);

            // Assert: Null is returned.
            Assert.Null(result);
        }

        // --- Tests for InitiateTransactionAsync ---
        [Fact]
        public async Task InitiateTransactionAsync_ValidRequest_ShouldCreateTransactionAndReturnId()
        {
            // Arrange
            var buyerId = 1;
            var sellerId = 2;
            var itemId = 10;
            var createDto = new TransactionCreateDto { ItemId = itemId, Type = TransactionType.Donation }; // Assuming TransactionType enum
            var itemEntity = new Item { Id = itemId, UserId = sellerId, IsAvailable = true };
            
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);
            _mockTransactionRepository.Setup(repo => repo.SingleOrDefaultAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync((Transaction)null); // No existing pending transaction

            // Simulate ID assignment after AddAsync
            _mockTransactionRepository.Setup(repo => repo.AddAsync(It.IsAny<Transaction>()))
                .Callback<Transaction>(t => t.Id = 123)
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            var newTransactionId = await _transactionService.InitiateTransactionAsync(createDto, buyerId);

            // Assert
            Assert.Equal(123, newTransactionId);
            Assert.False(itemEntity.IsAvailable); // Item should be marked as unavailable
            _mockItemRepository.Verify(repo => repo.GetByIdAsync(itemId), Times.Once);
            _mockTransactionRepository.Verify(repo => repo.AddAsync(It.Is<Transaction>(t => 
                t.ItemId == itemId &&
                t.BuyerId == buyerId &&
                t.SellerId == sellerId &&
                t.Status == TransactionStatus.Pending 
            )), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once); // Within ExecuteInTransactionAsync
        }

        [Fact]
        public async Task InitiateTransactionAsync_ItemNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var createDto = new TransactionCreateDto { ItemId = 99 };
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(createDto.ItemId)).ReturnsAsync((Item)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _transactionService.InitiateTransactionAsync(createDto, 1)
            );
        }

        [Fact]
        public async Task InitiateTransactionAsync_ItemNotAvailable_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var createDto = new TransactionCreateDto { ItemId = 10 };
            var itemEntity = new Item { Id = 10, IsAvailable = false }; // Item is not available
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(createDto.ItemId)).ReturnsAsync(itemEntity);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.InitiateTransactionAsync(createDto, 1)
            );
        }
        
        [Fact]
        public async Task InitiateTransactionAsync_BuyerIsOwner_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var userId = 1; // Buyer is also the seller
            var itemId = 10;
            var createDto = new TransactionCreateDto { ItemId = itemId };
            var itemEntity = new Item { Id = itemId, UserId = userId, IsAvailable = true };
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.InitiateTransactionAsync(createDto, userId)
            );
        }

        [Fact]
        public async Task InitiateTransactionAsync_ExistingPendingTransaction_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var buyerId = 1;
            var itemId = 10;
            var createDto = new TransactionCreateDto { ItemId = itemId };
            var itemEntity = new Item { Id = itemId, UserId = 2, IsAvailable = true };
            var existingTransaction = new Transaction { Id = 50, ItemId = itemId, BuyerId = buyerId, Status = TransactionStatus.Pending };

            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);
            _mockTransactionRepository.Setup(repo => repo.SingleOrDefaultAsync(It.IsAny<Expression<Func<Transaction, bool>>>()))
                .ReturnsAsync(existingTransaction);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.InitiateTransactionAsync(createDto, buyerId)
            );
        }


        // --- Tests for UpdateTransactionStatusAsync ---
        [Fact]
        public async Task UpdateTransactionStatusAsync_ValidUpdate_ShouldUpdateStatus()
        {
            // Arrange
            var transactionId = 1;
            var userId = 5; // Assume user is part of transaction (e.g., buyer)
            var newStatus = TransactionStatus.InProgress;
            var transactionEntity = new Transaction { Id = transactionId, BuyerId = userId, SellerId = 6, Status = TransactionStatus.Pending };

            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(transactionId)).ReturnsAsync(transactionEntity);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _transactionService.UpdateTransactionStatusAsync(transactionId, newStatus, userId);

            // Assert
            Assert.Equal(newStatus, transactionEntity.Status);
            Assert.NotEqual(default(DateTime), transactionEntity.LastUpdateDate);
            if (newStatus == TransactionStatus.Completed || newStatus == TransactionStatus.Cancelled)
            {
                Assert.NotNull(transactionEntity.EndDate);
            }
            _mockTransactionRepository.Verify(repo => repo.GetByIdAsync(transactionId), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateTransactionStatusAsync_TransactionNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((Transaction)null);

            // Act & Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _transactionService.UpdateTransactionStatusAsync(99, TransactionStatus.InProgress, 1)
            );
        }

        [Fact]
        public async Task UpdateTransactionStatusAsync_UserNotAuthorized_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange
            var transaction = new Transaction { Id = 1, BuyerId = 5, SellerId = 6, Status = TransactionStatus.Pending };
            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(transaction);
            var unauthorizedUserId = 7; // Not buyer or seller

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _transactionService.UpdateTransactionStatusAsync(1, TransactionStatus.InProgress, unauthorizedUserId)
            );
        }

        [Theory]
        [InlineData(TransactionStatus.Pending, TransactionStatus.Completed)] // Invalid
        [InlineData(TransactionStatus.InProgress, TransactionStatus.Pending)] // Invalid
        [InlineData(TransactionStatus.Completed, TransactionStatus.InProgress)] // Invalid
        [InlineData(TransactionStatus.Cancelled, TransactionStatus.InProgress)] // Invalid
        public async Task UpdateTransactionStatusAsync_InvalidTransition_ShouldThrowInvalidOperationException(
            TransactionStatus currentStatus, TransactionStatus newStatus)
        {
            // Arrange
            var transaction = new Transaction { Id = 1, BuyerId = 5, SellerId = 6, Status = currentStatus };
            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(transaction);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.UpdateTransactionStatusAsync(1, newStatus, 5)
            );
        }


        // --- Tests for ConfirmTransactionCompletionAsync ---
        [Fact]
        public async Task ConfirmTransactionCompletionAsync_FirstPartyConfirms_ShouldUpdateConfirmationFlag()
        {
            // Arrange
            var transactionId = 1;
            var buyerId = 5;
            var sellerId = 6;
            var transactionEntity = new Transaction { 
                Id = transactionId, BuyerId = buyerId, SellerId = sellerId, 
                Status = TransactionStatus.InProgress, BuyerConfirmed = false, SellerConfirmed = false 
            };
            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(transactionId)).ReturnsAsync(transactionEntity);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act: Buyer confirms
            await _transactionService.ConfirmTransactionCompletionAsync(transactionId, buyerId);

            // Assert
            Assert.True(transactionEntity.BuyerConfirmed);
            Assert.False(transactionEntity.SellerConfirmed);
            Assert.Equal(TransactionStatus.InProgress, transactionEntity.Status); // Status should not change yet
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task ConfirmTransactionCompletionAsync_SecondPartyConfirms_ShouldCompleteTransactionAndUpdateEcoScores()
        {
            // Arrange
            var transactionId = 1;
            var buyerId = 5;
            var sellerId = 6;
            var itemId = 10;
            var transactionEntity = new Transaction { 
                Id = transactionId, ItemId = itemId, BuyerId = buyerId, SellerId = sellerId, 
                Status = TransactionStatus.InProgress, BuyerConfirmed = true, SellerConfirmed = false // Buyer already confirmed
            };
            var itemEntity = new Item { Id = itemId, IsAvailable = true };
            var sellerEntity = new User { Id = sellerId, EcoScore = 10 };
            var buyerEntity = new User { Id = buyerId, EcoScore = 20 };

            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(transactionId)).ReturnsAsync(transactionEntity);
            _mockItemRepository.Setup(repo => repo.GetByIdAsync(itemId)).ReturnsAsync(itemEntity);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(sellerId)).ReturnsAsync(sellerEntity);
            _mockUserRepository.Setup(repo => repo.GetByIdAsync(buyerId)).ReturnsAsync(buyerEntity);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1); // One for main transaction, one for EcoScore

            // Act: Seller confirms
            await _transactionService.ConfirmTransactionCompletionAsync(transactionId, sellerId);

            // Assert
            Assert.True(transactionEntity.SellerConfirmed);
            Assert.True(transactionEntity.BuyerConfirmed);
            Assert.Equal(TransactionStatus.Completed, transactionEntity.Status);
            Assert.NotNull(transactionEntity.EndDate);
            Assert.False(itemEntity.IsAvailable); // Item marked as unavailable
            Assert.Equal(15, sellerEntity.EcoScore); // 10 + 5
            Assert.Equal(23, buyerEntity.EcoScore); // 20 + 3
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Exactly(2)); // Transaction save + EcoScore save
        }
        
        [Fact]
        public async Task ConfirmTransactionCompletionAsync_UserAlreadyConfirmed_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var transactionId = 1;
            var buyerId = 5;
            var transactionEntity = new Transaction { 
                Id = transactionId, BuyerId = buyerId, SellerId = 6, 
                Status = TransactionStatus.InProgress, BuyerConfirmed = true // Buyer already confirmed
            };
            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(transactionId)).ReturnsAsync(transactionEntity);

            // Act & Assert: Buyer tries to confirm again
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => _transactionService.ConfirmTransactionCompletionAsync(transactionId, buyerId)
            );
        }


       
    }
}