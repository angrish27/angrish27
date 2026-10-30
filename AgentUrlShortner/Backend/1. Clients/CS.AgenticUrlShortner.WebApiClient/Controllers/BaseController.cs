using System;
using CS.AgenticUrlShortner.Managers;
using Microsoft.AspNetCore.Mvc;

namespace CS.AgenticUrlShortner.WebApiClient.Controllers
{
    [ApiController]
    public abstract class BaseController : ControllerBase
    {
        protected ManagerFactory ManagerFactory { get; }

        protected BaseController(ManagerFactory managerFactory)
        {
            ManagerFactory = managerFactory ?? throw new ArgumentNullException(nameof(managerFactory));
        }
    }
}
