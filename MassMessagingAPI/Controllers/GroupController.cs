using MassMessagingAPI.Models;
using MassMessagingAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MassMessagingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupController : ControllerBase
    {
        // AppDbContext yerine sadece Group deposunu alıyoruz
        private readonly IGenericRepository<Group> _groupRepository;

        public GroupController(IGenericRepository<Group> groupRepository)
        {
            _groupRepository = groupRepository;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateGroup([FromBody] string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return BadRequest("Grup adı boş olamaz.");

            var group = new Group { Name = groupName };

            // Veritabanı kayıt işlemi Repository üzerinden yapılıyor
            await _groupRepository.AddAsync(group);

            return Ok(new { Message = $"{groupName} grubu başarıyla oluşturuldu.", GroupId = group.Id });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetGroups()
        {
            // Tüm grupları çek
            var groups = await _groupRepository.GetAllAsync();

            // Arayüze sadece gereken bilgileri dön
            var result = groups.Select(g => new { g.Id, g.Name });

            return Ok(result);
        }
    }
}