namespace DTOs.Contacts;

    public class CreateContactDto
    {
        public required string Name { get; set; }
        public  required  string Email { get; set; }
        public  required  string Phone { get; set; }
        public int UserTaxId { get; set; }
        public int CompanyId{ get; set; }
    }

     public class UpdateContactDto
    {
        public int Id { get; set; } // Important for update
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
    }

    public class ContactDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public DateTime CreatedAt { get; set; }
    }

