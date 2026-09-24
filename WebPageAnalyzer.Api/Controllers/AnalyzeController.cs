using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebPageAnalyzer.Api.Models.Requests;
using WebPageAnalyzer.Api.Models.Responses;
using WebPageAnalyzer.Api.Services;

namespace WebPageAnalyzer.Api.Controllers
{
    /// <summary>
    /// Принимает запросы на анализ HTML-страниц и преобразует ошибки сервиса в HTTP-ответы.
    /// </summary>
    [Route("api/analyze")]
    [ApiController]
    public class AnalyzeController : ControllerBase
    {
        private readonly IValidator<AnalyzeRequest> _validator;
        private readonly IPageAnalyzerService _pageAnalyzerService;

        public AnalyzeController(
            IValidator<AnalyzeRequest> validator,
            IPageAnalyzerService pageAnalyzerService)
        {
            _validator = validator;
            _pageAnalyzerService = pageAnalyzerService;
        }

        /// <summary>
        /// Анализирует переданную HTML-страницу, извлекает элементы и адреса электронной почты,
        /// расшифровывает текст и сохраняет найденные элементы.
        /// </summary>
        /// <param name="request">Параметры анализа и данные страницы.</param>
        /// <param name="cancellationToken">Токен отмены запроса.</param>
        /// <returns>Результат анализа либо описание ошибки в едином формате API.</returns>
        [HttpPost]
        public async Task<ActionResult<AnalyzeResponse>> AnalyzeAsync(
           [FromBody] AnalyzeRequest request,
           CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(
                request,
                cancellationToken);

            if (!validationResult.IsValid)
            {
                var error = validationResult.Errors[0];

                return BadRequest(new AnalyzeResponse
                {
                    IsError = 1,
                    ErrorCode = error.ErrorCode,
                    ErrorMessage = error.ErrorMessage
                });
            }

            var response = await _pageAnalyzerService.AnalyzeAsync(
                                    request,
                                    cancellationToken);

            if (response.IsError == 0)
                return Ok(response);

            return response.ErrorCode switch
            {
                "INVALID_SELECTOR" => BadRequest(response),
                "DECRYPTION_ERROR" => BadRequest(response),

                "DATABASE_ERROR" => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    response),

                _ => StatusCode(
                    StatusCodes.Status500InternalServerError,
                    response)
            };
        }
    }
}
