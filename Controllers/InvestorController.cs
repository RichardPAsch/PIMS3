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


namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize] // 5.22.19 - to be implemented
    public class InvestorController : ControllerBase
    {
        private readonly PIMS3Context _context;
        private IInvestorSvc _investorSvc;
        private readonly AppSettings _appSettings;


        public InvestorController(PIMS3Context context, IInvestorSvc investorSvc, AppSettings appSettings)
        {
            _context = context;
            _investorSvc = investorSvc;
            _appSettings = appSettings;
        }

        

        // GET: api/Investor
        [HttpGet]
        public ActionResult GetAllInvestors()
        {
            InvestorDataProcessing investorDataAccessComponent = new InvestorDataProcessing(_context);
            IQueryable<InvestorVm> investors = MapToVm(investorDataAccessComponent.RetreiveAll());
            return Ok(investors);
        }


        // POST: api/Investor
        [AllowAnonymous]
        [HttpPost()]
        public IActionResult AuthenticateInvestor([FromBody] InvestorVm newInvestor)
        {
            /*
                Upon successful authentication, a JSON Web Token is generated via JwtSecurityTokenHandler();  the generated
                token is digitally signed using a secret key stored in appsettings.json. The JWT is returned to the client,
                which then must include it in the HTTP Authorization header of any subsequent web api requests for authentication.
            */

            Investor registeredInvestor = _investorSvc.Authenticate(newInvestor.LoginName, newInvestor.Password);

            // TODO: log results ?
            if (registeredInvestor == null)
                return BadRequest(new { message = "Unable to validate login credentials, check name and/or password." });

            // Generate jwt.
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            byte[] key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[] {
                     new Claim(ClaimTypes.Name,  registeredInvestor.InvestorId.ToString())
                }),
                Expires = DateTime.Now.AddDays(3),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            SecurityToken generatedToken = tokenHandler.CreateToken(tokenDescriptor);
            string tokenString = tokenHandler.WriteToken(generatedToken);

            // Return investor info for use/storage by UI.
            return Ok(new
            {
                Id = registeredInvestor.InvestorId,
                Username = registeredInvestor.LoginName,
                registeredInvestor.FirstName,
                registeredInvestor.LastName,
                Token = tokenString
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


        // == Default template: =========================================================
        // GET: api/Investor/5
        //[HttpGet("{id}")]
        //public async Task<IActionResult> GetAssetInvestor([FromRoute] string id)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var assetInvestor = await _context.AssetInvestor.FindAsync(id);

        //    if (assetInvestor == null)
        //    {
        //        return NotFound();
        //    }

        //    return Ok(assetInvestor);
        //}

        //// PUT: api/Investor/5
        //[HttpPut("{id}")]
        //public async Task<IActionResult> PutAssetInvestor([FromRoute] string id, [FromBody] AssetInvestor assetInvestor)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    if (id != assetInvestor.AssetId)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(assetInvestor).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!AssetInvestorExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}


        //// DELETE: api/Investor/5
        //[HttpDelete("{id}")]
        //public async Task<IActionResult> DeleteAssetInvestor([FromRoute] string id)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var assetInvestor = await _context.AssetInvestor.FindAsync(id);
        //    if (assetInvestor == null)
        //    {
        //        return NotFound();
        //    }

        //    _context.AssetInvestor.Remove(assetInvestor);
        //    await _context.SaveChangesAsync();

        //    return Ok(assetInvestor);
        //}







    }
}