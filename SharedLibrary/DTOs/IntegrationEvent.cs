namespace SharedLibrary.DTOs;

/// <summary>Clase base para todos los eventos de integración.</summary>
/// En C#, un abstract record (registro abstracto) es una declaración de clase que se utiliza para definir un tipo de registro con un comportamiento abstracto. Esto significa que la clase no puede ser instanciada directamente, sino que debe ser heredada por otras clases que implementen sus métodos abstractos.
public abstract record IntegrationEvent(Guid Id, DateTime OccurredOn);
