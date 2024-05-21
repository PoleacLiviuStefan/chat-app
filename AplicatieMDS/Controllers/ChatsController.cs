using AplicatieMDS.Data;
using AplicatieMDS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace AplicatieMDS.Controllers
{
    public class ChatsController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public ChatsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public IActionResult Index()
        {
            var chatUserDictionary = db.ChatUsers
                .AsEnumerable()
                .GroupBy(chatUser => chatUser.ChatId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(chatUser => chatUser.UserId).ToList()
                );

            ViewBag.ChatAccess = chatUserDictionary;
            ViewBag.ChatPendingAccess = chatUserDictionary;

            var searchTerm = HttpContext.Request.Query["searchTerm"].ToString();

            int _perPage = 3;
            var chats = db.Chats.Include(c => c.CurrentUser).Include(c => c.FriendUser).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                chats = chats.Where(ch => ch.CurrentUser.UserName.Contains(searchTerm) || ch.FriendUser.UserName.Contains(searchTerm));
            }

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            int totalItems = chats.Count();
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset = currentPage == 0 ? 0 : (currentPage - 1) * _perPage;

            var paginatedChats = chats.Skip(offset).Take(_perPage);

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.Chats = paginatedChats;

            SetAccessRights();

            return View();
        }

        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Index2()
        {
            var chatUserDictionary = db.ChatUsers
                .AsEnumerable()
                .GroupBy(chatUser => chatUser.ChatId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(chatUser => chatUser.UserId).ToList()
                );

            ViewBag.ChatAccess = chatUserDictionary;
            ViewBag.ChatPendingAccess = chatUserDictionary;

            var searchTerm = HttpContext.Request.Query["searchTerm"].ToString();

            int _perPage = 3;
            var userId = _userManager.GetUserId(User);
            var chatInclude = db.ChatUsers.Where(row => row.UserId == userId).Select(row => row.ChatId).ToList();

            if (chatInclude.Count == 0)
            {
                TempData["message"] = "Nu sunteti inscris in niciun canal!";
                TempData["messageType"] = "alert-danger";
            }

            ViewBag.ChatInclude = chatInclude;

            var chats = db.Chats.Include(c => c.CurrentUser).Include(c => c.FriendUser).Where(ch => chatInclude.Contains(ch.Id)).AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                chats = chats.Where(ch => ch.CurrentUser.UserName.Contains(searchTerm) || ch.FriendUser.UserName.Contains(searchTerm));
            }

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            int totalItems = chats.Count();
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            var offset = currentPage == 0 ? 0 : (currentPage - 1) * _perPage;

            var paginatedChats = chats.Skip(offset).Take(_perPage);

            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);
            ViewBag.Chats = paginatedChats;

            SetAccessRights();

            return View();
        }

        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Show(int id)
        {
            var chat = db.Chats.Include(c => c.CurrentUser)
                                .Include(c => c.FriendUser)
                                .Include(c => c.Messages)
                                .ThenInclude(m => m.User)
                                .FirstOrDefault(c => c.Id == id);

            if (chat == null)
            {
                TempData["message"] = "Acest chat nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            var chatUserList = db.ChatUsers.Where(ch => ch.ChatId == id).ToList();
            SetAccessRights();

            if (User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                return View(chat);
            }
            else
            {
                if (chatUserList.Any(cu => cu.UserId == _userManager.GetUserId(User)))
                {
                    return View(chat);
                }

                TempData["message"] = "Nu aveti drepturi pentru a vizualiza acest canal!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Show([FromForm] Message message)
        {
            message.Date = DateTime.Now;
            message.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Messages.Add(message);
                db.SaveChanges();
                return Redirect("/Chats/Show/" + message.ChatId);
            }
            else
            {
                var chat = db.Chats.Include(c => c.CurrentUser)
                                   .Include(c => c.FriendUser)
                                   .Include(c => c.Messages)
                                   .ThenInclude(m => m.User)
                                   .FirstOrDefault(c => c.Id == message.ChatId);

                SetAccessRights();
                return View(chat);
            }
        }

        [Authorize(Roles = "Moderator,Admin,User")]
        public IActionResult New()
        {
            var chat = new Chat();
            return View(chat);
        }

        [Authorize(Roles = "Moderator,Admin,User")]
        [HttpPost]
        public IActionResult New(Chat chat)
        {
            var currentUserId = _userManager.GetUserId(User);
            chat.CurrentUserId = currentUserId;

            // Check if a chat already exists between the current user and the friend
            var existingChat = db.Chats
                .FirstOrDefault(c => (c.CurrentUserId == currentUserId && c.FriendUserId == chat.FriendUserId) ||
                                     (c.CurrentUserId == chat.FriendUserId && c.FriendUserId == currentUserId));

            if (existingChat != null)
            {
                TempData["message"] = "Chat already exists between the users.";
                TempData["messageType"] = "alert-info";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                db.Chats.Add(chat);
                db.SaveChanges();

                var chatUser = new ChatUser
                {
                    ChatId = chat.Id,
                    UserId = chat.CurrentUserId
                };

                db.ChatUsers.Add(chatUser);
                db.SaveChanges();

                TempData["message"] = "The chat has been created";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                return View(chat);
            }
        }


        [Authorize(Roles = "Moderator,Admin,User")]
        public IActionResult Edit(int id)
        {
            var chat = db.Chats.FirstOrDefault(c => c.Id == id);

            if (chat == null)
            {
                TempData["message"] = "Canalul nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (chat.CurrentUserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin"))
            {
                return View(chat);
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui canal care nu va apartine";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public IActionResult Edit(int id, Chat requestChat)
        {
            var chat = db.Chats.Find(id);

            if (chat == null)
            {
                TempData["message"] = "Canalul nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            if (ModelState.IsValid)
            {
                if (chat.CurrentUserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin"))
                {
                    chat.FriendUserId = requestChat.FriendUserId; // Update friend user ID or other properties as needed

                    db.SaveChanges();
                    TempData["message"] = "Canalul a fost modificat";
                    TempData["messageType"] = "alert-success";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui canal care nu va apartine";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            else
            {
                return View(requestChat);
            }
        }

        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public ActionResult Delete(int id)
        {
            var chat = db.Chats.Include(c => c.Messages)
                               .FirstOrDefault(c => c.Id == id);

            if (chat == null)
            {
                TempData["message"] = "Canalul nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            var chatUserList = db.ChatUsers.Where(cu => cu.ChatId == id).ToList();

            if (chat.CurrentUserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin"))
            {
                db.ChatUsers.RemoveRange(chatUserList);
                db.Chats.Remove(chat);
                db.SaveChanges();

                TempData["message"] = "Canalul a fost sters!";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti un canal care nu va apartine";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public ActionResult CancelJoin(int id)
        {
            var chatUser = db.ChatUsers
                             .FirstOrDefault(cu => cu.ChatId == id && cu.UserId == _userManager.GetUserId(User));

            if (chatUser == null)
            {
                TempData["message"] = "Cererea nu a fost găsită!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            db.ChatUsers.Remove(chatUser);
            db.SaveChanges();

            TempData["message"] = "Cererea a fost stearsa!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public ActionResult Reject(string id)
        {
            var parts = id.Split("-");
            var userName = parts[0];
            var chatId = int.Parse(parts[1]);
            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            if (user == null)
            {
                TempData["message"] = "Utilizatorul nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = chatId });
            }

            var chatUser = db.ChatUsers
                             .FirstOrDefault(cu => cu.ChatId == chatId && cu.UserId == user.Id);

            if (chatUser == null)
            {
                TempData["message"] = "Cererea nu a fost găsită!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = chatId });
            }

            db.ChatUsers.Remove(chatUser);
            db.SaveChanges();

            TempData["message"] = "Cererea a fost respinsa!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", new { id = chatId });
        }

        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public ActionResult Accept(string id)
        {
            var parts = id.Split("-");
            var userName = parts[0];
            var chatId = int.Parse(parts[1]);
            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            if (user == null)
            {
                TempData["message"] = "Utilizatorul nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = chatId });
            }

            var chatUser = db.ChatUsers
                             .FirstOrDefault(cu => cu.ChatId == chatId && cu.UserId == user.Id);

            if (chatUser == null)
            {
                TempData["message"] = "Cererea nu a fost găsită!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = chatId });
            }

            db.ChatUsers.Remove(chatUser);

            var updatedChatUser = new ChatUser
            {
                ChatId = chatId,
                UserId = user.Id
            };

            db.ChatUsers.Add(updatedChatUser);
            db.SaveChanges();

            TempData["message"] = "Cererea a fost acceptata!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", new { id = chatId });
        }

        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public ActionResult Join(int id)
        {
            var chatUser = new ChatUser
            {
                ChatId = id,
                UserId = _userManager.GetUserId(User)
            };

            db.ChatUsers.Add(chatUser);
            db.SaveChanges();

            TempData["message"] = "S-a solicitat inscrierea!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public ActionResult Leave(string id)
        {
            var parts = id.Split("-");
            var userName = parts[0];
            var chatId = int.Parse(parts[1]);
            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            if (user == null)
            {
                TempData["message"] = "Utilizatorul nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = chatId });
            }

            var chatUser = db.ChatUsers
                             .FirstOrDefault(cu => cu.ChatId == chatId && cu.UserId == user.Id);

            if (chatUser == null)
            {
                TempData["message"] = "Utilizatorul nu a fost găsit în canal!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Show", new { id = chatId });
            }

            db.ChatUsers.Remove(chatUser);
            db.SaveChanges();

            TempData["message"] = "Utilizatorul a fost exclus!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", new { id = chatId });
        }

        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> GetOrCreateChat(string friendId)
        {
            var currentUserId = _userManager.GetUserId(User);

            // Check if a chat already exists between the current user and the friend
            var existingChat = await db.Chats
                .FirstOrDefaultAsync(c => (c.CurrentUserId == currentUserId && c.FriendUserId == friendId) ||
                                          (c.CurrentUserId == friendId && c.FriendUserId == currentUserId));

            if (existingChat != null)
            {
                // Chat already exists, redirect to the chat
                return RedirectToAction("Show", new { id = existingChat.Id });
            }

            // Create a new chat if it doesn't exist
            var chat = new Chat
            {
                CurrentUserId = currentUserId,
                FriendUserId = friendId
            };

            if (ModelState.IsValid)
            {
                db.Chats.Add(chat);
                await db.SaveChangesAsync();

                var chatUser = new ChatUser
                {
                    ChatId = chat.Id,
                    UserId = chat.CurrentUserId
                };

                db.ChatUsers.Add(chatUser);
                await db.SaveChangesAsync();

                TempData["message"] = "The chat has been created";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Show", new { id = chat.Id });
            }
            else
            {
                TempData["message"] = "Failed to create the chat";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("ShowAllFriends");
            }
        }




        [Authorize(Roles = "Moderator,Admin,User")]
        public ActionResult Exit(string id)
        {
            var parts = id.Split("-");
            var userName = parts[0];
            var chatId = int.Parse(parts[1]);
            var user = db.Users.FirstOrDefault(u => u.UserName == userName);

            if (user == null)
            {
                TempData["message"] = "Utilizatorul nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index2", new { id = chatId });
            }

            var chatUser = db.ChatUsers
                             .FirstOrDefault(cu => cu.ChatId == chatId && cu.UserId == user.Id);

            if (chatUser == null)
            {
                TempData["message"] = "Utilizatorul nu a fost găsit în canal!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index2", new { id = chatId });
            }

            db.ChatUsers.Remove(chatUser);
            db.SaveChanges();

            return RedirectToAction("Index2", new { id = chatId });
        }

        private void SetAccessRights()
        {
            ViewBag.EsteAdmin = User.IsInRole("Admin");
            ViewBag.EsteModerator = User.IsInRole("Moderator");
            ViewBag.UserCurent = _userManager.GetUserId(User);
            ViewBag.UserNameCurent = _userManager.GetUserName(User);
        }

        public IActionResult IndexNou()
        {
            return View();
        }
    }
}
