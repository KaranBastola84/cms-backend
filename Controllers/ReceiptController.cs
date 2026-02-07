using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReceiptController : ControllerBase
    {
        private readonly IReceiptService _receiptService;

        public ReceiptController(IReceiptService receiptService)
        {
            _receiptService = receiptService;
        }

        [HttpPost]
        [Authorize(Roles = $"{Roles.Admin},{Roles.Staff}")]
        public async Task<IActionResult> GenerateReceipt([FromBody] CreateReceiptDto createDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _receiptService.GenerateReceiptAsync(createDto, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetReceiptById(int id)
        {
            var result = await _receiptService.GetReceiptByIdAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetReceiptsByStudentId(int studentId)
        {
            var result = await _receiptService.GetReceiptsByStudentIdAsync(studentId);
            return Ok(result);
        }

        [HttpGet("number/{receiptNumber}")]
        public async Task<IActionResult> GetReceiptByNumber(string receiptNumber)
        {
            var result = await _receiptService.GetReceiptByNumberAsync(receiptNumber);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            return Ok(result);
        }

        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadReceipt(int id)
        {
            var result = await _receiptService.DownloadReceiptAsync(id);

            if (!result.IsSuccess)
            {
                return NotFound(result);
            }

            var (fileData, contentType, fileName) = result.Result;
            return File(fileData, contentType, fileName);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = Roles.Admin)]
        public async Task<IActionResult> DeleteReceipt(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _receiptService.DeleteReceiptAsync(id, userId);

            if (!result.IsSuccess)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
    }
}
