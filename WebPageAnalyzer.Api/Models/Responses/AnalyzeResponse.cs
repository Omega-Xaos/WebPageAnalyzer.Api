namespace WebPageAnalyzer.Api.Models.Responses
{
    /// <summary>
    /// Содержит результат анализа страницы или сведения об ошибке.
    /// </summary>
    public sealed class AnalyzeResponse
    {
        /// <summary>
        /// Признак ошибки: <c>0</c> — операция выполнена успешно, <c>1</c> — произошла ошибка.
        /// </summary>
        public int IsError { get; set; }

        /// <summary>
        /// Машиночитаемый код ошибки; пустая строка при успешном выполнении.
        /// </summary>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>
        /// Описание ошибки; пустая строка при успешном выполнении.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Количество элементов, соответствующих CSS-селектору.
        /// </summary>
        public int ElementsCount { get; set; }

        /// <summary>
        /// Количество найденных адресов электронной почты.
        /// </summary>
        public int EmailsCount { get; set; }

        /// <summary>
        /// Декодированный URL страницы.
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// Текст, полученный после расшифровки входных данных.
        /// </summary>
        public string DecryptedPlainText { get; set; } = string.Empty;

        /// <summary>
        /// Значения запрошенного атрибута найденных элементов.
        /// </summary>
        public List<string> ElementsAttrList { get; set; } = [];

        /// <summary>
        /// Адреса электронной почты, найденные в HTML-коде страницы.
        /// </summary>
        public List<string> EmailsList { get; set; } = [];
    }
}
