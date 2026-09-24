using AngleSharp.Dom;
using AngleSharp.Html.Parser;
using Dapper;
using Npgsql;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using WebPageAnalyzer.Api.Models.Data;
using WebPageAnalyzer.Api.Models.Requests;
using WebPageAnalyzer.Api.Models.Responses;

namespace WebPageAnalyzer.Api.Services
{
    /// <summary>
    /// Анализирует HTML-страницы, извлекает запрошенные данные и сохраняет найденные элементы в PostgreSQL.
    /// </summary>
    public sealed class PageAnalyzerService : IPageAnalyzerService
    {
        private readonly string _connectionString;

        private static readonly Regex EmailRegex = new(
            @"\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\.[A-Z]{2,}\b",
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant,
            TimeSpan.FromSeconds(1));

        /// <summary>
        /// Инициализирует сервис и получает обязательную строку подключения к PostgreSQL.
        /// </summary>
        /// <param name="configuration">Конфигурация приложения.</param>
        /// <exception cref="InvalidOperationException">
        /// Строка подключения <c>PostgreSql</c> отсутствует.
        /// </exception>
        public PageAnalyzerService(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("PostgreSql")
                ?? throw new InvalidOperationException(
                    "PostgreSQL connection string is not configured.");
        }

        /// <summary>
        /// Декодирует входные данные, анализирует HTML, расшифровывает текст и сохраняет найденные элементы.
        /// </summary>
        /// <param name="request">Параметры анализа и данные страницы.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Результат анализа либо описание ошибки в едином формате сервиса.</returns>
        public async Task<AnalyzeResponse> AnalyzeAsync(
            AnalyzeRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // URL возвращается клиенту в декодированном виде, а HTML используется как источник анализа.
                string decodedUrl =
                    DecodeBase64Utf8(request.UrlB64);

                string html =
                    DecodeBase64Utf8(request.PageB64);

                var parser = new HtmlParser();

                var document = await parser.ParseDocumentAsync(
                    html,
                    cancellationToken);

                IHtmlCollection<IElement> elements;

                try
                {
                    elements =
                        document.QuerySelectorAll(request.Selector);
                }
                catch (DomException exception)
                {
                    return CreateErrorResponse(
                        "INVALID_SELECTOR",
                        exception.Message);
                }

                // За один проход формируем данные и для ответа, и для последующего сохранения.
                var (attributeValues, databaseElements) =
                    BuildElementData(
                        elements,
                        request.Attribute,
                        cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // Поиск выполняется по исходному HTML, а не только внутри элементов выбранного селектора.
                List<string> emails;

                try
                {
                    emails = ExtractEmails(html);
                }
                catch (RegexMatchTimeoutException exception)
                {
                    return CreateErrorResponse(
                        "EMAIL_SEARCH_TIMEOUT",
                        exception.Message);
                }

                cancellationToken.ThrowIfCancellationRequested();

                string decryptedText;

                try
                {
                    decryptedText = DecryptText(
                        request.EncryptedTextBytesB64,
                        request.KeyBytesB64);
                }
                catch (CryptographicException exception)
                {
                    return CreateErrorResponse(
                        "DECRYPTION_ERROR",
                        exception.Message);
                }

                cancellationToken.ThrowIfCancellationRequested();

                // Сохраняем элементы только после успешного завершения всех предыдущих этапов анализа.
                try
                {
                    await SaveElementsAsync(
                        databaseElements,
                        cancellationToken);
                }
                catch (NpgsqlException)
                {
                    return CreateErrorResponse(
                        "DATABASE_ERROR",
                        "Failed to access PostgreSQL database.");
                }

                return new AnalyzeResponse
                {
                    IsError = 0,
                    ErrorCode = string.Empty,
                    ErrorMessage = string.Empty,

                    ElementsCount = elements.Length,
                    EmailsCount = emails.Count,

                    Url = decodedUrl,
                    DecryptedPlainText = decryptedText,

                    ElementsAttrList = attributeValues,
                    EmailsList = emails
                };
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                // Отмена должна обрабатываться стандартным конвейером ASP.NET Core.
                throw;
            }
            catch (FormatException exception)
            {
                // Защита для вызовов сервиса в обход валидатора входной модели.
                return CreateErrorResponse(
                    "INVALID_DATA_FORMAT",
                    exception.Message);
            }
            catch (Exception exception)
            {
                // Непредвиденные сбои также возвращаются в едином контракте сервиса.
                return CreateErrorResponse(
                    "INTERNAL_ERROR",
                    exception.Message);
            }
        }

