namespace Common;

public enum PortalAccess
{
    Staff = 1, // tax-preparer, admin, user interno, etc.
    Customer = 2, // customers finales
    Both = 3, // (opcional) si un rol aplica a ambos
}
