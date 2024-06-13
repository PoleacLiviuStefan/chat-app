using GEmojiSharp;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace AplicatieMDS.ViewComponents
{
    public class EmojiPickerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke()
        {
            var emojis = Emoji.All;
            return View(emojis);
        }
    }
}
