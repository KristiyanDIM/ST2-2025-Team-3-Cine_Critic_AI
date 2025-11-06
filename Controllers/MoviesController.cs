using Microsoft.AspNetCore.Mvc;
using Cine_Critic_AI.Models;
using Cine_Critic_AI.Services;
using Microsoft.AspNetCore.Authorization;

namespace Cine_Critic_AI.Controllers
{
    public class MoviesController : Controller
    {
        // Зависимости, инжектирани чрез DI
        private readonly DatabaseService _database;
        private readonly AppLoggerSingleton _appLogger; // Singleton Logger

        // Конструктор с Dependency Injection
        public MoviesController(DatabaseService database, AppLoggerSingleton appLogger)
        {
            _database = database;
            _appLogger = appLogger;
        }

        private string GetCurrentUser()
        {
            return User.Identity != null && User.Identity.IsAuthenticated
                ? User.Identity.Name
                : "Анонимен потребител";
        }

        // GET: Movies
        public IActionResult Index(string? search, string? genre, int? year, string? sort, DateTime? addedAfter, int page = 1)
        {
            const int pageSize = 20; // По 20 филма на страница

            var allMovies = _database.GetAllMovies(); // връща List<Movie>
            var q = allMovies.AsQueryable();

            // Филтри
            if (!string.IsNullOrWhiteSpace(search))
                q = q.Where(m => m.Title.Contains(search, StringComparison.OrdinalIgnoreCase)
                              || (m.Director ?? "").Contains(search, StringComparison.OrdinalIgnoreCase)
                              || (m.Description ?? "").Contains(search, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(genre))
                q = q.Where(m => string.Equals(m.Genre, genre, StringComparison.OrdinalIgnoreCase));

            if (year.HasValue)
                q = q.Where(m => m.Year == year.Value);

            if (addedAfter.HasValue)
                q = q.Where(m => m.AddedOn >= addedAfter.Value);

            // Сортиране
            q = sort switch
            {
                "date_asc" => q.OrderBy(m => m.AddedOn),
                "date_desc" => q.OrderByDescending(m => m.AddedOn),
                _ => q.OrderByDescending(m => m.AddedOn)
            };

            // Пагинация
            int totalMovies = q.Count();
            int totalPages = (int)Math.Ceiling(totalMovies / (double)pageSize);
            page = Math.Clamp(page, 1, totalPages == 0 ? 1 : totalPages); // безопасност

            var moviesOnPage = q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            // Данни към View
            ViewBag.Search = search;
            ViewBag.Genre = genre;
            ViewBag.Year = year;
            ViewBag.Sort = sort ?? "date_desc";
            ViewBag.AddedAfter = addedAfter;
            ViewBag.Genres = allMovies.Select(m => m.Genre).Distinct().OrderBy(g => g).ToList();
            ViewBag.Years = allMovies.Select(m => m.Year).Distinct().OrderByDescending(y => y).ToList();

            // Параметри за навигацията
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(moviesOnPage);
        }


        // GET: Movies/Details/5
        public IActionResult Details(int? id)
        {
            if (id == null)
            {
                _appLogger.Log($"{GetCurrentUser()} опита да достъпи детайли на филм без ID.");
                return NotFound();
            }

            var movie = _database.GetMovieById(id.Value);
            if (movie == null)
            {
                _appLogger.Log($"{GetCurrentUser()} опита да достъпи несъществуващ филм (ID {id}).");
                return NotFound();
            }

            _appLogger.Log($"{GetCurrentUser()} прегледа детайлите на филма: {movie.Title}");
            return View(movie);
        }

        [Authorize]
        public IActionResult Create()
        {
            // Логваме отварянето на страницата за създаване на филм
            _appLogger.Log($"{GetCurrentUser()} отвори страницата за създаване на нов филм.");

            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Movie movie)
        {
            Console.WriteLine($"DEBUG: ImageUrl = '{movie.ImageUrl}'");

            if (ModelState.IsValid)
            {
                // адаваме дата на добавяне
                movie.AddedOn = DateTime.Now;

                _database.InsertMovie(movie);

                // Лог за добавяне
                _appLogger.Log($"{GetCurrentUser()} добави нов филм: {movie.Title} ({movie.Year})");
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        [Authorize]
        public IActionResult Edit(int? id)
        {
            if (id == null)
            {
                _appLogger.Log($"{GetCurrentUser()} опита да редактира филм без ID.");
                return NotFound();
            }

            var movie = _database.GetMovieById(id.Value);
            if (movie == null)
            {
                _appLogger.Log($"{GetCurrentUser()} опита да редактира несъществуващ филм (ID {id}).");
                return NotFound();
            }

            _appLogger.Log($"{GetCurrentUser()} отвори страницата за редакция на филма: {movie.Title}");
            return View(movie);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(int id, Movie movie)
        {
            if (id != movie.Id)
            {
                _appLogger.Log($"{GetCurrentUser()} опита да редактира филм с несъответстващо ID ({id}).");
                return NotFound();
            }

            var existing = _database.GetMovieById(id);
            if (existing == null)
                return NotFound();

            if (ModelState.IsValid)
            {
                // ⚠️ Запазваме старата дата на добавяне, за да не се презаписва
                movie.AddedOn = existing.AddedOn;

                _database.UpdateMovie(movie);

                // 🪵 Лог за редактиране
                _appLogger.Log($"{GetCurrentUser()} редактира филма: {movie.Title}");
                return RedirectToAction(nameof(Index));
            }
            return View(movie);
        }

        [Authorize]
        public IActionResult Delete(int? id)
        {
            if (id == null)
            {
                _appLogger.Log($"{GetCurrentUser()} опита да изтрие филм без ID.");
                return NotFound();
            }

            var movie = _database.GetMovieById(id.Value);
            if (movie == null)
            {
                _appLogger.Log($"{GetCurrentUser()} опита да изтрие несъществуващ филм (ID {id}).");
                return NotFound();
            }

            _appLogger.Log($"{GetCurrentUser()} отвори страницата за изтриване на филма: {movie.Title}");
            return View(movie);
        }

        [Authorize]
        [HttpPost, ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            var movie = _database.GetMovieById(id);
            if (movie != null)
            {
                _database.DeleteMovie(id);
                // Логваме изтриването на филм
                _appLogger.Log($"{GetCurrentUser()} изтри филма: {movie.Title}");
            }
            else
            {
                _appLogger.Log($"{GetCurrentUser()} опита да изтрие несъществуващ филм с ID {id}.");
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
