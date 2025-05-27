using System.Reflection;
using System.Text.RegularExpressions;
using EmailServices.Services;

public sealed class EmailTemplateRenderer : IEmailTemplateRenderer
{
  private static readonly Regex _placeholder = new(@"{{(\w+)}}", RegexOptions.Compiled);

  public string RenderTemplate(string path, object model)
  {
    string html = File.ReadAllText(path);
    var props = model.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

    html = _placeholder.Replace(html, m =>
    {
      var name = m.Groups[1].Value;
      var prop = props.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
      if (prop == null) return m.Value;
      var val = prop.GetValue(model);
      return val?.ToString() ?? string.Empty;
    });

    // Reemplazo de {{Year}} genérico
    html = html.Replace("{{Year}}", DateTime.UtcNow.Year.ToString());
    return html;
  }
}
