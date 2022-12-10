
using AspNetCore.Identity.MongoDbCore.Models;
using MongoDbGenericRepository.Attributes;

namespace HVZ.Web.Identity.Models
{
    /// <summary>
    /// Class used to store Identity users
    /// </summary>
    [CollectionName("Users")]
    public class ApplicationUser : MongoIdentityUser<Guid>
    {
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
        public string DatabaseId { get; set; } = "";
    }
}