using EVChargingApi.Data.Models;


namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public interface IUserRepository
    {
        Task<User> FindByEmailAndPasswordAsync(string email, string password);
        Task CreateAsync(User user);
    }
}