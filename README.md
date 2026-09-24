# WebPageAnalyzer API

REST API на .NET 10 для анализа HTML-страницы, переданной в формате Base64.

Приложение находит элементы по CSS-селектору, извлекает значения указанного атрибута и адреса электронной почты, расшифровывает текст с помощью AES-256 и сохраняет найденные элементы в PostgreSQL.

## Возможности

- валидация входного запроса с помощью FluentValidation;
- декодирование URL и HTML-страницы из Base64;
- разбор HTML и поиск элементов с помощью AngleSharp;
- извлечение адресов электронной почты скомпилированным регулярным выражением;
- расшифровка AES-256 в режиме ECB с `PaddingMode.None`;
- сохранение найденных элементов в PostgreSQL через Dapper;
- единый JSON-формат успешных ответов и ошибок;
- Swagger UI для отправки и проверки запросов.

## Технологии

- .NET 10 / ASP.NET Core Web API;
- FluentValidation;
- AngleSharp;
- Dapper;
- PostgreSQL 18;
- pgAdmin 4;
- Docker Compose.

## Требования

Для запуска необходимы Docker Desktop и Docker Compose.

## Запуск

В корне проекта выполните:

```powershell
docker compose up --build
```

Исходный код API монтируется в контейнер. При каждом запуске контейнер выполняет восстановление зависимостей, сборку проекта и запуск приложения.

После запуска будут доступны:

- Swagger UI: <http://localhost:8090/api/swagger>;
- API endpoint: <http://localhost:8090/api/analyze>;
- pgAdmin: <http://localhost:8080>.

Доступ к PostgreSQL в pgAdmin настроен автоматически. Вводить учётные данные при открытии pgAdmin не требуется.

## Использование API

### Анализ страницы

```http
POST /api/analyze
Content-Type: application/json
```

Тело запроса:

```json
{
  "selector": "a[href]",
  "attribute": "href",
  "url_b64": "...",
  "encrypted_text_bytes_b64": "...",
  "key_bytes_b64": "...",
  "page_b64": "..."
}
```

Назначение полей:

- `selector` — CSS-селектор искомых элементов;
- `attribute` — имя извлекаемого HTML-атрибута;
- `url_b64` — URL страницы в формате Base64;
- `encrypted_text_bytes_b64` — зашифрованный текст в формате Base64;
- `key_bytes_b64` — 256-битный ключ AES в формате Base64;
- `page_b64` — HTML-код страницы в формате Base64.

Пример запроса с использованием тестового файла:

```powershell
curl.exe -X POST "http://localhost:8090/api/analyze" `
  -H "Content-Type: application/json" `
  --data-binary "@json_payload_1.txt"
```

## Тестовые данные и результаты

В корне проекта находятся:

- `json_payload_1.txt` и `json_payload_2.txt` — тестовые входные запросы;
- `json_result_1.txt` и `json_result_2.txt` — результаты обработки соответствующих запросов.

## PostgreSQL

Найденные элементы сохраняются в таблицу `elements`:

- `id` — идентификатор записи;
- `attribute_value` — значение запрошенного атрибута;
- `element_html` — полная HTML-разметка элемента.

Данные PostgreSQL хранятся в именованном Docker volume и сохраняются после остановки контейнеров.

## Остановка

Остановить контейнеры без удаления данных PostgreSQL:

```powershell
docker compose down
```

Остановить контейнеры и удалить volume с данными PostgreSQL:

```powershell
docker compose down -v
```

Команда с параметром `-v` безвозвратно удаляет локальные данные базы.

## Локальная сборка

Для проверки сборки без Docker необходим .NET 10 SDK:

```powershell
dotnet build WebPageAnalyzer.Api.slnx
```

## Структура проекта

```text
.
├── database/                      # Инициализация PostgreSQL
├── pgadmin/                       # Конфигурация подключения pgAdmin
├── WebPageAnalyzer.Api/
│   ├── Controllers/               # API-контроллер
│   ├── Models/                    # Модели запросов, ответов и данных
│   ├── Services/                  # Бизнес-логика анализа страницы
│   ├── Validators/                # FluentValidation-правила
│   ├── Dockerfile
│   └── Program.cs
├── compose.yml
├── json_payload_1.txt
├── json_payload_2.txt
├── json_result_1.txt
└── json_result_2.txt
```
