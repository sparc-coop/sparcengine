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
    public string? Email { get; set; }
    public string? HomePhone { get; set; }
    public string? MobilePhone { get; set; }
    public string? WorkPhone { get; set; }
    public Contact(string? address1, string? address2, string? city, string? state, string? postalCode, string? country, string? email, string? homePhone, string? mobilePhone, string? workPhone)
    {
        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        PostalCode = postalCode;
        Country = country;
        Email = email;
        HomePhone = homePhone;
        MobilePhone = mobilePhone;
        WorkPhone = workPhone;
    }
}