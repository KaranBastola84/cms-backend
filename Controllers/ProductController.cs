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
    public class ProductController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProductController> _logger;

        // Allowed file extensions and max size
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5 MB

        public ProductController(
            ApplicationDbContext context,
            IAuditService auditService,
            IWebHostEnvironment environment,
            ILogger<ProductController> logger)
        {
            _context = context;
            _auditService = auditService;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Get all products with optional filters and pagination (Public)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllProducts(
            [FromQuery] string? category = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] bool? isFeatured = null,
            [FromQuery] bool? lowStock = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // Validate pagination
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Products.AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(category))
                {
                    query = query.Where(p => p.Category.ToLower() == category.ToLower());
                }

                if (isActive.HasValue)
                {
                    query = query.Where(p => p.IsActive == isActive.Value);
                }

                if (isFeatured.HasValue)
                {
                    query = query.Where(p => p.IsFeatured == isFeatured.Value);
                }

                if (lowStock.HasValue && lowStock.Value)
                {
                    query = query.Where(p => p.StockQuantity <= p.LowStockThreshold);
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()) ||
                                            p.Description.ToLower().Contains(search.ToLower()));
                }

                var totalCount = await query.CountAsync();

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        IsLowStock = p.StockQuantity <= p.LowStockThreshold
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(new
                {
                    products,
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
                _logger.LogError(ex, "Error retrieving products");
                return StatusCode(500, ResponseHelper.Error<object>("An error occurred while retrieving products", 500));
            }
        }

        /// <summary>
        /// Get product by ID (Public)
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(int id)
        {
            try
            {
                var product = await _context.Products
                    .Where(p => p.Id == id)
                    .Select(p => new ProductDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Description = p.Description,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        LowStockThreshold = p.LowStockThreshold,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        IsLowStock = p.StockQuantity <= p.LowStockThreshold,
                        CreatedAt = p.CreatedAt,
                        UpdatedAt = p.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                return Ok(ResponseHelper.Success(product));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product by ID: {ProductId}", id);
                return StatusCode(500, ResponseHelper.Error<object>("An error occurred while retrieving the product", 500));
            }
        }

        /// <summary>
        /// Get featured products (Public)
        /// </summary>
        [HttpGet("featured")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeaturedProducts([FromQuery] int limit = 10)
        {
            try
            {
                if (limit < 1) limit = 10;
                if (limit > 50) limit = 50;

                var products = await _context.Products
                    .Where(p => p.IsFeatured && p.IsActive)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(limit)
                    .Select(p => new ProductListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        IsLowStock = p.StockQuantity <= p.LowStockThreshold
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(products));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving featured products");
                return StatusCode(500, ResponseHelper.Error<object>("An error occurred while retrieving featured products", 500));
            }
        }

        /// <summary>
        /// Get products by category (Public)
        /// </summary>
        [HttpGet("category/{category}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductsByCategory(string category, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1) pageSize = 20;
                if (pageSize > 100) pageSize = 100;

                var query = _context.Products
                    .Where(p => p.Category.ToLower() == category.ToLower() && p.IsActive);

                var totalCount = await query.CountAsync();

                var products = await query
                    .OrderByDescending(p => p.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(p => new ProductListDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        Price = p.Price,
                        StockQuantity = p.StockQuantity,
                        Category = p.Category,
                        ImageUrl = p.ImageUrl,
                        IsActive = p.IsActive,
                        IsFeatured = p.IsFeatured,
                        IsLowStock = p.StockQuantity <= p.LowStockThreshold
                    })
                    .ToListAsync();

                return Ok(ResponseHelper.Success(new
                {
                    category,
                    products,
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
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving products by category: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Create a new product (Admin only)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid product data"));
                }

                var product = new Product
                {
                    Name = dto.Name,
                    Description = dto.Description,
                    Price = dto.Price,
                    StockQuantity = dto.StockQuantity,
                    LowStockThreshold = dto.LowStockThreshold,
                    Category = dto.Category,
                    IsActive = dto.IsActive,
                    IsFeatured = dto.IsFeatured,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Log product creation
                await _auditService.LogAsync(
                    ActionType.CREATE,
                    "Product",
                    product.Id.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new { product.Name, product.Price, product.StockQuantity }),
                    $"Product created: {product.Name}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    LowStockThreshold = product.LowStockThreshold,
                    Category = product.Category,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    IsFeatured = product.IsFeatured,
                    IsLowStock = product.StockQuantity <= product.LowStockThreshold,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                }, "Product created successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, ResponseHelper.Error<object>("An error occurred while creating the product", 500));
            }
        }

        /// <summary>
        /// Update a product (Admin only)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid product data"));
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                // Store old values for audit
                var oldValues = System.Text.Json.JsonSerializer.Serialize(new
                {
                    product.Name,
                    product.Price,
                    product.StockQuantity,
                    product.IsActive,
                    product.IsFeatured
                });

                // Update product
                product.Name = dto.Name;
                product.Description = dto.Description;
                product.Price = dto.Price;
                product.StockQuantity = dto.StockQuantity;
                product.LowStockThreshold = dto.LowStockThreshold;
                product.Category = dto.Category;
                product.IsActive = dto.IsActive;
                product.IsFeatured = dto.IsFeatured;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log product update
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Product",
                    product.Id.ToString(),
                    oldValues,
                    System.Text.Json.JsonSerializer.Serialize(new
                    {
                        product.Name,
                        product.Price,
                        product.StockQuantity,
                        product.IsActive,
                        product.IsFeatured
                    }),
                    $"Product updated: {product.Name}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new ProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = product.Price,
                    StockQuantity = product.StockQuantity,
                    LowStockThreshold = product.LowStockThreshold,
                    Category = product.Category,
                    ImageUrl = product.ImageUrl,
                    IsActive = product.IsActive,
                    IsFeatured = product.IsFeatured,
                    IsLowStock = product.StockQuantity <= product.LowStockThreshold,
                    CreatedAt = product.CreatedAt,
                    UpdatedAt = product.UpdatedAt
                }, "Product updated successfully"));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another user modified the product, reload and inform user
                return Conflict(ResponseHelper.Error<object>("This product was modified by another user. Please refresh and try again.", 409));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product: {ProductId}", id);
                return StatusCode(500, ResponseHelper.Error<object>("An error occurred while updating the product", 500));
            }
        }

        /// <summary>
        /// Update product stock quantity (Admin only)
        /// </summary>
        [HttpPut("{id}/stock")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ResponseHelper.Error<object>("Invalid stock data"));
                }

                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                var oldStock = product.StockQuantity;
                product.StockQuantity = dto.StockQuantity;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log stock update
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Product",
                    product.Id.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(new { StockQuantity = oldStock }),
                    System.Text.Json.JsonSerializer.Serialize(new { StockQuantity = product.StockQuantity }),
                    $"Stock updated for {product.Name}: {oldStock} → {product.StockQuantity}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new
                {
                    productId = product.Id,
                    productName = product.Name,
                    oldStock,
                    newStock = product.StockQuantity,
                    isLowStock = product.StockQuantity <= product.LowStockThreshold
                }, "Stock updated successfully"));
            }
            catch (DbUpdateConcurrencyException)
            {
                // Another user modified the stock, reload and inform user
                return Conflict(ResponseHelper.Error<object>("This product's stock was modified by another process. Please refresh and try again.", 409));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product: {ProductId}", id);
                return StatusCode(500, ResponseHelper.Error<object>("An error occurred while updating stock", 500));
            }
        }

        /// <summary>
        /// Delete a product (Admin only)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                // Check if product is in any orders
                var hasOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
                if (hasOrders)
                {
                    return BadRequest(ResponseHelper.Error<object>("Cannot delete product that has orders. Consider deactivating it instead."));
                }

                // Log deletion before removing
                await _auditService.LogAsync(
                    ActionType.DELETE,
                    "Product",
                    product.Id.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(new { product.Name, product.Price, product.StockQuantity }),
                    null,
                    $"Product deleted: {product.Name}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                _context.Products.Remove(product);
                await _context.SaveChangesAsync();

                return Ok(ResponseHelper.Success(new { message = "Product deleted successfully" }));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error deleting product: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Get available product categories (Public)
        /// </summary>
        [HttpGet("categories")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Products
                    .Where(p => p.IsActive)
                    .Select(p => p.Category)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToListAsync();

                return Ok(ResponseHelper.Success(categories));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ResponseHelper.Error<object>($"Error retrieving categories: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Upload product image (Admin only)
        /// </summary>
        [HttpPost("{id}/image")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> UploadProductImage(int id, IFormFile image)
        {
            try
            {
                // Check if product exists
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                // Validate image file
                if (image == null || image.Length == 0)
                {
                    return BadRequest(ResponseHelper.Error<object>("No image file provided"));
                }

                // Validate file size
                if (image.Length > _maxFileSize)
                {
                    return BadRequest(ResponseHelper.Error<object>($"File size cannot exceed {_maxFileSize / 1024 / 1024}MB"));
                }

                // Validate file extension
                var fileExtension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!_allowedImageExtensions.Contains(fileExtension))
                {
                    return BadRequest(ResponseHelper.Error<object>($"Invalid file type. Allowed types: {string.Join(", ", _allowedImageExtensions)}"));
                }

                // SECURITY: Validate content type matches expected image format
                var contentType = image.ContentType.ToLower();
                if (!contentType.StartsWith("image/"))
                {
                    return BadRequest(ResponseHelper.Error<object>("File content type does not match expected image format"));
                }

                // Create Products folder if it doesn't exist
                var uploadsFolder = Path.Combine(_environment.ContentRootPath, "Uploads", "Products");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Delete old image file if exists
                if (!string.IsNullOrEmpty(product.ImageUrl))
                {
                    var oldImagePath = Path.Combine(_environment.ContentRootPath, product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        try
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to delete old product image: {ImagePath}", oldImagePath);
                        }
                    }
                }

                // Generate unique filename
                var uniqueFileName = $"product_{id}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(stream);
                }

                // Update product ImageUrl
                product.ImageUrl = $"/Uploads/Products/{uniqueFileName}";
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the action
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Product",
                    product.Id.ToString(),
                    null,
                    System.Text.Json.JsonSerializer.Serialize(new { ImageUrl = product.ImageUrl }),
                    $"Product image uploaded: {product.Name}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new
                {
                    imageUrl = product.ImageUrl,
                    fileName = uniqueFileName,
                    fileSize = image.Length,
                    message = "Image uploaded successfully"
                }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading product image");
                return StatusCode(500, ResponseHelper.Error<object>($"Error uploading image: {ex.Message}", 500));
            }
        }

        /// <summary>
        /// Delete product image (Admin only)
        /// </summary>
        [HttpDelete("{id}/image")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteProductImage(int id)
        {
            try
            {
                var product = await _context.Products.FindAsync(id);
                if (product == null)
                {
                    return NotFound(ResponseHelper.Error<object>("Product not found", 404));
                }

                if (string.IsNullOrEmpty(product.ImageUrl))
                {
                    return BadRequest(ResponseHelper.Error<object>("Product has no image to delete"));
                }

                // Delete physical file
                var imagePath = Path.Combine(_environment.ContentRootPath, product.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    try
                    {
                        System.IO.File.Delete(imagePath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to delete product image file: {ImagePath}", imagePath);
                    }
                }

                // Update product record
                var oldImageUrl = product.ImageUrl;
                product.ImageUrl = null;
                product.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log the action
                await _auditService.LogAsync(
                    ActionType.UPDATE,
                    "Product",
                    product.Id.ToString(),
                    System.Text.Json.JsonSerializer.Serialize(new { ImageUrl = oldImageUrl }),
                    System.Text.Json.JsonSerializer.Serialize(new { ImageUrl = (string?)null }),
                    $"Product image deleted: {product.Name}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,
                    User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                );

                return Ok(ResponseHelper.Success(new { message = "Image deleted successfully" }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product image");
                return StatusCode(500, ResponseHelper.Error<object>($"Error deleting image: {ex.Message}", 500));
            }
        }
    }
}
