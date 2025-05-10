using System;
using System.ComponentModel.DataAnnotations;
using Common;
using Domains.Signers;

namespace Domains.Contacts;

    public class Contact:BaseEntity
    {   
        public required string Name { get; set; } 
        public required string Email { get; set; }
        public required string Phone { get; set; }

        public int UserTaxId { get; set; }
        public int CompanyId{ get; set; }

        public ICollection<ExternalSigner>? ExternalSigners { get; set; }
        
    }

