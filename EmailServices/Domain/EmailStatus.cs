namespace EmailServices.Domain
{
    public enum EmailStatus
    {
        Pending, // Email record created but not yet sent
        Sent, // Email has been successfully sent
        Failed, // Sending attempted but failed
    }
}
