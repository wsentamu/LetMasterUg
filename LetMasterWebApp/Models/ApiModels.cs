namespace LetMasterWebApp.Models;
public class OAuthRequest
{
    public required string client_id { get; set; }
    public required string client_secret { get; set; }
    public string? grant_type { get; set; } = "client_credentials";
}
public class OAuthResponse
{
    public string access_token { get; set; } = default!;
    public int expires_in { get; set; }
    public string token_type { get; set; } = default!;
}
public class CollectionRequest
{
    public string reference {  get; set; }=string.Empty;
    public required Subscriber subscriber { get; set; }
    public required Transaction transaction { get; set; }
}
public class Subscriber
{
    public string? country { get; set; } = "UG";
    public string? currency { get; set; } = "UGX";
    public string msisdn { get; set; } = string.Empty;
}
public class Transaction
{
    public decimal? amount { get; set; }
    public string? country { get; set; } = "UG";
    public string? currency { get; set; } = "UGX";
    public string? id { get; set; }
    public string message { get; set; }
    public string? status_code { get; set; }
    public string? airtel_money_id { get; set; }
}
public class CollectionResponse
{
    public Data? data { get; set; }
    public Status? status { get; set; }
}
public class Data
{
    public Transaction? transaction { get; set; }
}
public class Status
{
    public string? code { get; set; }
    public string? message {  get; set; }
    public string? result_code {  get; set; }
    public string? response_code { get; set; }
    public bool? success { get; set; }
}

public class CallBackRequest
{
    public Transaction? Transaction { get; set; }
}
public class CallBackResponse
{
    public string? status_code { get; set; }
    public string? message { get; set; }
}