namespace WebPageAnalyzer.Api.Models.Requests
{
    /// <summary>
    /// Содержит параметры и данные для анализа HTML-страницы.
    /// </summary>
    public sealed class AnalyzeRequest
    {
        /// <summary>
        /// CSS-селектор искомых элементов.
        /// </summary>
        public string Selector { get; set; } = string.Empty;

        /// <summary>
        /// Имя атрибута, значение которого нужно извлечь из найденных элементов.
        /// </summary>
        public string Attribute { get; set; } = string.Empty;

        /// <summary>
        /// URL страницы в кодировке Base64 (UTF-8).
        /// </summary>
        public string UrlB64 { get; set; } = string.Empty;

        /// <summary>
        /// Зашифрованный текст в виде Base64-представления массива байтов.
        /// </summary>
        public string EncryptedTextBytesB64 { get; set; } = string.Empty;

        /// <summary>
        /// 256-битный ключ AES в виде Base64-представления массива байтов.
        /// </summary>
        public string KeyBytesB64 { get; set; } = string.Empty;

        /// <summary>
        /// HTML-код страницы в кодировке Base64 (UTF-8).
        /// </summary>
        public string PageB64 { get; set; } = string.Empty;
    }
}
