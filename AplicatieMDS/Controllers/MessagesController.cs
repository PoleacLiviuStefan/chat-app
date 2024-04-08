using AplicatieMDS.Data;
using AplicatieMDS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Channels;

namespace AplicatieMDS.Controllers
{
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext db;

        private readonly UserManager<ApplicationUser> _userManager;

        private readonly RoleManager<IdentityRole> _roleManager;

        public MessagesController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }


        // Adaugarea unui mesaj asociat unui chat in baza de date
        [HttpPost]
        public IActionResult New(Message mess)
        {
            mess.Date = DateTime.Now;

            if (ModelState.IsValid)
            {
                db.Messages.Add(mess);
                db.SaveChanges();
                return Redirect("/Chats/Show/" + mess.ChatId);
            }

            else
            {
                return Redirect("/Chats/Show/" + mess.ChatId);
            }

        }


        // Stergerea unui mesaj asociat unui chat din baza de date
        // Se poate sterge mesajul doar de catre userii cu rolul Admin sau Moderator
        // sau de catre userii cu rolul User doar daca mesajul 
        // a fost lasat de acestia
        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Delete(int id)
        {
            Message mess = db.Messages.Find(id);

            if (mess.UserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin"))
            {
                db.Messages.Remove(mess);
                db.SaveChanges();
                return Redirect("/Chats/Show/" + mess.ChatId);
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa stergeti mesajul";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Chats");
            }
        }

        // In acest moment vom implementa editarea intr-o pagina View separata
        // Se editeaza un mesaj existent
        // Editarea unui mesaj asociat unui canal din baza de date
        // Se poate edita mesajul doar de catre userii cu rolul Admin sau Moderator
        // sau de catre userii cu rolul User doar daca mesajul 
        // a fost lasat de acestia
        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Edit(int id, int chatId)
        {
            Message mess = db.Messages.Find(id);
            Chat ch = db.Chats.Find(chatId);

            if (mess.UserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin") || ch.UserId == _userManager.GetUserId(User))
            {
                return View(mess);
            }

            else
            {
                TempData["message"] = "Nu aveti dreptul sa editati mesajul";
                TempData["messageType"] = "alert-danger";
                ViewBag.message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
                return RedirectToAction("Show", "Chats", new { id = chatId });
            }

        }

        [HttpPost]
        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Edit(int id, Message requestMessage)
        {
            Message mess = db.Messages.Find(id);

            if (mess.UserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin"))
            {
                if (ModelState.IsValid)
                {
                    mess.Content = requestMessage.Content;

                    db.SaveChanges();

                    return Redirect("/Chats/Show/" + mess.ChatId);
                }
                else
                {
                    return View(requestMessage);
                }
            }
            else
            {
                TempData["message"] = "Nu aveti dreptul sa faceti modificari";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index", "Chats");
            }

        }
    }
}
