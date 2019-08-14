using System;
using System.Linq;
using PIMS3.Data;
using PIMS3.Data.Entities;
using PIMS3.DataAccess.Investor;
using PIMS3.Helpers;
using System.Text;



namespace PIMS3.Services
{
    /*  ====== Notes: =========
        Provides application-wide necessary security-related functionality, and is 
        therefore separately encapsulated from general data processing functionality 
        available via 'InvestorDataProcessing'.
    */

    public class InvestorSvc 
    {

        private readonly PIMS3Context _ctx;
        const string NULL_OR_WS_MSG = "Password may not be empty, nor contain only whitespace.";


        public InvestorSvc( PIMS3Context ctx)
        {
            _ctx = ctx;
        }


        public Investor GetById(string id)
        {
            return _ctx.Investor.Find(id);
        }


        public Investor GetByLogin(string loginName)
        {
            return _ctx.Investor.Where(i => i.LoginName == loginName)
                       .FirstOrDefault();
        }


        public Investor Authenticate(string investorLogin, string password)
        {
            if (string.IsNullOrEmpty(investorLogin) || string.IsNullOrEmpty(password))
                return null;

            Investor currentInvestor = GetAll().SingleOrDefault(i => i.LoginName == investorLogin);
            if (currentInvestor == null)
            {
                return null;
            }

            // Verify password.
            if (!VerifyPasswordHash(password, currentInvestor.PasswordHash, currentInvestor.PasswordSalt))
                return null;

            // Return authenticated investor.
            return currentInvestor;
        }


        public Investor Create(Investor investor, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                throw new AppException("Unable to register:: a password is required.");

            if(_ctx.Investor.Any(i => i.LoginName == investor.LoginName))
                throw new AppException("Unable to register: login name \"" + investor.LoginName + "\" already exists.");

            CreatePasswordHash(password, out byte[] passwordHash, out byte[] passwordSalt);
            investor.PasswordHash = passwordHash;
            investor.PasswordSalt = passwordSalt;
            investor.Role = "Investor";  // TODO: deferred - ability to register as "Admin".

            _ctx.Investor.Add(investor);
            _ctx.SaveChanges();

            return investor;
        }


        public void Delete(int id)
        {
            throw new NotImplementedException();
        }


        public IQueryable<Investor> GetAll()
        {
            InvestorDataProcessing investorDataAccessComponent = new InvestorDataProcessing(_ctx);
            return investorDataAccessComponent.RetreiveAll();
        }


        public void Update(Investor investor, string password = null)
        {
            throw new NotImplementedException();
        }



        #region Helpers

            public void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
            {
                if (password == null) throw new ArgumentNullException("password");
                if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException(NULL_OR_WS_MSG, "password");

                using (var hmac = new System.Security.Cryptography.HMACSHA512())
                {
                    passwordSalt = hmac.Key;
                    passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                }
            }

            public bool VerifyPasswordHash(string password, byte[] storedHash, byte[] storedSalt)
            {
                if (password == null) throw new ArgumentNullException("password");
                if (storedHash == null) throw new ArgumentNullException("storedHash");
                if (storedSalt == null) throw new ArgumentNullException("storedSalt");

                if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException(NULL_OR_WS_MSG, "password");
                if (storedHash.Length != 64) throw new ArgumentException("Invalid length of password hash (64 bytes expected).", "passwordHash");
                if (storedSalt.Length != 128) throw new ArgumentException("Invalid length of password salt/key (128 bytes expected).", "passwordHash");

                using (var hmac = new System.Security.Cryptography.HMACSHA512(storedSalt))
                {
                    var computedHash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                    for (int i = 0; i < computedHash.Length; i++)
                    {
                        if (computedHash[i] != storedHash[i]) return false;
                    }
                }

                return true;
            }

            public Investor GetById()
            {
                throw new NotImplementedException();
            }

        #endregion


    }
}
