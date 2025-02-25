using LetMasterWebApp.Models;
using LetMasterWebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace LetMasterWebApp.Controllers;


[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly ILogger<PaymentsController> _logger;
    private readonly IPaymentService _paymentService;
    public PaymentsController(ILogger<PaymentsController> logger, IPaymentService paymentService)
    {
        _logger = logger;
        _paymentService = paymentService;
    }
    [HttpGet("example")]
    public IActionResult GetExample()
    {
        return Ok(new { Message = "Hello from API!" });
    }
    [HttpPost("post-airtel-callback")]
    public async Task<IActionResult>ReceiveCallBack(CallBackRequest request)
    {
        try
        {
            _logger.LogInformation($"ReceiveCallBack: {request}");
            var callBackResp = await _paymentService.ReceiveCallBackAsync(request); 
            return Ok(callBackResp);
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"Error occured: {ex.Message}");
            return BadRequest(ex.Message);  // Return 400 Bad Request for null search model
        }
        catch (ApplicationException ex)
        {
            _logger.LogError($"Application error occured: {ex.Message}");
            return StatusCode(500, ex.Message);  // Return 500 Internal Server Error for application-level issues
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error occured: {ex}");
            return StatusCode(500, "An unexpected error occurred while processing the request.");
        }
    }
    [HttpGet("process-debit-requests")]
    public async Task<IActionResult> ProcessDebitRequests()
    {
        try
        {
            var result = await _paymentService.ProcessPendingAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Unexpected error occured: {ex}");
            return StatusCode(500, "An unexpected error occurred while processing the request.");
        }
    }
}
