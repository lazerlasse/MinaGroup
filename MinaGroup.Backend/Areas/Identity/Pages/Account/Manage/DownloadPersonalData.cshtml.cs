using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MinaGroup.Backend.Data;
using MinaGroup.Backend.Models;
using System.Text.Json;

public class DownloadPersonalDataModel : PageModel
{
    private readonly UserManager<AppUser> _userManager;
    private readonly AppDbContext _context;

    public DownloadPersonalDataModel(
        UserManager<AppUser> userManager,
        AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
        }

        var personalData = new Dictionary<string, string>();

        // 🔹 Find alle felter på AppUser markeret med [PersonalData]
        var personalDataProps = typeof(AppUser).GetProperties()
            .Where(prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));

        foreach (var p in personalDataProps)
        {
            var value = p.GetValue(user);
            personalData.Add(p.Name, value?.ToString() ?? "null");
        }

        // 🔹 Eksempel på relaterede data: SelfEvaluations
        var selfEvaluations = await _context.SelfEvaluations
            .Where(se => se.UserId == user.Id)
            .ToListAsync();

        personalData.Add("SelfEvaluations", JsonSerializer.Serialize(selfEvaluations));

        // 🔹 Returner som JSON-download
        var json = JsonSerializer.SerializeToUtf8Bytes(personalData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        Response.Headers.Append("Content-Disposition", "attachment; filename=PersonalData.json");
        return new FileContentResult(json, "application/json");
    }
}