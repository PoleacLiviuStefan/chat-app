using AplicatieMDS.Data;
using AplicatieMDS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


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
            RoleManager<IdentityRole> roleManager
            )
        {
            db = context;

            _userManager = userManager;

            _roleManager = roleManager;
        }


        // Se afiseaza lista tuturor canalelor impreuna cu categoria 
        // din care fac parte
        // Pentru fiecare canal se afiseaza si userul care a creat canalul respectiv
        // HttpGet implicit
        
        public IActionResult Index()
        {

            Dictionary<int, List<string>> chatUserDictionary = db.ChatUsers
    .AsEnumerable()
    .GroupBy(chatUser => chatUser.ChatId)
    .ToDictionary(
        group => group.Key,
        group => group.Select(chatUser => chatUser.UserId).ToList()
    );

            ViewBag.ChatAccess = chatUserDictionary;

            Dictionary<int, List<string>> chatPendingUserDictionary = db.ChatUsers
    .AsEnumerable()
    .GroupBy(chatUser => chatUser.ChatId)
    .ToDictionary(
        group => group.Key,
        group => group.Select(chatUser => chatUser.UserId).ToList()
    );

            ViewBag.ChatPendingAccess = chatPendingUserDictionary;

            var searchTerm = (HttpContext.Request.Query["searchTerm"]);

            // Alegem sa afisam 3 canale pe pagina
            int _perPage = 3;

            var chats = db.Chats.Include("User");

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var query = db.Chats
                .Include("User");

               
            }

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            // Fiind un numar variabil de canale, verificam de fiecare data utilizand
            // metoda Count()
            int totalItems = chats.Count();

            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Chats/Index?page=valoare
            var currentPage =
            Convert.ToInt32(HttpContext.Request.Query["page"]);
            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3
            // Asadar offsetul este egal cu numarul de canale care au fost deja afisate pe paginile anterioare
            var offset = 0;
            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }



            // Se preiau canalele corespunzatoare pentru fiecare pagina la care ne aflam
            // in functie de offset
            var paginatedChats =
            chats.Skip(offset).Take(_perPage);

            // Preluam numarul ultimei pagini
            ViewBag.lastPage = Math.Ceiling((float)totalItems /
            (float)_perPage);
            // Trimitem canalele cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Chats = paginatedChats;

            SetAccessRights();

            return View();
        }

        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Index2()
        {

            Dictionary<int, List<string>> chatUserDictionary = db.ChatUsers
    .AsEnumerable()
    .GroupBy(chatUser => chatUser.ChatId)
    .ToDictionary(
        group => group.Key,
        group => group.Select(chatUser => chatUser.UserId).ToList()
    );

            ViewBag.ChatAccess = chatUserDictionary;

            Dictionary<int, List<string>> chatPendingUserDictionary = db.ChatUsers
    .AsEnumerable()
    .GroupBy(chatUser => chatUser.ChatId)
    .ToDictionary(
        group => group.Key,
        group => group.Select(chatUser => chatUser.UserId).ToList()
    );

            ViewBag.ChatPendingAccess = chatPendingUserDictionary;

            var searchTerm = (HttpContext.Request.Query["searchTerm"]);

            // Alegem sa afisam 3 canale pe pagina
            int _perPage = 3;

            List<int> chatInclude = db.ChatUsers.Where(row => row.UserId == _userManager.GetUserId(User)).Select(row => row.ChatId).ToList();


            if (chatInclude.Count == 0)
            {
                TempData["message"] = "Nu sunteti inscris in niciun canal!";
                TempData["messageType"] = "alert-danger";
            }


            ViewBag.ChatInclude = chatInclude;

            var chats = db.Chats.Include("User").Where(row => chatInclude.Contains(row.Id));



            if (!string.IsNullOrEmpty(searchTerm))
            {
                var query = db.Chats
                .Include("User");

                query = query.Where(ch => ch.UserId.Contains(searchTerm));

                chats = query;
             
            }



            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            // Fiind un numar variabil de canale, verificam de fiecare data utilizand
            // metoda Count()
            int totalItems = chats.Count();

            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Chats/Index?page=valoare
            var currentPage =
            Convert.ToInt32(HttpContext.Request.Query["page"]);
            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3
            // Asadar offsetul este egal cu numarul de canale care au fost deja afisate pe paginile anterioare
            var offset = 0;
            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }



            // Se preiau canalele corespunzatoare pentru fiecare pagina la care ne aflam
            // in functie de offset
            var paginatedChats =
            chats.Skip(offset).Take(_perPage);

            // Preluam numarul ultimei pagini
            ViewBag.lastPage = Math.Ceiling((float)totalItems /
            (float)_perPage);
            // Trimitem canalele cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Chats = paginatedChats;

            SetAccessRights();

            return View();
        }

        // Se afiseaza un singur canal in functie de id-ul sau 
        // impreuna cu categoria din care face parte
        // In plus sunt preluate si toate mesajele asociate unui canal
        // Se afiseaza si userul care a postat canalul respectiv
        // HttpGet implicit

        [Authorize(Roles = "User,Moderator,Admin")]
        public IActionResult Show(int id)
        {
            Chat chat = db.Chats.Include("User")
                                         .Include("Messages")
                                         .Include("Messages.User")
                                         .Where(ch => ch.Id == id)
                                         .First();

            List<ChatUser> chatUserList = db.ChatUsers
                                                      .Where(ch => ch.ChatId == id).ToList();

      


            SetAccessRights();

            if (User.IsInRole("Admin") || User.IsInRole("Moderator"))
            {
                return View(chat);
            }
            else

                foreach (ChatUser user in chatUserList)
                {
                    if (user.UserId == _userManager.GetUserId(User))
                        return View(chat);
                }

            TempData["message"] = "Nu aveti drepturi pentru a vizualiza acest canal!";
            TempData["messageType"] = "alert-danger";


            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            return RedirectToAction("Index");



        }


        // Adaugarea unui mesaj asociat unui canal in baza de date
        // Toate rolurile pot adauga mesaje in baza de date
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
                Chat ch = db.Chats.Include("User")
                                         .Include("Messages")
                                         .Include("Messages.User")
                                         .Where(ch => ch.Id == message.ChatId)
                                         .First();





                SetAccessRights();

                return View(ch);
            }
        }



        // Se afiseaza formularul in care se vor completa datele unui canal
        // impreuna cu selectarea categoriei din care face parte

        // HttpGet implicit

        [Authorize(Roles = "Moderator,Admin,User")]
        public IActionResult New()
        {
            Chat chat = new Chat();

            // Se preia lista de categorii cu ajutorul metodei GetAllCategories()
            


            return View(chat);
        }

        // Se adauga canalul in baza de date

        [Authorize(Roles = "Moderator,Admin,User")]
        [HttpPost]
        public IActionResult New(Chat chat)
        {
         
            // preluam id-ul utilizatorului care creeaza canalul
            chat.UserId = _userManager.GetUserId(User);

            if (ModelState.IsValid)
            {
                db.Chats.Add(chat);

                db.SaveChanges();

                ChatUser chatUser = new ChatUser();
                chatUser.ChatId = chat.Id;
                chatUser.UserId = chat.UserId;
             
                db.ChatUsers.Add(chatUser);

                db.SaveChanges();

                TempData["message"] = "Canalul a fost creat";
                TempData["messageType"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
               
                return View(chat);
            }
        }

        // Se editeaza un canal existent in baza de date impreuna cu categoria din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // Se afiseaza formularul impreuna cu datele aferente canalului din baza de date
        // Doar utilizatorii cu rolul de Moderator sau Admin pot edita canale
        // Adminii pot edita orice canal din baza de date
        // Moderatorii pot edita doar canelele proprii (cele pe care ei le-au creat)
        // HttpGet implicit

        [Authorize(Roles = "Moderator,Admin,User")]
        public IActionResult Edit(int id)
        {

            Chat chat = db.Chats.Where(ch => ch.Id == id)
                                        .First();

           
            if (chat.UserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin"))
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

        // Se adauga canalul modificat in baza de date
        // Verificam rolul utilizatorilor care au dreptul sa editeze (Moderator,Admin sau User)
        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public IActionResult Edit(int id, Chat requestChat)
        {
            Chat chat = db.Chats.Find(id);


            if (ModelState.IsValid)
            {
                if (chat.UserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin"))
                {
                 
                    TempData["message"] = "Canalul a fost modificat";
                    TempData["messageType"] = "alert-success";
                    db.SaveChanges();
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



        // Se sterge un canal din baza de date
        // Utilizatorii cu rolul de Moderator sau Admin pot sterge canalele
        // Adminii pot sterge orice canal din baza de date

        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public ActionResult Delete(int id)
        {
            Chat chat = db.Chats.Include("Messages")
                                         .Where(ch => ch.Id == id)
                                         .First();

            List<ChatUser> chatUserList = db.ChatUsers.Where(ch => ch.ChatId == id).ToList();

            if (chat.UserId == _userManager.GetUserId(User) || User.IsInRole("Moderator") || User.IsInRole("Admin"))
            {
                for (int i = 0; i < chatUserList.Count(); i++)
                    db.ChatUsers.Remove(chatUserList[i]);

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
            ChatUser chatUser = db.ChatUsers
            .FirstOrDefault(ch => ch.ChatId == id && ch.UserId == _userManager.GetUserId(User));

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
            var strings = id.Split("-");
            var userName = strings[0];
            var chatId = int.Parse(strings[1]);
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName == userName);


            ChatUser chatUser = db.ChatUsers
            .FirstOrDefault(ch => ch.ChatId == chatId && ch.UserId == user.Id);

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
            var strings = id.Split("-");
            var userName = strings[0];
            var chatId = int.Parse(strings[1]);
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName == userName);


            ChatUser chatUser = db.ChatUsers
            .FirstOrDefault(ch => ch.ChatId == chatId && ch.UserId == user.Id);
            db.ChatUsers.Remove(chatUser);

            ChatUser updatedChatUser = new ChatUser();
            updatedChatUser.ChatId = chatId;
            updatedChatUser.UserId = user.Id;
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
            ChatUser chatUser = new ChatUser();

            chatUser.ChatId = id;
            chatUser.UserId = _userManager.GetUserId(User);


            db.ChatUsers.Add(chatUser);
            db.SaveChanges();


            TempData["message"] = "S-a solicitat inscrierea!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Index");
        }


        //Eliminarea userilor din canale
        [HttpPost]
        [Authorize(Roles = "Moderator,Admin,User")]
        public ActionResult Leave(string id)
        {
            var strings = id.Split("-");
            var userName = strings[0];
            var channelId = int.Parse(strings[1]);
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName == userName);


            ChatUser chatUser = db.ChatUsers
            .FirstOrDefault(ch => ch.ChatId == channelId && ch.UserId == user.Id);

            db.ChatUsers.Remove(chatUser);
            db.SaveChanges();


            TempData["message"] = "Utilizatorul a fost exclus!";
            TempData["messageType"] = "alert-success";
            return RedirectToAction("Show", new { id = channelId });
        }

        //Userii pot iesi singuri din canale
        public ActionResult Exit(string id)
        {
            var strings = id.Split("-");
            var userName = strings[0];
            var channelId = int.Parse(strings[1]);
            ApplicationUser user = db.Users.FirstOrDefault(u => u.UserName == userName);


            ChatUser chatUser = db.ChatUsers
            .FirstOrDefault(ch => ch.ChatId == channelId && ch.UserId == user.Id);

            db.ChatUsers.Remove(chatUser);
            db.SaveChanges();

            return RedirectToAction("Index2", new { id = channelId });
        }


        // Conditiile de afisare a butoanelor de editare si stergere
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
