using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PIMS3.Data;
using PIMS3.Data.Entities;
using PIMS3.DataAccess.Investor;
using PIMS3.Services;
using PIMS3.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using PIMS3.Helpers;
using System.Text;
using Microsoft.Extensions.Options;
using Serilog;


namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] 
    public class InvestorController : ControllerBase
    {
        private readonly PIMS3Context _context;
        private InvestorSvc _investorSvc;
        private readonly AppSettings _appSettings;
        

        public InvestorController(PIMS3Context context, InvestorSvc investorSvc, IOptions<AppSettings> appSettings)
        {
            _context = context;
            _investorSvc = investorSvc;
            _appSettings = appSettings.Value;
        }

        

        // GET: api/Investor
        [HttpGet]
        public ActionResult GetAllInvestors()
        {
            InvestorDataProcessing investorDataAccessComponent = new InvestorDataProcessing(_context);
            IQueryable<InvestorVm> investors = MapToVm(investorDataAccessComponent.RetreiveAll());
            return Ok(investors);
        }

        [HttpGet("{loginName}/{oldPassword}/{newPassword}")]
        public ActionResult GetInvestorForPasswordEditVerification(string loginName, string oldPassword, string newPassword)
        {
            InvestorDataProcessing investorDataAccessComponent = new InvestorDataProcessing(_context);
            Investor fetchedInvestor = investorDataAccessComponent.RetreiveByName(loginName);

            bool isValidPassword = _investorSvc.VerifyPasswordHash(oldPassword, fetchedInvestor.PasswordHash, fetchedInvestor.PasswordSalt);
            if (isValidPassword)
            {
                _investorSvc.CreatePasswordHash(newPassword, out byte[] passwordHash, out byte[] passwordSalt);
                Investor entityToUpdate = _context.Investor.Where(i => i.LoginName == loginName).First();
                entityToUpdate.PasswordHash = passwordHash;
                entityToUpdate.PasswordSalt = passwordSalt;
                
                investorDataAccessComponent.Update(entityToUpdate); 
            }
            else
            {
                return BadRequest("Unable to verify password.");
            }

            return Ok(fetchedInvestor); 
        }


        // POST: api/Investor
        [AllowAnonymous]
        [HttpPost()]
        [Route("authenticateInvestor")]
        public IActionResult AuthenticateInvestor([FromBody] InvestorVm newInvestor)
        {
            /*  Description:
                Upon successful authentication, a JSON Web Token is generated via JwtSecurityTokenHandler();  the generated
                token is digitally signed using a secret key stored in appsettings.json. The JWT is returned to the client,
                who then must include it in the HTTP Authorization header of any subsequent web api request(s) for authentication.
            */

            Investor registeredInvestor = _investorSvc.Authenticate(newInvestor.LoginName, newInvestor.Password);

            if (registeredInvestor == null)
                return BadRequest(new { message = "Unable to validate registration; please check login name and/or password." });

            // Generate jwt.
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] {
                     new Claim(ClaimTypes.Name,  registeredInvestor.InvestorId.ToString())
                }),
                Expires = DateTime.Now.AddDays(1), // modified for testing - DateTime.Now.AddMinutes(20),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken generatedToken = tokenHandler.CreateToken(tokenDescriptor);
            string tokenString = tokenHandler.WriteToken(generatedToken);

            if(registeredInvestor.InvestorId != string.Empty)
            {
                Log.Information("Login successful for: {0}", registeredInvestor.LoginName);
            }

            // Return investor info for use/storage by UI.
            return Ok(new
            {
                Id = registeredInvestor.InvestorId,
                Username = registeredInvestor.LoginName,
                registeredInvestor.FirstName,
                registeredInvestor.LastName,
                Token = tokenString,
                registeredInvestor.Role
            });
        }


        // Research: Attribute routing with Http[Verb] attributes.
        [AllowAnonymous]
        [HttpPost()]
        [Route("Register")]
        public IActionResult RegisterInvestor([FromBody] InvestorVm investorToRegister)
        {
            Investor mappedInvestor = MapToEntity(investorToRegister);

            try
            {
                _investorSvc.Create(mappedInvestor, investorToRegister.Password);
                Log.Information("Registration successful for: {0}", mappedInvestor.FirstName + " " + mappedInvestor.LastName);
                return Ok(mappedInvestor);
            }
            catch (AppException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            
        }
         



        private IQueryable<InvestorVm> MapToVm(IQueryable<Investor> investorEntitiesToMap)
        {
            IQueryable<InvestorVm> mappedVms = null;
            foreach (var entity in investorEntitiesToMap)
            {
                var mappedVm = new InvestorVm
                {
                    InvestorId = Guid.Parse(entity.InvestorId),
                    FirstName = entity.FirstName,
                    LastName = entity.LastName,
                    LoginName = entity.LoginName,
                };
                mappedVms.Append(mappedVm);
            }
            return mappedVms;
        }


        private Investor MapToEntity(InvestorVm vmToMap)
        {
            return new Investor
            {
                InvestorId = vmToMap.InvestorId.ToString(),
                FirstName = vmToMap.FirstName,
                LastName = vmToMap.LastName,
                LoginName = vmToMap.LoginName,
            };
        }


        private bool AssetInvestorExists(string id)
        {
            return _context.Investor.Any(i => i.InvestorId == id);
        }




    }
}