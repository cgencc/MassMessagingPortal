using MassMessagingAPI.Models;
using MassMessagingAPI.Repositories;
using MassMessagingAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MassMessagingAPI.DTOs;

namespace MassMessagingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GroupController : ControllerBase
    {
        private readonly IGenericRepository<Group> _groupRepository;
        private readonly IGenericRepository<UserGroup> _userGroupRepository;
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public GroupController(
            IGenericRepository<Group> groupRepository,
            IGenericRepository<UserGroup> userGroupRepository,
            UserManager<AppUser> userManager,
            AppDbContext context)
        {
            _groupRepository = groupRepository;
            _userGroupRepository = userGroupRepository;
            _userManager = userManager;
            _context = context;
        }

        [HttpPost("create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return BadRequest(new { Message = "Grup adı boş olamaz." });
            var group = new Group { Name = dto.Name };
            await _groupRepository.AddAsync(group);
            return Ok(new { Message = $"{dto.Name} grubu başarıyla oluşturuldu.", GroupId = group.Id });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetGroups()
        {
            var groups = await _context.Groups
                .Select(g => new
                {
                    id = g.Id,    
                    name = g.Name  
                })
                .ToListAsync();

            return Ok(groups);
        }

        [HttpGet("my-groups")]
        public async Task<IActionResult> GetMyGroups()
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (userId == null) return Unauthorized();

            var userGroups = await _context.UserGroups
                .Where(ug => ug.UserId == userId)
                .Select(ug => new
                {
                    id = ug.Group.Id,  
                    name = ug.Group.Name 
                })
                .ToListAsync();

            return Ok(userGroups);
        }

        [HttpPost("add-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUserToGroup([FromBody] AddUserToGroupDto dto)
        {
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null) return NotFound(new { Message = "Kullanıcı bulunamadı." });
            var group = await _groupRepository.GetByIdAsync(dto.GroupId);
            if (group == null) return NotFound(new { Message = "Grup bulunamadı." });
            var existing = await _userGroupRepository.FindAsync(ug => ug.UserId == dto.UserId && ug.GroupId == dto.GroupId);
            if (existing.Any()) return Conflict(new { Message = "Kullanıcı zaten bu grubun üyesi." });
            await _userGroupRepository.AddAsync(new UserGroup { UserId = dto.UserId, GroupId = dto.GroupId });
            return Ok(new { Message = $"{user.FirstName} {user.LastName} gruba eklendi." });
        }

        [HttpPost("add-users-bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUsersToGroup([FromBody] BulkUserGroupDto dto)
        {
            var group = await _groupRepository.GetByIdAsync(dto.GroupId);
            if (group == null) return NotFound(new { Message = "Grup bulunamadı." });
            int addedCount = 0;
            foreach (var userId in dto.UserIds)
            {
                var exists = await _userGroupRepository.FindAsync(ug => ug.UserId == userId && ug.GroupId == dto.GroupId);
                if (!exists.Any())
                {
                    await _userGroupRepository.AddAsync(new UserGroup { UserId = userId, GroupId = dto.GroupId });
                    addedCount++;
                }
            }
            return Ok(new { Message = $"{addedCount} kullanıcı gruba eklendi." });
        }

        [HttpDelete("remove-user/{groupId}/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveUserFromGroup(int groupId, string userId)
        {
            var members = await _userGroupRepository.FindAsync(ug => ug.UserId == userId && ug.GroupId == groupId);
            var membership = members.FirstOrDefault();
            if (membership == null)
                return NotFound(new { Message = "Bu kullanıcı bu grubun üyesi değil." });

            var entry = _context.UserGroups.FirstOrDefault(ug => ug.UserId == userId && ug.GroupId == groupId);
            if (entry != null)
            {
                _context.UserGroups.Remove(entry);
                await _context.SaveChangesAsync();
            }
            return Ok(new { Message = "Kullanıcı gruptan çıkarıldı." });
        }

        [HttpDelete("remove-users-bulk")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RemoveUsersFromGroup([FromBody] BulkUserGroupDto dto)
        {
            var group = await _groupRepository.GetByIdAsync(dto.GroupId);
            if (group == null) return NotFound(new { Message = "Grup bulunamadı." });

            var toRemove = _context.UserGroups
                .Where(ug => ug.GroupId == dto.GroupId && dto.UserIds.Contains(ug.UserId));

            _context.UserGroups.RemoveRange(toRemove);
            await _context.SaveChangesAsync();
            return Ok(new { Message = $"{dto.UserIds.Count} kullanıcı gruptan çıkarıldı." });
        }

        [HttpGet("{groupId}/members")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGroupMembers(int groupId)
        {
            var members = await _userGroupRepository.FindAsync(ug => ug.GroupId == groupId, ug => ug.User!);
            return Ok(members.Select(ug => new
            {
                ug.UserId,
                FullName = ug.User!.FirstName + " " + ug.User.LastName,
                ug.User.Email
            }));
        }
    }
}