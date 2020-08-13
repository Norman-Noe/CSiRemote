using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF
{
    public class MachineLocation
    {
        public string _MachineNumber { get; set; }

        public int _Port { get; set; }

        public MachineLocation(string MachineNumber, int Port)
        {
            _MachineNumber = MachineNumber;
            _Port = Port;
        }

        public override string ToString()
        {
            return _MachineNumber + "["+ _Port.ToString() +"]";
        }
    }
}
