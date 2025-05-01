using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LeafLoop.Models;
using LeafLoop.Services.DTOs;
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LeafLoop.Api
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All transaction endpoints require authentication
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IMessageService _messageService;
        private readonly IRatingService _ratingService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            ITransactionService transactionService,
            IMessageService messageService,
            IRatingService ratingService,
            UserManager<User> userManager,
            ILogger<TransactionsController> logger)
        {
            _transactionService = transactionService;
            _messageService = messageService;
            _ratingService = ratingService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: api/transactions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TransactionDto>>> GetUserTransactions(
            [FromQuery] bool asSeller = false)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var transactions = await _transactionService.GetTransactionsByUserAsync(user.Id, asSeller);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user transactions");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving transactions");
            }
        }

        // GET: api/transactions/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TransactionWithDetailsDto>> GetTransaction(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                var transaction = await _transactionService.GetTransactionWithDetailsAsync(id);

                if (transaction == null)
                {
                    return NotFound();
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction details. TransactionId: {TransactionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving transaction details");
            }
        }

        // POST: api/transactions
        [HttpPost]
        public async Task<ActionResult<int>> InitiateTransaction(TransactionCreateDto transactionDto)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Set the buyer ID to current user
                transactionDto.BuyerId = user.Id;

                var transactionId = await _transactionService.InitiateTransactionAsync(transactionDto);

                return CreatedAtAction(nameof(GetTransaction), new { id = transactionId }, transactionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initiating transaction");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error initiating transaction");
            }
        }

        // PUT: api/transactions/5/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTransactionStatus(int id,
            [FromBody] TransactionStatusUpdateDto statusUpdateDto)
        {
            if (id != statusUpdateDto.TransactionId)
            {
                return BadRequest("Transaction ID mismatch");
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);

                await _transactionService.UpdateTransactionStatusAsync(id, statusUpdateDto.Status, user.Id);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction status. TransactionId: {TransactionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error updating transaction status");
            }
        }

        // POST: api/transactions/5/complete
        [HttpPost("{id}/complete")]
        public async Task<IActionResult> CompleteTransaction(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                await _transactionService.CompleteTransactionAsync(id, user.Id);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing transaction. TransactionId: {TransactionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error completing transaction");
            }
        }

        // POST: api/transactions/5/cancel
        [HttpPost("{id}/cancel")]
        public async Task<IActionResult> CancelTransaction(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                await _transactionService.CancelTransactionAsync(id, user.Id);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling transaction. TransactionId: {TransactionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error cancelling transaction");
            }
        }

        // GET: api/transactions/5/messages
        [HttpGet("{id}/messages")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetTransactionMessages(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                var messages = await _messageService.GetTransactionMessagesAsync(id);
                return Ok(messages);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction messages. TransactionId: {TransactionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving transaction messages");
            }
        }

        // POST: api/transactions/5/messages
        [HttpPost("{id}/messages")]
        public async Task<ActionResult<int>> SendTransactionMessage(int id, [FromBody] TransactionMessageDto messageDto)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                // Get transaction to determine sender and receiver
                var transaction = await _transactionService.GetTransactionByIdAsync(id);

                var messageCreateDto = new MessageCreateDto
                {
                    Content = messageDto.Content,
                    SenderId = user.Id,
                    // Set receiver to the other party in the transaction
                    ReceiverId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId,
                    TransactionId = id
                };

                var messageId = await _messageService.SendMessageAsync(messageCreateDto);

                return Ok(messageId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending transaction message. TransactionId: {TransactionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error sending message");
            }
        }

        // POST: api/transactions/5/ratings
        [HttpPost("{id}/ratings")]
        public async Task<ActionResult<int>> RateTransaction(int id, [FromBody] TransactionRatingDto ratingDto)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                // Get transaction to determine who's being rated
                var transaction = await _transactionService.GetTransactionByIdAsync(id);

                // Create rating
                var ratingCreateDto = new RatingCreateDto
                {
                    Value = ratingDto.Value,
                    Comment = ratingDto.Comment,
                    RaterId = user.Id,
                    // Set rated entity to the other party in the transaction
                    RatedEntityId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId,
                    RatedEntityType = RatedEntityType.User,
                    TransactionId = id
                };

                var ratingId = await _ratingService.AddRatingAsync(ratingCreateDto);

                return Ok(ratingId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Transaction with ID {id} not found");
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rating transaction. TransactionId: {TransactionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error rating transaction");
            }
        }

        // GET: api/transactions/5/ratings
        [HttpGet("{id}/ratings")]
        public async Task<ActionResult<IEnumerable<RatingDto>>> GetTransactionRatings(int id)
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);

                // Check if user can access this transaction
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    return Forbid();
                }

                var ratings = await _ratingService.GetRatingsByTransactionAsync(id);
                return Ok(ratings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction ratings. TransactionId: {TransactionId}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving transaction ratings");
            }
        }
    }

    // Helper class for transaction status update
    public class TransactionStatusUpdateDto
    {
        public int TransactionId { get; set; }
        public TransactionStatus Status { get; set; }
    }

    // Helper class for transaction message
    public class TransactionMessageDto
    {
        public string Content { get; set; }
    }

    // Helper class for transaction rating
    public class TransactionRatingDto
    {
        public int Value { get; set; }
        public string Comment { get; set; }
    }
}