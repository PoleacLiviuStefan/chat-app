using AplicatieMDS.Data;
using AplicatieMDS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            RoleManager<IdentityRole> roleManager)
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Add a new message associated with a chat in the database
        [HttpPost]
        public IActionResult New(Message mess)
        {
            mess.Date = DateTime.Now;
            mess.Status = Message.MessageStatus.Unseen; // Ensure the correct enum usage
            mess.UserId = _userManager.GetUserId(User); // Ensure the message has the correct user ID

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

        // Delete a message associated with a chat from the database
        // Only users with the role Admin or Moderator, or the users who left the message can delete it
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

        // Edit an existing message
        // Only users with the role Admin, Moderator, or the users who left the message can edit it
        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Edit(int id, int chatId)
        {
            Message mess = db.Messages.Find(id);
            Chat ch = db.Chats.Include(c => c.CurrentUser).FirstOrDefault(c => c.Id == chatId);

            if (mess.UserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin") || ch.CurrentUserId == _userManager.GetUserId(User))
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
                    mess.Status = requestMessage.Status;

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
