using Cine_Critic_AI.Models;
using Cine_Critic_AI.Services;
using Cine_Critic_AI.Services.Factories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.RegularExpressions;

namespace Cine_Critic_AI.Controllers
{
    public class ReviewsController : Controller
    {
        // Зависимости, инжектирани чрез DI
        private readonly DatabaseService _database;
        private readonly AppLoggerSingleton _appLogger;
        private readonly LocalAIService _ai;
        private readonly IReviewFactory _reviewFactory;

        // Конструктор с Dependency Injection
        public ReviewsController(DatabaseService database, AppLoggerSingleton appLogger, LocalAIService ai)
        {
            _database = database;
            _appLogger = appLogger;
            _ai = ai;

            // Избираш коя фабрика да използваш
            _reviewFactory = new ManualReviewFactory(); 
            _reviewFactory = new AIReviewFactory(_ai);

        }

        // GET: Reviews
        public IActionResult Index()
        {
            var reviews = _database.GetAllReviews();
            return View(reviews);
        }

        // GET: Reviews/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
                return NotFound();

            var review = _database.GetReviewById(id.Value);
            if (review == null)
                return NotFound();

            return View(review);
        }

        // GET: Reviews/Create
        [Authorize]
        public IActionResult Create()
        {
            PopulateMoviesDropDown();
            return View();
        }

        // POST: Reviews/Create
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Review review)
        {
            if (ModelState.IsValid)
            {
                _database.InsertReview(review);
                // Логваме събитието чрез Singleton Logger
                _appLogger.Log($"Потребителят създаде ново ревю (ID {review.Id}).");
                return RedirectToAction(nameof(Index));
            }

            PopulateMoviesDropDown(review.MovieId);
            return View(review);
        }

        // GET: Reviews/Edit/5
        [Authorize]
        public IActionResult Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var review = _database.GetReviewById(id.Value);
            if (review == null)
                return NotFound();

            PopulateMoviesDropDown(review.MovieId);
            return View(review);
        }

        // POST: Reviews/Edit/5
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Review review)
        {
            if (id != review.Id)
                return NotFound();

            if (ModelState.IsValid)
            {
                _database.UpdateReview(review);
                // Логваме редакцията чрез Singleton Logger
                _appLogger.Log($"Потребителят редактира ревюто (ID {review.Id}).");
                return RedirectToAction(nameof(Index));
            }

            PopulateMoviesDropDown(review.MovieId);
            return View(review);
        }

        // GET: Reviews/Delete/5
        [Authorize]
        public IActionResult Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var review = _database.GetReviewById(id.Value);
            if (review == null)
                return NotFound();

            return View(review);
        }

        // POST: Reviews/DeleteConfirmed/5
        [Authorize]
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var review = _database.GetReviewById(id);
            if (review != null)
            {
                _database.DeleteReview(id);
                // Логваме изтриването чрез Singleton Logger
                _appLogger.Log($"Потребителят изтри ревюто (ID {id}).");
            }

            return RedirectToAction(nameof(Index));
        }

        // AI функционалност
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Generate(string description)
        {
            if (string.IsNullOrWhiteSpace(description))
            {
                return Json(new { error = "Моля, въведете описание на филма." });
            }

            // 🔹 Разпознава и отрицателни числа, например "-2" или "–3"
            var match = Regex.Match(description, @"-?\d+");
            if (match.Success && int.TryParse(match.Value, out int rate))
            {
                if (rate < 1 || rate > 5)
                {
                    // ⚠️ Извън диапазона 1–5
                    return Json(new { error = "Моля, въведете оценка между 1 и 5." });
                }
            }

            // 🔹 Генериране само ако оценката е валидна
            var generatedText = await _ai.GenerateReviewAsync("AI Generated Review", description);
            var emotion = await _ai.ExtractEmotionFromTextAsync(generatedText);
            var rating = ExtractRatingFromText(generatedText ?? "");

            return Json(new
            {
                comment = generatedText ?? "",
                emotion = emotion ?? "неутрален",
                rate = rating > 0 ? rating : 3
            });
        }



        private int ExtractRatingFromText(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return 0;

            var match = Regex.Match(text, @"([1-5])\s*(\/\s*5|от\s*5|рейтинг|rating|оценка)?", RegexOptions.IgnoreCase);
            return match.Success && int.TryParse(match.Groups[1].Value, out int value) ? value : 0;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AnalyzeEmotion([FromForm] string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return Content("неутрален");

            try
            {
                var emotion = await _ai.ExtractEmotionFromTextAsync(text);
                if (string.IsNullOrWhiteSpace(emotion))
                    emotion = "неутрален";

                return Content(emotion);
            }
            catch (Exception ex)
            {
                // Логваме грешките чрез Singleton Logger
                _appLogger.Log($"AnalyzeEmotion error: {ex.Message}");
                return Content("грешка при анализ");
            }
        }

        // ✅ В помощен метод попълваме падащото меню с филми
        private void PopulateMoviesDropDown(object selectedMovieId = null)
        {
            var movies = _database.GetAllMovies(); // метод за всички филми
            ViewBag.Movies = new SelectList(movies, "Id", "Title", selectedMovieId);
        }
    }
}
