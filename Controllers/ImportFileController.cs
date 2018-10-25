using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PIMS3.Controllers
{
    /* ASPNET Core 2.1 documentation:
        The ControllerBase class provides access to several properties and methods, e.g. BadRequest(ModelStateDictionary) and
        CreatedAtAction(String, Object, Object). These methods are called within action methods to return HTTP 400 and 201 status codes respectively. 

        The ModelState property, also provided by ControllerBase, is accessed to handle request model validation.
        The [ApiController] attribute is commonly coupled with ControllerBase to enable REST-specific behavior for controllers. 
        ControllerBase provides access to methods such as NotFound and File.
    */

    [Route("api/[controller]")]
    [ApiController]
    public class ImportFileController : ControllerBase
    {
    }




}
