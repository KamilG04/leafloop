using AutoMapper;
using LeafLoop.Models; // For Message, User, Transaction etc.
using LeafLoop.Repositories.Interfaces; // For IUnitOfWork, IMessageRepository, ITransactionRepository
using LeafLoop.Services; // For MessageService
using LeafLoop.Services.DTOs; // For MessageDto, MessageCreateDto etc.
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; // For NullLogger
using Moq;
using Xunit;

namespace LeafLoop.Tests.Unit.Services
{
    public class MessageServiceTests
    {
        private readonly Mock<IUnitOfWork> _mockUnitOfWork;
        private readonly Mock<IMessageRepository> _mockMessageRepository;
        private readonly Mock<ITransactionRepository> _mockTransactionRepository; // For SendMessageAsync
        private readonly Mock<IMapper> _mockMapper;
        private readonly ILogger<MessageService> _logger;
        private readonly MessageService _messageService;

        public MessageServiceTests()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMessageRepository = new Mock<IMessageRepository>();
            _mockTransactionRepository = new Mock<ITransactionRepository>();
            _mockMapper = new Mock<IMapper>();
            _logger = NullLogger<MessageService>.Instance;

            // Setup UnitOfWork mocks
            _mockUnitOfWork.Setup(uow => uow.Messages).Returns(_mockMessageRepository.Object);
            _mockUnitOfWork.Setup(uow => uow.Transactions).Returns(_mockTransactionRepository.Object);

