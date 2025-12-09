using Microsoft.AspNetCore.Razor.TagHelpers;

namespace MinaGroup.Backend.TagHelpers
{
    /// <summary>
    /// Bruges til at sætte selected="selected" på et <option>-tag,
    /// når betingelsen er true.
    /// </summary>
    [HtmlTargetElement("option", Attributes = "asp-selected-when")]
    public class SelectedWhenTagHelper : TagHelper
    {
        /// <summary>
        /// Hvis true -> tilføj selected="selected" på option.
        /// </summary>
        [HtmlAttributeName("asp-selected-when")]
        public bool IsSelected { get; set; }

        public override void Process(TagHelperContext context, TagHelperOutput output)
        {
            // Fjern vores helper-attribut fra den endelige HTML
            output.Attributes.RemoveAll("asp-selected-when");

            if (IsSelected)
            {
                // Sæt selected-attributten
                output.Attributes.SetAttribute("selected", "selected");
            }
        }
    }
}