        /// <summary>
        /// Декодирует Base64-строку в текст UTF-8.
        /// </summary>
        /// <param name="base64">Строка в кодировке Base64.</param>
        /// <returns>Декодированный текст.</returns>
        /// <exception cref="FormatException">Входная строка имеет неверный формат Base64.</exception>
        private static string DecodeBase64Utf8(string base64)
        {
            byte[] bytes =
                Convert.FromBase64String(base64);

            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Формирует значения запрошенного атрибута и записи для сохранения в базе данных.
        /// </summary>
        /// <param name="elements">Элементы, найденные по CSS-селектору.</param>
        /// <param name="attribute">Имя извлекаемого атрибута.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Списки значений атрибута и записей для базы данных.</returns>
        private static (
            List<string> AttributeValues,
            List<ElementRecord> DatabaseElements)
            BuildElementData(
                IHtmlCollection<IElement> elements,
                string attribute,
                CancellationToken cancellationToken)
        {
            var attributeValues =
                new List<string>(elements.Length);

            var databaseElements =
                new List<ElementRecord>(elements.Length);

            foreach (var element in elements)
            {
                cancellationToken.ThrowIfCancellationRequested();

                string attributeValue =
                    element.GetAttribute(attribute)
                    ?? string.Empty;

                attributeValues.Add(attributeValue);

                databaseElements.Add(new ElementRecord
                {
                    AttributeValue = attributeValue,
                    ElementHtml = element.OuterHtml
                });
            }

            return (attributeValues, databaseElements);
        }

        /// <summary>
        /// Извлекает адреса электронной почты из HTML-кода в порядке их обнаружения.
        /// </summary>
        /// <param name="html">Исходный HTML-код страницы.</param>
        /// <returns>Список найденных адресов электронной почты.</returns>
        private static List<string> ExtractEmails(string html)
        {
            return EmailRegex
                .Matches(html)
                .Cast<Match>()
                .Select(match => match.Value)
                .ToList();
        }

        /// <summary>
        /// Расшифровывает текст алгоритмом AES-256 в режиме ECB без заполнения.
        /// </summary>
        /// <param name="encryptedTextBytesB64">Зашифрованные байты в формате Base64.</param>
        /// <param name="keyBytesB64">Ключ AES-256 в формате Base64.</param>
        /// <returns>Расшифрованный текст в кодировке UTF-8.</returns>
        /// <exception cref="FormatException">Один из аргументов имеет неверный формат Base64.</exception>
        /// <exception cref="CryptographicException">Данные не соответствуют параметрам шифрования.</exception>
        private static string DecryptText(
            string encryptedTextBytesB64,
            string keyBytesB64)
        {
            byte[] encryptedBytes =
                Convert.FromBase64String(
                    encryptedTextBytesB64);

            byte[] keyBytes =
                Convert.FromBase64String(
                    keyBytesB64);

            using Aes aes = Aes.Create();

            // Режим и отсутствие заполнения являются частью контракта входных данных.
            aes.KeySize = 256;
            aes.Mode = CipherMode.ECB;
            aes.Padding = PaddingMode.None;
            aes.Key = keyBytes;

            using ICryptoTransform decryptor =
                aes.CreateDecryptor();

            byte[] decryptedBytes =
                decryptor.TransformFinalBlock(
                    encryptedBytes,
                    0,
                    encryptedBytes.Length);

            return Encoding.UTF8.GetString(
                decryptedBytes);
        }

        /// <summary>
        /// Сохраняет найденные элементы в PostgreSQL в рамках одной транзакции.
        /// </summary>
        /// <param name="elements">Записи для сохранения.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        private async Task SaveElementsAsync(
            IReadOnlyCollection<ElementRecord> elements,
            CancellationToken cancellationToken)
        {
            if (elements.Count == 0)
                return;

            const string sql = """
                INSERT INTO elements
                    (attribute_value, element_html)
                VALUES
                    (@AttributeValue, @ElementHtml);
                """;

            await using var connection =
                new NpgsqlConnection(_connectionString);

            await connection.OpenAsync(
                cancellationToken);

            await using var transaction =
                await connection.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var command = new CommandDefinition(
                    sql,
                    elements,
                    transaction: transaction,
                    cancellationToken: cancellationToken);

                await connection.ExecuteAsync(command);

                await transaction.CommitAsync(
                    cancellationToken);
            }
            catch
            {
                // Откат выполняется независимо от отмены запроса, чтобы не оставлять транзакцию незавершённой.
                await transaction.RollbackAsync(
                    CancellationToken.None);

                throw;
            }
        }

        /// <summary>
        /// Создаёт ответ об ошибке в едином формате сервиса.
        /// </summary>
        /// <param name="errorCode">Машиночитаемый код ошибки.</param>
        /// <param name="errorMessage">Описание ошибки.</param>
        /// <returns>Ответ с установленным признаком ошибки.</returns>
        private static AnalyzeResponse CreateErrorResponse(
            string errorCode,
            string errorMessage)
        {
            return new AnalyzeResponse
            {
                IsError = 1,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage
            };
        }
    }
}
