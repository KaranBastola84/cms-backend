using JWTAuthAPI.Models;
using JWTAuthAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JWTAuthAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Staff")]
    public class FeeStructureController : ControllerBase
    {
        private readonly IFeeStructureService _feeStructureService;

        public FeeStructureController(IFeeStructureService feeStructureService)
        {
            _feeStructureService = feeStructureService;
        }

        [HttpPost]
        public async Task<IActionResult> CreateFeeStructure([FromBody] CreateFeeStructureDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _feeStructureService.CreateFeeStructureAsync(dto, userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFeeStructure(int id)
        {
            var result = await _feeStructureService.GetFeeStructureByIdAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpGet("course/{courseId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeeStructuresByCourse(int courseId)
        {
            var result = await _feeStructureService.GetFeeStructuresByCourseIdAsync(courseId);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFeeStructures()
        {
            var result = await _feeStructureService.GetAllFeeStructuresAsync();
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFeeStructure(int id, [FromBody] UpdateFeeStructureDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _feeStructureService.UpdateFeeStructureAsync(id, dto, userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFeeStructure(int id)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "System";
            var result = await _feeStructureService.DeleteFeeStructureAsync(id, userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("course/{courseId}/total")]
        [AllowAnonymous]
        public async Task<IActionResult> GetTotalCourseFee(int courseId)
        {
            var result = await _feeStructureService.GetTotalCourseFeeAsync(courseId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}
