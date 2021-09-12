using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Shared;

namespace Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RazorFileController : ControllerBase
    {
        private readonly ILogger<RazorFileController> _logger;
        private readonly SplendidContext _dbContext;

        public RazorFileController(ILogger<RazorFileController> logger, SplendidContext dbContext)
        {
            _logger = logger;
            _dbContext = dbContext;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRazorFile([FromBody] RazorFile file)
        {
            if (file.Id == Guid.Empty)
            {
                file.Id = Guid.NewGuid();
                await _dbContext.RazorFiles.AddAsync(file);
            }
            else
            {
                _dbContext.RazorFiles.Update(file);
            }

            await _dbContext.SaveChangesAsync();
            return Ok(file);
        }

        [HttpGet]
        public async Task<IActionResult> GetRazorFiles()
        {
            if (User == null || !User.Identity.IsAuthenticated)
                return Ok(new List<object>());
            var files = await _dbContext.RazorFiles.ToListAsync();
            return Ok(files);
        }
    }
}