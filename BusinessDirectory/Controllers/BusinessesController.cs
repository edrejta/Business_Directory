using Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BusinessDirectory.Domain.Enums;
using BusinessDirectory.Application.Dtos;

namespace BusinessDirectory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusinessesController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public BusinessesController(ApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusinessPublicDto>>> GetApproved()
    {
       
        var businesses = await _context.Businesses
            .Where(b => b.Status == BusinessStatus.Approved)
            .Select(b => new BusinessPublicDto
            {
                Id = b.Id, 
                BusinessName = b.BusinessName,
                City = b.City,
                Description = b.Description
                
            })
            .ToListAsync();

       
        return Ok(businesses);
    }
}