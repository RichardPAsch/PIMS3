using PIMS3.Data.Entities;
using System.Linq;

namespace PIMS3.Services
{
    public interface IInvestorSvc
    {
        Investor Authenticate(string username, string password);
        IQueryable<Investor> GetAll();
        Investor Create(Investor investor, string password);
        void Update(Investor investor, string password = null);
        void Delete(int id);
        Investor GetById(string id);
        bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt);
    }
}
