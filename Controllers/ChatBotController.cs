using Cine_Critic_AI.Models;
using Cine_Critic_AI.Services;
using Cine_Critic_AI.Services.ChatStrategies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Cine_Critic_AI.Controllers
{
    public class ChatBotController : Controller
    {
        private readonly LocalAIService _ai;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly AppLoggerSingleton _appLogger; // Singleton Logger

        public ChatBotController(LocalAIService ai, IHttpContextAccessor httpContextAccessor, AppLoggerSingleton appLogger)
        {
            _ai = ai;
            _httpContextAccessor = httpContextAccessor;
            _appLogger = appLogger;
        }


        [HttpGet]
        public IActionResult Index()
        {
            _appLogger.Log("Потребителят е посетил страницата с Чат бота.");

            // Вземаме userId от сесия
            var userId = HttpContext.Session.GetInt32("UserId") ?? 0;

            List<ChatMessage> messages = new List<ChatMessage>();

            if (userId > 0)
            {
                // Взимаме съобщенията на потребителя
                messages = DatabaseService.Instance.GetChatMessagesByUser(userId);
            }

            return View(messages);
        }
        [HttpPost]
        public async Task<IActionResult> SendMessage([FromForm] string userMessage, [FromForm] int userId)
        {
            if (userId <= 0)
                return Json(new { response = "⚠️ Не е намерен валиден потребител. Моля, влезте в системата." });

            if (string.IsNullOrWhiteSpace(userMessage))
                return Json(new { response = "Моля, попитай нещо за филми 🎬" });

            var userChatMessage = new ChatMessage
            {
                UserId = userId,
                Sender = "User",
                Message = userMessage.Trim(),
                Timestamp = DateTime.Now
            };
            DatabaseService.Instance.InsertChatMessage(userChatMessage);
            
            // 🔹 Избор на стратегия
            var chatContext = new ChatContext();
            IChatStrategy chosenStrategy;

            if (Regex.IsMatch(userMessage, "препоръ", RegexOptions.IgnoreCase))
                chosenStrategy = new RecommendationStrategy();
            else if (Regex.IsMatch(userMessage, "оцен", RegexOptions.IgnoreCase))
                chosenStrategy = new RatingStrategy();
            else
                chosenStrategy = new AnalysisStrategy();

            chatContext.SetStrategy(chosenStrategy);

            _appLogger.Log($"Използвана стратегия: {chosenStrategy.GetType().Name} за съобщение '{userMessage}'.");


            // 🔹 Изпълнение на стратегия
            string responseText = await chatContext.ExecuteStrategy(userMessage, userId, _ai);
            responseText = string.IsNullOrWhiteSpace(responseText) ? "⚠️ AI не върна отговор." : responseText;

            var botChatMessage = new ChatMessage
            {
                UserId = userId,
                Sender = "Bot",
                Message = responseText,
                Timestamp = DateTime.Now
            };
            DatabaseService.Instance.InsertChatMessage(botChatMessage);

            return Json(new { response = responseText });
        }


        [HttpPost]
        public IActionResult ClearChat([FromForm] int userId)
        {
            DatabaseService.Instance.ClearChatByUser(userId);
            return Ok();
        }




    }
}
