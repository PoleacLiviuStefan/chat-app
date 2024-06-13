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
    public class ChatsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;

        public ChatsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            db = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return Redirect("/Identity/Account/Login");
            }

            // Load chats
            var chats = await db.Chats
                .Include(c => c.CurrentUser)
                .Include(c => c.FriendUser)
                .Where(c => c.CurrentUserId == userId || c.FriendUserId == userId)
                .ToListAsync();

            ViewBag.UserCurent = userId;
            return View(chats);
        }

        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> Show(int id)
        {
            var chat = await db.Chats
                .Include(c => c.CurrentUser)
                .Include(c => c.FriendUser)
                .Include(c => c.Messages)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chat == null)
            {
                TempData["message"] = "Acest chat nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            var chatUserList = await db.ChatUsers.Where(ch => ch.ChatId == id).ToListAsync();
            SetAccessRights();

            if (User.IsInRole("Admin") || User.IsInRole("Moderator") || chatUserList.Any(cu => cu.UserId == _userManager.GetUserId(User)))
            {
                return View(chat);
            }

            TempData["message"] = "Nu aveți drepturi pentru a vizualiza acest canal!";
            TempData["messageType"] = "alert-danger";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> Show([FromForm] Message message)
        {
            message.Date = DateTime.Now;
            message.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Messages.Add(message);
                await db.SaveChangesAsync();

                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var chatMessages = db.Messages.Where(m => m.ChatId == message.ChatId).Include(m => m.User).ToList();
                    return PartialView("_MessagesPartial", chatMessages);
                }

                return RedirectToAction("Show", new { id = message.ChatId });
            }

            var chat = await db.Chats
                .Include(c => c.CurrentUser)
                .Include(c => c.FriendUser)
                .Include(c => c.Messages)
                .ThenInclude(m => m.User)
                .FirstOrDefaultAsync(c => c.Id == message.ChatId);

            SetAccessRights();
            return View(chat);
        }

        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> GetOrCreateChat(string friendId)
        {
            var currentUserId = _userManager.GetUserId(User);

            // Check if a chat already exists between the current user and the friend
            var existingChat = await db.Chats
                .FirstOrDefaultAsync(c => (c.CurrentUserId == currentUserId && c.FriendUserId == friendId) ||
                                          (c.CurrentUserId == friendId && c.FriendUserId == currentUserId));

            // Return success with a message if the chat already exists
            if (existingChat != null)
            {
                return Json(new { success = true, message = "A chat already exists with this friend", chatId = existingChat.Id });
            }

            // Create a new chat if it doesn't exist
            var chat = new Chat
            {
                CurrentUserId = currentUserId,
                FriendUserId = friendId
            };

            db.Chats.Add(chat);
            await db.SaveChangesAsync();

            var chatUser = new ChatUser
            {
                ChatId = chat.Id,
                UserId = chat.CurrentUserId
            };

            db.ChatUsers.Add(chatUser);
            await db.SaveChangesAsync();

            return Json(new { success = true, message = "The chat has been created", chatId = chat.Id });
        }





        private void SetAccessRights()
        {
            ViewBag.EsteAdmin = User.IsInRole("Admin");
            ViewBag.EsteModerator = User.IsInRole("Moderator");
            ViewBag.UserCurent = _userManager.GetUserId(User);
            ViewBag.UserNameCurent = _userManager.GetUserName(User);
        }
    }
}
