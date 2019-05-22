using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Microsoft.Extensions.Options;
using PIMS3.Data;
using PIMS3.Data.Entities;
using PIMS3.DataAccess.Investor;
using PIMS3.Helpers;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace PIMS3.Services
{
    /*  ====== Notes: =========
        Provides application-wide necessary security-related functionality, and is 
        therefore seperately encapsulated from general data processing functionality 
        available via 'InvestorDataProcessing'.
    */

    public class InvestorSvc : IInvestorSvc
    {
        private readonly AppSettings _appSettings;
        private readonly PIMS3Context _ctx;

        public InvestorSvc(IOptions<AppSettings> appSettings, PIMS3Context ctx)
        {
            _appSettings = appSettings.Value;
            _ctx = ctx;
        }


        public Investor Authenticate(string investorLogin, string password)
        {
            /*
                Upon successful authentication, a JSON Web Token is generated via JwtSecurityTokenHandler();  the generated
                token is digitally signed using a secret key stored in appsettings.json. The JWT is returned to the client,
                which then must include it in the HTTP Authorization header of any subsequent web api requests for authentication.
            */

            // Authenticate investor.
            var currentInvestor = GetAll().SingleOrDefault(i => i.LoginName == investorLogin && i.Password == password);
            if (currentInvestor == null)
                return null;

            // Generate jwt token.
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] {
                     new Claim(ClaimTypes.Name,  currentInvestor.InvestorId.ToString())
                }),
                Expires = DateTime.Now.AddDays(3),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var generatedToken = tokenHandler.CreateToken(tokenDescriptor);
            currentInvestor.Token = tokenHandler.WriteToken(generatedToken);

            // Clear password before returning.
            currentInvestor.Password = null;

            return currentInvestor;
        }


        public IQueryable<Investor> GetAll()
        {
            InvestorDataProcessing investorDataAccessComponent = new InvestorDataProcessing(_ctx);
            return investorDataAccessComponent.RetreiveAll();
        }

       
    }
}
