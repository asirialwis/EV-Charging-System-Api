using EVChargingApi.Data.Models;


namespace EVChargingSystem.WebAPI.Data.Repositories
{
    public interface IUserRepository
    {
        
        Task CreateAsync(User user);
        Task<User> FindByEmailAsync(string email);

        // New method for suspend user
        Task<bool> UpdateStatusAsync(string userId, string newStatus);

        Task<List<User>> GetUsersByRoleAsync(string role);
        Task<List<User>> FindManyByIdsAsync(List<string> userIds);

        Task<User> FindByIdAsync(string userId);
        Task<bool> DeleteAsync(string userId);
        
    }
}