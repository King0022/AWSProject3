using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InsuraceDataService
{
    internal class Patient
    {

        public string PatientID { get; set; }
        public string PatientName { get; set; }

        public Dictionary<string, string> MediccalRecord { get; set; }

        public bool HasMediccalRecord { get; set; }
    }
}
