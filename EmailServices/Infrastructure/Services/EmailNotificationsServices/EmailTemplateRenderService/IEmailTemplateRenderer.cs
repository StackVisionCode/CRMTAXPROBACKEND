namespace EmailServices.Services;

public interface IEmailTemplateRenderer
{
    string RenderTemplate(string path, object model);
}
