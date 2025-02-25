using AutoMapper;
using LetMasterWebApp.DataLayer;
using LetMasterWebApp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Cryptography;
using System.Text;

namespace LetMasterWebApp.Services;
public interface IPaymentService
{
    public Task<OAuthResponse> OAuthAsync();
    public Task<CollectionResponse> UssdPushAsync(MobileMoneyCreateModel request);
    public Task<CallBackResponse> ReceiveCallBackAsync(CallBackRequest request);
    public Task<string> ProcessPendingAsync();
}
public class PaymentService : IPaymentService
{
    private ILogger<PaymentService> _logger { get; set; }
    private readonly ApplicationDbContext _context;
    private readonly UserManager<User> _userManager;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly INotificationService _notificationService;
    public PaymentService(ILogger<PaymentService> logger, ApplicationDbContext context, UserManager<User> userManager, IMapper mapper, IConfiguration configuration, HttpClient httpClient, INotificationService notificationService)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
        _mapper = mapper;
        _configuration = configuration;
        _httpClient = httpClient;
        _notificationService = notificationService;
    }
    //authentication
    private OAuthResponse? _cachedToken;
    private DateTime _tokenExpiry;

    public async Task<OAuthResponse> OAuthAsync()
    {
        var action = "IPaymentService:OAuthAsync";
        // Return cached token if it's valid
        if (_cachedToken != null && DateTime.UtcNow < _tokenExpiry)
        {
            _logger.LogInformation($"{action}: Using cached token");
            return _cachedToken;
        }
        try
        {
            var clientId = _configuration["IntergrationSettings:AirtelClientId"];
            var clientSecret = _configuration["IntergrationSettings:AirtelClientSecret"];
            var oauthReq = new OAuthRequest { client_id = clientId!, client_secret = clientSecret! };
            _logger.LogInformation($"{action} attempt");
            var jsonStr = JsonConvert.SerializeObject(oauthReq);
            var content = new StringContent(jsonStr, Encoding.UTF8, "application/json");
            var baseUrl = _configuration["IntergrationSettings:BaseUrl"];
            var url = _configuration["IntergrationSettings:OAuth2"];
            var reqUrl = baseUrl + url;
            var resp = await _httpClient.PostAsync(reqUrl, content);
            var respContent = await resp.Content.ReadAsStringAsync();
            if (resp.IsSuccessStatusCode)
            {
                var token = JsonConvert.DeserializeObject<OAuthResponse>(respContent);

                if (token == null)
                    throw new ArgumentNullException("Null token object returned");
                _cachedToken = token;
                _tokenExpiry = DateTime.UtcNow.AddSeconds(token.expires_in);

                _logger.LogInformation($"{action}: Token received successfully");
                return token;
            }
            else
            {
                _logger.LogError($"{action}: OAuth request failed with status code {resp.StatusCode}");
                throw new UnauthorizedAccessException("OAuth request failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"{action} Exception {ex.Message}");
            throw new ApplicationException(" Failed to authenticate user");
        }
    }
    //Message signing
    public static (string Key, string IV) GenerateAesKeyAndIV()
    {
        using (Aes aes = Aes.Create())
        {
            aes.KeySize = 256; // 256 bits
            aes.GenerateKey();
            aes.GenerateIV();

            string keyBase64 = Convert.ToBase64String(aes.Key);
            string ivBase64 = Convert.ToBase64String(aes.IV);

            return (keyBase64, ivBase64);
        }
    }
    public async Task<string> FetchRsaKeyAsync()
    {
        var action = "IPaymentService:FetchRsaKAsync";
        try
        {
            _logger.LogInformation($"{action}: Get Key");
            var baseUrl = _configuration["IntergrationSettings:BaseUrl"];
            var url = _configuration["IntergrationSettings:EncryptionKeys"];
            var endpointUrl = baseUrl + url;
            var token = await OAuthAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, endpointUrl);
            request.Headers.Add("Authorization", $"Bearer {token.access_token}");
            request.Headers.Add("X-Country", "UG");
            request.Headers.Add("X-Currency", "UGX");

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();

            // Parse the JSON response to get the RSA key
            JObject jsonResponse = JObject.Parse(responseBody);
            if (jsonResponse == null)
                throw new InvalidOperationException("Response body parse failed");
            if (jsonResponse["data"] == null)
                throw new InvalidOperationException("Response body has no data");
            string rsaKey = jsonResponse!["data"]!["key"]!.ToString();

            return rsaKey;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError($"{action}: Response body parse failed. {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{action}: Exception {ex.Message}");
            throw new ApplicationException(" Failed to get RSA key");
        }
    }
    public static string EncryptPayloadWithAes(string payload, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter writer = new StreamWriter(cs))
                    {
                        writer.Write(payload);
                    }
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
    public static string EncryptKeyIvWithRsa(string keyIv, string rsaPublicKey)
    {
        using (RSA rsa = RSA.Create())
        {
            rsa.ImportFromPem(rsaPublicKey.ToCharArray()); // Use the PEM format public key

            byte[] keyIvBytes = Encoding.UTF8.GetBytes(keyIv);
            byte[] encryptedKeyIv = rsa.Encrypt(keyIvBytes, RSAEncryptionPadding.OaepSHA256);

            return Convert.ToBase64String(encryptedKeyIv);
        }
    }

    //send signed request
    public async Task<string> SendSignedRequestAsync(string payload, HttpMethod requestType, string requestUrl)
    {
        var action = "IPaymentService:SendSignedRequestAsync";
        try
        {
            _logger.LogInformation($"{action}: Params ({requestType}, {requestUrl}, {payload})");
            var (aesKey, aesIv) = GenerateAesKeyAndIV();
            var rsaPublicKey = await FetchRsaKeyAsync();
            var encryptedPayload = EncryptPayloadWithAes(payload, Convert.FromBase64String(aesKey), Convert.FromBase64String(aesIv));
            string keyIv = $"{aesKey}:{aesIv}";
            string encryptedKeyIv = EncryptKeyIvWithRsa(keyIv, rsaPublicKey);
            //fetch token
            var token = await OAuthAsync();
            var request = new HttpRequestMessage(requestType, requestUrl);
            request.Headers.Add("Authorization", $"Bearer {token.access_token}");
            request.Headers.Add("X-Country", "UG");
            request.Headers.Add("X-Currency", "UGX");
            request.Headers.Add("x-signature", encryptedPayload);
            request.Headers.Add("x-key", encryptedKeyIv);
            //process payload
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            var respContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                _logger.LogInformation($"{action}: Request success");
            else
                _logger.LogError($"{action}: Request failed");
            return respContent;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{action}: Exception caught - {ex.Message}");
            throw new ApplicationException($"Failed to send request: {ex.Message}", ex);
        }
    }
    //send unsigned request (collections)
    private async Task<string> SendUnsignedRequest(string payload, HttpMethod requestType, string requestUrl)
    {
        var action = "IPaymentService:SendUnsignedRequest";
        try
        {
            var token = await OAuthAsync();
            var request = new HttpRequestMessage(requestType, requestUrl);
            request.Headers.Add("Authorization", $"Bearer {token.access_token}");
            request.Headers.Add("X-Country", "UG");
            request.Headers.Add("X-Currency", "UGX");
            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            request.Content = content;
            var response = await _httpClient.SendAsync(request);
            var respContent = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
                _logger.LogInformation($"{action}: Request success");
            else
                _logger.LogError($"{action}: Request failed");
            return respContent;
        }
        catch (ApplicationException ex)
        {
            _logger.LogError($"{action}: Exception caught {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{action}: Exception caught - {ex.Message}");
            throw new ApplicationException(" Failed send payment request");
        }
    }
    //collection requests (Post Debit request, Transaction inquiry)
    public async Task<CollectionResponse> UssdPushAsync(MobileMoneyCreateModel request)
    {
        var action = "IPaymentService:UssdPushAsync";
        try
        {
            _logger.LogInformation($"{action}: {request}");
            //save Client Debit Request
            await ProcessPendingAsync();
            var clientDebitRequest = new ClientDebitRequest
            {
                Amount = request.Amount,
                ClientAccountId = request.TenantUnitId,
                CreatedBy = request.CreatedBy,
                ProviderName = "AIRTEL",
                SvcStatus = "PENDING",
            };
            await _context.ClientDebitRequests.AddAsync(clientDebitRequest);
            await _context.SaveChangesAsync();
            //get tenant acc details
            var tenantUnit = await _context.TenantUnits.Where(t => t.Id.Equals(request.TenantUnitId)).FirstOrDefaultAsync();
            if (tenantUnit == null)
                throw new InvalidOperationException("Tenant Account Not Found");
            var unit = await _context.Units.Where(u => u.Id.Equals(tenantUnit.UnitId)).FirstOrDefaultAsync();
            if (unit == null)
                throw new InvalidOperationException("Tenant Unit Not Found");
            var tenant = await _context.Tenants.Where(t => t.Id.Equals(tenantUnit.TenantId)).FirstOrDefaultAsync();
            if (tenant == null)
                throw new InvalidOperationException("Tenant Not Found");
            string txnRef = $"{tenant.Name}";
            var collectionRequest = new CollectionRequest
            {
                reference = txnRef,
                subscriber = new Subscriber
                {
                    msisdn = request.DebitNumber,
                },
                transaction = new Transaction
                {
                    amount = request.Amount,
                    id = clientDebitRequest.Id.ToString(),
                }
            };
            var requestString = JsonConvert.SerializeObject(collectionRequest);
            var baseUrl = _configuration["IntergrationSettings:BaseUrl"];
            var url = _configuration["IntergrationSettings:DebitRequest"];
            var requestUrl = baseUrl + url;
            var apiResponse = await SendUnsignedRequest(requestString, HttpMethod.Post, requestUrl);
            _logger.LogInformation($"{action} : API Resp - {apiResponse}");
            if (!string.IsNullOrEmpty(apiResponse))
            {
                var rsp = JsonConvert.DeserializeObject<CollectionResponse>(apiResponse);
                //update client debit request
                clientDebitRequest.SvcRequestBody = requestString;
                clientDebitRequest.SvcResponseBody = apiResponse;
                if (rsp != null && rsp.status != null)
                    clientDebitRequest.SvcStatus = rsp.status.response_code;
                await _context.SaveChangesAsync();
                return rsp!;
            }
            else
                throw new InvalidOperationException("USSD push request failed");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError($"{action}: Exception caught {ex.Message}");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{action}: Exception caught - {ex.Message}");
            throw new ApplicationException(" Failed send request");
        }
    }
    //Recieve Callback, update client debit add CR TXN if MM Debit successful
    public async Task<CallBackResponse> ReceiveCallBackAsync(CallBackRequest request)
    {
        var action = "IPaymentService:ReceiveCallBackAsync";
        try
        {
            _logger.LogInformation($"{action}: {request}");
            var resp = new CallBackResponse
            {
                status_code = "200",
                message = "success"
            };
            if (request.Transaction == null)
                throw new ArgumentNullException("Mandatory data missing!");
            if (string.IsNullOrEmpty(request.Transaction.airtel_money_id) || string.IsNullOrEmpty(request.Transaction.status_code) || string.IsNullOrEmpty(request.Transaction.id))
                throw new ArgumentNullException("Mandatory data missing!");
            //find the client debit request and update
            if (int.TryParse(request.Transaction.id, out int crId))
            {
                var debitReq = await _context.ClientDebitRequests.Where(d => d.Id.Equals(crId)).FirstOrDefaultAsync();
                if (debitReq == null)
                    throw new InvalidOperationException("Transaction Id Data Not Found");
                debitReq.UpdatedDate = DateTime.Now;
                debitReq.SvcCallBackBody = JsonConvert.SerializeObject(request.Transaction);

                //if callback status==TS create CR entry in tenant acc
                if (request.Transaction.status_code == "TS")
                {
                    debitReq.ReconcileStatus = "C";
                    //find tenant acc
                    var tenantAcc = await _context.TenantUnits.Where(a => a.Id.Equals(debitReq.ClientAccountId)).FirstOrDefaultAsync();
                    if (tenantAcc == null)
                        throw new InvalidOperationException("Client Account Not Found");
                    var txn = new TenantUnitTransaction
                    {
                        Description = "Mobile Money Payment",
                        Amount = debitReq.Amount,
                        CreatedBy = debitReq.CreatedBy,
                        TenantUnitId = debitReq.ClientAccountId,
                        TransactionDate = DateTime.Now,
                        TransactionMode = "MOBILE",
                        TransactionType = "C",
                        TransactionRef = $"C{debitReq.ClientAccountId}-{DateTime.Now.Year}{DateTime.Now.Month}({request.Transaction.airtel_money_id})",
                    };
                    await _context.TenantUnitTransactions.AddAsync(txn);
                    tenantAcc.UpdatedDate = DateTime.Now;
                    tenantAcc.CurrentBalance = tenantAcc.CurrentBalance - txn.Amount;
                    //send sms
                    var unit = await _context.Units.Where(u => u.Id == tenantAcc.UnitId).FirstOrDefaultAsync();
                    var complex = await _context.Properties.Where(p => p.Id == unit.PropertyId).FirstOrDefaultAsync();
                    var tenant = await _context.Tenants.Where(t => t.Id == tenantAcc.TenantId).FirstOrDefaultAsync();
                    string accName = $"{complex.Name} ({unit.Name})";
                    var sms = _configuration["NotificationTemplates:PaymentReceivedSms"];
                    string amt = txn.Amount.Value.ToString("#,##0");
                    string bal = tenantAcc.CurrentBalance!.Value.ToString("#,##0");
                    sms = sms!.Replace("{FULLNAME}", tenant.Name).Replace("{BAL}", bal).Replace("{AMT}", amt).Replace("{ACC}", accName);
                    if (!string.IsNullOrEmpty(tenant.PhoneNumber))
                        await _notificationService.SendSms(tenant.PhoneNumber, sms);
                }
                else
                    debitReq.ReconcileStatus = "F";
            }
            else
                throw new InvalidOperationException("Invalid transaction Id");
            await _context.SaveChangesAsync();
            return resp;
        }
        catch (ArgumentNullException ex)
        {
            _logger.LogError($"{action}: {ex.Message}");
            return new CallBackResponse
            {
                message = ex.Message,
                status_code = "400"
            };
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError($"{action}: Exception caught {ex.Message}");
            return new CallBackResponse
            {
                message = $"Call back failed: {ex.Message}",
                status_code = "500"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError($"{action}: Exception caught - {ex.Message}");
            return new CallBackResponse
            {
                message = $"Call back failed: {ex.Message}",
                status_code = "500"
            };
        }
    }
    //check MM transaction status of pending req, update debit req, create txn entries
    public async Task<string> ProcessPendingAsync()
    {
        var action = "IPaymentService:ReceiveCallBackAsync";
        try
        {
            var baseUrl = _configuration["IntergrationSettings:BaseUrl"];
            var url = _configuration["IntergrationSettings:CheckTransaction"];
            var requestUrl = $"{baseUrl}{url}";
            int count = 0;
            //list pending debit requests and process them (get current status, forward to ReceiveCallBackAsync)
            var pendingRequests = await _context.ClientDebitRequests.Where(c=>c.ReconcileStatus=="P").ToListAsync();
            foreach(var request in pendingRequests)
            {
                var finalUrl = $"{requestUrl}{request.Id}";
                var apiResponse = await SendUnsignedRequest("",HttpMethod.Get, finalUrl);
                var response = JsonConvert.DeserializeObject<CollectionResponse>(apiResponse);
                _logger.LogInformation($"{action}: {finalUrl} | response: {response}");
                if (response != null && response.status!=null && response.status.success!=null && response.status.success.Value && response.data!=null&& response.data.transaction!=null)
                {
                    //update via manual call back call
                    var callBackReq = new CallBackRequest
                    {
                        Transaction = new Transaction
                        {
                            id = request.Id.ToString(),
                            airtel_money_id = response.data.transaction.airtel_money_id,
                            status_code=response.data.transaction.status_code,
                        }
                    };
                    var callBackResp = await ReceiveCallBackAsync(callBackReq);
                    _logger.LogInformation($"{action}: CallBackResp: {callBackResp}");
                }
                count++;
            }
            return $"{count} debit requests processed";
        }
        catch (Exception ex)
        {
            _logger.LogError($"{action}: Exception {ex.Message}");
            return ex.Message;
        }
    }
}
