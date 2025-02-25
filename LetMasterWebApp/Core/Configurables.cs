namespace LetMasterWebApp.Core;

public class PropertyType:BaseModel
{
    public string Name { get; set; } = default!;
}
public class UnitType : BaseModel
{
    public string Name { get; set; } = default!;
}
public class ExpenseType : BaseModel
{
    public string Name { get; set; } = default!;
}
public class UserMessage:BaseModel
{
    public string MessageMode {  get; set; } = default!;
    public string? MessageSubject { get; set; }
    public string MessageBody { get; set; } = default!;
    public string MessageReciepient {  get; set; } = default!;
}
public class AuditTrail : BaseModel
{
    public string? UserId { get; set; }
    public string? Username { get; set; }
    public string? OriginalData { get; set; }
    public string? RequestData { get; set; }
    public string? RequestPath { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
}
public class ScheduledJob : BaseModel
{
    public string JobName { get; set; }=default!;
    public DateTime NextRunTime { get; set; }
}
public class SmsRequest
{
    public string api_id { get; set; } = default!;
    public string api_password { get; set; } = default!;
    public string sms_type { get; set; } = "P";
    public string encoding { get; set; } = "T";
    public string sender_id { get; set; } = "speedamobile";
    public string phonenumber { get; set; } = default!;
    public string textmessage { get; set; } = default!;
}