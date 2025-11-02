# 🎬 CineCritic AI  
### STD-2025-Team-CineCriticAI  

Това е ASP.NET Core MVC уеб приложение за филмови ревюта, което комбинира традиционен CRUD модел с изкуствен интелект. 
Потребителите могат да разглеждат и създават ревюта, а интегрираният AI (чрез Ollama + Llama 3) може автоматично да 
генерира мнения, оценки и да анализира емоционалния тон на текстовете. 
Системата комбинира **MVC архитектура**, **Entity Framework**, **Singleton**, **Factory** и **Decorator** дизайн патърни.

Проектът е разработен като част от университетската дисциплина **"Софтуерни технологии 2" (2025)**.

---

## ⚙️ Основни технологии

| Компонент | Технология |
|------------|-------------|
| Backend | ASP.NET Core MVC (.NET 9.0) |
| Database | SQL Server / SQLite (CineCriticDB.sqlite) |
| ORM | Entity Framework Core 9.0.9 |
| Frontend | Razor Views, Bootstrap |
| AI | Ollama (Llama 3 Model) |
| Logging | Singleton AppLoggerSingleton |
| Hosting | IIS Express / Kestrel (локално) |
| Design Pattern-и | Singleton, Factory, Dependency Injection |

---

## 🧩 Архитектура

Проектът следва **Model–View–Controller (MVC)** шаблона:
Cine_Critic_AI/
│
├── Controllers/ → Контролери (Movies, Reviews, Account, ChatBot, Statistics, Home)
├── Models/ → Модели (Movie, Review, User, ViewModels)
├── Views/ → Razor изгледи по контролери
├── Services/ → Singleton и Factory AI логика
├── Database/ → DatabaseService (CRUD операции)
├── wwwroot/ → CSS, JS, изображения
├── Program.cs → Конфигурация и middleware
└── appsettings.json → Настройки на базата и логването


---

## 🧱 Design Patterns

Проектът имплементира три основни шаблона за проектиране:

1. **Singleton**  
   - Използван за `AppLoggerSingleton` и `DatabaseService`.  
   - Гарантира, че логерът и връзката с базата данни съществуват само в една инстанция.

2. **Factory Method**  
   - Използван за създаване на различни типове ревюта (`IReviewFactory`, `AIReviewFactory`, `ManualReviewFactory`).  
   - Позволява лесно разширяване на системата с нови видове анализи.

3. **Decorator**  
   - Използван при обогатяването на ревютата с AI оценка и емоционален тон.  
   - Добавя функционалност без промяна на основния модел `Review`.

---

## 💾 CRUD функционалности

**Филми (Movies)**  
- Добавяне, редактиране, преглед и изтриване на филми.  
- Показване на всички филми в модерен киносалонов изглед.

**Ревюта (Reviews)**  
- Потребителите могат да създават ревю за всеки филм.  
- AI анализаторът автоматично определя емоционалния тон на текста.  
- CRUD операции: `Create`, `Edit`, `Details`, `Delete`.

**Потребители (Account)**  
- Регистрация, вход и редакция на профил.  

---

## 🤖 AI интеграция

AI модулът (`LocalAIService`) комуникира с локален езиков модел за:
- Автоматично генериране на **емоционален тон** (Positive / Neutral / Negative)
- Кратък **AI summary** на ревюто
- Използва **Ollama** и модел **Llama 3**
- Генерира текстови ревюта с оценка и емоция
- Анализира емоционалния тон 
- Поддържа стрийминг отговори и fallback механизъм

---

## 📊 Статистики

Панелът **StatisticsController** визуализира обобщени данни:
- Средна оценка на филмите  
- Най-популярен филм  
- Обща бройка на филмите и ревютата  

---

## 🧪 Тестове и стабилност

- Логване на събития чрез `AppLoggerSingleton`  
- Валидация на данни чрез `[Required]`, `[Range]`, `[StringLength]`  
- Middleware за грешки → `Shared/Error.cshtml`

---

## 🗂️ Структура на базата данни

**Таблици:**
- `Movies (Id, Title, Year, Genre, Director, Description)`
- `Reviews (Id, Rate, Comment, EmotionTone, Date)`
- `Users (Id, Username, PasswordHash, Email, Role)`

**Връзки:**
- Един филм → Много ревюта  
- Един потребител → Много ревюта

---
