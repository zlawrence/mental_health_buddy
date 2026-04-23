using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MentalHealthApp.fx;

namespace MentalHealthApp.fx.Classes.Abstract
{
    public abstract class PaymentGateway
    {
        private string _environment = SystemConstants.DEVELOPMENT;
        public bool IsDevelopment
        {
            get
            {
                return _environment == SystemConstants.DEVELOPMENT;
            }
        }
        protected PaymentGateway(string environment) { 
            _environment = environment; 
        }


    }
}
