namespace Frontend.Models;

public class JobModel
{
	public int Id { get; set; }
	public string Title { get; set; } = "";
	public string Company { get; set; } = "";
	public string Location { get; set; } = "";
	public string Category { get; set; } = "";
	public string Salary { get; set; } = "";
	public string Description { get; set; } = "";
	public string JobType { get; set; } = "";
	public string[] Tags { get; set; } = Array.Empty<string>();
}

public static class JobData
{
	public static List<JobModel> AllJobs = new()
	{
		new JobModel { Id = 1, Title = "Full Stack C# Developer", Company = "TechSolutions", Location = "Remote", Category = "IT & Software", Salary = "৳80,000 - ৳120,000/mo", Description = "Looking for an experienced Blazor & ASP.NET Core developer to build web apps.", JobType = "Full-Time", Tags = new[] { "C#", "Blazor", "SQL" } },
		new JobModel { Id = 2, Title = "Customer Service Executive", Company = "DigiConnect Ltd", Location = "Dhaka, BD", Category = "Customer Support", Salary = "৳22,000 - ৳30,000/mo", Description = "Handle inbound customer queries via call and chat. Good communication skills required.", JobType = "Full-Time", Tags = new[] { "Communication", "English", "Support" } },
		new JobModel { Id = 3, Title = "Front Desk & Admin Officer", Company = "Apex Group", Location = "Chittagong, BD", Category = "Office Admin", Salary = "৳18,000 - ৳25,000/mo", Description = "Manage visitor reception, office phone calls, and administrative paperwork.", JobType = "Full-Time", Tags = new[] { "MS Office", "Management" } },
		new JobModel { Id = 4, Title = "Delivery Fleet Rider", Company = "QuickExpress", Location = "Dhaka, BD", Category = "Delivery & Logistics", Salary = "৳15,000 - ৳22,000/mo", Description = "Deliver parcel packages across local areas. Must have valid driving license.", JobType = "Full-Time", Tags = new[] { "Motorbike", "Local Delivery" } },
		new JobModel { Id = 5, Title = "Sales Representative", Company = "BrandMart", Location = "Sylhet, BD", Category = "Sales & Marketing", Salary = "৳20,000 + Commission", Description = "Visit retail outlets to promote consumer products and collect product orders.", JobType = "Full-Time", Tags = new[] { "Sales", "Negotiation" } }
	};

	public static JobModel? GetById(int id) => AllJobs.FirstOrDefault(j => j.Id == id);
}