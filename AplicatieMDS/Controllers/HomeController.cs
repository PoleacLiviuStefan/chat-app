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
                .ToListAsync();

            ViewBag.UserCurent = userId;
            ViewBag.Friends = friends;
            ViewBag.Chats = chats;

            return View();
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







    }
}
