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

        public GroupController(
            IGenericRepository<Group> groupRepository,
            IGenericRepository<UserGroup> userGroupRepository,
            UserManager<AppUser> userManager)
        {
            _groupRepository = groupRepository;
            _userGroupRepository = userGroupRepository;
            _userManager = userManager;
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
            var groups = await _groupRepository.GetAllAsync();
            var result = groups.Select(g => new { g.Id, g.Name });
            return Ok(result);
        }

        // FIX #3: Add a user to a group (was missing — UserGroup table was never populated)
        [HttpPost("add-user")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddUserToGroup([FromBody] AddUserToGroupDto dto)
        {
            // Validate user exists
            var user = await _userManager.FindByIdAsync(dto.UserId);
            if (user == null)
                return NotFound(new { Message = "Kullanıcı bulunamadı." });

            // Validate group exists
            var group = await _groupRepository.GetByIdAsync(dto.GroupId);
            if (group == null)
                return NotFound(new { Message = "Grup bulunamadı." });

            // Check if already a member
            var existing = await _userGroupRepository.FindAsync(
                ug => ug.UserId == dto.UserId && ug.GroupId == dto.GroupId);

            if (existing.Any())
                return Conflict(new { Message = "Kullanıcı zaten bu grubun üyesi." });

            var userGroup = new UserGroup { UserId = dto.UserId, GroupId = dto.GroupId };
            await _userGroupRepository.AddAsync(userGroup);

            return Ok(new { Message = $"{user.FirstName} {user.LastName} gruba eklendi." });
        }

        // Get members of a group (useful for Admin UI)
        [HttpGet("{groupId}/members")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetGroupMembers(int groupId)
        {
            var members = await _userGroupRepository.FindAsync(
                ug => ug.GroupId == groupId,
                ug => ug.User!);

            var result = members.Select(ug => new
            {
                ug.UserId,
                FullName = ug.User!.FirstName + " " + ug.User.LastName,
                ug.User.Email
            });

            return Ok(result);
        }
    }
}