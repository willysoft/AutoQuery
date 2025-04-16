namespace AutoQueryApiDemo.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime DateOfBirth { get; set; }

    public User()
    {
    }
    public User(int id, string name, string email, DateTime dateOfBirth)
    {
        Id = id;
        Name = name;
        Email = email;
        DateOfBirth = dateOfBirth;
    }
}