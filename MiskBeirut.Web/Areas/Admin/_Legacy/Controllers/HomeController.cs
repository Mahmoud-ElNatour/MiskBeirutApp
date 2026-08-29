using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MiskBeirut.Application.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiskBeirut.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ISystemService _systemService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ISystemService systemService, ILogger<HomeController> logger)
        {
            _systemService = systemService;
            _logger = logger;
        }

        [HttpGet("/index")]
        [HttpGet("index")]
        [Authorize]
        public IActionResult Index()
        {
            _logger.LogInformation("User {UserName} is accessing the dashboard.", User.Identity?.Name);
            ViewData["Title"] = "Home Dashboard";
            return View();
        }

        [HttpGet("")]
        [HttpGet("/landing")]
        public async Task<IActionResult> Landing()
        {
            _logger.LogInformation("Visitor accessed the landing page.");
            var settingsList = await _systemService.GetSettingsAsync();
            var settings = settingsList.ToDictionary(s => s.Key, s => s.Value ?? "");

            string GetSetting(string key, string def) => settings.TryGetValue(key, out var val) ? val : def;

            var phoneTel = GetSetting("phone_tel", "");
            string? whatsappDigits = null;
            if (!string.IsNullOrEmpty(phoneTel))
            {
                whatsappDigits = Regex.Replace(phoneTel, @"[^\d]", "");
            }

            ViewData["BrandName"] = GetSetting("brand_name", "Misk Beirut");
            ViewData["Address"] = GetSetting("address", "Beirut, Coastal Road, Lebanon");
            ViewData["Hours"] = GetSetting("hours", "Mon-Sun: 8:00 AM - 12:00 AM");
            ViewData["PhoneDisplay"] = GetSetting("phone_display", "+961 1 000 000");
            ViewData["PhoneTel"] = phoneTel;
            ViewData["WhatsappDigits"] = whatsappDigits;
            ViewData["InstagramUrl"] = GetSetting("instagram_url", "");
            ViewData["MapsUrl"] = GetSetting("maps_url", "");
            ViewData["MenuUrl"] = GetSetting("menu_url", "");

            var gallery = await _systemService.GetGalleryAsync();
            var activeGallery = gallery.Where(g => g.IsActive).ToList();

            ViewData["IsAdmin"] = User.Identity?.IsAuthenticated == true && User.IsInRole("admin");

            return View("Landing", activeGallery);
        }
    }
}
