namespace Sparc.MCN.Users;
public class Contact
{
    public string? UserId { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? PostalCode { get; set; }
    public string? Country { get; set; }
    public List<Email>? EmailAddresses { get; set; } = new List<Email>();
    public List<Phone>? PhoneNumbers { get; set; } = new List<Phone>();
    public Contact(string? address1, string? address2, string? city, string? state, string? postalCode, string? country, List<Email>? emails, List<Phone>? phoneNumbers)
    {
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        EmailAddresses = emails;
        PhoneNumbers = phoneNumbers;
    }
}

public class Email
{
    public string? Type { get; set; }
    public string? Address { get; set; }
    public Email(string? type, string? address)
    {
        Type = type;
        Address = address;
    }
}

public class Phone
{
    public string? Type { get; set; }
    public string? Number { get; set; }
    public Phone(string? type, string? number)
    {
        Type = type;
        Number = number;
    }
}