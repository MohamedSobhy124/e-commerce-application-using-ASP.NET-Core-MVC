using IdealWeightNutrition.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdealWeightNutrition.Areas.Admin.Controllers
{
    [Route("api/notifications")]
    [ApiController]
    [Authorize]
    public class NotificationApiController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationApiController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet("unread")]
        public IActionResult GetUnreadNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = _unitOfWork.notification.GetUnreadNotifications(userId);
            
            return Ok(notifications.Select(n => new
            {
                id = n.Id,
                title = n.Title,
                message = n.Message,
                type = n.Type,
                icon = n.Icon,
                link = n.Link,
                isRead = n.IsRead,
                createdAt = n.CreatedAt
            }));
        }

        [HttpPost("mark-read/{id}")]
        public IActionResult MarkAsRead(int id)
        {
            _unitOfWork.notification.MarkAsRead(id);
            _unitOfWork.save();
            
            return Ok(new { success = true });
        }

        [HttpPost("mark-all-read")]
        public IActionResult MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _unitOfWork.notification.MarkAllAsRead(userId);
            _unitOfWork.save();
            
            return Ok(new { success = true });
        }

        [HttpGet("count")]
        public IActionResult GetNotificationCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = _unitOfWork.notification.GetUnreadNotifications(userId).Count();
            
            return Ok(new { count = count });
        }
    }
}

