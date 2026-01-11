using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UssdDevelopmentCore.Entities;
using UssdDevelopmentCore.Extensions;
using UssdDevelopmentCore.Services;

namespace UssdDevelopmentCore.Endpoints;

[ApiController, Route("api/v1/callback")]
public class PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger) : ControllerBase
{
    [HttpPost, AllowAnonymous]
    public IActionResult Callback([FromBody] LipilaCallBackResponse response)
    {
        logger.LogInformation($"Received call back: {response.ExternalId}");
        BackgroundJobExtension.TryEnqueue(() => paymentService.ProcessCallback(response));
        logger.LogInformation($"Sent call back: {response.ExternalId}");
        return Ok();
    }
}

