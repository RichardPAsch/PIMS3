using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using PIMS3.Data;
using PIMS3.DataAccess.Account;
using System.Linq;


namespace PIMS3.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AccountTypesController : ControllerBase
    {
        private readonly PIMS3Context _context;

        public AccountTypesController(PIMS3Context context)
        {
            _context = context;
        }

        [HttpGet]
        public IQueryable<string> GetAccountTypes()
        {
            AccountDataProcessing accountTypeDataAccessComponent = new AccountDataProcessing(_context);
            return accountTypeDataAccessComponent.GetAllAccountTypes();
        }
    }
}