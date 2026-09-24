using FluentValidation;
using WebPageAnalyzer.Api.Models.Requests;

namespace WebPageAnalyzer.Api.Validators
{
    /// <summary>
    /// Проверяет обязательные параметры анализа и формат криптографических данных.
    /// </summary>
    public class AnalyzeRequestValidator : AbstractValidator<AnalyzeRequest>
    {
        /// <summary>
        /// Задаёт правила проверки обязательных полей, Base64-данных и параметров AES-256.
        /// </summary>
        public AnalyzeRequestValidator()
        {
            // Селектор и имя атрибута обязательны для поиска и формирования результата.
            RuleFor(x => x.Selector)
                .NotEmpty()
                    .WithErrorCode("EMPTY_SELECTOR")
                    .WithMessage("Selector cannot be empty.");
            RuleFor(x => x.Attribute)
                .NotEmpty()
                    .WithErrorCode("EMPTY_ATTRIBUTE")
                    .WithMessage("Attribute cannot be empty.");

            // Проверка формата выполняется только после проверки на пустое значение.
            RuleFor(x => x.UrlB64)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithErrorCode("EMPTY_URL_BASE64")
                    .WithMessage("UrlB64 cannot be empty.")
                .Must(BeValidBase64)
                    .WithErrorCode("INVALID_URL_BASE64")
                    .WithMessage("UrlB64 is not valid Base64.");

            // Шифротекст должен содержать целое число 16-байтовых блоков AES.
            RuleFor(x => x.EncryptedTextBytesB64)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithErrorCode("EMPTY_ENCRYPTED_TEXT_BASE64")
                    .WithMessage("EncryptedTextBytesB64 cannot be empty.")
                .Must(BeValidBase64)
                    .WithErrorCode("INVALID_ENCRYPTED_TEXT_BASE64")
                    .WithMessage("EncryptedTextBytesB64 is not valid Base64.")
                .Must(HaveValidCipherTextLength)
                    .WithErrorCode("INVALID_ENCRYPTED_TEXT_LENGTH")
                    .WithMessage("Encrypted text length must be a multiple of 16 bytes.");

            // Ключ AES-256 после декодирования должен иметь длину ровно 32 байта.
            RuleFor(x => x.KeyBytesB64)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithErrorCode("EMPTY_KEY_BASE64")
                    .WithMessage("KeyBytesB64 cannot be empty.")
                .Must(BeValidBase64)
                    .WithErrorCode("INVALID_KEY_BASE64")
                    .WithMessage("KeyBytesB64 is not valid Base64.")
                .Must(HaveValidAes256KeyLength)
                    .WithErrorCode("INVALID_AES256_KEY_LENGTH")
                    .WithMessage("KeyBytesB64 does not have a valid AES-256 key length.");

            // HTML-страница должна быть передана непустой корректной Base64-строкой.
            RuleFor(x => x.PageB64)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                    .WithErrorCode("EMPTY_PAGE_BASE64")
                    .WithMessage("PageB64 cannot be empty.")
                .Must(BeValidBase64)
                    .WithErrorCode("INVALID_PAGE_BASE64")
                    .WithMessage("PageB64 is not valid Base64.");
        }

        /// <summary>
        /// Проверяет, можно ли декодировать строку из Base64 без создания промежуточного результата.
        /// </summary>
        /// <param name="base64String">Проверяемая строка.</param>
        /// <returns><c>true</c>, если строка непустая и имеет корректный формат Base64.</returns>
        private static bool BeValidBase64(string base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return false;

            byte[] buffer = new byte[base64String.Length];

            return Convert.TryFromBase64String(base64String, buffer, out _);
        }

        /// <summary>
        /// Проверяет, что Base64-строка содержит 256-битный ключ AES.
        /// </summary>
        /// <param name="base64String">Ключ в формате Base64.</param>
        /// <returns><c>true</c>, если декодированный ключ имеет длину 32 байта.</returns>
        private static bool HaveValidAes256KeyLength(string? base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return false;

            try
            {
                byte[] keyBytes = Convert.FromBase64String(base64String);

                return keyBytes.Length == 32;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        /// <summary>
        /// Проверяет допустимую длину шифротекста для AES без заполнения.
        /// </summary>
        /// <param name="base64String">Шифротекст в формате Base64.</param>
        /// <returns>
        /// <c>true</c>, если декодированные данные непусты и их длина кратна размеру блока AES.
        /// </returns>
        private static bool HaveValidCipherTextLength(string? base64String)
        {
            if (string.IsNullOrWhiteSpace(base64String))
                return false;

            try
            {
                byte[] encryptedBytes =
                    Convert.FromBase64String(base64String);

                return encryptedBytes.Length > 0 &&
                       encryptedBytes.Length % 16 == 0;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
