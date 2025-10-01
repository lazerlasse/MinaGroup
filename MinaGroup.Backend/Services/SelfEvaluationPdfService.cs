using MinaGroup.Backend.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MinaGroup.Backend.Services
{
    public class SelfEvaluationPdfService
    {
        public byte[] GeneratePdf(SelfEvaluation evaluation)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.PageColor(Colors.White);

                    // Header
                    page.Header()
                        .Text($"Selvevaluering - {evaluation.User.FullName}")
                        .SemiBold()
                        .FontSize(18)
                        .FontColor(Colors.Blue.Medium)
                        .AlignCenter();

                    // Content
                    page.Content()
                        .PaddingVertical(10)
                        .Column(col =>
                        {
                            void AddField(string label, string? value)
                            {
                                col.Item().Row(row =>
                                {
                                    row.RelativeItem(3).Text(label).Bold();
                                    row.RelativeItem(5).Text(string.IsNullOrWhiteSpace(value) ? "-" : value);
                                });
                            }

                            void AddCheckboxField(string label, bool value)
                            {
                                AddField(label, value ? "Ja" : "Nej");
                            }

                            // Sektion: Personlige oplysninger
                            col.Item().Text("Borger").Bold().FontSize(14);
                            col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            AddField("CPR Nr.", evaluation.User.PersonNumberCPR);
                            AddField("Navn", evaluation.User.FullName);
                            col.Item().PaddingBottom(5);

                            // Sektion: Mødetid og arbejdstid
                            col.Item().Text("Arbejdstid og sygdom").Bold().FontSize(14);
                            col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            AddCheckboxField("Sygemeldt", evaluation.IsSick);
                            AddField("Mødetid", evaluation.ArrivalTime?.ToString(@"hh\:mm"));
                            AddField("Gik hjem", evaluation.DepartureTime?.ToString(@"hh\:mm"));
                            AddField("Afholdt pause", evaluation.BreakDuration?.ToString(@"hh\:mm"));
                            AddField("Arbejdstid", evaluation.TotalHours?.ToString(@"hh\:mm"));
                            AddField("Kom borger til tiden", evaluation.ArrivalStatus);
                            col.Item().PaddingBottom(5);

                            // Sektion: Samarbejde og hjælp
                            col.Item().Text("Samarbejde").Bold().FontSize(14);
                            col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            AddField("Samarbejde", evaluation.Collaboration);
                            col.Item().PaddingBottom(5);

                            // Sektion: Helbred og hjælpemidler
                            col.Item().Text("Helbred og hjælpemidler").Bold().FontSize(14);
                            col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            AddField("Brug for hjælp", evaluation.Assistance);
                            AddField("Brug for hjælpemidler", evaluation.Aid);

                            if (evaluation.Aid == "Ja – hvilke?" || evaluation.Aid == "Har brug for noget – hvad?")
                                AddField("Hvad/hvilken?", evaluation.AidDescription);

                            AddCheckboxField("Havde borger ubehag", evaluation.HadDiscomfort);

                            if (evaluation.HadDiscomfort)
                                AddField("Hvilke?", evaluation.DiscomfortDescription);

                            col.Item().PaddingBottom(5);

                            // Sektion: Arbejdsopgaver
                            col.Item().Text("Arbejdsopgaver").Bold().FontSize(14);
                            col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            foreach (var task in evaluation.SelectedTask)
                            {
                                AddCheckboxField(task.TaskName, true);
                            }

                            col.Item().PaddingBottom(5);

                            // Sektion: Aftaler
                            col.Item().Text("Aftaler").Bold().FontSize(14);
                            col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            AddField("Aftaler til næste gang", evaluation.NextMeetingNotes);
                            col.Item().PaddingBottom(5);

                            // Sektion: Kommentarer
                            col.Item().Text("Kommentarer").Bold().FontSize(14);
                            col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                            AddField("Kommentar fra borger", evaluation.CommentFromUser);
                            AddField("Kommentar fra leder", evaluation.CommentFromLeader);
                        });

                    // Footer
                    page.Footer()
                        .AlignCenter()
                        .Text(x => x.Span("MinaGroup Backend").FontSize(9).FontColor(Colors.Grey.Darken2));
                });
            }).GeneratePdf();
        }
    }
}