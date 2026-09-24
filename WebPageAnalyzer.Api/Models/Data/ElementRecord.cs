namespace WebPageAnalyzer.Api.Models.Data
{
    /// <summary>
    /// Представляет найденный HTML-элемент в хранилище.
    /// </summary>
    public sealed class ElementRecord
    {
        /// <summary>
        /// Идентификатор записи, назначаемый базой данных.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Значение запрошенного атрибута элемента.
        /// </summary>
        public string AttributeValue { get; set; } = string.Empty;

        /// <summary>
        /// Полная HTML-разметка элемента.
        /// </summary>
        public string ElementHtml { get; set; } = string.Empty;
    }
}
