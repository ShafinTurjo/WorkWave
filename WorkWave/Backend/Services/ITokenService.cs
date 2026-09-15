using Backend.Models;

namespace Backend.Services;

public interface ITokenService
{
    string CreateToken(User user);
}