            _messageService = new MessageService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _logger
            );
        }

        // --- Tests for GetMessageByIdAsync ---
        [Fact]
        public async Task GetMessageByIdAsync_MessageExists_ShouldReturnMappedMessageDto()
        {
            // Arrange: Mock repository to return a message entity and mapper to convert it.
            var messageId = 1;
            var messageEntity = new Message { Id = messageId, Content = "Test message" };
            var expectedDto = new MessageDto { Id = messageId, Content = "Test message" };

            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync(messageEntity);
            _mockMapper.Setup(mapper => mapper.Map<MessageDto>(messageEntity)).Returns(expectedDto);

            // Act
            var result = await _messageService.GetMessageByIdAsync(messageId);

            // Assert: Verify correct DTO is returned.
            Assert.NotNull(result);
            Assert.Equal(expectedDto.Id, result.Id);
            Assert.Equal(expectedDto.Content, result.Content);
            _mockMessageRepository.Verify(repo => repo.GetByIdAsync(messageId), Times.Once);
            _mockMapper.Verify(mapper => mapper.Map<MessageDto>(messageEntity), Times.Once);
        }

        [Fact]
        public async Task GetMessageByIdAsync_MessageDoesNotExist_ShouldReturnNull()
        {
            // Arrange: Mock repository to return null.
            var messageId = 99;
            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync((Message)null);
            _mockMapper.Setup(mapper => mapper.Map<MessageDto>(null)).Returns((MessageDto)null);


            // Act
            var result = await _messageService.GetMessageByIdAsync(messageId);

            // Assert: Verify null is returned.
            Assert.Null(result);
            _mockMessageRepository.Verify(repo => repo.GetByIdAsync(messageId), Times.Once);
        }

        // --- Tests for SendMessageAsync ---
        [Fact]
        public async Task SendMessageAsync_ValidDto_ShouldAddMessageAndReturnNewId()
        {
            // Arrange: Setup DTO, mapper, and repository AddAsync/CompleteAsync.
            var messageCreateDto = new MessageCreateDto { Content = "Hello", SenderId = 1, ReceiverId = 2 };
            var mappedMessageEntity = new Message { Content = "Hello", SenderId = 1, ReceiverId = 2 }; // Entity after mapping

            _mockMapper.Setup(m => m.Map<Message>(messageCreateDto)).Returns(mappedMessageEntity);
            // Simulate the repository/DB setting the ID on the entity after adding it
            _mockMessageRepository.Setup(repo => repo.AddAsync(mappedMessageEntity))
                .Callback<Message>(msg => msg.Id = 123) // Simulate ID assignment
                .Returns(Task.CompletedTask);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1); // Simulate successful save

            // Act
            var newMessageId = await _messageService.SendMessageAsync(messageCreateDto);

            // Assert: Verify ID, properties set by service, and mock calls.
            Assert.Equal(123, newMessageId);
            Assert.False(mappedMessageEntity.IsRead); // IsRead should be false by default
            Assert.NotEqual(default(DateTime), mappedMessageEntity.SentDate); // SentDate should be set
            _mockMapper.Verify(m => m.Map<Message>(messageCreateDto), Times.Once);
            _mockMessageRepository.Verify(repo => repo.AddAsync(mappedMessageEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
            // Transaction update logic is commented out in service, so no verify for it yet.
            _mockTransactionRepository.Verify(repo => repo.GetByIdAsync(It.IsAny<int>()), Times.Never);
        }
        
        [Fact]
        public async Task SendMessageAsync_WithTransactionId_ShouldAttemptToLoadTransaction()
        {
            // Arrange: DTO includes a TransactionId.
            var transactionId = 50;
            var messageCreateDto = new MessageCreateDto { Content = "Hello", SenderId = 1, ReceiverId = 2, TransactionId = transactionId };
            var mappedMessageEntity = new Message { Content = "Hello", SenderId = 1, ReceiverId = 2, TransactionId = transactionId };
            var mockTransaction = new Transaction { Id = transactionId };

            _mockMapper.Setup(m => m.Map<Message>(messageCreateDto)).Returns(mappedMessageEntity);
            _mockMessageRepository.Setup(repo => repo.AddAsync(mappedMessageEntity))
                .Callback<Message>(msg => msg.Id = 124)
                .Returns(Task.CompletedTask);
            // Setup transaction repository to return a transaction
            _mockTransactionRepository.Setup(repo => repo.GetByIdAsync(transactionId)).ReturnsAsync(mockTransaction);
            // We expect CompleteAsync to be called once for the message, and potentially again if transaction update logic was active.
            // Since it's commented out in the service, we expect it once for the message.
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);


            // Act
            var newMessageId = await _messageService.SendMessageAsync(messageCreateDto);

            // Assert
            Assert.Equal(124, newMessageId);
            _mockMessageRepository.Verify(repo => repo.AddAsync(mappedMessageEntity), Times.Once);
            _mockTransactionRepository.Verify(repo => repo.GetByIdAsync(transactionId), Times.Once); // Verify transaction was fetched
            // _mockUnitOfWork.Verify(uow => uow.Transactions.Update(mockTransaction), Times.Once); // Would be used if transaction update logic was active
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once); // Currently called once for message
        }


        // --- Tests for MarkMessageAsReadAsync ---
        [Fact]
        public async Task MarkMessageAsReadAsync_MessageExistsAndUserIsReceiver_ShouldMarkAsRead()
        {
            // Arrange: Message exists, current user is the receiver.
            var messageId = 1;
            var receiverId = 5;
            var messageEntity = new Message { Id = messageId, ReceiverId = receiverId, IsRead = false };

            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync(messageEntity);
            _mockUnitOfWork.Setup(uow => uow.CompleteAsync()).ReturnsAsync(1);

            // Act
            await _messageService.MarkMessageAsReadAsync(messageId, receiverId);

            // Assert: IsRead is true, repository/UoW methods called.
            Assert.True(messageEntity.IsRead);
            _mockMessageRepository.Verify(repo => repo.GetByIdAsync(messageId), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.Messages.Update(messageEntity), Times.Once);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task MarkMessageAsReadAsync_MessageNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange: Message does not exist.
            var messageId = 99;
            var userId = 5;
            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync((Message)null);

            // Act & Assert
            var ex = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _messageService.MarkMessageAsReadAsync(messageId, userId)
            );
            Assert.Equal($"Message with ID {messageId} not found", ex.Message);
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task MarkMessageAsReadAsync_UserIsNotReceiver_ShouldThrowUnauthorizedAccessException()
        {
            // Arrange: Message exists, but user is not the receiver.
            var messageId = 1;
            var receiverId = 5;
            var wrongUserId = 6; // Different user
            var messageEntity = new Message { Id = messageId, ReceiverId = receiverId, IsRead = false };

            _mockMessageRepository.Setup(repo => repo.GetByIdAsync(messageId)).ReturnsAsync(messageEntity);

            // Act & Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(
                () => _messageService.MarkMessageAsReadAsync(messageId, wrongUserId)
            );
            Assert.False(messageEntity.IsRead); // Ensure IsRead was not changed
            _mockUnitOfWork.Verify(uow => uow.CompleteAsync(), Times.Never);
        }

        // --- Tests for GetUnreadMessagesCountAsync ---
        [Fact]
        public async Task GetUnreadMessagesCountAsync_UserHasUnreadMessages_ShouldReturnCorrectCount()
        {
            // Arrange
            var userId = 1;
            var expectedUnreadCount = 5;
            _mockMessageRepository.Setup(repo => repo.GetUnreadMessagesCountAsync(userId)).ReturnsAsync(expectedUnreadCount);

            // Act
            var result = await _messageService.GetUnreadMessagesCountAsync(userId);

            // Assert
            Assert.Equal(expectedUnreadCount, result);
            _mockMessageRepository.Verify(repo => repo.GetUnreadMessagesCountAsync(userId), Times.Once);
        }

        
    }
}