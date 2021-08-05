using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace name_generator_web.Pages
{
    public class AcknowledgementsModel : PageModel
    {
        private readonly ILogger<AcknowledgementsModel> _logger;

        public AcknowledgementsModel(ILogger<AcknowledgementsModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
