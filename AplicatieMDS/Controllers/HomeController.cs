using AplicatieMDS.Data;
using AplicatieMDS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AplicatieMDS.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            // Load friends
            var friends = await db.UserFriends
                .Include(uf => uf.User)
                .Include(uf => uf.Friend)
                .Where(uf => uf.UserId == userId || uf.FriendId == userId)
                .ToListAsync();

            var friendIds = friends
                .Select(uf => uf.UserId == userId ? uf.FriendId : uf.UserId)
                .ToList();

            // Load chats
            var chats = await db.Chats
                .Where(c => (friendIds.Contains(c.CurrentUserId) && c.FriendUserId == userId) ||
                            (friendIds.Contains(c.FriendUserId) && c.CurrentUserId == userId))
                .Include(c => c.Messages.OrderByDescending(m => m.Date).Take(1))
                .ToListAsync();

            ViewBag.UserCurent = userId;
            ViewBag.Friends = friends;
            ViewBag.Chats = chats;

            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> MarkMessagesAsSeen(int chatId)
        {
            try
            {
                var userId = _userManager.GetUserId(User);
                var messages = await db.Messages
                    .Where(m => m.ChatId == chatId && m.UserId != userId && m.Status == Message.MessageStatus.Unseen)
                    .ToListAsync();

                foreach (var message in messages)
                {
                    message.Status = Message.MessageStatus.Seen;
                }

                await db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> GetChatMessages(int chatId)
        {
            var chat = await db.Chats
                .Include(c => c.Messages)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == chatId);

            if (chat == null)
            {
                return NotFound();
            }

                ViewBag.CurrentUserId = _userManager.GetUserId(User); // Transmiterea CurrentUserId prin ViewBag
    return PartialView("_MessagesPartial", chat.Messages);
        }





        [HttpGet]
        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult LoadMessageForm(int chatId)
        {
            var message = new Message { ChatId = chatId };
            return PartialView("_MessageFormPartial", message);
        }

        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> SendMessage(Message message)
        {
            message.Date = DateTime.Now;
            message.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Messages.Add(message);
                await db.SaveChangesAsync();

                var newMessage = await db.Messages
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.Id == message.Id);

                ViewBag.CurrentUserId = _userManager.GetUserId(User); // Transmiterea CurrentUserId prin ViewBag
                return PartialView("_SingleMessagePartial", newMessage);
            }

            return BadRequest("Invalid message");
        }


        [HttpGet]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> LoadEditMessageForm(int id)
        {
            var message = await db.Messages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null)
            {
                return NotFound();
            }
            return PartialView("_EditMessageModal", message);
        }


[HttpPost]
[Authorize(Roles = "User,Moderator,Admin")]
public async Task<IActionResult> EditMessage(Message message)
{
    if (ModelState.IsValid)
    {
        var existingMessage = await db.Messages
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.Id == message.Id);
        
        if (existingMessage == null)
        {
            return NotFound();
        }

        existingMessage.Content = message.Content;
        db.Messages.Update(existingMessage);
        await db.SaveChangesAsync();

        ViewBag.CurrentUserId = _userManager.GetUserId(User); // Transmiterea CurrentUserId prin ViewBag
        return PartialView("_SingleMessagePartial", existingMessage);
    }
    return BadRequest("Invalid message");
}




        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> DeleteMessage(int id)
        {
            var message = await db.Messages.FindAsync(id);
            if (message != null)
            {
                db.Messages.Remove(message);
                await db.SaveChangesAsync();
                return Ok();
            }
            return NotFound();
        }


        public async Task<IActionResult> Search()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            var searchTerm = HttpContext.Request.Query["searchTerm"].ToString();

            // Get the current user's friends
            var friends = await db.UserFriends
                .Include(uf => uf.User)
                .Include(uf => uf.Friend)
                .Where(uf => uf.UserId == userId || uf.FriendId == userId)
                .ToListAsync();

            var friendIds = friends
                .Select(uf => uf.UserId == userId ? uf.FriendId : uf.UserId)
                .ToList();

            // Get users who are friends of the current user
            IQueryable<ApplicationUser> friendUsers = db.Users
                .Where(u => friendIds.Contains(u.Id))
                .OrderBy(u => u.UserName);

            if (!string.IsNullOrEmpty(searchTerm))
            {
                friendUsers = friendUsers.Where(user => user.UserName.Contains(searchTerm));
            }

            var allUsers = await friendUsers.ToListAsync();

            ViewBag.UsersList = allUsers;

            return View();
        }





    }
}
