using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPF
{
    public class LoadCase
    {
        public string _name { get; set; }
        public string _status { get; set; }
        public string _action { get; set; }
        public string _location { get; set; }


        public LoadCase(string name, int status, bool action, string location)
        {

            _name = name;
            
            switch (status)
            {
                case 1:
                    _status = "Not Run";
                    break;
                case 2:
                    _status = "Could Not Start";
                    break;
                case 3:
                    _status = "Not Finished";
                    break;
                case 4:
                    _status = "Finished";
                    break;
                default:
                    _status = "Not Run";
                    break;
            }

            if (action)
            {
                _action = "Run";
            }
            else
            {
                _action = "Do Not Run";
            }

            _location = location;
        }

    }
}
