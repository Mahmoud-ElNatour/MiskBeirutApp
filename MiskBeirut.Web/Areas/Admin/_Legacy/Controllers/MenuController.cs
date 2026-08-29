using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MiskBeirut.Application.DTOs;
using MiskBeirut.Application.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MiskBeirut.Web.Controllers
{
    [Route("[controller]")]
    public class MenuController : Controller
    {
        private readonly IMenuService _menuService;
        private readonly ISystemService _systemService;
        private readonly ILogger<MenuController> _logger;

        public MenuController(IMenuService menuService, ISystemService systemService, ILogger<MenuController> logger)
        {
            _menuService = menuService;
            _systemService = systemService;
            _logger = logger;
        }

        // GET: /Menu (Public Menu)
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Retrieving all active menu categories and items for public view");
            var menu = await _menuService.GetMenuAsync();

            // Fetch site settings for contact buttons
            var settingsList = await _systemService.GetSettingsAsync();
            var settings = settingsList.ToDictionary(s => s.Key, s => s.Value ?? "");

            string GetSetting(string key, string def) => settings.TryGetValue(key, out var val) ? val : def;

            var phoneTel = GetSetting("phone_tel", "");
            string? whatsappDigits = null;
            if (!string.IsNullOrEmpty(phoneTel))
            {
                whatsappDigits = Regex.Replace(phoneTel, @"[^\d]", "");
            }

            ViewData["PhoneTel"] = phoneTel;
            ViewData["WhatsappDigits"] = whatsappDigits;
            ViewData["InstagramUrl"] = GetSetting("instagram_url", "");
            ViewData["MapsUrl"] = GetSetting("maps_url", "");
            ViewData["Address"] = GetSetting("address", "Beirut, Coastal Road, Lebanon");

            return View("PublicMenu", menu);
        }

        // GET: /Menu/Admin
        [HttpGet("Admin")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> AdminIndex()
        {
            _logger.LogInformation("Retrieving all active menu categories and items for admin view");
            var menu = await _menuService.GetMenuAsync();
            return View("Index", menu);
        }

        // GET: /Menu/categories
        [HttpGet("categories")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetCategories()
        {
            _logger.LogInformation("Retrieving all menu categories");
            return Ok(await _menuService.GetCategoriesAsync());
        }

        // GET: /Menu/categories/5
        [HttpGet("categories/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetCategory(int id)
        {
            _logger.LogInformation("Retrieving MenuCategory ID {ResourceId}", id);
            var category = await _menuService.GetCategoryAsync(id);
            if (category == null)
            {
                _logger.LogWarning("MenuCategory with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            return Ok(category);
        }

        // POST: /Menu/categories
        [HttpPost("categories")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateCategory([FromBody] MenuCategoryDto categoryDto)
        {
            _logger.LogInformation("Creating new MenuCategory with data: {@RequestData}", categoryDto);
            var created = await _menuService.CreateCategoryAsync(categoryDto);
            _logger.LogInformation("Successfully created MenuCategory with ID: {ResourceId}", created.Id);
            return Ok(created);
        }

        // PUT: /Menu/categories/5
        [HttpPut("categories/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] MenuCategoryDto categoryDto)
        {
            _logger.LogInformation("Updating MenuCategory ID {ResourceId}", id);
            var success = await _menuService.UpdateCategoryAsync(id, categoryDto);
            if (!success)
            {
                _logger.LogWarning("MenuCategory with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully updated MenuCategory ID {ResourceId}", id);
            return NoContent();
        }

        // DELETE: /Menu/categories/5
        [HttpDelete("categories/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            _logger.LogWarning("Initiating deletion of MenuCategory ID {ResourceId}", id);
            var success = await _menuService.DeleteCategoryAsync(id);
            if (!success)
            {
                _logger.LogWarning("MenuCategory with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted MenuCategory ID {ResourceId}", id);
            return NoContent();
        }

        // GET: /Menu/items/5
        [HttpGet("items/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> GetItem(int id)
        {
            _logger.LogInformation("Retrieving MenuItem ID {ResourceId}", id);
            var item = await _menuService.GetItemAsync(id);
            if (item == null)
            {
                _logger.LogWarning("MenuItem with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            return Ok(item);
        }

        // POST: /Menu/items
        [HttpPost("items")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> CreateItem([FromBody] MenuItemDto itemDto)
        {
            _logger.LogInformation("Creating new MenuItem with data: {@RequestData}", itemDto);
            var created = await _menuService.CreateItemAsync(itemDto);
            _logger.LogInformation("Successfully created MenuItem with ID: {ResourceId}", created.Id);
            return Ok(created);
        }

        // PUT: /Menu/items/5
        [HttpPut("items/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> UpdateItem(int id, [FromBody] MenuItemDto itemDto)
        {
            _logger.LogInformation("Updating MenuItem ID {ResourceId}", id);
            var success = await _menuService.UpdateItemAsync(id, itemDto);
            if (!success)
            {
                _logger.LogWarning("MenuItem with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully updated MenuItem ID {ResourceId}", id);
            return NoContent();
        }

        // DELETE: /Menu/items/5
        [HttpDelete("items/{id}")]
        [Authorize(Roles = "admin")]
        public async Task<IActionResult> DeleteItem(int id)
        {
            _logger.LogWarning("Initiating deletion of MenuItem ID {ResourceId}", id);
            var success = await _menuService.DeleteItemAsync(id);
            if (!success)
            {
                _logger.LogWarning("MenuItem with ID {ResourceId} was not found.", id);
                return NotFound();
            }
            _logger.LogInformation("Successfully deleted MenuItem ID {ResourceId}", id);
            return NoContent();
        }
    }
}
