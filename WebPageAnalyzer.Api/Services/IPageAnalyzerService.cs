using WebPageAnalyzer.Api.Models.Requests;
using WebPageAnalyzer.Api.Models.Responses;

namespace WebPageAnalyzer.Api.Services
{
    /// <summary>
    /// Определяет операцию анализа HTML-страницы и сохранения найденных элементов.
    /// </summary>
    public interface IPageAnalyzerService
    {
        /// <summary>
        /// Выполняет анализ страницы по параметрам запроса.
        /// </summary>
        /// <param name="request">Параметры анализа и данные страницы.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Результат анализа либо описание ошибки.</returns>
        Task<AnalyzeResponse> AnalyzeAsync(
            AnalyzeRequest request,
            CancellationToken cancellationToken);
    }
}
