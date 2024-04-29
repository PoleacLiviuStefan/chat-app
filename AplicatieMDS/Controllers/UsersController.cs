using AplicatieMDS.Data;
using AplicatieMDS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace AplicatieMDS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class UsersController : Controller
    {
            private readonly ApplicationDbContext db;

            private readonly UserManager<ApplicationUser> _userManager;

            private readonly RoleManager<IdentityRole> _roleManager;

            public UsersController(
                ApplicationDbContext context,
                UserManager<ApplicationUser> userManager,
                RoleManager<IdentityRole> roleManager
                )
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

            // Extragem utilizatorii și îi ordonăm
            IQueryable<ApplicationUser> users = from user in db.Users
                                                orderby user.UserName
                                                select user;

            // Verificăm dacă un termen de căutare a fost furnizat și filtrăm lista de utilizatori
            if (!string.IsNullOrEmpty(searchTerm))
            {
                users = users.Where(user => user.UserName.Contains(searchTerm));
            }

            // Afisarea mesajului din TempData, dacă există
            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            // Numărul total de utilizatori după filtrare
            int totalItems = users.Count();

            // Stabilim numărul de utilizatori pe pagină
            int pageSize = 3;

            // Obținem numărul paginii curente sau folosim 1 dacă nu este specificat
            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);
            if (currentPage == 0) currentPage = 1;

            // Calculăm numărul total de pagini
            var totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // Extragem utilizatorii pentru pagina curentă
            var paginatedUsers = users.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

            // Pasăm lista de utilizatori, numărul total de pagini și pagina curentă la View
            ViewBag.UsersList = paginatedUsers;
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;

            return View();
        }



        public async Task<ActionResult> Show(string id)
            {
                ApplicationUser user = db.Users.Find(id);
                var roles = await _userManager.GetRolesAsync(user);

                ViewBag.Roles = roles;

                return View(user);
            }

            public async Task<ActionResult> Edit(string id)
            {
                ApplicationUser user = db.Users.Find(id);

                user.AllRoles = GetAllRoles();

                var roleNames = await _userManager.GetRolesAsync(user); // Lista de nume de roluri

                // Cautam ID-ul rolului in baza de date
                var currentUserRole = _roleManager.Roles
                                                  .Where(r => roleNames.Contains(r.Name))
                                                  .Select(r => r.Id)
                                                  .First(); // Selectam 1 singur rol
                ViewBag.UserRole = currentUserRole;

                return View(user);
            }

            [HttpPost]
            public async Task<ActionResult> Edit(string id, ApplicationUser newData, [FromForm] string newRole)
            {
                ApplicationUser user = db.Users.Find(id);

                user.AllRoles = GetAllRoles();


                if (ModelState.IsValid)
                {
                    user.UserName = newData.UserName;
                    user.Email = newData.Email;
                    user.FirstName = newData.FirstName;
                    user.LastName = newData.LastName;
                    user.PhoneNumber = newData.PhoneNumber;


                    // Cautam toate rolurile din baza de date
                    var roles = db.Roles.ToList();

                    foreach (var role in roles)
                    {
                        // Scoatem userul din rolurile anterioare
                        await _userManager.RemoveFromRoleAsync(user, role.Name);
                    }
                    // Adaugam noul rol selectat
                    var roleName = await _roleManager.FindByIdAsync(newRole);
                    await _userManager.AddToRoleAsync(user, roleName.ToString());

                    db.SaveChanges();

                }
                return RedirectToAction("Index");
            }


            [HttpPost]
            public IActionResult Delete(string id)
            {
                var user = db.Users
                             .Include("Chat")
                             .Include("Messages")
                             .Where(u => u.Id == id)
                             .First();

                // Delete user messages
                if (user.Messages.Count > 0)
                {
                    foreach (var message in user.Messages)
                    {
                        db.Messages.Remove(message);
                    }
                }



                // Delete user chats
                if (user.Chats.Count > 0)
                {
                    foreach (var chat in user.Chats)
                    {
                        db.Chats.Remove(chat);
                    }
                }

                db.ApplicationUsers.Remove(user);

                db.SaveChanges();

                return RedirectToAction("Index");
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

