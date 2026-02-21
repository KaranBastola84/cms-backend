using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JWTAuthAPI.Data;
using JWTAuthAPI.Models;
using JWTAuthAPI.Helpers;
using JWTAuthAPI.Services;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductReviewController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly ILogger<ProductReviewController> _logger;

        public ProductReviewController(
            ApplicationDbContext context,
            IAuditService auditService,
            ILogger<ProductReviewController> logger)
        {
            _context = context;
            _auditService = auditService;
            _logger = logger;
        }

        /// <summary>
        /// Submit a product review (Public - no authentication required)
        /// </summary>
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SubmitReview([FromBody] CreateProductReviewDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid review data"));
                }

                // Validate product exists and is active
                var product = await _context.Products.FindAsync(dto.ProductId);
                if (product == null || !product.IsActive)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                // Create review
                var review = new ProductReview
                {
                    ProductId = dto.ProductId,
                    CustomerName = dto.CustomerName,
                    CustomerEmail = dto.CustomerEmail,
                    Rating = dto.Rating,
                    ReviewText = dto.ReviewText,
                    IsApproved = false,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductReviews.Add(review);
                await _context.SaveChangesAsync();

                // Log review submission
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "ProductReview",
                    review.Id.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        review.CustomerEmail,
                        review.Rating
                    }),
                    $"New review submitted for {product.Name} by {review.CustomerName}",
                    null,
                    review.CustomerEmail
                );

                return Ok(ResponseHelper.Success(new
                {
                    reviewId = review.Id,
                    message = "Thank you for your review! It will be published after approval."
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting review");
                return StatusCode(500, ResponseHelper.Error<object>("An error occurred while submitting your review", 500));
            }
        }

        /// <summary>
        /// Get approved reviews for a product (Public)
        /// </summary>
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            try
            {
                var reviews = await _context.ProductReviews
                    .Where(r => r.ProductId == productId && r.IsApproved)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new ProductReviewDto
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        ProductName = "",
                        CustomerName = r.CustomerName,
                        CustomerEmail = r.CustomerEmail,
                        Rating = r.Rating,
                        ReviewText = r.ReviewText,
                        IsApproved = r.IsApproved,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                // Calculate average rating
                double averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

                return Ok(ResponseHelper.Success(new
                {
                    reviews,
                    totalReviews = reviews.Count,
                    averageRating = Math.Round(averageRating, 1)
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving reviews: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get all reviews with filtering (Admin only)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetAllReviews(
            [FromQuery] int? productId = null,
            [FromQuery] bool? isApproved = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.ProductReviews
                    .Include(r => r.Product)
                    .AsQueryable();

                // Apply filters
                if (productId.HasValue)
                {
                    query = query.Where(r => r.ProductId == productId.Value);
                }

                if (isApproved.HasValue)
                {
                    query = query.Where(r => r.IsApproved == isApproved.Value);
                }

                var totalCount = await query.CountAsync();

                var reviews = await query
                    .OrderByDescending(r => r.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ProductReviewDto
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        ProductName = r.Product.Name,
                        CustomerName = r.CustomerName,
                        CustomerEmail = r.CustomerEmail,
                        Rating = r.Rating,
                        ReviewText = r.ReviewText,
                        IsApproved = r.IsApproved,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(new
                {
                    reviews,
                    pagination = new
                    {
                        currentPage = page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                    }
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving reviews: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get pending reviews (Admin only)
        /// </summary>
        [HttpGet("pending")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetPendingReviews()
        {
            try
            {
                var reviews = await _context.ProductReviews
                    .Include(r => r.Product)
                    .Where(r => !r.IsApproved)
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new ProductReviewDto
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        ProductName = r.Product.Name,
                        CustomerName = r.CustomerName,
                        CustomerEmail = r.CustomerEmail,
                        Rating = r.Rating,
                        ReviewText = r.ReviewText,
                        IsApproved = r.IsApproved,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(reviews));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving pending reviews: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get review by ID (Admin only)
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> GetReviewById(int id)
        {
            try
            {
                var review = await _context.ProductReviews
                    .Include(r => r.Product)
                    .Where(r => r.Id == id)
                    .Select(r => new ProductReviewDto
                    {
                        Id = r.Id,
                        ProductId = r.ProductId,
                        ProductName = r.Product.Name,
                        CustomerName = r.CustomerName,
                        CustomerEmail = r.CustomerEmail,
                        Rating = r.Rating,
                        ReviewText = r.ReviewText,
                        IsApproved = r.IsApproved,
                        CreatedAt = r.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (review == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Review not found", 404));
                }

                return Ok(ResponseHelper.Success(review));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving review: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Approve a review (Admin only)
        /// </summary>
        [HttpPut("{id}/approve")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> ApproveReview(int id)
        {
            try
            {
                var review = await _context.ProductReviews
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (review == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Review not found", 404));
                }

                if (review.IsApproved)
                {
                    return BadRequest(ResponseHelper.Error<object>("Review is already approved"));
                }

                review.IsApproved = true;

                await _context.SaveChangesAsync();

                // Log approval
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "ProductReview",
                    review.Id.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(new { IsApproved = false }),
                    System.Text.Json.JsonSerializer.Serialize(new { IsApproved = true }),
                    $"Review approved for {review.Product.Name} by {review.CustomerName}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new
                {
                    reviewId = review.Id,
                    message = "Review approved successfully"
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error approving review: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Reject a review (Admin only)
        /// </summary>
        [HttpPut("{id}/reject")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> RejectReview(int id)
        {
            try
            {
                var review = await _context.ProductReviews
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (review == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Review not found", 404));
                }

                review.IsApproved = false;

                await _context.SaveChangesAsync();

                // Log rejection
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "ProductReview",
                    review.Id.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new { IsApproved = false, Rejected = true }),
                    $"Review rejected for {review.Product.Name} by {review.CustomerName}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new
                {
                    reviewId = review.Id,
                    message = "Review rejected successfully"
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error rejecting review: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Delete a review (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var review = await _context.ProductReviews
                    .Include(r => r.Product)
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (review == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Review not found", 404));
                }

                var productId = review.ProductId;
                var productName = review.Product.Name;
                var customerName = review.CustomerName;

                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();

                // Log deletion
                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "ProductReview",
                    id.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        ProductName = productName,
                        CustomerName = customerName,
                        review.Rating
                    }),
                    null,
                    $"Review deleted for {productName}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new
                {
                    message = "Review deleted successfully"
                }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error deleting review: {ex.Message}", 500));
            }
        }

    }
}
