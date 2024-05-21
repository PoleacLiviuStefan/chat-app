using AplicatieMDS.Data;
using AplicatieMDS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AplicatieMDS.Controllers
{
    [Authorize(Roles = "Admin,User,Moderator")]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UsersController(
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
            var users = from user in db.Users
                        orderby user.UserName
                        select user;

            ViewBag.UsersList = users;

            return View();
        }

        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Index2()
        {
            var searchTerm = HttpContext.Request.Query["searchTerm"].ToString();
            IQueryable<ApplicationUser> users = from user in db.Users
                                                orderby user.UserName
                                                select user;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                users = users.Where(user => user.UserName.Contains(searchTerm));
            }

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            int totalItems = users.Count();
            int pageSize = 3;
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            if (currentPage == 0) currentPage = 1;
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            var paginatedUsers = users.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.UsersList = paginatedUsers;
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;

            return View();
        }

        public async Task<ActionResult> Show(string id)
        {
            ApplicationUser user = await db.Users.FindAsync(id);
            var roles = await _userManager.GetRolesAsync(user);

            ViewBag.Roles = roles;

            return View(user);
        }

        public async Task<ActionResult> Edit(string id)
        {
            ApplicationUser user = await db.Users.FindAsync(id);
            user.AllRoles = GetAllRoles();
            var roleNames = await _userManager.GetRolesAsync(user);
            var currentUserRole = _roleManager.Roles
                                              .Where(r => roleNames.Contains(r.Name))
                                              .Select(r => r.Id)
                                              .FirstOrDefault();

            ViewBag.UserRole = currentUserRole;

            return View(user);
        }

        [HttpPost]
        public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
        {
            ApplicationUser user = await db.Users.FindAsync(id);
            user.AllRoles = GetAllRoles();

            if (ModelState.IsValid)
            {
                user.UserName = newData.UserName;
                user.Email = newData.Email;
                user.FirstName = newData.FirstName;
                user.LastName = newData.LastName;
                user.PhoneNumber = newData.PhoneNumber;

                var roles = db.Roles.ToList();
                foreach (var role in roles)
                {
                    await _userManager.RemoveFromRoleAsync(user, role.Name);
                }

                var roleName = await _roleManager.FindByIdAsync(newRole);
                await _userManager.AddToRoleAsync(user, roleName.ToString());

                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await db.Users
                               .Include(u => u.ChatUsers)
                               .Include(u => u.Messages)
                               .FirstOrDefaultAsync(u => u.Id == id);

            if (user.Messages.Any())
            {
                db.Messages.RemoveRange(user.Messages);
            }

            if (user.ChatUsers.Any())
            {
                db.ChatUsers.RemoveRange(user.ChatUsers);
            }

            db.Users.Remove(user);
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<ActionResult> SendInvitation(string receiverId)
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(receiverId) || userId == receiverId)
            {
                TempData["Message"] = "Invalid receiver ID or cannot invite yourself.";
                TempData["MessageType"] = "alert-danger";
                return RedirectToAction("Index2");
            }

            if (await InvitationAlreadyExists(userId, receiverId))
            {
                TempData["Message"] = "Invitation already sent.";
                TempData["MessageType"] = "alert-danger";
                return RedirectToAction("Index2");
            }

            var invitation = new FriendInvitation
            {
                SenderId = userId,
                ReceiverId = receiverId
            };
            db.FriendInvitations.Add(invitation);
            await db.SaveChangesAsync();

            TempData["Message"] = "Invitation sent successfully.";
            TempData["MessageType"] = "alert-success";
            return RedirectToAction("Index2");
        }

        private async Task<bool> InvitationAlreadyExists(string senderId, string receiverId)
        {
            return await db.FriendInvitations.AnyAsync(fi => fi.SenderId == senderId && fi.ReceiverId == receiverId);
        }

        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> ReceivedInvitations()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("User not found.");
            }

            var invitations = await db.FriendInvitations
                                      .Include(fi => fi.Sender)
                                      .Where(fi => fi.ReceiverId == userId && fi.Accepted==null)
                                      .ToListAsync();

            return View(invitations);
        }

        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> AcceptInvitation(int invitationId)
        {
            var userId = _userManager.GetUserId(User);
            var invitation = await db.FriendInvitations.FindAsync(invitationId);

            if (invitation == null || invitation.ReceiverId != userId)
            {
                TempData["Message"] = "Invitation not found or you are not the receiver.";
                TempData["MessageType"] = "alert-danger";
                return RedirectToAction("ReceivedInvitations");
            }

            invitation.Accepted = true;
            db.Update(invitation);

            var userFriend = new UserFriend
            {
                UserId = userId,
                FriendId = invitation.SenderId
            };
            db.UserFriends.Add(userFriend);

            await db.SaveChangesAsync();

            TempData["Message"] = "Invitation accepted successfully.";
            TempData["MessageType"] = "alert-success";
            return RedirectToAction("Index2");
        }

        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> DeclineInvitation(int invitationId)
        {
            var userId = _userManager.GetUserId(User);
            var invitation = await db.FriendInvitations.FindAsync(invitationId);

            if (invitation == null || invitation.ReceiverId != userId)
            {
                TempData["Message"] = "Invitation not found or you are not the receiver.";
                TempData["MessageType"] = "alert-danger";
                return RedirectToAction("ReceivedInvitations");
            }

            invitation.Accepted = false;
            db.Update(invitation);
            await db.SaveChangesAsync();

            TempData["Message"] = "Invitation declined successfully.";
            TempData["MessageType"] = "alert-warning";
            return RedirectToAction("Index2");
        }

        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public async Task<IActionResult> ShowAllFriends()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound("User not found.");
            }

            var friends = await db.UserFriends
                                  .Include(uf => uf.User)
                                  .Include(uf => uf.Friend)
                                  .Where(uf => uf.UserId == userId || uf.FriendId == userId)
                                  .ToListAsync();

            return View(friends);
        }


        [NonAction]
        public IEnumerable<SelectListItem> GetAllRoles()
        {
            var selectList = new List<SelectListItem>();

            var roles = from role in db.Roles
                        select role;

            foreach (var role in roles)
            {
                selectList.Add(new SelectListItem
                {
                    Value = role.Id.ToString(),
                    Text = role.Name.ToString()
                });
            }
            return selectList;
        }
    }
}
