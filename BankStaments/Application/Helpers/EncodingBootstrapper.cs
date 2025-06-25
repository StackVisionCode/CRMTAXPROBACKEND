using System.Text;

namespace Application.Helpers;

public static class EncodingBootstrapper
{
    static EncodingBootstrapper()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        try
        {
            _ = Encoding.GetEncoding("symbol");            // evita el error
            _ = Encoding.GetEncoding("standardencoding");  // alias adicional de iText
        }
        catch { /* ignora si no existe */ }
    }

    public static void Init() { /* solo para forzar el constructor estático */ }
}