namespace Backend.Options;

public class EmailOptions
{
    public bool Enabled { get; set; } = false;
    public string SmtpHost { get; set; } = "";
    public int SmtpPort { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderEmail { get; set; } = "";
    public string SenderPassword { get; set; } = "";
    public string SenderName { get; set; } = "WorkWave";
}
