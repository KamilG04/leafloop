using AutoMapper;
using LeafLoop.Models;
using LeafLoop.Models.API; // For ApiResponse and ApiResponse<T>
using LeafLoop.Repositories.Interfaces; // For IUnitOfWork
using LeafLoop.Services.DTOs; // For Transaction DTOs, MessageDto, RatingDto
using LeafLoop.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // For StatusCodes
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic; // For IEnumerable
using System.Linq; // For Count() and ModelState error logging
using System.Security.Claims; // For ClaimTypes
using System.Threading.Tasks; // For Task

namespace LeafLoop.Api
{
    /// <summary>
    /// Manages transactions between users for items.
    /// All actions require authentication according to the 'ApiAuthPolicy'.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "ApiAuthPolicy")] 
    [Produces("application/json")]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly IMessageService _messageService;
        private readonly IRatingService _ratingService;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<TransactionsController> _logger;
        private readonly IUnitOfWork _unitOfWork; 
        private readonly IMapper _mapper;         

        public TransactionsController(
            ITransactionService transactionService,
            IMessageService messageService,
            IRatingService ratingService,
            UserManager<User> userManager,
            ILogger<TransactionsController> logger,
            IUnitOfWork unitOfWork,
            IMapper mapper)
        {
            _transactionService = transactionService ?? throw new ArgumentNullException(nameof(transactionService));
            _messageService = messageService ?? throw new ArgumentNullException(nameof(messageService));
            _ratingService = ratingService ?? throw new ArgumentNullException(nameof(ratingService));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Initiates a new transaction for an item (e.g., a user expresses intent to buy/borrow).
        /// </summary>
        /// <param name="transactionCreateDto">The data for the new transaction, including ItemId and Type.</param>
        /// <returns>The newly created transaction details if successful.</returns>
        /// <response code="201">Transaction initiated successfully. Returns the created transaction and its location.</response>
        /// <response code="400">If the transaction data is invalid (e.g., invalid type, item not available for transaction, self-transaction).</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="404">If the item specified in the DTO is not found.</response>
        /// <response code="500">If an internal server error occurs during transaction initiation.</response>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<TransactionDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> InitiateTransaction([FromBody] TransactionCreateDto transactionCreateDto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API InitiateTransaction START for ItemID: {ItemId} by UserID Claim: {UserIdClaim}. DTO: {@TransactionCreateDto}", 
                transactionCreateDto?.ItemId, userIdClaim ?? "N/A", transactionCreateDto);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API InitiateTransaction BAD_REQUEST: Invalid model state by UserID Claim: {UserIdClaim}. Errors: {@ModelStateErrors}", 
                    userIdClaim ?? "N/A", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }
            if (!Enum.IsDefined(typeof(TransactionType), transactionCreateDto.Type))
            {
                _logger.LogWarning("API InitiateTransaction BAD_REQUEST: Invalid transaction type '{TransactionType}' by UserID Claim: {UserIdClaim}.", 
                    transactionCreateDto.Type, userIdClaim ?? "N/A");
                return this.ApiBadRequest($"Invalid transaction type value: {transactionCreateDto.Type}.");
            }

            try
            {
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("API InitiateTransaction UNAUTHORIZED: Could not parse UserID from claims: {UserIdClaim}", userIdClaim);
                    return this.ApiUnauthorized("Unable to identify user.");
                }

                var transactionId = await _transactionService.InitiateTransactionAsync(transactionCreateDto, userId);
                var createdTransaction = await _transactionService.GetTransactionByIdAsync(transactionId); 

                 if (createdTransaction == null) {
                     _logger.LogError("API InitiateTransaction ERROR: Transaction created (ID: {TransactionId}) but GetTransactionByIdAsync returned null. UserID: {UserId}", 
                        transactionId, userId);
                     return this.ApiInternalError("Transaction created but failed to retrieve details.");
                 }
                _logger.LogInformation("API InitiateTransaction SUCCESS. New TransactionID: {TransactionId} for ItemID: {ItemId}, UserID: {UserId}", 
                    transactionId, createdTransaction.ItemId, userId);
                return this.ApiCreatedAtAction(
                    createdTransaction, 
                    nameof(GetTransaction), 
                    this.ControllerContext.ActionDescriptor.ControllerName, // Dynamically get controller name         
                    new { id = transactionId } 
                );
            }
            catch (KeyNotFoundException ex) 
            {
                _logger.LogWarning(ex, "API InitiateTransaction NOT_FOUND for ItemID: {ItemId} by UserID Claim: {UserIdClaim}", 
                    transactionCreateDto?.ItemId, userIdClaim ?? "N/A");
                return this.ApiNotFound(ex.Message); 
            }
            catch (InvalidOperationException ex) 
            {
                _logger.LogWarning(ex, "API InitiateTransaction BAD_REQUEST (Invalid Op) for ItemID: {ItemId} by UserID Claim: {UserIdClaim}", 
                    transactionCreateDto?.ItemId, userIdClaim ?? "N/A");
                return this.ApiBadRequest(ex.Message); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API InitiateTransaction ERROR for ItemID: {ItemId} by UserID Claim: {UserIdClaim}", 
                    transactionCreateDto?.ItemId, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error initiating transaction.", ex);
            }
        }

        /// <summary>
        /// Retrieves transactions for the authenticated user, either as a buyer or a seller.
        /// </summary>
        /// <param name="asSeller">If true, retrieves transactions where the user is the seller. If false (default), retrieves transactions where the user is the buyer.</param>
        /// <returns>A list of the user's transactions.</returns>
        /// <response code="200">Successfully retrieved transactions.</response>
        /// <response code="401">If the user is not authenticated or cannot be identified.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<TransactionDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserTransactions([FromQuery] bool asSeller = false)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API GetUserTransactions START for UserID Claim: {UserIdClaim}, AsSeller: {AsSeller}", userIdClaim ?? "N/A", asSeller);
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API GetUserTransactions UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}", userIdClaim ?? "N/A");
                    return this.ApiUnauthorized("User not found.");
                }

                var transactions = await _transactionService.GetTransactionsByUserAsync(user.Id, asSeller);
                _logger.LogInformation("API GetUserTransactions SUCCESS for UserID: {UserId}, AsSeller: {AsSeller}. Count: {Count}", 
                    user.Id, asSeller, transactions?.Count() ?? 0);
                return this.ApiOk(transactions ?? new List<TransactionDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetUserTransactions ERROR for UserID Claim: {UserIdClaim}, AsSeller: {AsSeller}", userIdClaim ?? "N/A", asSeller);
                return this.ApiInternalError("Error retrieving transactions.", ex);
            }
        }

        /// <summary>
        /// Retrieves a specific transaction by its ID, including detailed information.
        /// The authenticated user must be a party to the transaction (buyer or seller).
        /// </summary>
        /// <param name="id">The ID of the transaction to retrieve.</param>
        /// <returns>The detailed information for the specified transaction.</returns>
        /// <response code="200">Successfully retrieved transaction details.</response>
        /// <response code="400">If the transaction ID is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not a party to this transaction.</response>
        /// <response code="404">If the transaction with the specified ID is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}", Name = "GetTransaction")] 
        [ProducesResponseType(typeof(ApiResponse<TransactionWithDetailsDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransaction(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API GetTransaction START for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", id, userIdClaim ?? "N/A");

            if (id <= 0)
            {
                _logger.LogWarning("API GetTransaction BAD_REQUEST: Invalid Transaction ID: {TransactionId}", id);
                return this.ApiBadRequest("Invalid Transaction ID.");
            }
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API GetTransaction UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}. TransactionID: {TransactionId}", 
                        userIdClaim ?? "N/A", id);
                    return this.ApiUnauthorized("User not found.");
                }

                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    _logger.LogWarning("API GetTransaction FORBIDDEN: UserID {UserId} not authorized for TransactionID {TransactionId}.", user.Id, id);
                    return this.ApiForbidden("You are not authorized to view this transaction.");
                }

                var transaction = await _transactionService.GetTransactionWithDetailsAsync(id);
                if (transaction == null)
                {
                    _logger.LogWarning("API GetTransaction NOT_FOUND: TransactionID {TransactionId} not found for UserID {UserId}.", id, user.Id);
                    return this.ApiNotFound($"Transaction with ID {id} not found.");
                }
                _logger.LogInformation("API GetTransaction SUCCESS for TransactionID: {TransactionId}, UserID: {UserId}", id, user.Id);
                return this.ApiOk(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetTransaction ERROR for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", id, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error retrieving transaction details.", ex);
            }
        }

        /// <summary>
        /// Updates the status of a transaction (e.g., accept, decline, ship).
        /// The authenticated user must be a party to the transaction and authorized to perform the status change.
        /// </summary>
        /// <param name="id">The ID of the transaction to update.</param>
        /// <param name="statusUpdateDto">The DTO containing the new status.</param>
        /// <returns>A 200 OK response with a success message indicating the new status.</returns>
        /// <response code="200">Transaction status updated successfully.</response>
        /// <response code="400">If the transaction ID or new status is invalid, or the status transition is not allowed.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to update this transaction's status.</response>
        /// <response code="404">If the transaction is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPut("{id:int}/status")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)] 
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTransactionStatus(int id, [FromBody] TransactionStatusUpdateDto statusUpdateDto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API UpdateTransactionStatus START for TransactionID: {TransactionId}, NewStatus: {NewStatus}, UserID Claim: {UserIdClaim}", 
                id, statusUpdateDto?.Status, userIdClaim ?? "N/A");

            if (id <= 0)
            {
                _logger.LogWarning("API UpdateTransactionStatus BAD_REQUEST: Invalid Transaction ID: {TransactionId}", id);
                return this.ApiBadRequest("Invalid Transaction ID.");
            }
            if (statusUpdateDto == null || !Enum.IsDefined(typeof(TransactionStatus), statusUpdateDto.Status)) 
            {
                _logger.LogWarning("API UpdateTransactionStatus BAD_REQUEST: Invalid transaction status value: {StatusValue} for TransactionID: {TransactionId}", 
                    statusUpdateDto?.Status, id);
                return this.ApiBadRequest($"Invalid transaction status value: {statusUpdateDto?.Status}.");
            }
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API UpdateTransactionStatus BAD_REQUEST: Invalid model state for TransactionID: {TransactionId}. Errors: {@ModelStateErrors}", 
                    id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API UpdateTransactionStatus UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}. TransactionID: {TransactionId}", 
                        userIdClaim ?? "N/A", id);
                    return this.ApiUnauthorized("User not found.");
                }

                await _transactionService.UpdateTransactionStatusAsync(id, statusUpdateDto.Status, user.Id);
                _logger.LogInformation("API UpdateTransactionStatus SUCCESS for TransactionID: {TransactionId}, NewStatus: {NewStatus}, UserID: {UserId}", 
                    id, statusUpdateDto.Status, user.Id);
                return this.ApiOk($"Transaction status updated to {statusUpdateDto.Status}.");
            }
            catch (KeyNotFoundException) 
            {
                _logger.LogWarning("API UpdateTransactionStatus NOT_FOUND: TransactionID {TransactionId} not found for UserID Claim: {UserIdClaim}.", 
                    id, userIdClaim ?? "N/A");
                return this.ApiNotFound($"Transaction with ID {id} not found."); 
            }
            catch (UnauthorizedAccessException) 
            {
                _logger.LogWarning("API UpdateTransactionStatus FORBIDDEN: UserID Claim {UserIdClaim} not authorized for TransactionID {TransactionId}.", 
                    userIdClaim ?? "N/A", id);
                return this.ApiForbidden("You are not authorized to update this transaction status."); 
            }
            catch (InvalidOperationException ex) 
            {
                _logger.LogWarning(ex, "API UpdateTransactionStatus BAD_REQUEST (Invalid Op) for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiBadRequest(ex.Message); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API UpdateTransactionStatus ERROR for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error updating transaction status.", ex);
            }
        }

        /// <summary>
        /// Marks a transaction as completed by the authenticated user.
        /// This might involve one or both parties confirming completion depending on the transaction flow.
        /// </summary>
        /// <param name="id">The ID of the transaction to complete.</param>
        /// <returns>A 200 OK response with a message indicating the outcome of the completion attempt.</returns>
        /// <response code="200">Transaction completion process initiated/confirmed successfully.</response>
        /// <response code="400">If the transaction ID is invalid or the transaction cannot be completed at this stage.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to complete this transaction.</response>
        /// <response code="404">If the transaction is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("{id:int}/complete")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CompleteTransaction(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API CompleteTransaction START for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", id, userIdClaim ?? "N/A");

            if (id <= 0)
            {
                _logger.LogWarning("API CompleteTransaction BAD_REQUEST: Invalid Transaction ID: {TransactionId}", id);
                return this.ApiBadRequest("Invalid Transaction ID.");
            }
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                     _logger.LogWarning("API CompleteTransaction UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}. TransactionID: {TransactionId}", 
                        userIdClaim ?? "N/A", id);
                    return this.ApiUnauthorized("User not found.");
                }
                // The service method should handle the logic of completion.
                // Assuming ConfirmTransactionCompletionAsync is the correct method name from previous context.
                // If the service returns a detailed result, it should be used to form the message.
                await _transactionService.ConfirmTransactionCompletionAsync(id, user.Id); 
                _logger.LogInformation("API CompleteTransaction SUCCESS for TransactionID: {TransactionId}, UserID: {UserId}", id, user.Id);
                // TODO: The response message could be more dynamic based on the actual state after the service call (e.g., if it auto-completes or waits for both parties).
                // For now, a generic success message.
                return this.ApiOk("Transaction completion process initiated successfully.");
            }
            catch (KeyNotFoundException) 
            {
                _logger.LogWarning("API CompleteTransaction NOT_FOUND: TransactionID {TransactionId} not found for UserID Claim: {UserIdClaim}.", 
                    id, userIdClaim ?? "N/A");
                return this.ApiNotFound($"Transaction with ID {id} not found."); 
            }
            catch (UnauthorizedAccessException) 
            {
                _logger.LogWarning("API CompleteTransaction FORBIDDEN: UserID Claim {UserIdClaim} not authorized for TransactionID {TransactionId}.", 
                    userIdClaim ?? "N/A", id);
                return this.ApiForbidden("You are not authorized to complete this transaction."); 
            }
            catch (InvalidOperationException ex) 
            {
                _logger.LogWarning(ex, "API CompleteTransaction BAD_REQUEST (Invalid Op) for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiBadRequest(ex.Message); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API CompleteTransaction ERROR for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error completing transaction.", ex);
            }
        }

        /// <summary>
        /// Cancels a transaction by the authenticated user.
        /// Applicable if the transaction is in a state that allows cancellation by one of the parties.
        /// </summary>
        /// <param name="id">The ID of the transaction to cancel.</param>
        /// <returns>A 200 OK response with a success message.</returns>
        /// <response code="200">Transaction cancelled successfully.</response>
        /// <response code="400">If the transaction ID is invalid or the transaction cannot be cancelled at this stage.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to cancel this transaction.</response>
        /// <response code="404">If the transaction is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("{id:int}/cancel")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelTransaction(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API CancelTransaction START for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", id, userIdClaim ?? "N/A");
            if (id <= 0)
            {
                _logger.LogWarning("API CancelTransaction BAD_REQUEST: Invalid Transaction ID: {TransactionId}", id);
                return this.ApiBadRequest("Invalid Transaction ID.");
            }
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API CancelTransaction UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}. TransactionID: {TransactionId}", 
                        userIdClaim ?? "N/A", id);
                    return this.ApiUnauthorized("User not found.");
                }
                await _transactionService.CancelTransactionAsync(id, user.Id);
                _logger.LogInformation("API CancelTransaction SUCCESS for TransactionID: {TransactionId}, UserID: {UserId}", id, user.Id);
                return this.ApiOk("Transaction cancelled successfully.");
            }
            catch (KeyNotFoundException) 
            {
                _logger.LogWarning("API CancelTransaction NOT_FOUND: TransactionID {TransactionId} not found for UserID Claim: {UserIdClaim}.", 
                    id, userIdClaim ?? "N/A");
                return this.ApiNotFound($"Transaction with ID {id} not found."); 
            }
            catch (UnauthorizedAccessException) 
            {
                _logger.LogWarning("API CancelTransaction FORBIDDEN: UserID Claim {UserIdClaim} not authorized for TransactionID {TransactionId}.", 
                    userIdClaim ?? "N/A", id);
                return this.ApiForbidden("You are not authorized to cancel this transaction."); 
            }
            catch (InvalidOperationException ex) 
            {
                 _logger.LogWarning(ex, "API CancelTransaction BAD_REQUEST (Invalid Op) for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiBadRequest(ex.Message); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API CancelTransaction ERROR for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error cancelling transaction.", ex);
            }
        }

        /// <summary>
        /// Retrieves messages for a specific transaction.
        /// The authenticated user must be a party to the transaction.
        /// </summary>
        /// <param name="id">The ID of the transaction.</param>
        /// <returns>A list of messages for the transaction.</returns>
        /// <response code="200">Successfully retrieved transaction messages.</response>
        /// <response code="400">If the transaction ID is invalid.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not a party to this transaction.</response>
        /// <response code="404">If the transaction is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}/messages")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<MessageDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransactionMessages(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API GetTransactionMessages START for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", id, userIdClaim ?? "N/A");
            if (id <= 0)
            {
                _logger.LogWarning("API GetTransactionMessages BAD_REQUEST: Invalid Transaction ID: {TransactionId}", id);
                return this.ApiBadRequest("Invalid Transaction ID.");
            }
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API GetTransactionMessages UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}. TransactionID: {TransactionId}", 
                        userIdClaim ?? "N/A", id);
                    return this.ApiUnauthorized("User not found.");
                }
                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    _logger.LogWarning("API GetTransactionMessages FORBIDDEN: UserID {UserId} not authorized for TransactionID {TransactionId}.", user.Id, id);
                    return this.ApiForbidden("You are not authorized to view messages for this transaction.");
                }

                var messages = await _messageService.GetTransactionMessagesAsync(id);
                _logger.LogInformation("API GetTransactionMessages SUCCESS for TransactionID: {TransactionId}, UserID: {UserId}. Message count: {Count}", 
                    id, user.Id, messages?.Count() ?? 0);
                return this.ApiOk(messages ?? new List<MessageDto>());
            }
            catch (KeyNotFoundException) 
            {
                _logger.LogWarning("API GetTransactionMessages NOT_FOUND: TransactionID {TransactionId} not found for UserID Claim: {UserIdClaim}.", 
                    id, userIdClaim ?? "N/A");
                return this.ApiNotFound($"Transaction with ID {id} not found.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetTransactionMessages ERROR for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error retrieving transaction messages.", ex);
            }
        }

        /// <summary>
        /// Sends a message related to a specific transaction.
        /// The authenticated user must be a party to the transaction.
        /// </summary>
        /// <param name="id">The ID of the transaction.</param>
        /// <param name="messageDto">The DTO containing the message content.</param>
        /// <returns>The details of the sent message if successfully retrieved, otherwise a success confirmation.</returns>
        /// <response code="200">Message sent successfully. May return message details or a simple success message.</response>
        /// <response code="400">If transaction ID is invalid, message content is empty, or transaction is not in a state to accept messages.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not authorized to send messages for this transaction.</response>
        /// <response code="404">If the transaction is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("{id:int}/messages")]
        [ProducesResponseType(typeof(ApiResponse<MessageDto>), StatusCodes.Status200OK)] 
        // One ProducesResponseType for 200 is enough; the description can clarify variations.
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> SendTransactionMessage(int id, [FromBody] TransactionMessageDto messageDto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API SendTransactionMessage START for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}. Content Preview: '{ContentPreview}'", 
                id, userIdClaim ?? "N/A", messageDto?.Content?.Substring(0, Math.Min(messageDto.Content.Length, 50)));

            if (id <= 0)
            {
                _logger.LogWarning("API SendTransactionMessage BAD_REQUEST: Invalid Transaction ID: {TransactionId}", id);
                return this.ApiBadRequest("Invalid Transaction ID.");
            }
            if (messageDto == null || string.IsNullOrWhiteSpace(messageDto.Content))
            {
                _logger.LogWarning("API SendTransactionMessage BAD_REQUEST: Message content cannot be empty for TransactionID: {TransactionId}", id);
                return this.ApiBadRequest("Message content cannot be empty.");
            }
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API SendTransactionMessage BAD_REQUEST: Invalid model state for TransactionID: {TransactionId}. Errors: {@ModelStateErrors}", 
                    id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API SendTransactionMessage UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}. TransactionID: {TransactionId}", 
                        userIdClaim ?? "N/A", id);
                    return this.ApiUnauthorized("User not found.");
                }

                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    _logger.LogWarning("API SendTransactionMessage FORBIDDEN: UserID {UserId} not authorized for TransactionID {TransactionId}.", user.Id, id);
                    return this.ApiForbidden("You are not authorized to send messages for this transaction.");
                }

                var transaction = await _transactionService.GetTransactionByIdAsync(id); 
                if (transaction == null)
                {
                    _logger.LogWarning("API SendTransactionMessage NOT_FOUND: TransactionID {TransactionId} not found for UserID {UserId}.", id, user.Id);
                    return this.ApiNotFound($"Transaction with ID {id} not found.");
                }

                if (transaction.Status != TransactionStatus.Pending && transaction.Status != TransactionStatus.InProgress)
                {
                    _logger.LogWarning("API SendTransactionMessage BAD_REQUEST: Cannot send messages for TransactionID {TransactionId} with status '{Status}'.", id, transaction.Status);
                    return this.ApiBadRequest($"Cannot send messages for a transaction with status '{transaction.Status}'.");
                }

                var messageCreateDto = new MessageCreateDto
                {
                    Content = messageDto.Content.Trim(),
                    SenderId = user.Id,
                    ReceiverId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId,
                    TransactionId = id
                };

                var messageId = await _messageService.SendMessageAsync(messageCreateDto);
                _logger.LogInformation("Message sent via service. New MessageID: {MessageId} for TransactionID: {TransactionId}, SenderID: {SenderId}", 
                    messageId, id, user.Id);

                var createdMessage = await _messageService.GetMessageByIdAsync(messageId);
                if (createdMessage == null)
                {
                    _logger.LogError("API SendTransactionMessage ERROR: Message sent (ID: {MessageId}) but could not be retrieved for TransactionID {TransactionId}.", messageId, id);
                    return this.ApiOk("Message sent, but an issue occurred retrieving its details."); 
                }
                _logger.LogInformation("API SendTransactionMessage SUCCESS. MessageID: {MessageId}, TransactionID: {TransactionId}", messageId, id);
                return this.ApiOk(createdMessage, "Message sent successfully."); 
            }
            catch (KeyNotFoundException) 
            {
                _logger.LogWarning("API SendTransactionMessage NOT_FOUND: TransactionID {TransactionId} not found for UserID Claim: {UserIdClaim}.", 
                    id, userIdClaim ?? "N/A");
                return this.ApiNotFound($"Transaction with ID {id} not found."); 
            }
            catch (UnauthorizedAccessException) 
            {
                _logger.LogWarning("API SendTransactionMessage FORBIDDEN: UserID Claim {UserIdClaim} not authorized for TransactionID {TransactionId}.", 
                    userIdClaim ?? "N/A", id);
                return this.ApiForbidden("You are not authorized for this transaction."); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API SendTransactionMessage ERROR for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error sending message.", ex);
            }
        }

        /// <summary>
        /// Submits a rating for a completed transaction by the authenticated user for the other party in the transaction.
        /// </summary>
        /// <param name="id">The ID of the transaction to rate.</param>
        /// <param name="ratingDto">The rating data (value and optional comment).</param>
        /// <returns>The newly created rating details.</returns>
        /// <response code="201">Rating submitted successfully. Returns the created rating and its location.</response>
        /// <response code="400">If transaction ID is invalid, rating data is invalid, transaction not completed, or user has already rated this transaction for the other party.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user was not part of this transaction or is attempting to rate themselves.</response>
        /// <response code="404">If the transaction is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("{id:int}/ratings")]
        [ProducesResponseType(typeof(ApiResponse<RatingDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RateTransaction(int id, [FromBody] TransactionRatingDto ratingDto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API RateTransaction START for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}. RatingValue: {RatingValue}",
                id, userIdClaim ?? "N/A", ratingDto?.Value);

            if (id <= 0)
            {
                _logger.LogWarning("API RateTransaction BAD_REQUEST: Invalid Transaction ID: {TransactionId}", id);
                return this.ApiBadRequest("Invalid Transaction ID.");
            }
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("API RateTransaction BAD_REQUEST: Invalid model state for TransactionID: {TransactionId}. Errors: {@ModelStateErrors}", 
                    id, ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ApiBadRequest(ModelState);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API RateTransaction UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}. TransactionID: {TransactionId}", 
                        userIdClaim ?? "N/A", id);
                    return this.ApiUnauthorized("User not found.");
                }

                var transaction = await _transactionService.GetTransactionByIdAsync(id);
                if (transaction == null)
                {
                    _logger.LogWarning("API RateTransaction NOT_FOUND: TransactionID {TransactionId} not found for UserID {UserId}.", id, user.Id);
                    return this.ApiNotFound($"Transaction with ID {id} not found.");
                }
                
                if (transaction.BuyerId != user.Id && transaction.SellerId != user.Id)
                {
                    _logger.LogWarning("API RateTransaction FORBIDDEN: UserID {UserId} was not part of TransactionID {TransactionId}.", user.Id, id);
                    return this.ApiForbidden("You were not part of this transaction.");
                }

                if (transaction.Status != TransactionStatus.Completed)
                {
                    _logger.LogWarning("API RateTransaction BAD_REQUEST: Cannot rate TransactionID {TransactionId} with status '{Status}'.", id, transaction.Status);
                    return this.ApiBadRequest($"Cannot rate a transaction with status '{transaction.Status}'. Only completed transactions can be rated.");
                }
                
                int ratedUserId = user.Id == transaction.SellerId ? transaction.BuyerId : transaction.SellerId;
                // This check ensures user is not rating themselves, which should be covered by BuyerId != SellerId in transaction logic.
                if (ratedUserId == user.Id) 
                {
                     _logger.LogWarning("API RateTransaction BAD_REQUEST: UserID {UserId} attempted to rate themselves in TransactionID {TransactionId}.", user.Id, id);
                    return this.ApiBadRequest("You cannot rate yourself.");
                }

                // Check if user already rated the other party for this transaction
                var existingRating = await _unitOfWork.Ratings.FirstOrDefaultAsync(r => 
                    r.TransactionId == id && r.RaterId == user.Id && r.RatedEntityId == ratedUserId && r.RatedEntityType == RatedEntityType.User);
                    
                if (existingRating != null)
                {
                    _logger.LogWarning("API RateTransaction BAD_REQUEST: UserID {UserId} already rated the other party (UserID {RatedUserId}) in TransactionID {TransactionId}.", 
                        user.Id, ratedUserId, id);
                    return this.ApiBadRequest("You have already rated the other party for this transaction.");
                }

                var ratingCreateDto = new RatingCreateDto
                {
                    Value = ratingDto.Value,
                    Comment = ratingDto.Comment,
                    RaterId = user.Id,
                    RatedEntityId = ratedUserId, 
                    RatedEntityType = RatedEntityType.User, 
                    TransactionId = id 
                };

                var ratingId = await _ratingService.AddRatingAsync(ratingCreateDto);
                var createdRating = await _ratingService.GetRatingByIdAsync(ratingId);

                if (createdRating == null)
                {
                    _logger.LogError("API RateTransaction ERROR: Rating submitted (ID: {RatingId}) but could not be retrieved for TransactionID {TransactionId}.", ratingId, id);
                    return this.ApiInternalError("Rating submitted but could not be retrieved.");
                }
                _logger.LogInformation("API RateTransaction SUCCESS. New RatingID: {RatingId} for TransactionID: {TransactionId} by UserID: {UserId}, RatedUserID: {RatedUserId}", 
                    ratingId, id, user.Id, ratedUserId);
                return this.ApiCreatedAtAction(
                    createdRating,
                    nameof(GetTransaction), // Or a specific GetRating endpoint if available
                    this.ControllerContext.ActionDescriptor.ControllerName,        
                    new { id = id }, // Route values for GetTransaction, as it contains ratings
                    "Rating submitted successfully."
                );
            }
            catch (KeyNotFoundException ex) 
            {
                 _logger.LogWarning(ex, "API RateTransaction NOT_FOUND: TransactionID {TransactionId} or related entity not found for UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiNotFound(ex.Message); 
            }
            catch (UnauthorizedAccessException ex) 
            {
                _logger.LogWarning(ex, "API RateTransaction FORBIDDEN: UserID Claim {UserIdClaim} not authorized for TransactionID {TransactionId}", 
                    userIdClaim ?? "N/A", id);
                return this.ApiForbidden(ex.Message); 
            }
            catch (InvalidOperationException ex) 
            {
                _logger.LogWarning(ex, "API RateTransaction BAD_REQUEST (Invalid Op) for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiBadRequest(ex.Message); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API RateTransaction ERROR for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error rating transaction.", ex);
            }
        }
        
        /// <summary>
        /// Retrieves ratings associated with a specific transaction.
        /// The authenticated user must be a party to the transaction.
        /// </summary>
        /// <param name="id">The ID of the transaction.</param>
        /// <returns>A list of ratings for the transaction.</returns>
        /// <response code="200">Successfully retrieved transaction ratings.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not a party to this transaction.</response>
        /// <response code="404">If the transaction is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpGet("{id:int}/ratings")]
        [ProducesResponseType(typeof(ApiResponse<IEnumerable<RatingDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTransactionRatings(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API GetTransactionRatings START for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", id, userIdClaim ?? "N/A");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API GetTransactionRatings UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}. TransactionID: {TransactionId}", 
                        userIdClaim ?? "N/A", id);
                    return this.ApiUnauthorized("User not found.");
                }

                var canAccess = await _transactionService.CanUserAccessTransactionAsync(id, user.Id);
                if (!canAccess)
                {
                    _logger.LogWarning("API GetTransactionRatings FORBIDDEN: UserID {UserId} not authorized for TransactionID {TransactionId}.", user.Id, id);
                    return this.ApiForbidden("You are not authorized to view ratings for this transaction.");
                }

                var transactionExists = await _transactionService.GetTransactionByIdAsync(id); 
                if (transactionExists == null)
                {
                    _logger.LogWarning("API GetTransactionRatings NOT_FOUND: TransactionID {TransactionId} not found for UserID {UserId}.", id, user.Id);
                    return this.ApiNotFound($"Transaction with ID {id} not found.");
                }

                var ratings = await _unitOfWork.Ratings.FindAsync(r => r.TransactionId == id);
                var ratingDtos = _mapper.Map<List<RatingDto>>(ratings);
                
                _logger.LogInformation("API GetTransactionRatings SUCCESS for TransactionID: {TransactionId}, UserID: {UserId}. Rating count: {Count}", 
                    id, user.Id, ratingDtos?.Count ?? 0);
                return this.ApiOk(ratingDtos); 
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API GetTransactionRatings ERROR for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error retrieving transaction ratings.", ex);
            }
        }
        
        /// <summary>
        /// Confirms completion of a transaction step by the authenticated user (buyer or seller).
        /// </summary>
        /// <param name="id">The ID of the transaction to confirm.</param>
        /// <returns>A 200 OK response with a message indicating the confirmation status.</returns>
        /// <response code="200">Confirmation recorded successfully. Message indicates if transaction is now complete or awaiting other party.</response>
        /// <response code="400">If transaction ID is invalid or transaction is not in a confirmable state.</response>
        /// <response code="401">If the user is not authenticated.</response>
        /// <response code="403">If the user is not a party to this transaction.</response>
        /// <response code="404">If the transaction is not found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        [HttpPost("{id:int}/confirm")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> ConfirmTransaction(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("API ConfirmTransaction START for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", id, userIdClaim ?? "N/A");
            try
            {
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    _logger.LogWarning("API ConfirmTransaction UNAUTHORIZED: User not found for UserID claim: {UserIdClaim}. TransactionID: {TransactionId}", 
                        userIdClaim ?? "N/A", id);
                    return this.ApiUnauthorized("User not found.");
                }

                var transaction = await _transactionService.GetTransactionByIdAsync(id); // Get the transaction to check its current state
                if (transaction == null)
                {
                    _logger.LogWarning("API ConfirmTransaction NOT_FOUND: TransactionID {TransactionId} not found for UserID {UserId}.", id, user.Id);
                    return this.ApiNotFound($"Transaction with ID {id} not found.");
                }

                if (transaction.BuyerId != user.Id && transaction.SellerId != user.Id)
                {
                    _logger.LogWarning("API ConfirmTransaction FORBIDDEN: UserID {UserId} is not a party to TransactionID {TransactionId}.", user.Id, id);
                    return this.ApiForbidden("You are not authorized to confirm this transaction.");
                }

                if (transaction.Status != TransactionStatus.InProgress)
                {
                     _logger.LogWarning("API ConfirmTransaction BAD_REQUEST: Cannot confirm TransactionID {TransactionId} with status '{Status}'.", id, transaction.Status);
                    return this.ApiBadRequest($"Cannot confirm a transaction with status '{transaction.Status}'. Only transactions with status 'InProgress' can be confirmed.");
                }

                // The service method should handle the logic of updating the confirmation status for the current user
                // and potentially changing the overall transaction status if both parties have confirmed.
                // TODO: The ITransactionService.ConfirmTransactionCompletionAsync should ideally return a more structured result 
                // (e.g., an object with flags like IsNowCompleted, AwaitingOtherParty) or the updated TransactionDto.
                await _transactionService.ConfirmTransactionCompletionAsync(id, user.Id);
                _logger.LogInformation("API ConfirmTransaction: Confirmation processed for TransactionID: {TransactionId}, UserID: {UserId}", id, user.Id);

                // Re-fetch or use updated data from service to provide a more accurate message.
                // For simplicity here, we'll use a generic message or a slightly more informed one if possible.
                var updatedTransaction = await _transactionService.GetTransactionByIdAsync(id); // Re-fetch to get latest status
                if (updatedTransaction?.Status == TransactionStatus.Completed)
                {
                    return this.ApiOk("Transaction has been completed successfully. Both parties have confirmed.");
                }
                else
                {
                    return this.ApiOk("Your confirmation has been recorded. The transaction will be completed when the other party confirms, or automatically after a set period.");
                }
            }
            catch (KeyNotFoundException) // Should be caught by GetTransactionByIdAsync if transaction disappears
            {
                _logger.LogWarning("API ConfirmTransaction NOT_FOUND: TransactionID {TransactionId} not found during confirmation for UserID Claim: {UserIdClaim}.", 
                    id, userIdClaim ?? "N/A");
                return this.ApiNotFound($"Transaction with ID {id} not found.");
            }
            catch (UnauthorizedAccessException) // If service layer performs additional auth checks
            {
                _logger.LogWarning("API ConfirmTransaction FORBIDDEN (service level): UserID Claim {UserIdClaim} not authorized for TransactionID {TransactionId}.", 
                    userIdClaim ?? "N/A", id);
                return this.ApiForbidden("You are not authorized to confirm this transaction.");
            }
            catch (InvalidOperationException ex) // If transaction is not in a confirmable state as per service logic
            {
                _logger.LogWarning(ex, "API ConfirmTransaction BAD_REQUEST (Invalid Op) for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiBadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "API ConfirmTransaction ERROR for TransactionID: {TransactionId}, UserID Claim: {UserIdClaim}", 
                    id, userIdClaim ?? "N/A");
                return this.ApiInternalError("Error confirming transaction.", ex);
            }
        }
    }
}
